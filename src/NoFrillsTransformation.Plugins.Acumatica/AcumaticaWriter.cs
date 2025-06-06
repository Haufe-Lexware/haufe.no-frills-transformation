using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Acumatica
{
    internal class AcumaticaWriter : ITargetWriter
    {
        private IContext _context;
        private string _fileName;
        private string[] _fieldNames;
        private int[] _fieldSizes;
        private string _config;
        private XmlTextWriter _xmlWriter;
        private int _recordsWritten = 0;
        private AcumaticaEntityConfig? _entityConfig;

        public AcumaticaWriter(IContext context, string target, string[] fieldNames, int[] fieldSizes, string? config)
        {
            this._context = context;
            var tempFileName = target.Substring(target.IndexOf("//") + 2);
            this._fileName = context.ResolveFileName(tempFileName, false);
            this._fieldNames = fieldNames;
            this._fieldSizes = fieldSizes;
            this._config = config ?? string.Empty;
            this._entityConfig = ReadConfig(this._config);

            _xmlWriter = new XmlTextWriter($"{_fileName}.tmp", Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 1,
                IndentChar = '\t'
            };
            _xmlWriter.WriteStartDocument();
            _xmlWriter.WriteStartElement("data");

            // Write <table name="TableName>">
            _xmlWriter.WriteStartElement("table");
            _xmlWriter.WriteAttributeString("name", _entityConfig.Table?.Name ?? "Unknown");
            if (_entityConfig.Table?.Status != null)
            {
                _xmlWriter.WriteAttributeString("status", _entityConfig.Table.Status);
            }
            // Copy the column names from the config file
            foreach (var col in _entityConfig.Table?.Columns ?? new AcumaticaEntityColumnConfig[0])
            {
                _xmlWriter.WriteStartElement("col");
                _xmlWriter.WriteAttributeString("name", col.Name ?? "Unknown");
                _xmlWriter.WriteAttributeString("type", col.Type ?? "Unknown");
                if (col.Default != null)
                {
                    _xmlWriter.WriteAttributeString("default", col.Default);
                }
                if (col.RawDefault != null)
                {
                    _xmlWriter.WriteAttributeString("raw-default", col.RawDefault);
                }
                if (col.Nullable != null)
                {
                    _xmlWriter.WriteAttributeString("nullable", col.Nullable);
                }
                _xmlWriter.WriteEndElement();
            }

            _xmlWriter.WriteEndElement();
            _xmlWriter.WriteStartElement("rows");
        }

        private List<string[]> _records = new List<string[]>();

        private AcumaticaEntityConfig ReadConfig(string configFileName)
        {
            // this._config contains an XML file with the AcumaticaEntityConfig structure
            // Read the file and parse it as XML according to AcumaticaEntityConfig
            // After that, we'll use that to write the output.
            string fileName = _context.ResolveFileName(configFileName);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(AcumaticaEntityConfig));
            AcumaticaEntityConfig? entityConfig;
            using (var fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
            {
                entityConfig = (AcumaticaEntityConfig?)xmlSerializer.Deserialize(fs);
            }
            if (entityConfig == null)
            {
                throw new ArgumentException("Could not read Acumatica configuration file: " + _config);
            }
            // Resolve some fields using the context
            if (entityConfig.Table?.Name != null)
            {
                entityConfig.Table.Name = _context.ReplaceParameters(entityConfig.Table.Name);
            }
            return entityConfig;
        }

        public void WriteRecord(string[] fieldValues)
        {
            // The engine reuses the same array all over again, so we need to clone it.
            _records.Add((string[])fieldValues.Clone());
            _recordsWritten++;
        }

        public int RecordsWritten
        {
            get
            {
                return _recordsWritten;
            }
        }

        public void FinishWrite()
        {
            // We have everything in _records, now we need to sort and write
            // the records to the file.
            // The sort order is given by the sort fields in the entity configuration. Let's create
            // a delegate Comparison for that.

            // Calculate field indexes once
            int[] sortFieldIndexes = _entityConfig?.SortFields?
                .Select(sortField => Array.IndexOf(_fieldNames, sortField))
                .ToArray() ?? Array.Empty<int>();
            // Build an array which contains whether a field is nullable or not
            bool[] isNullable = new bool[_fieldNames.Length];
            for (int i = 0; i < _fieldNames.Length; ++i)
            {
                // Find the field by name in the config file
                // and check if it is nullable
                // If the field is not found, it is not nullable
                isNullable[i] = false;
                for (int j = 0; j < _entityConfig?.Table?.Columns?.Length; ++j)
                {
                    if (_fieldNames[i] == _entityConfig.Table.Columns[j].Name)
                    {
                        isNullable[i] = _entityConfig.Table.Columns[j].Nullable == "true";
                        break;
                    }
                }
            }

            Comparison<string[]> comparison = (a, b) =>
            {
                for (int i = 0; i < sortFieldIndexes.Length; ++i)
                {
                    int fieldIndex = sortFieldIndexes[i];
                    if (fieldIndex < 0)
                    {
                        return 0;
                    }
                    int result = string.Compare(a[fieldIndex], b[fieldIndex]);
                    if (result != 0)
                    {
                        return result;
                    }
                }
                return 0;
            };

            // If there are no sort fields, we will not sort the records
            if (sortFieldIndexes.Length == 0)
            {
                _context.Logger.Info("No sort fields defined, skipping sorting.");
            }
            else
            {
                _context.Logger.Info("Sorting records...");
                // Now sort the _records
                _records.Sort(comparison);
            }

            // And output them
            foreach (var record in _records)
            {
                _xmlWriter.WriteStartElement("row");
                List<int>? cdataFields = null;
                for (int i = 0; i < _fieldNames.Length; ++i)
                {
                    // If the length of the field is >1000, use a CData section
                    if (record[i].Length > 1000)
                    {
                        if (null == cdataFields)
                        {
                            cdataFields = new List<int>();
                        }
                        cdataFields.Add(i);
                    }
                    else
                    {
                        // Check if the field is not nullable or the value is not empty
                        bool shouldBeNull = isNullable[i] && string.IsNullOrEmpty(record[i]);
                        bool shouldBeNullBecauseOfCompany = (_fieldNames[i] == "CompanyID" || _fieldNames[i] == "CompanyMask" || _fieldNames[i] == "tstamp") && string.IsNullOrEmpty(record[i]);
                        if (!(shouldBeNull || shouldBeNullBecauseOfCompany))
                        {
                            _xmlWriter.WriteAttributeString(_fieldNames[i], record[i]);
                        }
                    }
                }
                if (null != cdataFields)
                {
                    foreach (int i in cdataFields)
                    {
                        _xmlWriter.WriteStartElement("column");
                        _xmlWriter.WriteAttributeString("name", _fieldNames[i]);
                        _xmlWriter.WriteCData(record[i]);
                        _xmlWriter.WriteEndElement(); // column
                    }
                }
                _xmlWriter.WriteEndElement(); // row
            }

            _xmlWriter.WriteEndElement(); // rows
            _xmlWriter.WriteEndElement(); // data
            _xmlWriter.Close();

            // Now clean up the file and do the required Acumatica quirks...
            _context.Logger.Info("Postprocessing file...");
            PostProcess();
        }

        private void PostProcess()
        {
            // Acumatica requires all occurrences of tabs inside XML content to be encoded
            // as &#x9; instead of the tab character. This is a bit of a pain, but we have to do it.
            // Read the file, replace all tabs with &#x9; and write it back.
            string fileName = _fileName + ".tmp";
            string fileNameTarget = _fileName;
            System.IO.File.Delete(fileNameTarget);
            // The files are large, so we need to read and write them line by line.
            using (var reader = new System.IO.StreamReader(fileName))
            {
                using (var writer = new System.IO.StreamWriter(fileNameTarget, false, Encoding.UTF8))
                {
                    string? line;
                    bool inCData = false;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Replace all tabs AFTER the first ones on each line (indentations) with &#x9;
                        int firstNonTab = 0;
                        while (firstNonTab < line.Length && line[firstNonTab] == '\t')
                        {
                            firstNonTab++;
                        }
                        // Check for CDATA sections
                        if (line.Contains("<![CDATA["))
                        {
                            // Inside CDATA sections, tabs must NOT be replaced
                            // Two cases: CDATA ends on the same line, or on a different line
                            int cdataEnd = line.IndexOf("]]>");
                            if (cdataEnd > 0)
                            {
                                // CDATA ends on the same line
                                line = line.Substring(0, cdataEnd + 3) + line.Substring(cdataEnd + 3).Replace("\t", "&#x9;");
                            }
                            else
                            {
                                inCData = true;
                            }
                        }
                        else if (inCData)
                        {
                            // We are inside a CDATA section, so we need to check for the end
                            int cdataEnd = line.IndexOf("]]>");
                            if (cdataEnd > 0)
                            {
                                // CDATA ends on this line
                                inCData = false;
                                line = line.Substring(0, cdataEnd + 3) + line.Substring(cdataEnd + 3).Replace("\t", "&#x9;");
                            }
                        }
                        else
                        {
                            line = line.Substring(0, firstNonTab) + line.Substring(firstNonTab).Replace("\t", "&#x9;");
                        }
                        writer.WriteLine(line);
                    }
                }
            }
            // Delete the temporary file
            System.IO.File.Delete(fileName);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (null != _xmlWriter)
                {
                    _xmlWriter.Close();
                    // _xmlWriter = null;
                }
            }
        }
    }
}

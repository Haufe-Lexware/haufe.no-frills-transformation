using NoFrillsTransformation.Interfaces;
using System.Xml;

namespace NoFrillsTransformation.Plugins.Acumatica
{
    internal class AcumaticaReader : ISourceReader, IRecord
    {
        private IContext _context;
        private string _fileName;
        private string[] _fieldNames;
        private string[] _fieldValues;
        private Dictionary<string, int> _fieldIndexes = new Dictionary<string, int>();

        private XmlTextReader _xmlReader;

        public AcumaticaReader(IContext context, string source, string? config)
        {
            this._context = context;
            var tempFileName = source.Substring(source.IndexOf("//") + 2);
            this._fileName = context.ResolveFileName(tempFileName, true);

            using (var fieldsReader = new XmlTextReader(_fileName))
            {
                fieldsReader.WhitespaceHandling = WhitespaceHandling.None;

                // This is how the data looks like; the table node contains the fields, and the rows contain the data
                /*
                <?xml version="1.0" encoding="utf-8"?>
                <data>
                    <table name="LocalizationValue">
                        <col name="CompanyID" type="Int" default="Zero" />
                        <col name="Id" type="Char(32)" />
                        <col name="NeutralValue" type="NVarChar(MAX)" />
                        <col name="IsNotLocalized" type="Bit" />
                        <col name="IsSite" type="Bit" raw-default="1" />
                        <col name="IsPortal" type="Bit" default="Zero" />
                        <col name="IsObsolete" type="Bit" default="Zero" />
                        <col name="IsObsoletePortal" type="Bit" default="Zero" />
                        <col name="TranslationCount" type="Int" />
                        <col name="CompanyMask" type="VarBinary(32)" default="CompanyMaskReadOnly" />
                        <col name="CreatedByID" type="UniqueIdentifier" />
                        <col name="CreatedByScreenID" type="Char(8)" />
                        <col name="CreatedDateTime" type="DateTime" />
                        <col name="LastModifiedByID" type="UniqueIdentifier" />
                        <col name="LastModifiedByScreenID" type="Char(8)" />
                        <col name="LastModifiedDateTime" type="DateTime" />
                        <col name="tstamp" type="Timestamp" />
                    </table>
                    <rows>
                        <row Id="0001E8CACD459989BA90C5BB548CC2CB" NeutralValue="Packaging Type -&gt; Auto and Manual" IsNotLocalized="0" IsSite="1" IsPortal="1" IsObsolete="0" IsObsoletePortal="0" TranslationCount="1" CreatedByID="b5344897-037e-4d58-b5c3-1bdfd0f47bf9" CreatedByScreenID="SM200540" CreatedDateTime="2019-04-02 11:21:17.997" LastModifiedByID="b5344897-037e-4d58-b5c3-1bdfd0f47bf9" LastModifiedByScreenID="SM200540" LastModifiedDateTime="2022-06-02 09:15:36.21" />
                        <row Id="000289BE413833AFDB0215223ACFC75C" NeutralValue="A Boolean value that indicates whether users can archive the cost roll results without updating the pending costs." IsNotLocalized="0" IsSite="1" IsPortal="0" IsObsolete="0" IsObsoletePortal="0" TranslationCount="3" CreatedByID="b5344897-037e-4d58-b5c3-1bdfd0f47bf9" CreatedByScreenID="SM200540" CreatedDateTime="2024-10-11 08:27:27.847" LastModifiedByID="b5344897-037e-4d58-b5c3-1bdfd0f47bf9" LastModifiedByScreenID="SM200540" LastModifiedDateTime="2024-11-25 10:43:40.13" />
                    </rows>
                </data>
                */

                // Read the field names from the table node
                if (!fieldsReader.ReadToFollowing("table"))
                {
                    throw new Exception("No table node found in the Acumatica XML file.");
                }
                // Iterate over the col nodes
                var fieldNames = new List<string>();
                while (fieldsReader.ReadToFollowing("col"))
                {
                    // Read the name attribute
                    var name = fieldsReader.GetAttribute("name");
                    // Add the name to the field names
                    if (name != null)
                    {
                        fieldNames.Add(name);
                    }
                }
                _fieldNames = fieldNames.ToArray();
                _fieldValues = new string[_fieldNames.Length];

                // Create a dictionary with the field names and their indexes
                for (int i = 0; i < _fieldNames.Length; i++)
                {
                    _fieldIndexes.Add(_fieldNames[i], i);
                }
            }

            // Reopen the XML Text Reader to start over with the rows; there may be nicer ways
            // of doing this, but it works.
            _xmlReader = new XmlTextReader(_fileName);
            // Now jump to the rows node
            if (!_xmlReader.ReadToFollowing("rows"))
            {
                throw new Exception("No rows node found in the Acumatica XML file.");
            }
        }

        private void ReadRecord()
        {
            for (int i = 0; i < _fieldNames.Length; i++)
            {
                _fieldValues[i] = _xmlReader.GetAttribute(_fieldNames[i]) ?? "";
            }
            // Check if there are column node(s) inside the row node
            if (_xmlReader.ReadToDescendant("column"))
            {
                do
                {
                    var name = _xmlReader.GetAttribute("name");
                    if (name != null)
                    {
                        var index = _fieldIndexes[name];
                        // The value is in a CDATA section inside the column node
                        _fieldValues[index] = _xmlReader.ReadElementContentAsString();
                    }
                } while (_xmlReader.ReadToNextSibling("column"));
            }
        }

        public IRecord CurrentRecord => this;

        public string this[int index] => _fieldValues[index];

        public string this[string fieldName] => _fieldValues[_fieldIndexes[fieldName]];

        private bool _endOfStream = false;
        public bool IsEndOfStream => _endOfStream;
        public int FieldCount => _fieldNames.Length;

        public string[] FieldNames => _fieldNames;

        public void Dispose()
        {
            _xmlReader.Close();
        }

        public int GetFieldIndex(string fieldName)
        {
            return _fieldIndexes[fieldName];
        }

        public void NextRecord()
        {
            if (_endOfStream)
            {
                return;
            }
            if (!_xmlReader.ReadToFollowing("row"))
            {
                _endOfStream = true;
            }
            else
            {
                ReadRecord();
            }
        }

        public IRecord Query(string key)
        {
            throw new NotImplementedException();
        }
    }

}
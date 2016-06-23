using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Csv;
using NoFrillsTransformation.Plugins.Salesforce.Config;
using NoFrillsTransformation.Plugins.Salesforce.DotNet.Salesforce37;

namespace NoFrillsTransformation.Plugins.Salesforce.DotNet
{
    internal class TargetField
    {
        public string Name { get; set; }
        public string ExternalId { get; set; }
        public string FieldType { get; set; }
        public bool IsBase64 { get; set; }
    }

    class SfdcDotNetWriter : SfdcDotNetBase, ITargetWriter
    {
        public SfdcDotNetWriter(IContext context, SfdcTarget target, IFieldDefinition[] fieldDefs, SfdcConfig config)
            : base(context, config)
        {
            _target = target;
            _fieldDefs = fieldDefs;
            _fieldNames = fieldDefs.Select(def => def.FieldName).ToArray();

            if (_config.UseBulkApi)
                throw new NotImplementedException("SfdcDotNetWriter does not support the bulk API currently.");

            VerifySettings();
            CreateLoggers();
            Login();
            VerifyTarget();
        }

        private SfdcTarget _target;
        private IFieldDefinition[] _fieldDefs;
        private string[] _fieldNames;

        private Dictionary<string, string> _fields;
        //private string[] _targetFieldNames;
        //private string[] _targetExternalId;
        //private string[] _targetFieldTypes;
        private TargetField[] _targetFields;
        private bool _operationHasId;
        
        private int _pendingWrites = 0;
        private List<sObject> _pendingObjects = new List<sObject>();
        private int _writtenRecords = 0;

        private CsvWriterPlugin _successCsv;
        private CsvWriterPlugin _errorCsv;
        private bool _hasErrors = false;

        public void WriteRecord(string[] fieldValues)
        {
            _pendingObjects.Add(CreateSObject(fieldValues));
            _pendingWrites++;
            _writtenRecords++;

            if (_pendingWrites >= _config.LoadBatchSize)
                FlushWrite();
        }

        private static bool IsOldWordDocument(string fileName)
        {
            string fn = fileName.ToLowerInvariant();
            return fn.EndsWith("doc");
        }

        private int FindLength(string fileName, byte[] fileContent)
        {
            int length = fileContent.Length;

            while (length > 0 && fileContent[length - 1] == 0)
                length--;

            if (length == 0)
            {
                length = fileContent.Length;
                _context.Logger.Warning("SfdcDotNetWriter: Found all NULL characters file '" + fileName + "'. Leaving it as-is.");
            }
            else if (length != fileContent.Length)
            {
                _context.Logger.Info("SfdcDotNetWriter: Truncated trailing NULL chars in file '" + fileName + "'.");
            }

            return length;
        }

        private string ReadFileBase64(string filePath)
        {
            var fileName = _context.ResolveFileName(filePath, true);
            byte[] fileContent = File.ReadAllBytes(fileName);
            if (_config.TruncateTrailingNulls && IsOldWordDocument(fileName))
            {
                int findLength = FindLength(fileName, fileContent);
                return Convert.ToBase64String(fileContent, 0, findLength);
            }
            return Convert.ToBase64String(fileContent);
        }

        private static string MashXmlString(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\0':
                    case '\x01':
                    case '\x02':
                    case '\x03':
                    case '\x04':
                    case '\x05':
                    case '\x06':
                    case '\x0b':
                    case '\x10':
                    case '\x11':
                    case '\x12':
                    case '\x13':
                    case '\x14':
                    case '\x15':
                    case '\x16':
                    case '\x17':
                    case '\x18':
                    case '\x19':
                    case '\x1a':
                    case '\x1b':
                    case '\x1c':
                    case '\x1d':
                    case '\x1e':
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
            //return s.Replace("\0", "").Replace("\x1b", "").Replace("\x13", "").Replace("\x01", "").Replace("\x02", "").Replace("\x03", "").Replace("\x04", "").Replace("\x05", "").Replace("\x06", "").Replace("\x0b", "")
            //    .Replace("\x10", "").Replace("\x16", "").Replace("\x1a", "").Replace("\x1c", "").Replace("\x1d", "").Replace("\x1e", "");
        }

        private XmlDocument _xmlDoc = new XmlDocument();
        private sObject CreateSObject(string[] fieldValues)
        {
            var o = new sObject();
            o.type = _target.Entity;

            var xmlElements = new List<XmlElement>();
            //if (!_operationHasId)
            //    xmlElements = new XmlElement[fieldValues.Length];
            //else
            //    xmlElements = new XmlElement[fieldValues.Length - 1];
            //int elementPointer = 0;
            for (int i=0; i<fieldValues.Length; ++i)
            {
                if (i == 0 &&
                    _operationHasId)
                {
                    o.Id = fieldValues[i];
                }
                else
                {
                    if (!string.IsNullOrEmpty(fieldValues[i]))
                    {
                        var e = _xmlDoc.CreateElement(_targetFields[i].Name);
                        if (string.IsNullOrEmpty(_targetFields[i].ExternalId))
                        {
                            if (!_targetFields[i].IsBase64)
                                e.InnerText = MashXmlString(fieldValues[i]);
                            else
                                e.InnerText = ReadFileBase64(fieldValues[i]);
                        }
                        else
                        {
                            // Wow. But it seems to be the only way to get this:
                            // External ID reference on upsert/update.
                            var typeE = _xmlDoc.CreateElement("type");
                            typeE.InnerText = _targetFields[i].FieldType;
                            var idE = _xmlDoc.CreateElement(_targetFields[i].ExternalId);
                            idE.InnerText = MashXmlString(fieldValues[i]);
                            e.AppendChild(typeE);
                            e.AppendChild(idE);
                        }

                        xmlElements.Add(e);
                    }
                    else
                    {
                        // TODO: Check for "insertNulls"
                    }
                    //elementPointer++;
                }
            }
            o.Any = xmlElements.ToArray();
            return o;
        }

        public int RecordsWritten
        {
            get
            {
                return _writtenRecords;
            }
        }

        public void FinishWrite()
        {
            FlushWrite();

            _successCsv.FinishWrite();
            _errorCsv.FinishWrite();

            if (_hasErrors
                && _config.FailOnErrors)
            {
                throw new InvalidOperationException("SfdcDotNetWriter: There were errors during the operation. Please see log file for more details.");
            }
        }


        private void FlushWrite()
        {
            if (_pendingWrites > 0)
            {
                switch (_target.Operation.ToLowerInvariant())
                {
                    case "update":
                        UpdateFlush();
                        break;
                    case "upsert":
                        UpsertFlush();
                        break;
                    case "insert":
                        InsertFlush();
                        break;
                    case "delete":
                        DeleteFlush();
                        break;
                    default:
                        throw new ArgumentException("SfdcDotNetWriter: Unknown operation '" + _target.Operation + "'.");
                }

                _pendingObjects.Clear();
                _pendingWrites = 0;

                if (_hasErrors 
                    && _config.FailOnFirstError)
                {
                    _successCsv.FinishWrite();
                    _errorCsv.FinishWrite();

                    throw new InvalidOperationException("SfdcDotNetWriter: There were errors after writing to Salesforce. Aborting due to 'FailOnFirstError' setting. See error log for more information.");
                }
            }
        }

        private void InsertFlush()
        {
            var objects = _pendingObjects.ToArray();
            var saveResult = _sfdc.create(objects);

            LogSaveResults(objects, saveResult);
        }

        private void UpdateFlush()
        {
            var objects = _pendingObjects.ToArray();
            var saveResult = _sfdc.update(objects);

            LogSaveResults(objects, saveResult);
        }

        private void LogSaveResults(sObject[] objects, SaveResult[] saveResults)
        {
            for (int i = 0; i < saveResults.Length; ++i)
            {
                var row = saveResults[i];
                if (row.success)
                    LogSuccess(row.id, objects[i]);
                else
                    LogError(objects[i], row.errors[0].message);
            }
        }

        private void UpsertFlush()
        {
            var objects = _pendingObjects.ToArray();
            var upsertResult = _sfdc.upsert(_target.ExternalId, objects);

            LogUpsertResults(objects, upsertResult);
        }

        private void LogUpsertResults(sObject[] objects, UpsertResult[] upsertResults)
        {
            for (int i = 0; i < upsertResults.Length; ++i)
            {
                var row = upsertResults[i];
                if (row.success)
                    LogSuccess(row.id, objects[i]);
                else
                    LogError(objects[i], row.errors[0].message);
            }
        }

        private void DeleteFlush()
        {
            var objectIds = new string[_pendingObjects.Count];
            for (int i = 0; i < objectIds.Length; ++i)
                objectIds[i] = _pendingObjects[i].Id;
            var deleteResults = _sfdc.delete(objectIds);

            LogDeleteResults(deleteResults);
        }

        private void LogDeleteResults(DeleteResult[] deleteResults)
        {
            for (int i = 0; i < deleteResults.Length; ++i)
            {
                var row = deleteResults[i];
                if (row.success)
                    LogSuccess(row.id, _pendingObjects[i]);
                else
                    LogError(_pendingObjects[i], row.errors[0].message);
            }
        }

        private void VerifyTarget()
        {
            var desc = _sfdc.describeSObject(_target.Entity);
            _operationHasId = false;
            switch (_target.Operation.ToLowerInvariant())
            {
                case "update":
                case "delete":
                case "hard_delete":
                    _operationHasId = true;
                    break;
            }
            _fields = new Dictionary<string, string>();
            var fieldTypes = new Dictionary<string, fieldType>();
            foreach (var field in desc.fields)
            {
                _fields[field.name.ToLowerInvariant()] = field.name;
                fieldTypes[field.name.ToLowerInvariant()] = field.type;
            }
            //_targetFieldNames = new string[_fieldNames.Length];
            //_targetExternalId = new string[_fieldNames.Length];
            //_targetFieldTypes = new string[_fieldNames.Length];
            _targetFields = new TargetField[_fieldNames.Length];
            for (int i = 0; i < _fieldNames.Length; ++i)
            {
                _targetFields[i] = new TargetField();
                var lowerName = _fieldNames[i].ToLowerInvariant();

                if (_fields.ContainsKey(lowerName))
                {
                    _targetFields[i].Name = _fields[lowerName];
                    _targetFields[i].IsBase64 = (fieldTypes[lowerName] == fieldType.base64);
                }
                else
                {
                    _targetFields[i].Name = _fieldNames[i];
                }

                if (!string.IsNullOrEmpty(_fieldDefs[i].Config))
                {
                    var c = _fieldDefs[i].Config;
                    // Syntax: Type:ExternalIdField__c
                    // This was taken from how the DataLoader works (works the same there).
                    if (c.Contains(":"))
                    {
                        _targetFields[i].ExternalId = c.Substring(c.IndexOf(":") + 1);
                        _targetFields[i].FieldType = c.Substring(0, c.IndexOf(":"));
                    }
                    else
                    {
                        _targetFields[i].ExternalId = c;
                        _targetFields[i].FieldType = _targetFields[i].Name;
                    }
                }
            }
        }

        private void VerifySettings()
        {
            string useBulkApi = "";
            string bulkApiSerialMode = "";
            string bulkApiZipContent = "";
            SfdcBase.GetBulkApiSettings(_context, _config, ref useBulkApi, ref bulkApiSerialMode, ref bulkApiZipContent);
            SfdcBase.GetBatchSizeSettings(_context, _config, useBulkApi);
            SfdcBase.GetInsertNullsSetting(_context, _config);
        }

        private string[] GetSuccessFieldNames()
        {
            var fns = new string[_fieldNames.Length + 1];
            fns[0] = "ID";
            for (int i=0; i<_fieldNames.Length; ++i)
                fns[i + 1] = _fieldNames[i];

            return fns;
        }

        private string[] GetErrorFieldNames()
        {
            var fns = new string[_fieldNames.Length + 1];
            for (int i = 0; i < _fieldNames.Length; ++i)
                fns[i] = _fieldNames[i];
            fns[_fieldNames.Length] = "Error";

            return fns;
        }

        private void CheckLoggerTargetDir(string targetFile)
        {
            string resolvedFile = _context.ResolveFileName(targetFile, false);
            string directory = Path.GetDirectoryName(resolvedFile);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private void CreateLoggers()
        {
            CheckLoggerTargetDir(_config.SuccessFileName);
            _successCsv = new CsvWriterPlugin(_context, "file://" + _config.SuccessFileName, GetSuccessFieldNames(), new int[] { }, "delim=',' encoding='UTF-8'");
            CheckLoggerTargetDir(_config.ErrorFileName);
            _errorCsv = new CsvWriterPlugin(_context, "file://" + _config.ErrorFileName, GetErrorFieldNames(), new int[] { }, "delim=',' encoding='UTF-8'");
        }

        private string[] _tempLine;
        private void LogSuccess(string id, sObject o)
        {
            if (null == _tempLine)
                _tempLine = new string[_fieldNames.Length + 1];

            _tempLine[0] = id;
            TransferValues(1, o);

            _successCsv.WriteRecord(_tempLine);
        }

        private void LogError(sObject o, string message)
        {
            _hasErrors = true;

            if (null == _tempLine)
                _tempLine = new string[_fieldNames.Length + 1];

            TransferValues(0, o);
            _tempLine[_fieldNames.Length] = message;

            _errorCsv.WriteRecord(_tempLine);
            _context.Logger.Warning("SfdcDotNetWriter: Failed record, message: '" + message + "'.");
        }

        private string GetElementData(XmlElement[] elements, TargetField field)
        {
            if (field.IsBase64)
                return "(binary data)";
            var element = elements.FirstOrDefault(e => e.Name == field.Name);
            if (null == element)
                return string.Empty;
            if (!string.IsNullOrEmpty(field.ExternalId))
                return element.ChildNodes[1].InnerText; // Take second child InnerText, contains external ID
            return element.InnerText;
        }

        private void TransferValues(int offs, sObject o)
        {
            int elementPointer = 0;
            for (int i=0; i<_fieldNames.Length; ++i)
            {
                if (i == 0 && _operationHasId)
                {
                    _tempLine[i + offs] = o.Id;
                }
                else
                {
                    //if (string.IsNullOrEmpty(_targetFields[i].ExternalId))
                    //    _tempLine[i + offs] = o.Any[elementPointer].InnerText;
                    //else // Take second child InnerText, contains external ID
                    //    _tempLine[i + offs] = o.Any[elementPointer].ChildNodes[1].InnerText;
                    _tempLine[i + offs] = GetElementData(o.Any, _targetFields[i]);
                    elementPointer++;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (_successCsv != null)
                {
                    _successCsv.Dispose();
                    _successCsv = null;
                }
                if (_errorCsv != null)
                {
                    _errorCsv.Dispose();
                    _errorCsv = null;
                }
            }
        }
    }
}

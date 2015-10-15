using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Csv;
using NoFrillsTransformation.Plugins.Salesforce.Config;
using NoFrillsTransformation.Plugins.Salesforce.DotNet.Salesforce35;

namespace NoFrillsTransformation.Plugins.Salesforce.DotNet
{
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
        private string[] _targetFieldNames;
        private string[] _targetExternalId;
        private string[] _targetFieldTypes;
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

        private XmlDocument _xmlDoc = new XmlDocument();
        private sObject CreateSObject(string[] fieldValues)
        {
            var o = new sObject();
            o.type = _target.Entity;

            XmlElement[] xmlElements = null;
            if (!_operationHasId)
                xmlElements = new XmlElement[fieldValues.Length];
            else
                xmlElements = new XmlElement[fieldValues.Length - 1];
            int elementPointer = 0;
            for (int i=0; i<fieldValues.Length; ++i)
            {
                if (i == 0 &&
                    _operationHasId)
                {
                    o.Id = fieldValues[i];
                }
                else
                {
                    var e = _xmlDoc.CreateElement(_targetFieldNames[i]);
                    if (string.IsNullOrEmpty(_targetExternalId[i]))
                    {
                        e.InnerText = fieldValues[i];
                    }
                    else
                    {
                        // Wow. But it seems to be the only way to get this:
                        // External ID reference on upsert/update.
                        var typeE = _xmlDoc.CreateElement("type");
                        typeE.InnerText = _targetFieldTypes[i];
                        var idE = _xmlDoc.CreateElement(_targetExternalId[i]);
                        idE.InnerText = fieldValues[i];
                        e.AppendChild(typeE);
                        e.AppendChild(idE);
                    }

                    xmlElements[elementPointer] = e;
                    elementPointer++;
                }
            }
            o.Any = xmlElements;
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
            foreach (var field in desc.fields)
            {
                _fields[field.name.ToLowerInvariant()] = field.name;
            }
            _targetFieldNames = new string[_fieldNames.Length];
            _targetExternalId = new string[_fieldNames.Length];
            _targetFieldTypes = new string[_fieldNames.Length];
            for (int i = 0; i < _fieldNames.Length; ++i)
            {
                var lowerName = _fieldNames[i].ToLowerInvariant();

                if (_fields.ContainsKey(lowerName))
                    _targetFieldNames[i] = _fields[lowerName];
                else
                    _targetFieldNames[i] = _fieldNames[i];

                if (!string.IsNullOrEmpty(_fieldDefs[i].Config))
                {
                    var c = _fieldDefs[i].Config;
                    // Syntax: Type:ExternalIdField__c
                    // This was taken from how the DataLoader works (works the same there).
                    if (c.Contains(":"))
                    {
                        _targetExternalId[i] = c.Substring(c.IndexOf(":") + 1);
                        _targetFieldTypes[i] = c.Substring(0, c.IndexOf(":"));
                    }
                    else
                    {
                        _targetExternalId[i] = c;
                        _targetFieldTypes[i] = _targetFieldNames[i];
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

        private void CreateLoggers()
        {
            _successCsv = new CsvWriterPlugin(_context, "file://" + _config.SuccessFileName, GetSuccessFieldNames(), new int[] { }, "delim=',' encoding='UTF-8'");
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
                    if (string.IsNullOrEmpty(_targetExternalId[i]))
                        _tempLine[i + offs] = o.Any[elementPointer].InnerText;
                    else // Take second child InnerText, contains external ID
                        _tempLine[i + offs] = o.Any[elementPointer].ChildNodes[1].InnerText;
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

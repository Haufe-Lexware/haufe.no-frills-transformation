using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Sap.Config;
using SAP.Middleware.Connector;

namespace NoFrillsTransformation.Plugins.Sap
{
    internal class SapStructure
    {
        public SapStructure()
        {
            Fields = new HashSet<string>();
            Tables = new Dictionary<string, SapStructure>();
            Structures = new Dictionary<string, SapStructure>();
        }

        public string Name { get; set; }
        public HashSet<string> Fields { get; set; }
        public Dictionary<string, SapStructure> Tables { get; set; }
        public Dictionary<string, SapStructure> Structures { get; set; }

        public bool IsTable { get; set; }
        public bool Flatten { get; set; }
    }

    internal enum ParameterType
    {
        Input,
        Output
    }

    internal class SapTransformer : ISourceTransformer, IRecord
    {

        public SapTransformer(IContext context, SapConfig sapConfig, string rfcName, IParameter[] parameters, ISetting[] settings)
        {
            _context = context;
            _sapConfig = sapConfig;
            _rfcName = rfcName;
            _parameters = parameters;
            _settings = settings;
        }

        private bool _isInitialized = false;
        private IContext _context;
        private SapConfig _sapConfig;
        private string _rfcName;
        private IParameter[] _parameters;
        private ISetting[] _settings;

        private SapStructure _requestStructure;
        private SapStructure _responseStructure;

        private IRfcFunction _rfc;
        private RfcDestination _rfcDestination;

        private bool _hasFlatten = false;
        private int _flattenIndex = 0;
        private int _flattenCount = -1;

        private void Initialize()
        {
            _context.Logger.Info("SapTransformer: Initializing transformation.");
            RfcDestinationManager.RegisterDestinationConfiguration(new BackendConfig(_sapConfig));
            _rfcDestination = RfcDestinationManager.GetDestination(_sapConfig.RfcDestination);

            RfcRepository repo = _rfcDestination.Repository;
            _rfc = repo.CreateFunction(_rfcName);

            IEnumerable<IRfcParameter> paramList = _rfc;

            _responseStructure = ParseSapParameters(paramList, ParameterType.Output);
            _requestStructure = ParseSapParameters(paramList, ParameterType.Input);

            _context.Logger.Info("SapTransformer: Input Structure.");
            DebugOutputStructures(_context.Logger, "", _requestStructure);
            _context.Logger.Info("SapTransformer: Outputting available transformed fields list.");
            DebugOutputStructures(_context.Logger, "", _responseStructure);

            _context.Logger.Info("SapTransformer: Applying settings.");
            // Settings
            ApplySettings(_context);

            _isInitialized = true;
        }

        private static void DebugOutputStructures(ILogger logger, string prefix, SapStructure sap)
        {
            foreach (var f in sap.Fields)
            {
                logger.Info(prefix + f);
            }
            foreach (var s in sap.Structures.Keys)
            {
                DebugOutputStructures(logger, prefix + s + "-", sap.Structures[s]);
            }
            foreach (var t in sap.Tables.Keys)
            {
                DebugOutputStructures(logger, prefix + t + "[]-", sap.Tables[t]);
            }
        }

        private SapStructure ParseSapParameters(IEnumerable<IRfcParameter> paramList, ParameterType paramType)
        {
            var outputStructures = new SapStructure();
            foreach (var parameter in paramList)
            {
                var metadata = parameter.Metadata;
                if (paramType == ParameterType.Output)
                {
                    if (metadata.Direction != RfcDirection.EXPORT
                        && metadata.Direction != RfcDirection.CHANGING)
                        continue;
                }
                else if (paramType == ParameterType.Input)
                {
                    if (metadata.Direction != RfcDirection.IMPORT
                        && metadata.Direction != RfcDirection.CHANGING)
                        continue;
                }

                switch (metadata.DataType)
                {
                    case RfcDataType.STRUCTURE:
                        outputStructures.Structures.Add(metadata.Name, ParseSapStructure(metadata.Name, parameter.GetStructure().Metadata));
                        break;

                    case RfcDataType.TABLE:
                        if (!metadata.Name.Equals("CONTROLLER", StringComparison.InvariantCultureIgnoreCase))
                            outputStructures.Tables.Add(metadata.Name, ParseSapTable(metadata.Name, parameter.GetTable().Metadata));
                        break;

                    default: // treat as string, might be wrong
                        outputStructures.Fields.Add(metadata.Name.ToUpperInvariant());
                        break;
                }
            }
            return outputStructures;
        }

        private SapStructure ParseSapStructure(string name, RfcStructureMetadata rfcStructure)
        {
            var outputStructures = new SapStructure { Name = name };
            for (int i=0; i<rfcStructure.FieldCount; ++i)
            {
                var metadata = rfcStructure[i];

                switch (metadata.DataType)
                {
                    case RfcDataType.STRUCTURE:
                        outputStructures.Structures.Add(metadata.Name, ParseSapStructure(metadata.Name, metadata.ValueMetadataAsStructureMetadata));
                        break;

                    case RfcDataType.TABLE:
                        if (!metadata.Name.Equals("CONTROLLER", StringComparison.InvariantCultureIgnoreCase))
                            outputStructures.Tables.Add(metadata.Name, ParseSapTable(metadata.Name, metadata.ValueMetadataAsTableMetadata));
                        break;

                    default: // treat as string, might be wrong
                        outputStructures.Fields.Add(CheckName(metadata.Name));
                        break;
                }
            }
            return outputStructures;
        }

        //private IEnumerable<IRfcField> MakeDoof(RfcStructureMetadata structure)
        //{
        //    return null;
        //}

        private SapStructure ParseSapTable(string name, RfcTableMetadata tableMetadata)
        {
            var outputStructures = new SapStructure { Name = name };
            outputStructures.IsTable = true;

            var lineType = tableMetadata.LineType;
            //foreach (IRfcField field in lineType)
            for (int i = 0; i < lineType.FieldCount; ++i)
            {
                var metadata = lineType[i];

                switch (metadata.DataType)
                {
                    case RfcDataType.STRUCTURE:
                        //var structure = rfcTable.GetStructure(i);
                        var structure = metadata.ValueMetadataAsStructureMetadata;
                        outputStructures.Structures.Add(metadata.Name, ParseSapStructure(metadata.Name, structure));
                        break;

                    case RfcDataType.TABLE:
                        if (!metadata.Name.Equals("CONTROLLER", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //var subTable = rfcTable.GetTable(i);
                            var subTable = metadata.ValueMetadataAsTableMetadata;
                            outputStructures.Tables.Add(metadata.Name, ParseSapTable(metadata.Name, subTable));
                        }
                        break;

                    default: // treat as string, might be wrong
                        outputStructures.Fields.Add(CheckName(metadata.Name));
                        break;
                }
            }

            /*
            // TABLES or STRUCTURES inside TABLES aren't currently supported.
            for (int i = 0; i < lineType.FieldCount; ++i)
            {
                string metadataName = lineType[i].Name.ToUpperInvariant();
                switch (lineType[i].RfcDataType)
                {
                    case RfcDataType.STRUCTURE:
                        outputStructures.Structures.Add(metadataName, ParseSapStructure(metadataName, field.GetStructure()));
                        break;

                    case RfcDataType.TABLE:
                        if (!metadata.Name.Equals("CONTROLLER", StringComparison.InvariantCultureIgnoreCase))
                            outputStructures.Tables.Add(metadataName, ParseSapTable(metadataName, field.GetTable()));
                        break;

                    default: // treat as string, might be wrong
                        outputStructures.Fields.Add(metadataName.ToUpperInvariant());
                        break;

                }
//                outputStructures.Fields.Add(lineType[i].Name.ToUpperInvariant());
            }
            */

            return outputStructures;
        }

        private static string CheckName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "_VALUE";
            return name.ToUpperInvariant();
        }

        private static SapStructure GetSapStructure(SapStructure root, string fieldString)
        {
            fieldString = fieldString.ToUpperInvariant().Trim();
            if (fieldString.StartsWith("$"))
                fieldString = fieldString.Substring(1);
            string[] fields = fieldString.Split(new char[] { '-' });
            return GetSapStructureRecursive(root, fields, 0);
        }

        private static SapStructure GetSapStructureRecursive(SapStructure sap, string[] fields, int position)
        {
            if (position >= fields.Length)
                return null; // Overflow
            string fieldName = fields[position];
            bool lastField = (position == fields.Length - 1);
            if (sap.Fields.Contains(fieldName))
                return sap;
            if (sap.Tables.ContainsKey(fieldName))
            {
                if (lastField)
                    return sap.Tables[fieldName];
                return GetSapStructureRecursive(sap.Tables[fieldName], fields, position + 1);
            }
            else if (sap.Structures.ContainsKey(fieldName))
            {
                if (lastField)
                    return sap.Structures[fieldName];
                return GetSapStructureRecursive(sap.Structures[fieldName], fields, position + 1);
            }
            return null; // Not found
        }

        private void ApplySettings(IContext context)
        {
            // Only thing supported is "flatten"
            foreach (var setting in _settings)
            {
                switch (setting.Name.ToUpperInvariant())
                {
                    case "FLATTEN":
                        ApplyFlatten(setting.Setting);
                        break;
                    default:
                        context.Logger.Warning("SapTransformer: Unknown Setting '" + setting.Name + "'. Ignored.");
                        break;
                }
            }
        }

        private void ApplyFlatten(string tableName)
        {
            var sapTable = GetSapStructure(_responseStructure, tableName);
            if (null == sapTable)
                throw new ArgumentException("SapTransform - FLATTEN setting: Could not find table structure '" + tableName + "'.");
            if (!sapTable.IsTable)
                throw new ArgumentException("SapTransform - FLATTEN setting: The specified table to flatten is not a table: " + tableName);
            if (_hasFlatten)
                throw new ArgumentException("SapTransform - FLATTEN setting: Only one single table can be flattened.");
            sapTable.Flatten = true;
            _hasFlatten = true;

            _context.Logger.Info("SapTransformer: Flattening output table '" + tableName + "'.");
        }

        public void Transform(IContext context, IEvaluator eval)
        {
            if (!_isInitialized)
                Initialize();

            _flattenIndex = 0;
            _flattenCount = -1; // Not yet read out

            foreach (var parameter in _parameters)
            {
                SetParameter(parameter.Name, eval.Evaluate(eval, parameter.Function, context));
            }

            //_rfc = _rfcDestination.Repository.CreateFunction(_rfcName);
            CleanUpTables();
            _rfc.Invoke(_rfcDestination);
        }

        private void CleanUpTables()
        {
            var sap = _responseStructure;

            if (sap.Tables.Count > 0)
            {
                foreach (string tableName in sap.Tables.Keys)
                {
                    IRfcTable rfcTable = _rfc[tableName].GetTable();
                    while (rfcTable.RowCount > 0)
                        rfcTable.Delete();
                }
            }
            foreach (string structName in sap.Structures.Keys)
            {
                IRfcStructure structure = _rfc[structName].GetStructure();
                CleanUpTablesRecursive(sap.Structures[structName], structure);
            }
        }

        private void CleanUpTablesRecursive(SapStructure sap, IRfcStructure structure)
        {
            if (sap.Tables.Count > 0)
            {
                foreach (string tableName in sap.Tables.Keys)
                {
                    IRfcTable rfcTable = structure[tableName].GetTable();
                    while (rfcTable.RowCount > 0) 
                        rfcTable.Delete();
                }
            }
            foreach (string structName in sap.Structures.Keys)
            {
                IRfcStructure subStructure = structure[structName].GetStructure();
                CleanUpTablesRecursive(sap.Structures[structName], subStructure);
            }
        }

        public bool HasMoreRecords()
        {
            if (!_hasFlatten)
                return false;
            if (_flattenCount <= 0)
                return false;
            return (_flattenIndex + 1 < _flattenCount);
        }

        public void NextRecord()
        {
            _flattenIndex++;
        }

        private void SetParameter(string name, string value)
        {
            name = name.ToUpperInvariant();

            string[] fieldHierarchy = name.Split(new char[] { '-' });

            string field = fieldHierarchy[0];
            var sap = _requestStructure;
            if (sap.Fields.Contains(field))
            {
                _rfc[field].SetValue(value);
            }
            else if (sap.Structures.ContainsKey(field))
            {
                SetParameterRecursive(_rfc[field].GetStructure(), fieldHierarchy, 1, value, sap.Structures[field]);
            }
            else if (sap.Tables.ContainsKey(field))
            {
                throw new ArgumentException("Parameters inside TABLE elements are currently not supported: " + field);
            }
            else
            {
                throw new ArgumentException("SapTransformer - Unknown input parameter field: " + name + " (" + field + ").");
            }
        }

        private void SetParameterRecursive(IRfcStructure structure, string[] fieldHierarchy, int position, string value, SapStructure sap)
        {
            string field = fieldHierarchy[position];
            int index = -1;
            if (field.Contains("["))
            {
                int open = field.IndexOf('[');
                int close = field.IndexOf(']');
                if (open < 0
                    || close < 0)
                    throw new ArgumentException("SAP Transformer: SetParameter - Could not find index in expression " + field);
                index = int.Parse(field.Substring(open + 1, close - open - 1));
                field = field.Substring(0, open);
            }
            if (index >= 0)
            {
                // Has to be table
                if (!sap.Tables.ContainsKey(field))
                {
                    throw new ArgumentException("SAP Transformer: SetParameter - Index was passed with field " + field + ", but no TABLE found in current structure.");
                }
                var table = structure[field].GetTable();
                if (table.Count <= index)
                {
                    int appendCount = table.Count - index + 1;
                    if (appendCount > 0)
                        table.Append(appendCount);
                }
                SetParameterRecursive(table, index, fieldHierarchy, position + 1, value, sap.Tables[field]);
            }
            else if (sap.Fields.Contains(field))
            {
                structure[field].SetValue(value);
            }
            else if (sap.Structures.ContainsKey(field))
            {
                SetParameterRecursive(structure[field].GetStructure(), fieldHierarchy, position + 1, value, sap.Structures[field]);
            }
            else
            {
                throw new ArgumentException("SapTransformer - Unknown input parameter field: " + field + ".");
            }
        }
        
        private void SetParameterRecursive(IRfcTable table, int tableIndex, string[] fieldHierarchy, int position, string value, SapStructure sap)
        {
            string field = fieldHierarchy[position];
            int index = -1;
            if (field.Contains("["))
            {
                int open = field.IndexOf('[');
                int close = field.IndexOf(']');
                if (open < 0
                    || close < 0)
                    throw new ArgumentException("SAP Transformer: SetParameter - Could not find index in expression " + field);
                index = int.Parse(field.Substring(open + 1, close - open - 1));
            }
            if (index >= 0)
            {
                // Has to be table
                if (!sap.Tables.ContainsKey(field))
                {
                    throw new ArgumentException("SAP Transformer: SetParameter - Index was passed with field " + field + ", but no TABLE found in current structure.");
                }
                var subTable = table[tableIndex][field].GetTable();
                if (subTable.Count < index)
                {
                    int appendCount = subTable.Count - index;
                    if (appendCount > 0)
                        subTable.Append(appendCount);
                }
                SetParameterRecursive(subTable, index, fieldHierarchy, position + 1, value, sap.Tables[field]);
            }
            else if (sap.Fields.Contains(field))
            {
                table[tableIndex][CheckNameReverse(field)].SetValue(value);
            }
            else if (sap.Structures.ContainsKey(field))
            {
                SetParameterRecursive(table[tableIndex][field].GetStructure(), fieldHierarchy, position + 1, value, sap.Structures[field]);
            }
            else
            {
                throw new ArgumentException("SapTransformer - Unknown input parameter field: " + field + ".");
            }
        }

        private static string CheckNameReverse(string name)
        {
            if (name.Equals("_VALUE"))
                return "";
            return name;
        }

        private Dictionary<string, bool> _fieldCache = new Dictionary<string, bool>();

        public bool HasField(string fieldName)
        {
            fieldName = fieldName.ToUpperInvariant();

            if (_fieldCache.ContainsKey(fieldName))
                return _fieldCache[fieldName];

            string[] fieldHierarchy = fieldName.Split(new char[] { '-' });

            bool success = HasFieldRecursive(fieldHierarchy, 0, _responseStructure);

            _fieldCache[fieldName] = success;
            return success;
        }

        private bool HasFieldRecursive(string[] fields, int position, SapStructure structure)
        {
            if (position >= fields.Length)
                throw new ArgumentException("Last requested field was a structure or table, not a field: " + structure.Name);
            bool isLastPosition = ((position + 1) >= fields.Length);
            string field = fields[position];
            if (field.Contains('['))
                field = field.Substring(0, field.IndexOf('['));
            if (structure.Fields.Contains(field))
            {
                if (isLastPosition)
                    return true;
                throw new ArgumentException("Field '" + field + "' is a field (end node) in the structure, sub fields are requested.");
            }
            if (structure.Structures.ContainsKey(field))
            {
                return HasFieldRecursive(fields, position + 1, structure.Structures[field]);
            }
            if (structure.Tables.ContainsKey(field))
            {
                return HasFieldRecursive(fields, position + 1, structure.Tables[field]);
            }
            return false;
        }

        public IRecord CurrentRecord
        {
            get 
            {
                return this;
            }
        }


        public string this[int index]
        {
            get { throw new NotImplementedException(); }
        }

        public string this[string requestedFieldName]
        {
            get 
            {
                var name = requestedFieldName.ToUpperInvariant();
                var sap = _responseStructure;
                if (sap.Fields.Contains(name))
                {
                    return _rfc[name].GetString();
                }
                else
                {
                    string[] fieldNames = name.Split(new char[] { '-' });
                    string fieldName = fieldNames[0];
                    string field = fieldName;
                    if (field.Contains('['))
                        field = field.Substring(0, field.IndexOf('[')); 
                    if (sap.Structures.ContainsKey(field))
                    {
                        return GetStructureFieldValue(fieldNames, 1, _rfc[field].GetStructure(), sap.Structures[field]);
                    }
                    else if (sap.Tables.ContainsKey(field))
                    {
                        var table = sap.Tables[field];
                        var rfcTable = _rfc[field].GetTable();
                        return HandleTable(rfcTable, table, fieldNames, 0, fieldName, field);
                    }
                }
                throw new ArgumentException("SapTransformer: Output field '" + requestedFieldName + "' not found.");
            }
        }

        private string HandleTable(IRfcTable rfcTable, SapStructure table, string[] fieldNames, int position, string fieldName, string field)
        {
            if (table.Flatten)
            {
                int open = fieldName.IndexOf('[');
                if (open >= 0)
                    throw new ArgumentException("Table index must not be supplied with a flattened table: " + fieldName);
                if (_flattenCount < 0)
                {
                    _flattenCount = rfcTable.RowCount;
                }
                if (_flattenIndex >= _flattenCount)
                    return ""; // Return empty value here; this can only be the case if we have an empty table
                return GetTableFieldValue(fieldNames, position + 1, rfcTable, _flattenIndex, table);
            }
            else
            {
                int open = fieldName.IndexOf('[');
                int close = fieldName.IndexOf(']');
                if (open < 0
                    || close < 0)
                    throw new ArgumentException("Table index not supplied for table type: " + field + ", expected: " + field + "[<index>]. Consider flatten?");
                string indexString = fieldName.Substring(open + 1, close - open - 1);
                int index = int.Parse(indexString);
                return GetTableFieldValue(fieldNames, position + 1, rfcTable, index, table);
            }
        }

        private string GetTableFieldValue(string[] fieldNames, int pos, IRfcTable rfcTable, int index, SapStructure sap)
        {
            string fieldName = fieldNames[pos];
            string field = fieldName;
            if (field.Contains('['))
                field = field.Substring(0, field.IndexOf('['));
            if (sap.Fields.Contains(field))
            {
                return rfcTable[index][field].GetString();
            }
            else if (sap.Structures.ContainsKey(field))
            {
                return GetStructureFieldValue(fieldNames, pos + 1, rfcTable[index][field].GetStructure(), sap.Structures[field]);
            }
            else if (sap.Tables.ContainsKey(field))
            {
                var table = sap.Tables[field];
                var rfcSubTable = rfcTable[index][field].GetTable();
                return HandleTable(rfcSubTable, table, fieldNames, pos, fieldName, field);
                //int open = fieldName.IndexOf('[');
                //int close = fieldName.IndexOf(']');
                //if (open < 0
                //    || close < 0)
                //    throw new ArgumentException("Table index not supplied for table type: " + field + ", expected: " + field + "[<index>].");
                //string indexString = fieldName.Substring(open + 1, close - open - 1);
                //int index2 = int.Parse(indexString); 
                //return GetTableFieldValue(fieldNames, pos + 1, rfcTable[index][field].GetTable(), index2, sap.Tables[field]);
            }
            throw new ArgumentException("SapTransformer - Output field not found: " + field);
        }

        private string GetStructureFieldValue(string[] fieldNames, int pos, IRfcStructure rfcStructure, SapStructure sap)
        {
            string fieldName = fieldNames[pos];
            string field = fieldName;
            if (field.Contains('['))
                field = field.Substring(0, field.IndexOf('[')); 
            if (sap.Fields.Contains(field))
            {
                return rfcStructure[field].GetString();
            }
            else if (sap.Structures.ContainsKey(field))
            {
                return GetStructureFieldValue(fieldNames, pos + 1, rfcStructure[field].GetStructure(), sap.Structures[field]);
            }
            else if (sap.Tables.ContainsKey(field))
            {
                var table = sap.Tables[field];
                var rfcTable = rfcStructure[field].GetTable();
                return HandleTable(rfcTable, table, fieldNames, pos, fieldName, field);
                //int open = fieldName.IndexOf('[');
                //int close = fieldName.IndexOf(']');
                //if (open < 0
                //    || close < 0)
                //    throw new ArgumentException("Table index not supplied for table type: " + field + ", expected: " + field + "[<index>].");
                //string indexString = fieldName.Substring(open + 1, close - open - 1);
                //int index = int.Parse(indexString); 
                //return GetTableFieldValue(fieldNames, pos + 1, rfcStructure[field].GetTable(), index, sap.Tables[field]);
            }
            throw new ArgumentException("SapTransformer - Output field not found: " + field);
        }
        
        public void Dispose()
        {
            throw new NotImplementedException();
        }

    }
}

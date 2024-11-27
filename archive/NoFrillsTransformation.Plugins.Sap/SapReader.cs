using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Sap.Config;
using SAP.Middleware.Connector;

namespace NoFrillsTransformation.Plugins.Sap
{
    internal class SapReader : ISourceReader, IRecord
    {
        public SapReader(IContext context, SapQuery sapQuery, SapConfig sapConfig)
        {
            _context = context;
            _sapQuery = sapQuery;
            _config = sapConfig;

            Initialize();
        }

        private IContext _context;
        private SapQuery _sapQuery;
        private SapConfig _config;

        private IRfcTable _table;
        private int _recordPointer = -1;
        private int _recordCount = 0;

        public bool IsEndOfStream
        {
            get
            {
                if (_recordPointer >= _recordCount)
                    return true;
                return false;
            }
        }

        public void NextRecord()
        {
            _recordPointer++;
        }

        public IRecord CurrentRecord
        {
            get
            {
                return this;
            }
        }

        public int FieldCount
        {
            get
            {
                return _fieldNames.Length;
            }
        }

        private string[] _fieldNames;
        public string[] FieldNames
        {
            get
            {
                return _fieldNames;
            }
        }

        private Dictionary<string, int> _fieldIndexes;
        public int GetFieldIndex(string fieldName)
        {
            if (_fieldIndexes == null)
            {
                _fieldIndexes = new Dictionary<string, int>();
                for (int i = 0; i < FieldNames.Length; ++i)
                    _fieldIndexes[FieldNames[i].ToLowerInvariant()] = i;
            }
            return _fieldIndexes[fieldName.ToLowerInvariant()];
        }


        public string this[int index]
        {
            get
            {
                return _table[_recordPointer][index].GetString();
            }
        }

        public string this[string fieldName]
        {
            get
            {
                return _table[_recordPointer][fieldName].GetString();
            }
        }

        #region SAP Stuff
        private void Initialize()
        {
            RfcDestinationManager.RegisterDestinationConfiguration(new BackendConfig(_config));
            RfcDestination prd = RfcDestinationManager.GetDestination(_config.RfcDestination);

            RfcRepository repo = prd.Repository;
            IRfcFunction rfc = repo.CreateFunction(_sapQuery.RfcName);
            foreach (var parameter in _sapQuery.Parameters)
            {
                rfc.SetValue(parameter.Name, parameter.Value);
            }

            rfc.Invoke(prd);

            string exportTableName = null;

            for (int i = 0; i < rfc.ElementCount; ++i)
            {
                var meta = rfc[i].Metadata;
                if (meta.Direction.ToString() == "EXPORT"
                    && meta.DataType.ToString() == "TABLE")
                {
                    // Yay!
                    exportTableName = meta.Name;
                }
            }
            if (null == exportTableName)
                throw new InvalidOperationException("The RFC " + _sapQuery.RfcName + " does not have an EXPORT parameter with TABLE structure.");

            var tableObject = rfc[exportTableName];
            _table = tableObject.GetTable();
            _recordCount = _table.RowCount;
            if (_recordCount <= 0)
                throw new InvalidOperationException("The SAP Query '" + _sapQuery + "' returned an empty table. This is not supported!");
            _recordPointer = -1;
            _fieldNames = new string[_table[0].ElementCount];
            for (int i = 0; i < _fieldNames.Length; ++i)
                _fieldNames[i] = _table[0][i].Metadata.Name;
        }
        #endregion

        public IRecord Query(string key)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}

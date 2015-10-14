using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;

namespace NoFrillsTransformation.Plugins.Salesforce.DotNet
{
    public class SfdcDotNetReader : SfdcDotNetBase, ISourceReader, IRecord
    {
        public SfdcDotNetReader(IContext context, SoqlQuery query, SfdcConfig config) : base(context, config)
        {
            _query = query;
        }

        //// NOTE: Leave out the finalizer altogether if this class doesn't 
        //// own unmanaged resources itself, but leave the other methods
        //// exactly as they are. 
        //~SfdcReader()
        //{
        //    // Finalizer calls Dispose(false)
        //    Dispose(false);
        //}

        private SoqlQuery _query;
        private Salesforce35.QueryResult _queryResult;
        private int _pointer;
        //private bool _endOfStream;

        public void Initialize()
        {
            Login();

            _queryResult = _sfdc.query(_query.Soql);
            _pointer = -1;
        }

        public bool IsEndOfStream
        {
            get 
            {
                return (_pointer >= _queryResult.records.Length);
            }
        }

        public void NextRecord()
        {
            _pointer++;
            if (_pointer >= _queryResult.records.Length
                && !_queryResult.done)
            {
                _queryResult = _sfdc.queryMore(_queryResult.queryLocator);
                _pointer = 0;
            }
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
                return _query.FieldNames.Length;
            }
        }

        public string[] FieldNames
        {
            get
            {
                return _query.FieldNames;
            }
        }

        public IRecord Query(string key)
        {
            throw new NotImplementedException();
        }

        private Dictionary<string, int> _fieldIndexes;

        private void CheckFieldIndexes()
        {
            if (null != _fieldIndexes)
                return;

            _fieldIndexes = new Dictionary<string, int>();
            for (int i=0; i<_query.FieldNames.Length; ++i)
            {
                _fieldIndexes[_query.FieldNames[i].ToLowerInvariant()] = i;
            }
        }

        public int GetFieldIndex(string fieldName)
        {
            CheckFieldIndexes();
            var lowerName = fieldName.ToLowerInvariant();
            if (!_fieldIndexes.ContainsKey(lowerName))
                throw new ArgumentException("SfdcDotNet: Unknown field '" + fieldName + "'.");
            return _fieldIndexes[lowerName];
        }

        public string this[int index]
        {
            get 
            {
                return _queryResult.records[_pointer].Any[index].InnerText;
            }
        }

        public string this[string fieldName]
        {
            get 
            {
                return this[GetFieldIndex(fieldName)];
            }
        }
    }
}

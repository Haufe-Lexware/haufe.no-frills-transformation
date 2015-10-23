using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;

namespace NoFrillsTransformation.Plugins.Salesforce.DotNet
{
    class SfdcDotNetTransformer : SfdcDotNetBase, ISourceTransformer, IRecord
    {
        public SfdcDotNetTransformer(IContext context, SfdcConfig config, SoqlQuery query, IParameter[] parameters, ISetting[] settings)
            : base(context, config)
        {
            _query = query;
            _parameters = parameters;
            _settings = settings; // Not used

            Login();
        }

        private SoqlQuery _query;
        private IParameter[] _parameters;
        private ISetting[] _settings;

        private Salesforce35.QueryResult _queryResult;
        private int _pointer;

        public void Transform(IContext context, IEvaluator eval)
        {
            string soql = _query.Soql;
            foreach (var parameter in _parameters)
            {
                soql = soql.Replace("%" + parameter.Name, eval.Evaluate(eval, parameter.Function, context));
            }

            _queryResult = _sfdc.query(soql);
            _pointer = 0;
        }

        private HashSet<string> _fields;
        public bool HasField(string fieldName)
        {
            if (null == _fields)
            {
                _fields = new HashSet<string>();
                foreach (var field in _query.FieldNames)
                    _fields.Add(field);
            }
            return _fields.Contains(fieldName);
        }

        public IRecord CurrentRecord
        {
            get { return this; }
        }

        public bool HasMoreRecords()
        {
            if (null == _queryResult.records)
                return false;
            return (_pointer + 1 < _queryResult.records.Length);
        }

        public bool HasResult()
        {
            if (null == _queryResult.records)
                return false;
            return (_queryResult.records.Length > 0);
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

        public void FinishTransform()
        {
            // Nothing special needed here.
        }

        private Dictionary<string, int> _fieldIndexes;

        private void CheckFieldIndexes()
        {
            if (null != _fieldIndexes)
                return;

            _fieldIndexes = new Dictionary<string, int>();
            for (int i = 0; i < _query.FieldNames.Length; ++i)
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
                if (_queryResult.records == null 
                    || _pointer >= _queryResult.records.Length)
                    return ""; // Gracefully just return empty string; this kicks in if we have an empty query result
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

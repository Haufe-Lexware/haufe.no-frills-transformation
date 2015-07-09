using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado
{
    public class AdoReader : ISourceReader, IRecord
    {
        public AdoReader(IContext context, string config, string sqlQuery)
        {
            _context = context;
            _config = config;
            _sqlQuery = sqlQuery;

            Initialize();
        }

        private string _config;
        private string _sqlQuery;
        private IContext _context;
        private bool _endOfStream = false;

        //private SqlConnection _sqlConnection;
        //private SqlCommand _sqlCommand;
        //private IDataReader _sqlReader;

        protected string Config { get { return _config; } }
        protected string SqlQuery { get { return _sqlQuery; } }
        protected IContext Context { get { return _context; } }

        protected virtual IDataReader SqlReader { get { return null; } }

        protected virtual void Initialize()
        {
            //_sqlConnection = new SqlConnection(_config);
            //_sqlCommand = new SqlCommand(_sqlQuery, _sqlConnection);
            //_sqlConnection.Open();
            //_sqlReader = _sqlCommand.ExecuteReader();
        }

        public bool IsEndOfStream
        {
            get 
            { 
                return _endOfStream; 
            }
        }

        public void NextRecord()
        {
            _endOfStream = !SqlReader.Read();
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
                return SqlReader.FieldCount;
            }
        }

        private string[] _fieldNames;
        public string[] FieldNames
        {
            get
            {
                if (null == _fieldNames)
                {
                    int count = SqlReader.FieldCount;
                    _fieldNames = new string[count];
                    for (int i = 0; i < count; ++i)
                        _fieldNames[i] = SqlReader.GetName(i);
                }
                return _fieldNames;
            }
        }

        private Dictionary<string, int> _fieldIndexes;
        public int GetFieldIndex(string fieldName)
        {
            if (null == _fieldIndexes)
            {
                _fieldIndexes = new Dictionary<string, int>();
                for (int i = 0; i < FieldNames.Length; ++i)
                    _fieldIndexes.Add(FieldNames[i].ToLowerInvariant(), i);
            }
            fieldName = fieldName.ToLowerInvariant();
            if (_fieldIndexes.ContainsKey(fieldName))
                return _fieldIndexes[fieldName];
            return -1;
        }

        public IRecord Query(string key)
        {
            throw new NotImplementedException();
        }

        #region IRecord
        public string this[int index]
        {
            get
            {
                return SqlReader.GetValue(index).ToString();
            }
        }

        public string this[string fieldName]
        {
            get
            {
                int fieldIndex = GetFieldIndex(fieldName);
                if (fieldIndex < 0)
                    throw new ArgumentException("Unknown field name: " + fieldName);
                return this[fieldIndex];
            }
        }
        #endregion

        #region IDisposable
        // Dispose() calls Dispose(true)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
        #endregion
    }
}

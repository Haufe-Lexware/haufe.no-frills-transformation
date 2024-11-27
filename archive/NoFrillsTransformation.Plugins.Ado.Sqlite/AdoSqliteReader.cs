using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado.Sqlite
{
    public class AdoSqliteReader : AdoReader
    {
        public AdoSqliteReader(IContext context, string config, string sqlQuery)
            : base(context, config, sqlQuery)
        {
        }

        private SQLiteConnection _sqlConnection;
        private SQLiteCommand _sqlCommand;
        private IDataReader _sqlReader;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoSqliteReader initializing...");
            _sqlConnection = new SQLiteConnection(Config);
            _sqlCommand = _sqlConnection.CreateCommand();
            _sqlCommand.CommandText = SqlQuery;
            _sqlConnection.Open();
            _sqlReader = _sqlCommand.ExecuteReader();
            Context.Logger.Info("AdoSqliteReader initialized.");
        }

        protected override IDataReader SqlReader
        {
            get
            {
                return _sqlReader;
            }
        }

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (null != _sqlReader)
                {
                    _sqlReader.Dispose();
                    _sqlReader = null;
                }
                if (null != _sqlCommand)
                {
                    _sqlCommand.Dispose();
                    _sqlCommand = null;
                }
                if (null != _sqlConnection)
                {
                    _sqlConnection.Close();
                    _sqlConnection.Dispose();
                    _sqlConnection = null;
                }
            }
        }
        #endregion
    }
}

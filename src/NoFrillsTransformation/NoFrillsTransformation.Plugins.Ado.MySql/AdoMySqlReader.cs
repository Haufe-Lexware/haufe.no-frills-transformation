using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado.MySql
{
    public class AdoMySqlReader : AdoReader
    {
        public AdoMySqlReader(IContext context, string config, string sqlQuery)
            : base(context, config, sqlQuery)
        {
        }

        private MySqlConnection _sqlConnection;
        private MySqlCommand _sqlCommand;
        private IDataReader _sqlReader;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoMySqlReader initializing...");
            _sqlConnection = new MySqlConnection(Config);
            _sqlCommand = new MySqlCommand(SqlQuery, _sqlConnection);
            _sqlConnection.Open();
            _sqlReader = _sqlCommand.ExecuteReader();
            Context.Logger.Info("AdoMySqlReader initialized.");
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

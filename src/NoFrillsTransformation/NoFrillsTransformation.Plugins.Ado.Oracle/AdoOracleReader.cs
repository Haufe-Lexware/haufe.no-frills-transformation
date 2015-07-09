using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado.Oracle
{
    internal class AdoOracleReader : AdoReader
    {
        public AdoOracleReader(IContext context, string config, string sqlQuery)
            : base(context, config, sqlQuery)
        {
        }

        private OracleConnection _sqlConnection;
        private OracleCommand _sqlCommand;
        private IDataReader _sqlReader;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoOracleReader initializing...");
            _sqlConnection = new OracleConnection(Config);
            _sqlCommand = new OracleCommand(SqlQuery, _sqlConnection);
            _sqlConnection.Open();
            _sqlReader = _sqlCommand.ExecuteReader();
            Context.Logger.Info("AdoOracleReader initialized.");
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

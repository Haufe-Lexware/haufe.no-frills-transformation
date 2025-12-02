using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado.SqlServer
{
    internal class AdoSqlServerReader : AdoReader
    {
        public AdoSqlServerReader(IContext context, string? config, string sqlQuery)
            : base(context, config, sqlQuery)
        {
        }

        private SqlConnection? _sqlConnection;
        private SqlCommand? _sqlCommand;
        private IDataReader? _sqlReader;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoSqlServerReader initializing...");
            _sqlConnection = new SqlConnection(Config);
            _sqlCommand = new SqlCommand(SqlQuery, _sqlConnection);
            _sqlConnection.Open();
            _sqlReader = _sqlCommand.ExecuteReader();
            Context.Logger.Info("AdoSqlServerReader initialized.");
        }

        protected override IDataReader SqlReader
        {
            get
            {
                if (null == _sqlReader)
                    throw new InvalidOperationException("AdoSqlServerReader not initialized.");
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

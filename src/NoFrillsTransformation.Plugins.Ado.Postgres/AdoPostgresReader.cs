using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using Npgsql;

namespace NoFrillsTransformation.Plugins.Ado.Postgres
{
    internal class AdoPostgresReader : AdoReader
    {
        public AdoPostgresReader(IContext context, string? config, string sqlQuery)
            : base(context, config, sqlQuery)
        {
        }

        private NpgsqlConnection? _psqlConnection;
        private NpgsqlCommand? _psqlCommand;
        private IDataReader? _sqlReader;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoPostgresReader initializing...");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(Config);
            var dataSource = dataSourceBuilder.Build();

            _psqlConnection = dataSource.OpenConnection();
            _psqlCommand = new NpgsqlCommand(SqlQuery, _psqlConnection);
            _sqlReader = _psqlCommand.ExecuteReader();

            Context.Logger.Info("AdoPostgresReader initialized.");
        }

        protected override IDataReader SqlReader
        {
            get
            {
                if (null == _sqlReader)
                    throw new InvalidOperationException("AdoPostgresReader not initialized.");
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
                if (null != _psqlCommand)
                {
                    _psqlCommand.Dispose();
                    _psqlCommand = null;
                }
                if (null != _psqlConnection)
                {
                    _psqlConnection.Close();
                    _psqlConnection.Dispose();
                    _psqlConnection = null;
                }
            }
        }
        #endregion
    }
}

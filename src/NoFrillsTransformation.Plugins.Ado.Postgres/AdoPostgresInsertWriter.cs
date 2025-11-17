using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace NoFrillsTransformation.Plugins.Ado.Postgres
{
    class AdoPostgresInsertWriter : AdoPostgresWriterBase
    {
        public AdoPostgresInsertWriter(IContext context, string? config, string tableDef, IFieldDefinition[] fieldDefs)
            : base(context, config, tableDef, fieldDefs)
        {
        }

        private NpgsqlConnection? _psqlConnection;
        private NpgsqlCommand? _psqlCommand;
        private NpgsqlTransaction? _transaction;

        private bool _finished = false;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoPostgresInsertWriter initializing...");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(Config);
            var dataSource = dataSourceBuilder.Build();

            _psqlConnection = dataSource.OpenConnection();

            // Parse table definition to extract schema and table name
            var parts = Table.Split('.');
            string schema, tableName;
            if (parts.Length == 2)
            {
                schema = parts[0];
                tableName = parts[1];
            }
            else
            {
                schema = "public";
                tableName = Table;
            }

            RetrieveRemoteFields(_psqlConnection, schema, tableName);

            // Check all the target fields as well
            foreach (var fieldDef in FieldDefs)
            {
                if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                    throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + Table + "'.");
            }

            _psqlCommand = new NpgsqlCommand(GetInsertStatement(), _psqlConnection);
            _transaction = _psqlConnection.BeginTransaction();
            _psqlCommand.Transaction = _transaction;

            // Add the parameters to the command
            foreach (var fieldDef in FieldDefs)
            {
                var remoteField = RemoteFields[fieldDef.FieldName];
                if (remoteField.CharacterMaximumLength != null && remoteField.CharacterMaximumLength.Value != 0)
                {
                    _psqlCommand.Parameters.Add(new NpgsqlParameter(fieldDef.FieldName, GetSqlDbType(fieldDef), remoteField.CharacterMaximumLength.Value));
                }
                else
                {
                    _psqlCommand.Parameters.Add(new NpgsqlParameter(fieldDef.FieldName, GetSqlDbType(fieldDef)));
                }
            }
            if (_psqlCommand.Parameters.Count > 0)
            {
                _psqlCommand.Prepare();
            }

            Context.Logger.Info("AdoPostgresInsertWriter initialized.");
        }

        protected override string GetInsertStatement()
        {
            var sb = new StringBuilder();
            sb.Append("INSERT INTO ");
            sb.Append(Table);
            sb.Append(" (");
            bool first = true;
            foreach (var field in FieldDefs)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append(field.FieldName);
                first = false;
            }
            sb.Append(") VALUES (");
            first = true;
            foreach (var field in FieldDefs)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append("$");
                sb.Append(field.FieldName);
                first = false;
            }
            sb.Append(")");
            return sb.ToString();
        }

        protected override void BeginTransaction()
        {
            base.BeginTransaction();
        }

        protected override void EndTransaction()
        {
            base.EndTransaction();

            if (null != _transaction)
                _transaction.Commit();
            _finished = true;
        }

        protected override void Insert(string[] fieldValues)
        {
            if (null == _psqlCommand)
                throw new InvalidOperationException("Insert command not set.");
            for (int i = 0; i < FieldDefs.Length; ++i)
            {
                _psqlCommand.Parameters[$"${FieldDefs[i].FieldName}"].Value = GetFieldValue(FieldDefs[i], fieldValues[i]);
            }
            _psqlCommand.ExecuteNonQuery();
        }

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (null != _transaction)
                {
                    if (!_finished)
                    {
                        try
                        {
                            Context.Logger.Warning("AdoPostgresInsertWriter did not finish writing successfully; rolling back transaction!");
                            _transaction.Rollback();
                        }
                        catch (Exception e)
                        {
                            Context.Logger.Error("AdoPostgresInsertWriter: An exception occurred while rolling back the write transaction: " + e.Message);
                        }
                    }
                    _transaction.Dispose();
                    _transaction = null;
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

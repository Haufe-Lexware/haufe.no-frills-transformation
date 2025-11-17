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
    class AdoPostgresUpdateWriter : AdoPostgresWriterBase
    {
        public AdoPostgresUpdateWriter(IContext context, string? config, string tableDef, IFieldDefinition[] fieldDefs)
            : base(context, config, tableDef, fieldDefs)
        {
        }

        private string? _updateSchema;
        private string? _updateTable;
        private string[]? _updateWhereFields;

        private NpgsqlConnection? _psqlConnection;
        private NpgsqlCommand? _psqlCommand;
        private NpgsqlTransaction? _transaction;

        private bool _finished = false;

        // private OracleParameter[] _parameters;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoSqlServerUpdateWriter initializing...");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(Config);
            var dataSource = dataSourceBuilder.Build();

            _psqlConnection = dataSource.OpenConnection();

            var tempTableDef = Table;
            // Expected format: "schema.table_name(field1, field2)" whereas field1, field2 are the fields to use in the WHERE clause
            var parts = tempTableDef.Split('(');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid table definition: " + Table);
            var schemaTable = parts[0].Split('.');
            if (schemaTable.Length != 2)
                throw new ArgumentException("Invalid table definition: " + Table);
            _updateSchema = schemaTable[0];
            _updateTable = schemaTable[1];

            _updateWhereFields = parts[1].TrimEnd(')').Split(',');
            // Trim any whitespace from the whereFields
            for (int i = 0; i < _updateWhereFields.Length; ++i)
            {
                _updateWhereFields[i] = _updateWhereFields[i].Trim();
            }

            RetrieveRemoteFields(_psqlConnection, _updateSchema, _updateTable);
            // Check that all fields in the WHERE clause are present in the table
            foreach (var whereField in _updateWhereFields)
            {
                if (!RemoteFields.ContainsKey(whereField))
                    throw new ArgumentException("Field '" + whereField + "' in WHERE clause not found in table '" + _updateTable + "'.");
            }

            // Check all the target fields as well
            foreach (var fieldDef in FieldDefs)
            {
                if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                    throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + _updateTable + "'.");
            }

            _psqlCommand = new NpgsqlCommand(GetUpdateStatement(), _psqlConnection);
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

            Context.Logger.Info("AdoSqlServerUpdateWriter initialized.");
        }

        private string GetUpdateStatement()
        {
            if (null == _updateTable)
                throw new InvalidOperationException("Update table not set.");
            if (null == _updateSchema)
                throw new InvalidOperationException("Update schema not set.");
            if (null == _updateWhereFields)
                throw new InvalidOperationException("Update where fields not set.");
            var sb = new StringBuilder();
            sb.Append("update ");
            sb.Append(_updateSchema);
            sb.Append(".");
            sb.Append(_updateTable);
            sb.Append(" set ");
            bool first = true;
            foreach (var field in FieldDefs)
            {
                // Don't add the where fields
                if (_updateWhereFields.Contains(field.FieldName))
                    continue;
                if (!first)
                    sb.Append(", ");
                sb.Append(field.FieldName);
                sb.Append(" = @");
                sb.Append(field.FieldName);
                first = false;
            }
            sb.Append(" where ");
            first = true;
            foreach (var whereField in _updateWhereFields)
            {
                if (!first)
                    sb.Append(" and ");
                sb.Append(whereField);
                sb.Append(" = @");
                sb.Append(whereField);
                first = false;
            }
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
                throw new InvalidOperationException("Update command not set.");
            for (int i = 0; i < FieldDefs.Length; ++i)
            {
                _psqlCommand.Parameters[FieldDefs[i].FieldName].Value = GetFieldValue(FieldDefs[i], fieldValues[i]);
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
                            Context.Logger.Warning("AdoOracleWriter did not finish writing successfully; rolling back transaction!");
                            _transaction.Rollback();
                        }
                        catch (Exception e)
                        {
                            Context.Logger.Error("AdoOracleWriter: An exception occurred while rolling back the write transaction: " + e.Message);
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

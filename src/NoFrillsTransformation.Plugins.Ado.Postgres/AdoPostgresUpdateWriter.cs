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
    class AdoPostgresUpdateWriter : AdoWriter
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

            RetrieveRemoteFields();
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

        private class RemoteFieldDef
        {
            public string? FieldName { get; set; }
            public string? DataType { get; set; }
            public int? CharacterMaximumLength { get; set; }
            public bool IsNullable { get; set; }
        }

        private Dictionary<string, RemoteFieldDef> _remoteFields = new Dictionary<string, RemoteFieldDef>();
        private Dictionary<string, RemoteFieldDef> RemoteFields { get { return _remoteFields; } }

        private void RetrieveRemoteFields()
        {
            // Run a query to get the field names and types
            var sb = new StringBuilder();
            sb.Append("SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE ");
            sb.Append("FROM INFORMATION_SCHEMA.COLUMNS ");
            sb.Append("WHERE TABLE_NAME = '");
            sb.Append(_updateTable);
            sb.Append("' AND TABLE_SCHEMA = '");
            sb.Append(_updateSchema);
            sb.Append("';");
            var query = sb.ToString();

            using (var command = new NpgsqlCommand(query, _psqlConnection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        var characterMaximumLength = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        var isNullable = reader.GetString(3);
                        _remoteFields[columnName] = new RemoteFieldDef
                        {
                            FieldName = columnName,
                            DataType = dataType,
                            CharacterMaximumLength = characterMaximumLength,
                            IsNullable = (isNullable == "YES")
                        };
                    }
                    reader.Close();
                }
            }
        }

        private NpgsqlDbType GetSqlDbType(IFieldDefinition fieldDef)
        {
            if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + _updateTable + "'.");
            var remoteField = RemoteFields[fieldDef.FieldName];
            switch (remoteField.DataType)
            {
                case "integer":
                    return NpgsqlDbType.Integer;

                case "date":
                    return NpgsqlDbType.Date;

                case "timestamp":
                    return NpgsqlDbType.Timestamp;

                case "real":
                    return NpgsqlDbType.Real;

                case "numeric":
                case "decimal":
                    return NpgsqlDbType.Numeric;

                case "money":
                    return NpgsqlDbType.Money;

                case "varchar":
                case "character varying":
                    return NpgsqlDbType.Varchar;

                case "char":
                    return NpgsqlDbType.Char;

                case "text":
                    return NpgsqlDbType.Text;

                case "bit":
                    return NpgsqlDbType.Bit;

                default:
                    throw new ArgumentException("Unknown data type '" + remoteField.DataType + "' for field '" + fieldDef.FieldName + "'.");
            }
        }

        protected string GetUpdateStatement()
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

        private object GetFieldValue(IFieldDefinition fieldDef, string fieldValue)
        {
            // Cast according to the type of the remote field
            var remoteField = RemoteFields[fieldDef.FieldName];
            if (string.IsNullOrEmpty(fieldValue))
            {
                if (remoteField.IsNullable)
                    return DBNull.Value;
                else
                    return GetDefaultValue(fieldDef);
            }
            switch (remoteField.DataType)
            {
                case "integer":
                    return int.Parse(fieldValue);

                case "date":
                    return DateTime.Parse(fieldValue);

                case "timestamp":
                    return DateTime.Parse(fieldValue);

                case "real":
                    return float.Parse(fieldValue);

                case "decimal":
                case "double":
                    return decimal.Parse(fieldValue);

                case "money":
                    return decimal.Parse(fieldValue);

                case "character varying":
                    return fieldValue;

                case "varchar":
                    return fieldValue;

                case "char":
                    return fieldValue;

                case "nchar":
                    return fieldValue;

                case "text":
                    return fieldValue;

                case "ntext":
                    return fieldValue;

                case "bit":
                    return bool.Parse(fieldValue);

                default:
                    throw new ArgumentException("Unknown data type '" + remoteField.DataType + "' for field '" + fieldDef.FieldName + "'.");
            }
        }

        private object GetDefaultValue(IFieldDefinition fieldDef)
        {
            var remoteField = RemoteFields[fieldDef.FieldName];
            switch (remoteField.DataType)
            {
                case "int":
                    return 0;

                case "date":
                case "datetime":
                    return DateTime.MinValue;

                case "float":
                    return 0.0f;

                case "decimal":
                case "money":
                    return 0.0m;

                case "nvarchar":
                case "varchar":
                case "char":
                case "nchar":
                case "text":
                case "ntext":
                    return string.Empty;

                case "bit":
                    return false;

                default:
                    throw new ArgumentException("Unknown data type '" + remoteField.DataType + "' for field '" + fieldDef.FieldName + "'.");
            }
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

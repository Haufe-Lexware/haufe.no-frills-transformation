using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using System.Data;

namespace NoFrillsTransformation.Plugins.Ado.SqlServer
{
    class AdoSqlServerUpdateWriter : AdoWriter
    {
        public AdoSqlServerUpdateWriter(IContext context, string? config, string tableDef, IFieldDefinition[] fieldDefs)
            : base(context, config, tableDef, fieldDefs)
        {
        }

        private string? _updateTable;
        private string[]? _updateWhereFields;

        private SqlConnection? _sqlConnection;
        private SqlCommand? _sqlCommand;
        private SqlTransaction? _transaction;

        private bool _finished = false;

        // private OracleParameter[] _parameters;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoSqlServerUpdateWriter initializing...");

            _sqlConnection = new SqlConnection(Config);
            _sqlConnection.Open();

            var tempTableDef = Table;
            // Expected format: "table_name(field1, field2)" whereas field1, field2 are the fields to use in the WHERE clause
            var parts = tempTableDef.Split('(');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid table definition: " + Table);
            _updateTable = parts[0];
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

            _sqlCommand = new SqlCommand(GetUpdateStatement(), _sqlConnection);
            _transaction = _sqlConnection.BeginTransaction();
            _sqlCommand.Transaction = _transaction;

            // Add the parameters to the command
            foreach (var fieldDef in FieldDefs)
            {
                var remoteField = RemoteFields[fieldDef.FieldName];
                if (remoteField.CharacterMaximumLength != null && remoteField.CharacterMaximumLength.Value != 0)
                {
                    _sqlCommand.Parameters.Add(new SqlParameter("@" + fieldDef.FieldName, GetSqlDbType(fieldDef), remoteField.CharacterMaximumLength.Value));
                }
                else
                {
                    _sqlCommand.Parameters.Add(new SqlParameter("@" + fieldDef.FieldName, GetSqlDbType(fieldDef)));
                }
            }
            if (_sqlCommand.Parameters.Count > 0)
            {
                _sqlCommand.Prepare();
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
            sb.Append("';");
            var query = sb.ToString();

            using (var command = new SqlCommand(query, _sqlConnection))
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

        private SqlDbType GetSqlDbType(IFieldDefinition fieldDef)
        {
            if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + _updateTable + "'.");
            var remoteField = RemoteFields[fieldDef.FieldName];
            switch (remoteField.DataType)
            {
                case "int":
                    return SqlDbType.Int;

                case "date":
                    return SqlDbType.DateTime;

                case "datetime":
                    return SqlDbType.DateTime;

                case "float":
                    return SqlDbType.Float;

                case "decimal":
                    return SqlDbType.Decimal;

                case "money":
                    return SqlDbType.Money;

                case "nvarchar":
                    return SqlDbType.NVarChar;

                case "varchar":
                    return SqlDbType.VarChar;

                case "char":
                    return SqlDbType.Char;

                case "nchar":
                    return SqlDbType.NChar;

                case "text":
                    return SqlDbType.Text;

                case "ntext":
                    return SqlDbType.NText;

                case "bit":
                    return SqlDbType.Bit;

                default:
                    throw new ArgumentException("Unknown data type '" + remoteField.DataType + "' for field '" + fieldDef.FieldName + "'.");
            }
        }

        protected string GetUpdateStatement()
        {
            if (null == _updateTable)
                throw new InvalidOperationException("Update table not set.");
            if (null == _updateWhereFields)
                throw new InvalidOperationException("Update where fields not set.");
            var sb = new StringBuilder();
            sb.Append("update ");
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
            if (null == _sqlCommand)
                throw new InvalidOperationException("Update command not set.");
            for (int i = 0; i < FieldDefs.Length; ++i)
            {
                _sqlCommand.Parameters[$"@{FieldDefs[i].FieldName}"].Value = GetFieldValue(FieldDefs[i], fieldValues[i]);
            }
            _sqlCommand.ExecuteNonQuery();
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
                case "int":
                    return int.Parse(fieldValue);

                case "date":
                    return DateTime.Parse(fieldValue);

                case "datetime":
                    return DateTime.Parse(fieldValue);

                case "float":
                    return float.Parse(fieldValue);

                case "decimal":
                    return decimal.Parse(fieldValue);

                case "money":
                    return decimal.Parse(fieldValue);

                case "nvarchar":
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

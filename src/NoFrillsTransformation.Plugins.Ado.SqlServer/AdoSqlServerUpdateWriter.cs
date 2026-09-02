using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using System.Data;

namespace NoFrillsTransformation.Plugins.Ado.SqlServer
{
    class AdoSqlServerUpdateWriter : AdoSqlServerWriterBase
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

            _updateTable = RetrieveRemoteFields(_sqlConnection, _updateTable);
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
                GetSqlDbType(fieldDef);
            }

            _sqlCommand = new SqlCommand(GetUpdateStatement(), _sqlConnection);
            _transaction = _sqlConnection.BeginTransaction();
            _sqlCommand.Transaction = _transaction;

            // Add the parameters to the command
            foreach (var fieldDef in FieldDefs)
            {
                _sqlCommand.Parameters.Add(CreateParameter(fieldDef));
            }
            if (_sqlCommand.Parameters.Count > 0)
            {
                _sqlCommand.Prepare();
            }

            Context.Logger.Info("AdoSqlServerUpdateWriter initialized.");
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

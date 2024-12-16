using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using System.Data;

namespace NoFrillsTransformation.Plugins.Ado.SqlServer
{
    class AdoSqlServerInsertWriter : AdoSqlServerWriterBase
    {
        public AdoSqlServerInsertWriter(IContext context, string? config, string tableDef, IFieldDefinition[] fieldDefs)
            : base(context, config, tableDef, fieldDefs)
        {
        }

        private SqlConnection? _sqlConnection;
        private SqlCommand? _sqlCommand;
        private SqlTransaction? _transaction;

        private bool _finished = false;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoSqlServerUpdateWriter initializing...");

            _sqlConnection = new SqlConnection(Config);
            _sqlConnection.Open();

            RetrieveRemoteFields(_sqlConnection);

            // Check all the target fields as well
            foreach (var fieldDef in FieldDefs)
            {
                if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                    throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + Table + "'.");
            }

            _sqlCommand = new SqlCommand(GetInsertStatement(), _sqlConnection);
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

            Context.Logger.Info("AdoSqlServerInsertWriter initialized.");
        }

        protected override string GetInsertStatement()
        {
            var sb = new StringBuilder();
            sb.Append("insert into ");
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
            sb.Append(") values (");
            first = true;
            foreach (var field in FieldDefs)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append("@");
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

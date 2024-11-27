using System;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Ado.Oracle
{
    class AdoOracleWriter : AdoWriter
    {
        public AdoOracleWriter(IContext context, string config, string table, IFieldDefinition[] fieldDefs)
            : base(context, config, table, fieldDefs)
        {
        }

        private OracleConnection _sqlConnection;
        private OracleCommand _sqlCommand;
        private OracleTransaction _transaction;
        private bool _finished = false;

        private OracleParameter[] _parameters;

        protected override void Initialize()
        {
            Context.Logger.Info("AdoOracleWriter initializing...");
            _sqlConnection = new OracleConnection(Config);
            _sqlConnection.Open();

            _transaction = _sqlConnection.BeginTransaction();
            
            _sqlCommand = new OracleCommand(null, _sqlConnection);
            _sqlCommand.Transaction = _transaction;
            _sqlCommand.CommandText = GetInsertStatement();
            _sqlCommand.Prepare();

            _parameters = new OracleParameter[FieldDefs.Length];
            for (int i = 0; i < FieldDefs.Length; ++i)
            {
                _parameters[i] = new OracleParameter(FieldDefs[i].FieldName, GetOracleType(Context, FieldDefs[i].Config));
            }
            _sqlCommand.Parameters.AddRange(_parameters);

            Context.Logger.Info("AdoOracleWriter initialized.");
        }

        private static OracleType GetOracleType(IContext context, string cfg)
        {
            cfg = cfg ?? "";
            switch (cfg.ToLowerInvariant())
            {
                case "int": return OracleType.Int32;
                
                case "date": return OracleType.DateTime;
                
                case "double": return OracleType.Double;

                case "string":
                case "":
                    return OracleType.VarChar;

                default:
                    context.Logger.Warning("AdoOracleWriter: Unknown field type '" + cfg + "', setting VARCHAR.");
                    return OracleType.VarChar;
            }
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
                    sb.Append(",");
                sb.Append(":");
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

            _transaction.Commit();
            _finished = true;
        }

        protected override void Insert(string[] fieldValues)
        {
            for (int i = 0; i < FieldDefs.Length; ++i)
            {
                _parameters[i].Value = GetFieldValue(fieldValues[i], FieldDefs[i]);
            }
            _sqlCommand.ExecuteNonQuery();
        }

        private object GetFieldValue(string p, IFieldDefinition fieldDef)
        {
            if (null == fieldDef.Config
                || !fieldDef.Config.Equals("date"))
                return p;
            try
            {
                if (string.IsNullOrEmpty(p))
                    return DateTime.MinValue;
                return DateTime.Parse(p);
            }
            catch (Exception)
            {
                Context.Logger.Warning("Invalid DateTime passed into field " + fieldDef.FieldName + ": '" + p + "'.");
                throw;
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

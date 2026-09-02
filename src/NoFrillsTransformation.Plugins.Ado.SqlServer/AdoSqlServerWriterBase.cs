using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using NoFrillsTransformation.Interfaces;
using System.Data;
using System.Globalization;

namespace NoFrillsTransformation.Plugins.Ado.SqlServer
{
    abstract class AdoSqlServerWriterBase : AdoWriter
    {
        public AdoSqlServerWriterBase(IContext context, string? config, string tableDef, IFieldDefinition[] fieldDefs)
            : base(context, config, tableDef, fieldDefs)
        {
        }

        private Dictionary<string, SqlServerColumnDefinition> _remoteFields = new Dictionary<string, SqlServerColumnDefinition>();
        protected Dictionary<string, SqlServerColumnDefinition> RemoteFields { get { return _remoteFields; } }

        protected string RetrieveRemoteFields(SqlConnection sqlConnection, string? tableName = null)
        {
            var target = SqlServerTypeSupport.ParseTableName(tableName ?? Table);
            const string query = "WITH ResolvedTable AS (" +
                "SELECT TOP (1) TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES " +
                "WHERE TABLE_NAME = @table AND ((@schema IS NOT NULL AND TABLE_SCHEMA = @schema) " +
                "OR (@schema IS NULL AND TABLE_SCHEMA IN (SCHEMA_NAME(), 'dbo'))) " +
                "ORDER BY CASE WHEN @schema IS NOT NULL OR TABLE_SCHEMA = SCHEMA_NAME() THEN 0 ELSE 1 END) " +
                "SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.IS_NULLABLE, c.NUMERIC_PRECISION, c.NUMERIC_SCALE, c.TABLE_SCHEMA " +
                "FROM INFORMATION_SCHEMA.COLUMNS c INNER JOIN ResolvedTable t " +
                "ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME;";

            using (var command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@schema", SqlDbType.NVarChar, 128).Value = (object?)target.Schema ?? DBNull.Value;
                command.Parameters.Add("@table", SqlDbType.NVarChar, 128).Value = target.Table;
                using (var reader = command.ExecuteReader())
                {
                    _remoteFields.Clear();
                    string? resolvedSchema = null;
                    while (reader.Read())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        var characterMaximumLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                        var isNullable = reader.GetString(3);
                        var numericPrecision = reader.IsDBNull(4) ? (byte?)null : Convert.ToByte(reader.GetValue(4), CultureInfo.InvariantCulture);
                        var numericScale = reader.IsDBNull(5) ? (byte?)null : Convert.ToByte(reader.GetValue(5), CultureInfo.InvariantCulture);
                        resolvedSchema = reader.GetString(6);
                        _remoteFields[columnName] = new SqlServerColumnDefinition
                        {
                            FieldName = columnName,
                            DataType = dataType,
                            CharacterMaximumLength = characterMaximumLength,
                            NumericPrecision = numericPrecision,
                            NumericScale = numericScale,
                            IsNullable = (isNullable == "YES")
                        };
                    }
                    reader.Close();

                    if (resolvedSchema == null)
                        throw new ArgumentException("Table '" + (tableName ?? Table) + "' not found.");

                    return SqlServerTypeSupport.FormatTableName(resolvedSchema, target.Table);
                }
            }
        }

        protected SqlDbType GetSqlDbType(IFieldDefinition fieldDef)
        {
            if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + Table + "'.");
            return SqlServerTypeSupport.GetSqlDbType(RemoteFields[fieldDef.FieldName]);
        }

        protected object GetFieldValue(IFieldDefinition fieldDef, string fieldValue)
        {
            if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + Table + "'.");
            return SqlServerTypeSupport.ConvertValue(RemoteFields[fieldDef.FieldName], fieldValue);
        }

        protected SqlParameter CreateParameter(IFieldDefinition fieldDef)
        {
            if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + Table + "'.");
            return SqlServerTypeSupport.CreateParameter(RemoteFields[fieldDef.FieldName]);
        }
    }
}

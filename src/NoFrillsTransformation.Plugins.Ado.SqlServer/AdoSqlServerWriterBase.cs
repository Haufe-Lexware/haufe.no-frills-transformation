using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using System.Data;

namespace NoFrillsTransformation.Plugins.Ado.SqlServer
{
    abstract class AdoSqlServerWriterBase : AdoWriter
    {
        public AdoSqlServerWriterBase(IContext context, string? config, string tableDef, IFieldDefinition[] fieldDefs)
            : base(context, config, tableDef, fieldDefs)
        {
        }

        protected class RemoteFieldDef
        {
            public string? FieldName { get; set; }
            public string? DataType { get; set; }
            public int? CharacterMaximumLength { get; set; }
            public bool IsNullable { get; set; }
        }

        private Dictionary<string, RemoteFieldDef> _remoteFields = new Dictionary<string, RemoteFieldDef>();
        protected Dictionary<string, RemoteFieldDef> RemoteFields { get { return _remoteFields; } }

        protected void RetrieveRemoteFields(SqlConnection sqlConnection)
        {
            // Run a query to get the field names and types
            var sb = new StringBuilder();
            sb.Append("SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE ");
            sb.Append("FROM INFORMATION_SCHEMA.COLUMNS ");
            sb.Append("WHERE TABLE_NAME = '");
            sb.Append(Table);
            sb.Append("';");
            var query = sb.ToString();

            using (var command = new SqlCommand(query, sqlConnection))
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

        protected SqlDbType GetSqlDbType(IFieldDefinition fieldDef)
        {
            if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + Table + "'.");
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

        protected object GetFieldValue(IFieldDefinition fieldDef, string fieldValue)
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
    }
}

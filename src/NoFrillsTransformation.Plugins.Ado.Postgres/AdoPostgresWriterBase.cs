using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace NoFrillsTransformation.Plugins.Ado.Postgres
{
    abstract class AdoPostgresWriterBase : AdoWriter
    {
        public AdoPostgresWriterBase(IContext context, string? config, string tableDef, IFieldDefinition[] fieldDefs)
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

        protected void RetrieveRemoteFields(NpgsqlConnection psqlConnection, string schema, string tableName)
        {
            // Run a query to get the field names and types
            var sb = new StringBuilder();
            sb.Append("SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE ");
            sb.Append("FROM INFORMATION_SCHEMA.COLUMNS ");
            sb.Append("WHERE TABLE_NAME = '");
            sb.Append(tableName);
            sb.Append("' AND TABLE_SCHEMA = '");
            sb.Append(schema);
            sb.Append("';");
            var query = sb.ToString();

            using (var command = new NpgsqlCommand(query, psqlConnection))
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

        protected NpgsqlDbType GetSqlDbType(IFieldDefinition fieldDef)
        {
            if (!RemoteFields.ContainsKey(fieldDef.FieldName))
                throw new ArgumentException("Field '" + fieldDef.FieldName + "' not found in table '" + Table + "'.");
            var remoteField = RemoteFields[fieldDef.FieldName];
            switch (remoteField.DataType)
            {
                case "integer":
                case "int":
                case "int4":
                    return NpgsqlDbType.Integer;

                case "bigint":
                case "int8":
                    return NpgsqlDbType.Bigint;

                case "smallint":
                case "int2":
                    return NpgsqlDbType.Smallint;

                case "boolean":
                case "bool":
                    return NpgsqlDbType.Boolean;

                case "character varying":
                case "varchar":
                    return NpgsqlDbType.Varchar;

                case "character":
                case "char":
                    return NpgsqlDbType.Char;

                case "text":
                    return NpgsqlDbType.Text;

                case "timestamp without time zone":
                case "timestamp":
                    return NpgsqlDbType.Timestamp;

                case "timestamp with time zone":
                case "timestamptz":
                    return NpgsqlDbType.TimestampTz;

                case "date":
                    return NpgsqlDbType.Date;

                case "time without time zone":
                case "time":
                    return NpgsqlDbType.Time;

                case "numeric":
                case "decimal":
                    return NpgsqlDbType.Numeric;

                case "real":
                case "float4":
                    return NpgsqlDbType.Real;

                case "double precision":
                case "float8":
                    return NpgsqlDbType.Double;

                case "money":
                    return NpgsqlDbType.Money;

                case "uuid":
                    return NpgsqlDbType.Uuid;

                case "json":
                    return NpgsqlDbType.Json;

                case "jsonb":
                    return NpgsqlDbType.Jsonb;

                case "bytea":
                    return NpgsqlDbType.Bytea;

                case "bit":
                    return NpgsqlDbType.Bit;

                default:
                    throw new ArgumentException("Unsupported PostgreSQL data type: " + remoteField.DataType);
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
                case "integer":
                case "int":
                case "int4":
                case "smallint":
                case "int2":
                    return int.Parse(fieldValue);

                case "bigint":
                case "int8":
                    return long.Parse(fieldValue);

                case "date":
                case "timestamp":
                case "timestamp without time zone":
                case "timestamp with time zone":
                case "timestamptz":
                    return DateTime.Parse(fieldValue);

                case "real":
                case "float4":
                    return float.Parse(fieldValue);

                case "double precision":
                case "float8":
                case "double":
                    return double.Parse(fieldValue);

                case "numeric":
                case "decimal":
                case "money":
                    return decimal.Parse(fieldValue);

                case "character varying":
                case "varchar":
                case "character":
                case "char":
                case "text":
                    return fieldValue;

                case "boolean":
                case "bool":
                case "bit":
                    return bool.Parse(fieldValue);

                case "uuid":
                    return Guid.Parse(fieldValue);

                case "json":
                case "jsonb":
                    return fieldValue;

                case "bytea":
                    return Convert.FromBase64String(fieldValue);

                default:
                    throw new ArgumentException("Unknown data type '" + remoteField.DataType + "' for field '" + fieldDef.FieldName + "'.");
            }
        }

        private object GetDefaultValue(IFieldDefinition fieldDef)
        {
            var remoteField = RemoteFields[fieldDef.FieldName];
            switch (remoteField.DataType)
            {
                case "integer":
                case "int":
                case "int4":
                case "smallint":
                case "int2":
                    return 0;

                case "bigint":
                case "int8":
                    return 0L;

                case "date":
                case "timestamp":
                case "timestamp without time zone":
                case "timestamp with time zone":
                case "timestamptz":
                    return DateTime.MinValue;

                case "real":
                case "float4":
                    return 0.0f;

                case "double precision":
                case "float8":
                case "double":
                    return 0.0;

                case "numeric":
                case "decimal":
                case "money":
                    return 0.0m;

                case "character varying":
                case "varchar":
                case "character":
                case "char":
                case "text":
                    return string.Empty;

                case "boolean":
                case "bool":
                case "bit":
                    return false;

                case "uuid":
                    return Guid.Empty;

                case "json":
                case "jsonb":
                    return "{}";

                case "bytea":
                    return Array.Empty<byte>();

                default:
                    throw new ArgumentException("Unknown data type '" + remoteField.DataType + "' for field '" + fieldDef.FieldName + "'.");
            }
        }
    }
}

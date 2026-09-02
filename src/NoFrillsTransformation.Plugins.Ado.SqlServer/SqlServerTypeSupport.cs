using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace NoFrillsTransformation.Plugins.Ado.SqlServer
{
    internal sealed class SqlServerColumnDefinition
    {
        public string FieldName { get; init; } = string.Empty;
        public string DataType { get; init; } = string.Empty;
        public int? CharacterMaximumLength { get; init; }
        public byte? NumericPrecision { get; init; }
        public byte? NumericScale { get; init; }
        public bool IsNullable { get; init; }
    }

    internal static class SqlServerTypeSupport
    {
        private const NumberStyles DecimalStyles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        private const NumberStyles FloatingPointStyles = NumberStyles.Float;
        private const DateTimeStyles TemporalStyles = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind;

        internal static (string? Schema, string Table) ParseTableName(string tableName)
        {
            var parts = tableName.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                1 => (null, UnquoteIdentifier(parts[0])),
                2 => (UnquoteIdentifier(parts[0]), UnquoteIdentifier(parts[1])),
                _ => throw new ArgumentException($"Invalid SQL Server table name '{tableName}'. Use 'TableName' or 'SchemaName.TableName'.")
            };
        }

        internal static string FormatTableName(string schema, string table)
        {
            return $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";
        }

        internal static SqlDbType GetSqlDbType(SqlServerColumnDefinition column)
        {
            return column.DataType switch
            {
                "bigint" => SqlDbType.BigInt,
                "binary" => SqlDbType.Binary,
                "bit" => SqlDbType.Bit,
                "char" => SqlDbType.Char,
                "date" => SqlDbType.Date,
                "datetime" => SqlDbType.DateTime,
                "datetime2" => SqlDbType.DateTime2,
                "datetimeoffset" => SqlDbType.DateTimeOffset,
                "decimal" => SqlDbType.Decimal,
                "float" => SqlDbType.Float,
                "image" => SqlDbType.Image,
                "int" => SqlDbType.Int,
                "money" => SqlDbType.Money,
                "nchar" => SqlDbType.NChar,
                "ntext" => SqlDbType.NText,
                "numeric" => SqlDbType.Decimal,
                "nvarchar" => SqlDbType.NVarChar,
                "real" => SqlDbType.Real,
                "smalldatetime" => SqlDbType.SmallDateTime,
                "smallint" => SqlDbType.SmallInt,
                "smallmoney" => SqlDbType.SmallMoney,
                "text" => SqlDbType.Text,
                "time" => SqlDbType.Time,
                "tinyint" => SqlDbType.TinyInt,
                "uniqueidentifier" => SqlDbType.UniqueIdentifier,
                "varbinary" => SqlDbType.VarBinary,
                "varchar" => SqlDbType.VarChar,
                "xml" => SqlDbType.Xml,
                "timestamp" or "rowversion" => throw CreateGeneratedColumnException(column),
                _ => throw new ArgumentException($"Unknown data type '{column.DataType}' for field '{column.FieldName}'.")
            };
        }

        internal static object ConvertValue(SqlServerColumnDefinition column, string fieldValue)
        {
            if (string.IsNullOrEmpty(fieldValue))
            {
                return column.IsNullable ? DBNull.Value : GetDefaultValue(column);
            }

            return column.DataType switch
            {
                "bigint" => long.Parse(fieldValue, NumberStyles.Integer, CultureInfo.InvariantCulture),
                "binary" or "image" or "varbinary" => ParseBinary(column, fieldValue),
                "bit" => bool.Parse(fieldValue),
                "char" or "nchar" or "ntext" or "nvarchar" or "text" or "varchar" or "xml" => fieldValue,
                "date" or "datetime" or "datetime2" or "smalldatetime" => DateTime.Parse(fieldValue, CultureInfo.InvariantCulture, TemporalStyles),
                "datetimeoffset" => DateTimeOffset.Parse(fieldValue, CultureInfo.InvariantCulture, TemporalStyles),
                "decimal" or "money" or "numeric" or "smallmoney" => decimal.Parse(fieldValue, DecimalStyles, CultureInfo.InvariantCulture),
                "float" => double.Parse(fieldValue, FloatingPointStyles, CultureInfo.InvariantCulture),
                "int" => int.Parse(fieldValue, NumberStyles.Integer, CultureInfo.InvariantCulture),
                "real" => float.Parse(fieldValue, FloatingPointStyles, CultureInfo.InvariantCulture),
                "smallint" => short.Parse(fieldValue, NumberStyles.Integer, CultureInfo.InvariantCulture),
                "time" => TimeSpan.Parse(fieldValue, CultureInfo.InvariantCulture),
                "tinyint" => byte.Parse(fieldValue, NumberStyles.Integer, CultureInfo.InvariantCulture),
                "uniqueidentifier" => Guid.Parse(fieldValue),
                "timestamp" or "rowversion" => throw CreateGeneratedColumnException(column),
                _ => throw new ArgumentException($"Unknown data type '{column.DataType}' for field '{column.FieldName}'.")
            };
        }

        internal static SqlParameter CreateParameter(SqlServerColumnDefinition column)
        {
            var sqlDbType = GetSqlDbType(column);
            var parameter = new SqlParameter("@" + column.FieldName, sqlDbType);

            if (HasLength(sqlDbType) && column.CharacterMaximumLength is -1 or > 0)
            {
                parameter.Size = column.CharacterMaximumLength.Value;
            }

            if (sqlDbType == SqlDbType.Decimal)
            {
                if (column.NumericPrecision.HasValue)
                    parameter.Precision = column.NumericPrecision.Value;
                if (column.NumericScale.HasValue)
                    parameter.Scale = column.NumericScale.Value;
            }

            return parameter;
        }

        private static object GetDefaultValue(SqlServerColumnDefinition column)
        {
            return column.DataType switch
            {
                "bigint" => 0L,
                "binary" or "image" or "varbinary" => Array.Empty<byte>(),
                "bit" => false,
                "char" or "nchar" or "ntext" or "nvarchar" or "text" or "varchar" or "xml" => string.Empty,
                "date" or "datetime" or "datetime2" or "smalldatetime" => DateTime.MinValue,
                "datetimeoffset" => DateTimeOffset.MinValue,
                "decimal" or "money" or "numeric" or "smallmoney" => 0.0m,
                "float" => 0.0d,
                "int" => 0,
                "real" => 0.0f,
                "smallint" => (short)0,
                "time" => TimeSpan.Zero,
                "tinyint" => (byte)0,
                "uniqueidentifier" => Guid.Empty,
                "timestamp" or "rowversion" => throw CreateGeneratedColumnException(column),
                _ => throw new ArgumentException($"Unknown data type '{column.DataType}' for field '{column.FieldName}'.")
            };
        }

        private static byte[] ParseBinary(SqlServerColumnDefinition column, string fieldValue)
        {
            var hexadecimal = fieldValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? fieldValue.Substring(2)
                : fieldValue;

            if (hexadecimal.Length % 2 != 0)
                throw new ArgumentException($"Invalid hexadecimal value for binary field '{column.FieldName}': the number of hexadecimal characters must be even.");

            try
            {
                return Convert.FromHexString(hexadecimal);
            }
            catch (FormatException exception)
            {
                throw new ArgumentException($"Invalid hexadecimal value for binary field '{column.FieldName}'.", exception);
            }
        }

        private static bool HasLength(SqlDbType sqlDbType)
        {
            return sqlDbType is SqlDbType.Binary
                or SqlDbType.Char
                or SqlDbType.Image
                or SqlDbType.NChar
                or SqlDbType.NText
                or SqlDbType.NVarChar
                or SqlDbType.Text
                or SqlDbType.VarBinary
                or SqlDbType.VarChar;
        }

        private static string UnquoteIdentifier(string identifier)
        {
            if (identifier.Length >= 2 && identifier[0] == '[' && identifier[^1] == ']')
                return identifier.Substring(1, identifier.Length - 2).Replace("]]", "]");

            return identifier;
        }

        private static string QuoteIdentifier(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        private static ArgumentException CreateGeneratedColumnException(SqlServerColumnDefinition column)
        {
            return new ArgumentException($"Field '{column.FieldName}' has SQL Server type '{column.DataType}', which is generated by SQL Server and must be omitted from the transformation.");
        }
    }
}
using System.Data;
using System.Globalization;
using Xunit;

namespace NoFrillsTransformation.Plugins.Ado.SqlServer.Tests
{
    public class SqlServerTypeSupportTests
    {
        [Theory]
        [InlineData("uniqueidentifier", SqlDbType.UniqueIdentifier)]
        [InlineData("varbinary", SqlDbType.VarBinary)]
        [InlineData("binary", SqlDbType.Binary)]
        [InlineData("image", SqlDbType.Image)]
        [InlineData("smallint", SqlDbType.SmallInt)]
        [InlineData("bigint", SqlDbType.BigInt)]
        [InlineData("tinyint", SqlDbType.TinyInt)]
        [InlineData("float", SqlDbType.Float)]
        [InlineData("real", SqlDbType.Real)]
        [InlineData("numeric", SqlDbType.Decimal)]
        [InlineData("date", SqlDbType.Date)]
        [InlineData("datetime2", SqlDbType.DateTime2)]
        [InlineData("smalldatetime", SqlDbType.SmallDateTime)]
        [InlineData("datetimeoffset", SqlDbType.DateTimeOffset)]
        [InlineData("time", SqlDbType.Time)]
        public void GetSqlDbType_MapsSupportedTypes(string dataType, SqlDbType expected)
        {
            Assert.Equal(expected, SqlServerTypeSupport.GetSqlDbType(Column(dataType)));
        }

        [Fact]
        public void ConvertValue_ConvertsGuid()
        {
            var expected = Guid.Parse("718d07d7-4405-453b-a28a-3706f51bfa68");

            var result = SqlServerTypeSupport.ConvertValue(Column("uniqueidentifier"), expected.ToString());

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("smallint", "-123", typeof(short), (short)-123)]
        [InlineData("bigint", "922337203685477580", typeof(long), 922337203685477580L)]
        [InlineData("tinyint", "255", typeof(byte), (byte)255)]
        public void ConvertValue_ConvertsIntegerTypes(string dataType, string value, Type expectedType, object expected)
        {
            var result = SqlServerTypeSupport.ConvertValue(Column(dataType), value);

            Assert.IsType(expectedType, result);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("decimal")]
        [InlineData("numeric")]
        public void ConvertValue_ParsesDecimalWithInvariantCulture(string dataType)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

                var result = SqlServerTypeSupport.ConvertValue(Column(dataType), "1234.56");

                Assert.Equal(1234.56m, result);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Theory]
        [InlineData("decimal", "1234,56")]
        [InlineData("numeric", "1,234.56")]
        [InlineData("float", "1234,56")]
        [InlineData("real", "1,234.56")]
        public void ConvertValue_RejectsAmbiguousNumericSeparators(string dataType, string value)
        {
            Assert.Throws<FormatException>(() => SqlServerTypeSupport.ConvertValue(Column(dataType), value));
        }

        [Fact]
        public void ConvertValue_UsesExpectedFloatingPointPrecision()
        {
            var floatResult = SqlServerTypeSupport.ConvertValue(Column("float"), "123.456789012345");
            var realResult = SqlServerTypeSupport.ConvertValue(Column("real"), "123.4567");

            Assert.IsType<double>(floatResult);
            Assert.Equal(123.456789012345d, floatResult);
            Assert.IsType<float>(realResult);
            Assert.Equal(123.4567f, realResult);
        }

        [Fact]
        public void ConvertValue_ConvertsTemporalTypes()
        {
            Assert.Equal(
                new DateTime(2025, 4, 3),
                SqlServerTypeSupport.ConvertValue(Column("date"), "2025-04-03"));
            Assert.Equal(
                new DateTime(2025, 4, 3, 14, 15, 16, 123),
                SqlServerTypeSupport.ConvertValue(Column("datetime2"), "2025-04-03T14:15:16.123"));
            Assert.Equal(
                new DateTimeOffset(2025, 4, 3, 14, 15, 16, TimeSpan.FromHours(2)),
                SqlServerTypeSupport.ConvertValue(Column("datetimeoffset"), "2025-04-03T14:15:16+02:00"));
            Assert.Equal(
                new TimeSpan(1, 2, 3, 4, 567),
                SqlServerTypeSupport.ConvertValue(Column("time"), "1.02:03:04.567"));
        }

        [Theory]
        [InlineData("0x0123ABcd")]
        [InlineData("0123ABcd")]
        public void ConvertValue_ConvertsHexadecimalBinaryWithoutChangingBytes(string value)
        {
            var result = SqlServerTypeSupport.ConvertValue(Column("varbinary", "Payload"), value);

            Assert.Equal(new byte[] { 0x01, 0x23, 0xAB, 0xCD }, result);
        }

        [Fact]
        public void ConvertValue_ReturnsDbNullForEmptyNullableBinary()
        {
            var result = SqlServerTypeSupport.ConvertValue(Column("varbinary", isNullable: true), string.Empty);

            Assert.Equal(DBNull.Value, result);
        }

        [Theory]
        [InlineData("0x123", "even")]
        [InlineData("0x12XZ", "Invalid hexadecimal")]
        public void ConvertValue_RejectsInvalidBinaryWithFieldName(string value, string expectedMessage)
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                SqlServerTypeSupport.ConvertValue(Column("varbinary", "BinaryContents"), value));

            Assert.Contains("BinaryContents", exception.Message);
            Assert.Contains(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData("timestamp")]
        [InlineData("rowversion")]
        public void GetSqlDbType_RejectsGeneratedRowVersionColumns(string dataType)
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                SqlServerTypeSupport.GetSqlDbType(Column(dataType, "Version")));

            Assert.Contains("Version", exception.Message);
            Assert.Contains("must be omitted", exception.Message);
        }

        [Fact]
        public void ConvertValue_PreservesExistingConversions()
        {
            Assert.Equal(42, SqlServerTypeSupport.ConvertValue(Column("int"), "42"));
            Assert.Equal("hello", SqlServerTypeSupport.ConvertValue(Column("nvarchar"), "hello"));
            Assert.Equal(true, SqlServerTypeSupport.ConvertValue(Column("bit"), "true"));
            Assert.Equal(
                new DateTime(2025, 4, 3, 14, 15, 16),
                SqlServerTypeSupport.ConvertValue(Column("datetime"), "2025-04-03T14:15:16"));
        }

        [Fact]
        public void CreateParameter_SetsDecimalPrecisionAndScale()
        {
            var parameter = SqlServerTypeSupport.CreateParameter(new SqlServerColumnDefinition
            {
                FieldName = "Amount",
                DataType = "numeric",
                NumericPrecision = 28,
                NumericScale = 8
            });

            Assert.Equal(SqlDbType.Decimal, parameter.SqlDbType);
            Assert.Equal((byte)28, parameter.Precision);
            Assert.Equal((byte)8, parameter.Scale);
        }

        [Theory]
        [InlineData("nvarchar", -1)]
        [InlineData("varbinary", -1)]
        [InlineData("varchar", 120)]
        [InlineData("binary", 32)]
        public void CreateParameter_PreservesLengthMetadata(string dataType, int length)
        {
            var parameter = SqlServerTypeSupport.CreateParameter(new SqlServerColumnDefinition
            {
                FieldName = "Value",
                DataType = dataType,
                CharacterMaximumLength = length
            });

            Assert.Equal(length, parameter.Size);
        }

        [Theory]
        [InlineData("WikiPage", null, "WikiPage")]
        [InlineData("wiki.WikiPage", "wiki", "WikiPage")]
        [InlineData("[wiki].[WikiPage]", "wiki", "WikiPage")]
        public void ParseTableName_ResolvesSchemaAndTable(string value, string? expectedSchema, string expectedTable)
        {
            var result = SqlServerTypeSupport.ParseTableName(value);

            Assert.Equal(expectedSchema, result.Schema);
            Assert.Equal(expectedTable, result.Table);
        }

        [Fact]
        public void ParseTableName_RejectsThreePartNames()
        {
            var exception = Assert.Throws<ArgumentException>(() => SqlServerTypeSupport.ParseTableName("database.dbo.WikiPage"));

            Assert.Contains("SchemaName.TableName", exception.Message);
        }

        [Fact]
        public void FormatTableName_QuotesResolvedSchemaAndTable()
        {
            Assert.Equal("[tenant].[WikiPage]", SqlServerTypeSupport.FormatTableName("tenant", "WikiPage"));
            Assert.Equal("[strange]]schema].[Wiki.Page]", SqlServerTypeSupport.FormatTableName("strange]schema", "Wiki.Page"));
        }

        private static SqlServerColumnDefinition Column(string dataType, string fieldName = "Field", bool isNullable = false)
        {
            return new SqlServerColumnDefinition
            {
                FieldName = fieldName,
                DataType = dataType,
                IsNullable = isNullable
            };
        }
    }
}
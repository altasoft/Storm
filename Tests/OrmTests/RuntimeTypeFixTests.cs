using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.TestModels;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Regression tests for runtime type conversion, DDL, scalar helper and transaction scope fixes.
/// </summary>
public sealed class RuntimeTypeFixTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly string _connectionString;

    public RuntimeTypeFixTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        StormManager.SetLogger(new XunitLogger<DatabaseFixture>(output));
        _connectionString = fixture.ConnectionString;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecAsync(connection,
            """
            IF OBJECT_ID('[dbo].[SqlWhereTestEntity]') IS NOT NULL DROP TABLE [dbo].[SqlWhereTestEntity];
            IF OBJECT_ID('[dbo].[storm_fix_identity]') IS NOT NULL DROP TABLE [dbo].[storm_fix_identity];
            IF OBJECT_ID('[dbo].[storm_fix_seq]', 'SO') IS NOT NULL DROP SEQUENCE [dbo].[storm_fix_seq];
            """);
    }

    #region #1 TimeOnly / TimeSpan

    [Fact]
    public async Task TimeOnly_RoundTrips_ThroughCreateTableInsertAndSelect()
    {
        await using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await ExecAsync(connection, "IF OBJECT_ID('[dbo].[SqlWhereTestEntity]') IS NOT NULL DROP TABLE [dbo].[SqlWhereTestEntity];");
            await connection.CreateTableAsync<SqlWhereTestEntity>(checkNotExists: true);
        }

        // Fractional seconds (100ns resolution) also verify that DDL emits time(7), not time(0).
        var time = new TimeOnly(12, 34, 56).Add(TimeSpan.FromTicks(1234567));
        var entity = new SqlWhereTestEntity
        {
            CustomerId = 1,
            Ccy = "USD",
            DateValue = new DateOnly(2024, 1, 2),
            TimeValue = time,
            TimeValueN = time.AddHours(1)
        };
        var entityWithNull = new SqlWhereTestEntity
        {
            CustomerId = 2,
            Ccy = "USD",
            DateValue = new DateOnly(2024, 1, 2),
            TimeValue = TimeOnly.MinValue,
            TimeValueN = null
        };

        await using var context = new TestStormContext(_connectionString);
        await context.InsertIntoSqlWhereTestEntity().Values([entity, entityWithNull]).GoAsync();

        var rows = await context.SelectFromSqlWhereTestEntity().ListAsync();
        var loaded = Assert.Single(rows, x => x.CustomerId == 1);
        var loadedNull = Assert.Single(rows, x => x.CustomerId == 2);

        Assert.Equal(time, loaded.TimeValue);
        Assert.Equal(time.AddHours(1), loaded.TimeValueN);
        Assert.Equal(TimeOnly.MinValue, loadedNull.TimeValue);
        Assert.Null(loadedNull.TimeValueN);
    }

    [Fact]
    public async Task TimeReaderExtensions_ReadSqlTimeColumn()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT CAST('12:34:56.1234567' AS time(7)), CAST(NULL AS time), CAST('2024-01-02 01:02:03' AS datetime2)";
        await using var dr = await cmd.ExecuteReaderAsync();
        Assert.True(await dr.ReadAsync());

        var expected = new TimeSpan(0, 12, 34, 56).Add(TimeSpan.FromTicks(1234567));
        Assert.Equal(TimeOnly.FromTimeSpan(expected), dr.GetTime(0));
        Assert.Equal(TimeOnly.FromTimeSpan(expected), dr.GetTimeOrNull(0));
        Assert.Equal(expected, dr.GetTimeSpanOrNull(0));
        Assert.Null(dr.GetTimeOrNull(1));
        Assert.Null(dr.GetTimeSpanOrNull(1));
        // date/time columns still work (time-of-day part)
        Assert.Equal(new TimeOnly(1, 2, 3), dr.GetTime(2));
    }

    [Fact]
    public async Task TimeOnlyParameter_ForTimeType_IsSentAsTimeSpan()
    {
        var provider = new MsSqlOrmProvider();
        var time = new TimeOnly(23, 59, 58).Add(TimeSpan.FromTicks(1));

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT @t, @dt";
        provider.AddDbParameter(cmd, "@t", UnifiedDbType.Time, 0, time, ParameterDirection.Input);
        provider.AddDbParameter(cmd, "@dt", UnifiedDbType.DateTime2, 0, time, ParameterDirection.Input);

        await using var dr = await cmd.ExecuteReaderAsync();
        Assert.True(await dr.ReadAsync());
        Assert.Equal(time, dr.GetTime(0));
        Assert.Equal(time, dr.GetTime(1));
    }

    #endregion

    #region #2 Unsigned integers

    [Fact]
    public async Task UnsignedAndInt8_RoundTrip_ThroughNativeMapping()
    {
        var provider = new MsSqlOrmProvider();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT @u16, @u32, @i8, CAST(NULL AS int)";
        provider.AddDbParameter(cmd, "@u16", UnifiedDbType.UInt16, 0, ushort.MaxValue, ParameterDirection.Input);
        provider.AddDbParameter(cmd, "@u32", UnifiedDbType.UInt32, 0, uint.MaxValue, ParameterDirection.Input);
        provider.AddDbParameter(cmd, "@i8", UnifiedDbType.Int8, 0, sbyte.MinValue, ParameterDirection.Input);

        await using var dr = await cmd.ExecuteReaderAsync();
        Assert.True(await dr.ReadAsync());

        Assert.Equal(ushort.MaxValue, dr.GetUInt16(0));
        Assert.Equal(ushort.MaxValue, dr.GetUInt16OrNull(0));
        Assert.Equal(uint.MaxValue, dr.GetUInt32(1));
        Assert.Equal(sbyte.MinValue, dr.GetInt8(2));
        Assert.Null(dr.GetUInt16OrNull(3));
    }

    [Fact]
    public async Task GetUInt16_OutOfRangeValue_ThrowsOverflow()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT CAST(65536 AS int)";
        await using var dr = await cmd.ExecuteReaderAsync();
        Assert.True(await dr.ReadAsync());
        Assert.Throws<OverflowException>(() => dr.GetUInt16(0));
    }

    #endregion

    #region #3 DDL type text

    [Theory]
    [InlineData(UnifiedDbType.DateTime2, 0, 0, 0, "datetime2 (7)")]
    [InlineData(UnifiedDbType.DateTime2, 0, 3, 0, "datetime2 (3)")]
    [InlineData(UnifiedDbType.Time, 0, 0, 0, "time (7)")]
    [InlineData(UnifiedDbType.DateTimeOffset, 0, -1, 0, "datetimeoffset (7)")]
    [InlineData(UnifiedDbType.Decimal, 0, 0, 0, "decimal (19,4)")]
    [InlineData(UnifiedDbType.Decimal, 0, 10, 0, "decimal (10,0)")]
    [InlineData(UnifiedDbType.Decimal, 0, 0, 2, "decimal (19,2)")]
    [InlineData(UnifiedDbType.Decimal, 0, 12, 3, "decimal (12,3)")]
    [InlineData(UnifiedDbType.String, -1, 0, 0, "nvarchar (max)")]
    [InlineData(UnifiedDbType.String, 50, 0, 0, "nvarchar (50)")]
    [InlineData(UnifiedDbType.AnsiString, 0, 0, 0, "varchar (max)")]
    [InlineData(UnifiedDbType.VarBinary, -1, 0, 0, "varbinary (max)")]
    [InlineData(UnifiedDbType.Binary, -1, 0, 0, "binary")]
    [InlineData(UnifiedDbType.Binary, 16, 0, 0, "binary (16)")]
    [InlineData(UnifiedDbType.AnsiStringFixedLength, -1, 0, 0, "char")]
    [InlineData(UnifiedDbType.StringFixedLength, -1, 0, 0, "nchar")]
    [InlineData(UnifiedDbType.StringFixedLength, 3, 0, 0, "nchar (3)")]
    [InlineData(UnifiedDbType.AnsiText, -1, 0, 0, "text")]
    [InlineData(UnifiedDbType.Text, -1, 0, 0, "ntext")]
    [InlineData(UnifiedDbType.Double, 0, 0, 0, "float (53)")]
    public void ToSqlDbTypeText_UsesValidDefaults(UnifiedDbType dbType, int size, int precision, int scale, string expected)
    {
        Assert.Equal(expected, dbType.ToSqlDbTypeText(size, precision, scale));
    }

    [Fact]
    public async Task DefaultDdlTypes_AreAcceptedBySqlServer_AndKeepFractions()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var columns = string.Join(", ",
            "c1 " + UnifiedDbType.DateTime2.ToSqlDbTypeText(0, 0, 0),
            "c2 " + UnifiedDbType.Decimal.ToSqlDbTypeText(0, 0, 0),
            "c3 " + UnifiedDbType.Binary.ToSqlDbTypeText(-1, 0, 0),
            "c4 " + UnifiedDbType.StringFixedLength.ToSqlDbTypeText(-1, 0, 0),
            "c5 " + UnifiedDbType.AnsiText.ToSqlDbTypeText(-1, 0, 0),
            "c6 " + UnifiedDbType.Text.ToSqlDbTypeText(-1, 0, 0),
            "c7 " + UnifiedDbType.AnsiStringFixedLength.ToSqlDbTypeText(-1, 0, 0));

        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            $"""
             DECLARE @t TABLE ({columns});
             INSERT INTO @t (c1, c2) VALUES ('2024-01-02 03:04:05.1234567', 1.2345);
             SELECT CONVERT(varchar(30), c1, 121), c2 FROM @t;
             """;
        await using var dr = await cmd.ExecuteReaderAsync();
        Assert.True(await dr.ReadAsync());
        Assert.Equal("2024-01-02 03:04:05.1234567", dr.GetString(0));
        Assert.Equal(1.2345m, dr.GetDecimal(1));
    }

    #endregion

    #region #4 Scalar helpers

    [Fact]
    public async Task GetScopeIdentityAsync_DoesNotThrow_AndReturnsZeroWhenNull()
    {
        await using var context = new TestStormContext(_connectionString);
        // Separate command => SCOPE_IDENTITY() is NULL (numeric(38,0) when not null) => must convert, not throw
        Assert.Equal(0, await context.GetScopeIdentityAsync());
    }

    [Fact]
    public async Task SequenceHelpers_ConvertToRequestedIntegerType()
    {
        await using var context = new TestStormContext(_connectionString);
        await context.ExecuteSqlStatementAsync(
            """
            IF OBJECT_ID('[dbo].[storm_fix_seq]', 'SO') IS NOT NULL DROP SEQUENCE [dbo].[storm_fix_seq];
            CREATE SEQUENCE [dbo].[storm_fix_seq] AS bigint START WITH 100 INCREMENT BY 1;
            """);

        // bigint sequence read as int / long / short
        Assert.Equal(100, await context.GetNextSequenceAsync<int>("dbo", "storm_fix_seq"));
        Assert.Equal(101L, await context.GetNextSequenceAsync<long>("dbo", "storm_fix_seq"));
        Assert.Equal((short)102, await context.GetNextSequenceAsync<short>("dbo", "storm_fix_seq"));

        // current_value is sql_variant(bigint)
        Assert.Equal(102, await context.GetCurrentSequenceAsync<int>("dbo", "storm_fix_seq"));

        // non-existent sequence => no row/NULL => default
        Assert.Equal(0, await context.GetCurrentSequenceAsync<int>("dbo", "storm_fix_seq_missing"));
    }

    [Fact]
    public void ConvertScalarValue_HandlesNullDbNullNullableAndExactTypes()
    {
        Assert.Equal(0, DbConnectionExt.ConvertScalarValue<int>(DBNull.Value));
        Assert.Equal(0, DbConnectionExt.ConvertScalarValue<int>(null));
        Assert.Null(DbConnectionExt.ConvertScalarValue<int?>(DBNull.Value));
        Assert.Equal(42, DbConnectionExt.ConvertScalarValue<int?>(42m));
        Assert.Equal(42L, DbConnectionExt.ConvertScalarValue<long>(42m));
        var bytes = new byte[] { 1, 2 };
        Assert.Same(bytes, DbConnectionExt.ConvertScalarValue<byte[]>(bytes));
        Assert.Null(DbConnectionExt.ConvertScalarValue<byte[]>(DBNull.Value));
    }

    #endregion

    #region #5 CDC LSN for capture instance

    [Fact]
    public async Task GetLastLogSequenceNumberAsync_CaptureInstance_ReturnsMaxLsnOfChangeTable()
    {
        // CDC is not supported on LocalDB, so a stand-in change table cdc.[<capture_instance>_CT] with known LSNs is created instead.
        // The capture instance name contains ' and ] to verify that the table name is quoted safely.
        const string captureInstance = "dbo_it's]a [test";
        const string changeTable = "[cdc].[dbo_it's]]a [test_CT]";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecAsync(connection, "IF SCHEMA_ID('cdc') IS NULL EXEC('CREATE SCHEMA [cdc]')");
        await ExecAsync(connection, $"IF OBJECT_ID('{changeTable.Replace("'", "''")}') IS NOT NULL DROP TABLE {changeTable}; CREATE TABLE {changeTable} ([__$start_lsn] binary(10) NOT NULL)");

        try
        {
            await using var context = new TestStormContext(_connectionString);

            // No changes yet
            Assert.Equal(SqlLogSequenceNumber.Zero, await context.GetLastLogSequenceNumberAsync(captureInstance));

            await ExecAsync(connection, $"INSERT INTO {changeTable} VALUES (0x0000002A000001A80003), (0x0000002B000000100001), (0x0000002A000002000001)");

            var lsn = await context.GetLastLogSequenceNumberAsync(captureInstance);
            Assert.Equal(new SqlLogSequenceNumber([0x00, 0x00, 0x00, 0x2B, 0x00, 0x00, 0x00, 0x10, 0x00, 0x01]), lsn);
        }
        finally
        {
            await ExecAsync(connection, $"DROP TABLE {changeTable}");
        }
    }

    #endregion

    #region #10 Transaction scope rolled back by inner scope

    [Fact]
    public async Task OuterScope_AfterInnerRollback_ThrowsClearError_InsteadOfStartingNewTransaction()
    {
        using var outer = new StormTransactionScope();
        await using var context = new TestStormContext(_connectionString);

        // start the ambient transaction
        await context.ExecuteSqlStatementAsync("SELECT 1");

        using (new StormTransactionScope())
        {
            // inner joined scope disposed without CompleteAsync => rolls back the shared transaction
        }

        Assert.True(outer.Ambient.IsRollBacked);

        var workEx = await Assert.ThrowsAsync<StormException>(() => context.ExecuteSqlStatementAsync("SELECT 1"));
        Assert.Contains("rolled back", workEx.Message);

        var completeEx = await Assert.ThrowsAsync<StormException>(() => outer.CompleteAsync(CancellationToken.None));
        Assert.Contains("rolled back", completeEx.Message);
        Assert.DoesNotContain("TransactionCount", completeEx.Message);
    }

    #endregion

    private static async Task ExecAsync(SqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}

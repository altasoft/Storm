using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Tests that verify CreateTableAsync behaviour when the mapped type has no primary key columns.
/// </summary>
public class NoPrimaryKeyCreateTableTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly string _connectionString;

    public NoPrimaryKeyCreateTableTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);
        StormManager.SetLogger(logger);
        _connectionString = fixture.ConnectionString;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Drop the table created during the test so the fixture DB stays clean.
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "IF OBJECT_ID('[dbo].[NoPkEntity]') IS NOT NULL DROP TABLE [dbo].[NoPkEntity]";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task CreateTableAsync_WithNoPrimaryKey_OmitsPkConstraint_AndSkipsDetailTables()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act: create the table for a type with no [StormColumn(ColumnType = PrimaryKey)].
        await connection.CreateTableAsync<NoPrimaryKeyEntity>(checkNotExists: true, createDetailTables: true);

        // Assert: main table was created.
        Assert.Equal(1, await QueryScalarAsync(connection,
            """
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'NoPkEntity'
            """));

        // Assert: no PRIMARY KEY constraint was added to the main table.
        Assert.Equal(0, await QueryScalarAsync(connection,
            """
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'NoPkEntity'
              AND CONSTRAINT_TYPE = 'PRIMARY KEY'
            """));

        // Assert: the detail table (NoPkEntityTags) was NOT created because the parent has no PK.
        Assert.Equal(0, await QueryScalarAsync(connection,
            """
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'NoPkEntityTags'
            """));
    }

    private static async Task<int> QueryScalarAsync(SqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
        return result is int i ? i : 0;
    }
}

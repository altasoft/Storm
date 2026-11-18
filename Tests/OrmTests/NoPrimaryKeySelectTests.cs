using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Tests that verify SELECT with TOP/SKIP (OFFSET/FETCH) works when the mapped type has no primary key columns.
/// SQL Server requires ORDER BY for OFFSET/FETCH, so an ordering must be generated even without a PK.
/// </summary>
public class NoPrimaryKeySelectTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly TestStormContext _context;

    public NoPrimaryKeySelectTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);
        StormManager.SetLogger(logger);
        _connectionString = fixture.ConnectionString;
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "IF OBJECT_ID('[dbo].[NoPkEntity]') IS NOT NULL DROP TABLE [dbo].[NoPkEntity]");
        await connection.CreateTableAsync<NoPrimaryKeyEntity>(checkNotExists: true, createDetailTables: false);
        await ExecuteAsync(connection, "INSERT INTO [dbo].[NoPkEntity] ([Name], [Value]) VALUES ('a', 1), ('b', 2), ('c', 3)");
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "IF OBJECT_ID('[dbo].[NoPkEntity]') IS NOT NULL DROP TABLE [dbo].[NoPkEntity]");
    }

    [Fact]
    public async Task GetAsync_WithNoPrimaryKey_ReturnsRow()
    {
        var item = await _context.SelectFromNoPkEntity().Partially(NoPrimaryKeyEntity.PartialLoadFlags.AllExceptDetailTables).GetAsync();

        item.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_WithNoPrimaryKey_AndWhere_ReturnsMatchingRow()
    {
        var item = await _context.SelectFromNoPkEntity().Partially(NoPrimaryKeyEntity.PartialLoadFlags.AllExceptDetailTables).Where(x => x.Value == 2).GetAsync();

        item.Should().NotBeNull();
        item!.Name.Should().Be("b");
    }

    [Fact]
    public async Task ListAsync_WithNoPrimaryKey_AndTopSkip_ReturnsRows()
    {
        var items = await _context.SelectFromNoPkEntity().Partially(NoPrimaryKeyEntity.PartialLoadFlags.AllExceptDetailTables).Skip(1).Top(2).ListAsync();

        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAsync_WithColumnSelector_AndNoPrimaryKey_ReturnsValue()
    {
        var value = await _context.SelectFromNoPkEntity().Where(x => x.Value == 3).GetAsync(x => x.Name);

        value.HasValue.Should().BeTrue();
        value.Value.Should().Be("c");
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}

using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// List inserts and merges collect generated identity values in a table variable. For a <c>decimal</c> identity the buffer
/// must have scale 0 (not the default <c>decimal(19,4)</c>), otherwise identity values with 16+ integer digits overflow it.
/// </summary>
public class DecimalIdentityTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private const decimal BigSeed = 10_000_000_000_000_000m; // 10^16: fits decimal(19,0), overflows decimal(19,4)

    private readonly string _connectionString;
    private readonly TestStormContext _context;

    public DecimalIdentityTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        StormManager.SetLogger(new XunitLogger<DatabaseFixture>(output));
        _connectionString = fixture.ConnectionString;
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "IF OBJECT_ID('[dbo].[DecimalIdentityEntities]') IS NOT NULL DROP TABLE [dbo].[DecimalIdentityEntities]");
        await connection.CreateTableAsync<DecimalIdentityEntity>(checkNotExists: true, createDetailTables: false);
        await ExecuteAsync(connection, $"DBCC CHECKIDENT ('[dbo].[DecimalIdentityEntities]', RESEED, {BigSeed})");
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "IF OBJECT_ID('[dbo].[DecimalIdentityEntities]') IS NOT NULL DROP TABLE [dbo].[DecimalIdentityEntities]");
    }

    [Fact]
    public async Task InsertList_WithLargeDecimalIdentity_ReturnsGeneratedIds()
    {
        var a = new DecimalIdentityEntity { Name = "a" };
        var b = new DecimalIdentityEntity { Name = "b" };

        await _context.InsertIntoDecimalIdentityEntity().Values([a, b]).GoAsync();

        a.Id.Should().BeGreaterThanOrEqualTo(BigSeed);
        b.Id.Should().Be(a.Id + 1);
        (await _context.SelectFromDecimalIdentityEntity(b.Id).GetAsync())!.Name.Should().Be("b");
    }

    [Fact]
    public async Task MergeList_WithLargeDecimalIdentity_ReturnsGeneratedIds()
    {
        var a = new DecimalIdentityEntity { Name = "m1" };
        var b = new DecimalIdentityEntity { Name = "m2" };

        await _context.MergeIntoDecimalIdentityEntity().InsertOrUpdate([a, b]).GoAsync();

        a.Id.Should().BeGreaterThanOrEqualTo(BigSeed);
        b.Id.Should().Be(a.Id + 1);
        (await _context.SelectFromDecimalIdentityEntity(a.Id).GetAsync())!.Name.Should().Be("m1");
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}

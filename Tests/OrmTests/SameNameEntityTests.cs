using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;
using EntityA = AltaSoft.Storm.TestModels.SameNameA.SameNameEntity;
using EntityB = AltaSoft.Storm.TestModels.SameNameB.SameNameEntity;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Two [StormDbObject] types with the same name in different namespaces must both be generated and usable.
/// </summary>
public class SameNameEntityTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly TestModels.TestStormContext _context;

    public SameNameEntityTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);
        StormManager.SetLogger(logger);
        _connectionString = fixture.ConnectionString;
        _context = new TestModels.TestStormContext(fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await DropTablesAsync(connection);
        await connection.CreateTableAsync<EntityA>(checkNotExists: true, createDetailTables: false);
        await connection.CreateTableAsync<EntityB>(checkNotExists: true, createDetailTables: false);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await DropTablesAsync(connection);
    }

    [Fact]
    public async Task InsertAndSelect_BothSameNamedEntities_ShouldWork()
    {
        await TestModels.SameNameA.TestStormContextSameNameEntityExt.InsertIntoSameNameEntityA(_context).Values(new EntityA { Id = 1, Name = "a" }).GoAsync();
        await TestModels.SameNameB.TestStormContextSameNameEntityExt.InsertIntoSameNameEntityB(_context).Values(new EntityB { Id = 1, Value = 2 }).GoAsync();

        var a = await TestModels.SameNameA.TestStormContextSameNameEntityExt.SelectFromSameNameEntityA(_context, 1).GetAsync();
        var b = await TestModels.SameNameB.TestStormContextSameNameEntityExt.SelectFromSameNameEntityB(_context, 1).GetAsync();

        a.Should().NotBeNull();
        a!.Name.Should().Be("a");
        b.Should().NotBeNull();
        b!.Value.Should().Be(2);
    }

    private static async Task DropTablesAsync(SqlConnection connection)
    {
        foreach (var table in new[] { "SameNameEntitiesA", "SameNameEntitiesB" })
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"IF OBJECT_ID('[dbo].[{table}]') IS NOT NULL DROP TABLE [dbo].[{table}]";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

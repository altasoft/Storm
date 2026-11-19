using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using AltaSoft.Storm.TestModels.DomainTypes;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Reproductions of issues found while reviewing the bugfix/somebugs branch.
/// Each test asserts the correct behavior and is named after the review finding number.
/// </summary>
public class PrReviewReproTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private static readonly string[] s_tables = ["ReadOnlyFlatEntities", "SaveAsStringEntities"];

    private readonly string _connectionString;
    private readonly TestStormContext _context;

    public PrReviewReproTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        StormManager.SetLogger(new XunitLogger<DatabaseFixture>(output));
        _connectionString = fixture.ConnectionString;
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await DropTablesAsync(connection);

        await connection.CreateTableAsync<ReadOnlyFlatEntity>(checkNotExists: true, createDetailTables: false);
        await connection.CreateTableAsync<SaveAsStringEntity>(checkNotExists: true, createDetailTables: false);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await DropTablesAsync(connection);
    }

    // ---------- #3 Update().Set() of a domain primitive saved as string ----------

    [Fact]
    public async Task Finding3_UpdateSetDomainPrimitiveSavedAsString_ShouldUseRoundTripFormat()
    {
        var when = new DateTime(2024, 3, 15, 10, 20, 30, DateTimeKind.Utc).AddTicks(1234567);
        var eventAt = new EventTime(when);

        await _context.InsertIntoSaveAsStringEntity().Values(new SaveAsStringEntity { Id = 1, OwnerId = 1 }).GoAsync();

        await _context.UpdateSaveAsStringEntity(1).Set(x => x.EventAt, (EventTime?)eventAt).GoAsync();

        var raw = await ScalarAsync("SELECT [EventAt] FROM [dbo].[SaveAsStringEntities] WHERE [Id] = 1");
        raw.Should().Be(when.ToString("O", CultureInfo.InvariantCulture));

        var found = await _context.SelectFromSaveAsStringEntity().Where(x => x.EventAt == eventAt).ListAsync();
        found.Should().ContainSingle();
    }

    // ---------- #4 OrderBy ids with a read-only FlatObject sub-property ----------

    [Fact]
    public async Task Finding4_OrderByAfterReadOnlyFlatSubProperty_ShouldOrderByTheNamedColumn()
    {
        // Flat.B ascending is the reverse of Last ascending
        await _context.InsertIntoReadOnlyFlatEntity().Values(
        [
            new ReadOnlyFlatEntity { Id = 1, Flat = new FlatWithReadOnly { A = 1, B = 10 }, Last = 300 },
            new ReadOnlyFlatEntity { Id = 2, Flat = new FlatWithReadOnly { A = 2, B = 20 }, Last = 200 },
            new ReadOnlyFlatEntity { Id = 3, Flat = new FlatWithReadOnly { A = 3, B = 30 }, Last = 100 }
        ]).GoAsync();

        var byB = await _context.SelectFromReadOnlyFlatEntity().OrderBy(ReadOnlyFlatEntity.OrderBy.Flat_B).ListAsync();
        byB.Select(x => x.Id).Should().Equal(1, 2, 3);

        var byLast = await _context.SelectFromReadOnlyFlatEntity().OrderBy(ReadOnlyFlatEntity.OrderBy.Last).ListAsync();
        byLast.Select(x => x.Id).Should().Equal(3, 2, 1);
    }

    private async Task<object?> ScalarAsync(string sql)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return await cmd.ExecuteScalarAsync();
    }

    private static async Task DropTablesAsync(SqlConnection connection)
    {
        foreach (var table in s_tables)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"IF OBJECT_ID('[dbo].[{table}]') IS NOT NULL DROP TABLE [dbo].[{table}]";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Tests that generated column ids/flags are correct when an entity contains FlatObject properties
/// (a FlatObject property occupies one column per sub-property).
/// </summary>
public class FlatObjectColumnIdTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private static readonly string[] s_tables =
        ["AutoIncFlatEntities", "TerminatorFlatEntities", "PartialFlatEntities", "PartialFlatOnlyEntities", "ConcurrencyFlatEntities"];

    private readonly string _connectionString;
    private readonly TestStormContext _context;

    public FlatObjectColumnIdTests(DatabaseFixture fixture, ITestOutputHelper output)
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
        await DropTablesAsync(connection);

        await connection.CreateTableAsync<AutoIncFlatEntity>(checkNotExists: true, createDetailTables: false);
        await connection.CreateTableAsync<TerminatorFlatDerived>(checkNotExists: true, createDetailTables: false);
        await connection.CreateTableAsync<PartialFlatEntity>(checkNotExists: true, createDetailTables: false);
        await connection.CreateTableAsync<PartialFlatOnlyEntity>(checkNotExists: true, createDetailTables: false);
        await connection.CreateTableAsync<ConcurrencyFlatEntity>(checkNotExists: true, createDetailTables: false);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await DropTablesAsync(connection);
    }

    [Fact]
    public async Task Insert_AutoIncrementAfterFlatObject_ShouldSetAutoIncValue()
    {
        var first = new AutoIncFlatEntity { Id = 1, Flat = new FlatPair { A = 10, B = "first" } };
        var second = new AutoIncFlatEntity { Id = 2, Flat = new FlatPair { A = 20, B = "second" } };

        await _context.InsertIntoAutoIncFlatEntity().Values(first).GoAsync();
        await _context.InsertIntoAutoIncFlatEntity().Values(second).GoAsync();

        first.Seq.Should().BeGreaterThan(0);
        second.Seq.Should().Be(first.Seq + 1);

        var loaded = await _context.SelectFromAutoIncFlatEntity(2).GetAsync();
        loaded.Should().NotBeNull();
        loaded!.Seq.Should().Be(second.Seq);
        loaded.Flat.A.Should().Be(20);
        loaded.Flat.B.Should().Be("second");
    }

    [Fact]
    public async Task Select_ConditionalTerminatorBeforeFlatObject_ShouldSkipAllFlatColumns()
    {
        var stopped = new TerminatorFlatDerived { Id = 1, Stop = true, Flat = new FlatPair { A = 5, B = "flat" }, After = 42 };
        var notStopped = new TerminatorFlatDerived { Id = 2, Stop = false, Flat = new FlatPair { A = 6, B = "flat2" }, After = 43 };
        await _context.InsertIntoTerminatorFlatEntity().Values(stopped).GoAsync();
        await _context.InsertIntoTerminatorFlatEntity().Values(notStopped).GoAsync();

        var loadedStopped = await _context.SelectFromTerminatorFlatEntity(1).GetAsync();
        loadedStopped.Should().NotBeNull();
        loadedStopped!.Stop.Should().BeTrue();
        loadedStopped.After.Should().Be(42); // derived column is read after the skipped base columns

        var loadedNotStopped = await _context.SelectFromTerminatorFlatEntity(2).GetAsync();
        loadedNotStopped.Should().NotBeNull();
        loadedNotStopped!.Flat.A.Should().Be(6);
        loadedNotStopped.Flat.B.Should().Be("flat2");
        loadedNotStopped.After.Should().Be(43);
    }

    [Fact]
    public void PartialLoadFlags_FlatSubProperty_ShouldHaveOwnBitInParentSpace()
    {
        ((uint)PartialFlatEntity.PartialLoadFlags.Flat_Tags).Should().NotBe((uint)PartialFlatEntity.PartialLoadFlags.Notes);

        var ctrl = StormControllerCache.Get<PartialFlatEntity>(0);
        var tagsColumn = ctrl.ColumnDefs[3];
        tagsColumn.SubPropertyName.Should().Be(nameof(FlatWithJson.Tags));
        tagsColumn.PartialLoadFlags.Should().Be((uint)PartialFlatEntity.PartialLoadFlags.Flat_Tags);
        ctrl.PartialLoadFlagsAll.Should().Be((uint)PartialFlatEntity.PartialLoadFlags.All);
        (ctrl.PartialLoadFlagsAll & tagsColumn.PartialLoadFlags).Should().NotBe(0u);
    }

    [Fact]
    public async Task Select_PartialFlatSubProperty_ShouldBeLoadedOnlyWithItsFlag()
    {
        var entity = new PartialFlatEntity { Id = 1, Notes = "notes", Flat = new FlatWithJson { Code = 7, Tags = ["a", "b"] } };
        await _context.InsertIntoPartialFlatEntity().Values(entity).GoAsync();

        var all = await _context.SelectFromPartialFlatEntity(1).GetAsync();
        all.Should().NotBeNull();
        all!.Notes.Should().Be("notes");
        all.Flat.Code.Should().Be(7);
        all.Flat.Tags.Should().BeEquivalentTo(new List<string> { "a", "b" });

        var notesOnly = await _context.SelectFromPartialFlatEntity(1).Partially(PartialFlatEntity.PartialLoadFlags.Notes).GetAsync();
        notesOnly.Should().NotBeNull();
        notesOnly!.Notes.Should().Be("notes");
        notesOnly.Flat.Code.Should().Be(7);
        notesOnly.Flat.Tags.Should().BeNull();

        var tagsOnly = await _context.SelectFromPartialFlatEntity(1).Partially(PartialFlatEntity.PartialLoadFlags.Flat_Tags).GetAsync();
        tagsOnly.Should().NotBeNull();
        tagsOnly!.Notes.Should().BeNull();
        tagsOnly.Flat.Tags.Should().BeEquivalentTo(new List<string> { "a", "b" });
    }

    [Fact]
    public async Task Select_PartialFlatSubProperty_WithoutParentFlags_ShouldBeLoadedByDefault()
    {
        var entity = new PartialFlatOnlyEntity { Id = 1, Flat = new FlatWithJson { Code = 3, Tags = ["x"] } };
        await _context.InsertIntoPartialFlatOnlyEntity().Values(entity).GoAsync();

        var loaded = await _context.SelectFromPartialFlatOnlyEntity(1).GetAsync();
        loaded.Should().NotBeNull();
        loaded!.Flat.Code.Should().Be(3);
        loaded.Flat.Tags.Should().BeEquivalentTo(new List<string> { "x" });

        var basic = await _context.SelectFromPartialFlatOnlyEntity(1).Partially(PartialFlatOnlyEntity.PartialLoadFlags.Basic).GetAsync();
        basic.Should().NotBeNull();
        basic!.Flat.Tags.Should().BeNull();
    }

    [Fact]
    public async Task Update_ConcurrencyColumnInsideFlatObject_ShouldCheckConcurrency()
    {
        var entity = new ConcurrencyFlatEntity { Id = 1, Name = "name", Stamp = new StampPair { Version = 1, Note = "n" } };
        await _context.InsertIntoConcurrencyFlatEntity().Values(entity).GoAsync();

        var loaded = await _context.SelectFromConcurrencyFlatEntity(1).WithTracking().GetAsync();
        loaded.Should().NotBeNull();
        loaded!.Name = "updated";
        var updated = await _context.UpdateConcurrencyFlatEntity().WithConcurrencyCheck().Set(loaded).GoAsync();
        updated.Should().Be(1);

        var stale = await _context.SelectFromConcurrencyFlatEntity(1).WithTracking().GetAsync();
        stale.Should().NotBeNull();

        await using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await ExecuteAsync(connection, "UPDATE [dbo].[ConcurrencyFlatEntities] SET [Stamp.Version] = 2 WHERE [Id] = 1");
        }

        stale!.Name = "stale";
        // The list overload wraps the UPDATE into a transaction, so the concurrency error rolls the change back
        var act = () => _context.UpdateConcurrencyFlatEntity().WithConcurrencyCheck().Set([stale]).GoAsync();
        await act.Should().ThrowAsync<StormDbConcurrencyException>();

        var current = await _context.SelectFromConcurrencyFlatEntity(1).GetAsync();
        current!.Name.Should().Be("updated");
    }

    private static async Task DropTablesAsync(SqlConnection connection)
    {
        foreach (var table in s_tables)
            await ExecuteAsync(connection, $"IF OBJECT_ID('[dbo].[{table}]') IS NOT NULL DROP TABLE [dbo].[{table}]");
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}

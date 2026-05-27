using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Tests that WithTableHints works correctly on INSERT, UPDATE, and DELETE operations:
/// - the generated SQL is accepted by SQL Server (no syntax errors)
/// - the DML actually modifies the right rows
/// - hints propagate correctly through builder chains (e.g. UpdateFrom → UpdateFromSet)
/// </summary>
public class TableHintsModifyTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    // IDs that won't collide with the 10 pre-seeded users or 5 customer properties
    private const int TestUserId = 1001;
    private const short BranchId = 7;

    public TableHintsModifyTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        StormManager.SetLogger(new XunitLogger<DatabaseFixture>(output));
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // ──────────────────────────────────────────────────────────────────────────
    // INSERT
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Insert_WithTableHints_SingleValue_InsertsSuccessfully()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId);

        // Act – TABLOCK is the canonical INSERT hint; using it proves the SQL is valid
        var result = await _context.InsertIntoUsersTable()
            .WithTableHints(StormTableHints.TabLock)
            .Values(user)
            .GoAsync();

        // Assert – row inserted and readable back
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(TestUserId, BranchId).GetAsync();
        stored.Should().NotBeNull();
        stored!.FullName.Should().Be(user.FullName);

        // Cleanup
        await _context.DeleteFromUsersTable(TestUserId, BranchId).GoAsync();
    }

    [Fact]
    public async Task Insert_WithTableHints_MultipleValues_InsertsAllRows()
    {
        // Arrange – use a range that doesn't overlap with any other test
        var users = new[]
        {
            DatabaseHelper.NewUser(TestUserId + 10),
            DatabaseHelper.NewUser(TestUserId + 11),
            DatabaseHelper.NewUser(TestUserId + 12),
        };

        // Act
        var result = await _context.InsertIntoUsersTable()
            .WithTableHints(StormTableHints.TabLock)
            .Values(users)
            .GoAsync();

        // Assert
        result.Should().Be(users.Length);
        foreach (var user in users)
        {
            var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
            stored.Should().NotBeNull($"user {user.UserId} should have been inserted");
        }

        // Cleanup
        foreach (var user in users)
            await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    [Fact]
    public async Task Insert_WithCombinedTableHints_InsertsSuccessfully()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 20);

        // Act – TABLOCK | HOLDLOCK is a valid combination for INSERT
        var result = await _context.InsertIntoUsersTable()
            .WithTableHints(StormTableHints.TabLock | StormTableHints.HoldLock)
            .Values(user)
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored.Should().NotBeNull();

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UPDATE – entity-based (Set(T value))
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithTableHints_EntityBased_UpdatesCorrectly()
    {
        // Arrange – insert a fresh row to operate on
        var user = DatabaseHelper.NewUser(TestUserId + 30);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        user.FullName = "HintUpdatedName";

        // Act – ROWLOCK limits lock scope to a row level
        var result = await _context.UpdateUsersTable()
            .WithTableHints(StormTableHints.RowLock)
            .WithoutConcurrencyCheck()
            .Set(user)
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored!.FullName.Should().Be("HintUpdatedName");

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UPDATE – column-based (Set(col, val) → IUpdateFromSet)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithTableHints_ColumnBased_UpdatesCorrectly()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 40);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        // Act – set hint before chaining .Set(col, val) which moves to IUpdateFromSet
        var result = await _context.UpdateUsersTable()
            .WithTableHints(StormTableHints.UpdLock)
            .Set(x => x.FullName, "ColumnHintUpdated")
            .Where(x => x.UserId == (TestUserId + 40))
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored!.FullName.Should().Be("ColumnHintUpdated");

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    [Fact]
    public async Task Update_TableHints_SetDirectlyOnUpdateFromSet_UpdatesCorrectly()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 45);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        // Act – set hint directly on IUpdateFromSet (after .Set(col, val))
        var result = await _context.UpdateUsersTable()
            .Set(x => x.FullName, "SetHintDirect")
            .WithTableHints(StormTableHints.UpdLock)
            .Where(x => x.UserId == (TestUserId + 45))
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored!.FullName.Should().Be("SetHintDirect");

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    [Fact]
    public async Task Update_TableHints_PropagateThroughUpdateFromToUpdateFromSet()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 50);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        // Act – UPDLOCK set on IUpdateFrom, then chain to IUpdateFromSet via .Set(col, val)
        // The constructor of UpdateFromSet copies TableHints from the source UpdateFrom
        var result = await _context.UpdateUsersTable()
            .WithTableHints(StormTableHints.UpdLock | StormTableHints.RowLock)
            .Set(x => x.FullName, "PropagatedHint")
            .Where(x => x.UserId == (TestUserId + 50))
            .GoAsync();

        // Assert – if the SQL was malformed the server would throw; checking data is the proof
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored!.FullName.Should().Be("PropagatedHint");

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UPDATE – single (by PK) – IUpdateFromSingle / IUpdateFromSetSingle
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithTableHints_ByPrimaryKey_ColumnBased_UpdatesCorrectly()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 60);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        // Act – IUpdateFromSingle.WithTableHints(), then chain to IUpdateFromSetSingle via .Set(col, val)
        var result = await _context.UpdateUsersTable(TestUserId + 60, BranchId)
            .WithTableHints(StormTableHints.RowLock)
            .Set(x => x.FullName, "PkHintUpdated")
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored!.FullName.Should().Be("PkHintUpdated");

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    [Fact]
    public async Task Update_TableHints_PropagateThroughUpdateFromSingleToSetSingle()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 65);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        // Act – hint set on IUpdateFromSingle propagates into IUpdateFromSetSingle
        var result = await _context.UpdateUsersTable(TestUserId + 65, BranchId)
            .WithTableHints(StormTableHints.UpdLock | StormTableHints.RowLock)
            .Set(x => x.FullName, "SinglePropagated")
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored!.FullName.Should().Be("SinglePropagated");

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    [Fact]
    public async Task Update_WithTableHints_ByPrimaryKey_EntityBased_UpdatesCorrectly()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 70);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        user.FullName = "SingleEntityHint";

        // Act – IUpdateFromSingle.WithTableHints().Set(T value)
        var result = await _context.UpdateUsersTable(TestUserId + 70, BranchId)
            .WithTableHints(StormTableHints.RowLock)
            .Set(user)
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored!.FullName.Should().Be("SingleEntityHint");

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE – where-expression (IDeleteFrom)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithTableHints_WhereExpression_DeletesCorrectly()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 80);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        // Act
        var result = await _context.DeleteFromUsersTable()
            .WithTableHints(StormTableHints.RowLock)
            .Where(x => x.UserId == (TestUserId + 80))
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithTableHints_WhereExpression_NoMatchReturnsZero()
    {
        // Act – no such user, but the SQL should still be valid
        var result = await _context.DeleteFromUsersTable()
            .WithTableHints(StormTableHints.RowLock)
            .Where(x => x.UserId == (TestUserId + 999))
            .GoAsync();

        // Assert
        result.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE – by PK (IDeleteFromSingle)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithTableHints_ByPrimaryKey_DeletesCorrectly()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 90);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        // Act
        var result = await _context.DeleteFromUsersTable(TestUserId + 90, BranchId)
            .WithTableHints(StormTableHints.RowLock)
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithTableHints_ByPrimaryKey_NonExistent_ReturnsZero()
    {
        // Act – valid SQL, no matching row
        var result = await _context.DeleteFromUsersTable(TestUserId + 998, BranchId)
            .WithTableHints(StormTableHints.RowLock)
            .GoAsync();

        // Assert
        result.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE – single entity / entity list (IDeleteFromSingle)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithTableHints_SingleEntity_DeletesCorrectly()
    {
        // Arrange
        var user = DatabaseHelper.NewUser(TestUserId + 100);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        // Act
        var result = await _context.DeleteFromUsersTable(user)
            .WithTableHints(StormTableHints.RowLock)
            .GoAsync();

        // Assert
        result.Should().Be(1);
        var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
        stored.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithTableHints_EntityList_DeletesAllRows()
    {
        // Arrange
        var users = new[]
        {
            DatabaseHelper.NewUser(TestUserId + 110),
            DatabaseHelper.NewUser(TestUserId + 111),
        };
        await _context.InsertIntoUsersTable().Values(users).GoAsync();

        // Act
        var result = await _context.DeleteFromUsersTable(users)
            .WithTableHints(StormTableHints.RowLock)
            .GoAsync();

        // Assert
        result.Should().Be(users.Length);
        foreach (var user in users)
        {
            var stored = await _context.SelectFromUsersTable(user.UserId, BranchId).GetAsync();
            stored.Should().BeNull($"user {user.UserId} should have been deleted");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Combined – hints do not affect operations that match nothing
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Insert_WithNoneHint_BehavesIdenticallyToNoHint()
    {
        // StormTableHints.None should produce the same SQL as having no hints at all
        var user = DatabaseHelper.NewUser(TestUserId + 120);

        var result = await _context.InsertIntoUsersTable()
            .WithTableHints(StormTableHints.None)
            .Values(user)
            .GoAsync();

        result.Should().Be(1);

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    [Fact]
    public async Task Update_WithNoneHint_BehavesIdenticallyToNoHint()
    {
        var user = DatabaseHelper.NewUser(TestUserId + 130);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        user.FullName = "NoneHintUpdate";
        var result = await _context.UpdateUsersTable()
            .WithTableHints(StormTableHints.None)
            .WithoutConcurrencyCheck()
            .Set(user)
            .GoAsync();

        result.Should().Be(1);

        // Cleanup
        await _context.DeleteFromUsersTable(user.UserId, BranchId).GoAsync();
    }

    [Fact]
    public async Task Delete_WithNoneHint_BehavesIdenticallyToNoHint()
    {
        var user = DatabaseHelper.NewUser(TestUserId + 140);
        await _context.InsertIntoUsersTable().Values(user).GoAsync();

        var result = await _context.DeleteFromUsersTable()
            .WithTableHints(StormTableHints.None)
            .Where(x => x.UserId == (TestUserId + 140))
            .GoAsync();

        result.Should().Be(1);
    }
}

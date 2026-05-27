using System.Collections.Generic;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Verifies that WithTableHints on INSERT / UPDATE / DELETE actually writes
/// the WITH (...) clause into the generated SQL string.
///
/// Uses GenerateBatchCommands() instead of GoAsync() — the full SQL-generation
/// pipeline runs without opening a database connection, so the CommandText of
/// every SqlBatchCommand can be inspected directly.
/// IClassFixture&lt;DatabaseFixture&gt; is kept only to guarantee StormManager is
/// initialized before the first test runs.
/// </summary>
public class TableHintsSqlGenerationTests : IClassFixture<DatabaseFixture>
{
    private readonly TestStormContext _context;

    public TableHintsSqlGenerationTests(DatabaseFixture fixture)
    {
        // context is used purely to call the generated extension methods;
        // no connection is opened during SQL generation
        _context = new TestStormContext(fixture.ConnectionString);
    }

    // ── helper ────────────────────────────────────────────────────────────────

    private static string Sql(ISqlGo command)
    {
        var batch = new List<SqlBatchCommand>();
        command.GenerateBatchCommands(batch);
        batch.Should().NotBeEmpty("GenerateBatchCommands must produce at least one command");
        return batch[0].CommandText;
    }

    // ── INSERT ────────────────────────────────────────────────────────────────

    [Fact]
    public void Insert_WithTabLock_SqlContainsHint()
    {
        var sql = Sql(_context.InsertIntoUsersTable()
            .WithTableHints(StormTableHints.TabLock)
            .Values(DatabaseHelper.NewUser(1)));

        sql.Should().Contain("WITH (TABLOCK)", "hint must appear right after the table name");
    }

    [Fact]
    public void Insert_WithCombinedHints_SqlContainsBothHints()
    {
        var sql = Sql(_context.InsertIntoUsersTable()
            .WithTableHints(StormTableHints.TabLock | StormTableHints.HoldLock)
            .Values(DatabaseHelper.NewUser(1)));

        sql.Should().Contain("TABLOCK").And.Contain("HOLDLOCK");
    }

    [Fact]
    public void Insert_WithNoneHint_SqlContainsNoHintClause()
    {
        var sql = Sql(_context.InsertIntoUsersTable()
            .WithTableHints(StormTableHints.None)
            .Values(DatabaseHelper.NewUser(1)));

        sql.Should().NotContain("WITH (", "StormTableHints.None must produce no hint clause");
    }

    [Fact]
    public void Insert_WithoutCallingWithTableHints_SqlContainsNoHintClause()
    {
        var sql = Sql(_context.InsertIntoUsersTable()
            .Values(DatabaseHelper.NewUser(1)));

        sql.Should().NotContain("WITH (");
    }

    [Fact]
    public void Insert_HintAppearsAfterTableNameAndBeforeColumnList()
    {
        var sql = Sql(_context.InsertIntoUsersTable()
            .WithTableHints(StormTableHints.TabLock)
            .Values(DatabaseHelper.NewUser(1)));

        // Expected shape: INSERT INTO [dbo].[Users] WITH (TABLOCK) (col1, col2, ...)
        var insertPos = sql.IndexOf("INSERT INTO", System.StringComparison.OrdinalIgnoreCase);
        var hintPos   = sql.IndexOf("WITH (TABLOCK)", System.StringComparison.OrdinalIgnoreCase);
        var colPos    = sql.IndexOf("(", hintPos + 1, System.StringComparison.OrdinalIgnoreCase);

        insertPos.Should().BeGreaterThanOrEqualTo(0);
        hintPos.Should().BeGreaterThan(insertPos, "hint must come after INSERT INTO");
        colPos.Should().BeGreaterThan(hintPos,    "column list must come after hint");
    }

    // ── UPDATE – set-instruction path (UpdateFromSet) ─────────────────────────

    [Fact]
    public void Update_SetInstruction_WithUpdLock_SqlContainsHint()
    {
        var sql = Sql(_context.UpdateUsersTable()
            .WithTableHints(StormTableHints.UpdLock)
            .Set(x => x.FullName, "test"));

        sql.Should().Contain("WITH (UPDLOCK)");
    }

    [Fact]
    public void Update_SetInstruction_HintSetDirectlyOnUpdateFromSet_SqlContainsHint()
    {
        // WithTableHints called AFTER .Set() — directly on IUpdateFromSet<T>
        var sql = Sql(_context.UpdateUsersTable()
            .Set(x => x.FullName, "test")
            .WithTableHints(StormTableHints.UpdLock));

        sql.Should().Contain("WITH (UPDLOCK)");
    }

    [Fact]
    public void Update_SetInstruction_HintPropagatesFromUpdateFromToUpdateFromSet()
    {
        // Hint is set on IUpdateFrom<T>, then .Set(col,val) creates UpdateFromSet
        // whose constructor must copy TableHints from the source
        var sql = Sql(_context.UpdateUsersTable()
            .WithTableHints(StormTableHints.UpdLock | StormTableHints.RowLock)
            .Set(x => x.FullName, "test"));

        sql.Should().Contain("UPDLOCK").And.Contain("ROWLOCK");
    }

    [Fact]
    public void Update_SetInstruction_HintAppearsAfterTableNameAndBeforeSet()
    {
        var sql = Sql(_context.UpdateUsersTable()
            .WithTableHints(StormTableHints.UpdLock)
            .Set(x => x.FullName, "test"));

        // Expected shape: UPDATE [dbo].[Users] WITH (UPDLOCK)\nSET ...
        var updatePos = sql.IndexOf("UPDATE", System.StringComparison.OrdinalIgnoreCase);
        var hintPos   = sql.IndexOf("WITH (UPDLOCK)", System.StringComparison.OrdinalIgnoreCase);
        var setPos    = sql.IndexOf("SET ", System.StringComparison.OrdinalIgnoreCase);

        updatePos.Should().BeGreaterThanOrEqualTo(0);
        hintPos.Should().BeGreaterThan(updatePos, "hint must come after UPDATE");
        setPos.Should().BeGreaterThan(hintPos,    "SET must come after hint");
    }

    [Fact]
    public void Update_SetInstruction_NoneHint_SqlContainsNoHintClause()
    {
        var sql = Sql(_context.UpdateUsersTable()
            .WithTableHints(StormTableHints.None)
            .Set(x => x.FullName, "test"));

        sql.Should().NotContain("WITH (");
    }

    // ── UPDATE – entity path (UpdateFrom → Set(T value)) ─────────────────────

    [Fact]
    public void Update_Entity_WithRowLock_SqlContainsHint()
    {
        var sql = Sql(_context.UpdateUsersTable()
            .WithTableHints(StormTableHints.RowLock)
            .WithoutConcurrencyCheck()
            .Set(DatabaseHelper.NewUser(1)));

        sql.Should().Contain("WITH (ROWLOCK)");
    }

    [Fact]
    public void Update_Entity_NoneHint_SqlContainsNoHintClause()
    {
        var sql = Sql(_context.UpdateUsersTable()
            .WithTableHints(StormTableHints.None)
            .WithoutConcurrencyCheck()
            .Set(DatabaseHelper.NewUser(1)));

        sql.Should().NotContain("WITH (");
    }

    // ── UPDATE – single (by PK) – UpdateFromSingle / UpdateFromSetSingle ──────

    [Fact]
    public void Update_ByPk_SetInstruction_WithRowLock_SqlContainsHint()
    {
        var sql = Sql(_context.UpdateUsersTable(1, 7)
            .WithTableHints(StormTableHints.RowLock)
            .Set(x => x.FullName, "test"));

        sql.Should().Contain("WITH (ROWLOCK)");
    }

    [Fact]
    public void Update_ByPk_HintPropagatesFromUpdateFromSingleToUpdateFromSetSingle()
    {
        var sql = Sql(_context.UpdateUsersTable(1, 7)
            .WithTableHints(StormTableHints.UpdLock | StormTableHints.RowLock)
            .Set(x => x.FullName, "test"));

        sql.Should().Contain("UPDLOCK").And.Contain("ROWLOCK");
    }

    [Fact]
    public void Update_ByPk_Entity_WithRowLock_SqlContainsHint()
    {
        var user = DatabaseHelper.NewUser(1);
        var sql = Sql(_context.UpdateUsersTable(1, 7)
            .WithTableHints(StormTableHints.RowLock)
            .Set(user));

        sql.Should().Contain("WITH (ROWLOCK)");
    }

    // ── DELETE – where-expression path (DeleteFrom) ───────────────────────────

    [Fact]
    public void Delete_Where_WithRowLock_SqlContainsHint()
    {
        var sql = Sql(_context.DeleteFromUsersTable()
            .WithTableHints(StormTableHints.RowLock)
            .Where(x => x.UserId == 1));

        sql.Should().Contain("WITH (ROWLOCK)");
    }

    [Fact]
    public void Delete_Where_HintAppearsAfterTableNameAndBeforeWhere()
    {
        var sql = Sql(_context.DeleteFromUsersTable()
            .WithTableHints(StormTableHints.RowLock)
            .Where(x => x.UserId == 1));

        // The Users table has detail tables (UserDates, UserCars, …) whose DELETE
        // statements also emit WHERE — they appear earlier in the SQL.
        // We search for the hint first, then look for WHERE *after* the hint,
        // which is the WHERE clause of the main-table DELETE.
        var hintPos  = sql.IndexOf("WITH (ROWLOCK)", System.StringComparison.OrdinalIgnoreCase);
        var wherePos = sql.IndexOf("WHERE", hintPos + 1, System.StringComparison.OrdinalIgnoreCase);

        hintPos.Should().BeGreaterThan(0, "WITH (ROWLOCK) hint must be present");
        wherePos.Should().BeGreaterThan(hintPos, "WHERE for the main table must come after the hint");
    }

    [Fact]
    public void Delete_Where_NoneHint_SqlContainsNoHintClause()
    {
        var sql = Sql(_context.DeleteFromUsersTable()
            .WithTableHints(StormTableHints.None)
            .Where(x => x.UserId == 1));

        sql.Should().NotContain("WITH (");
    }

    // ── DELETE – by PK (DeleteFromSingle) ─────────────────────────────────────

    [Fact]
    public void Delete_ByPk_WithRowLock_SqlContainsHint()
    {
        var sql = Sql(_context.DeleteFromUsersTable(1, 7)
            .WithTableHints(StormTableHints.RowLock));

        sql.Should().Contain("WITH (ROWLOCK)");
    }

    [Fact]
    public void Delete_ByPk_NoneHint_SqlContainsNoHintClause()
    {
        var sql = Sql(_context.DeleteFromUsersTable(1, 7)
            .WithTableHints(StormTableHints.None));

        sql.Should().NotContain("WITH (");
    }

    // ── DELETE – by entity (DeleteFromSingle) ────────────────────────────────

    [Fact]
    public void Delete_ByEntity_WithRowLock_SqlContainsHint()
    {
        var sql = Sql(_context.DeleteFromUsersTable(DatabaseHelper.NewUser(1))
            .WithTableHints(StormTableHints.RowLock));

        sql.Should().Contain("WITH (ROWLOCK)");
    }
}

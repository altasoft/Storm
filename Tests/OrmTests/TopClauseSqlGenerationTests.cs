using System.Collections.Generic;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Verifies that Top(n) on DELETE / UPDATE actually writes a TOP (n) clause
/// into the generated SQL string, instead of being silently ignored.
///
/// Uses GenerateBatchCommands() instead of GoAsync() — the full SQL-generation
/// pipeline runs without opening a database connection, so the CommandText of
/// every SqlBatchCommand can be inspected directly.
/// IClassFixture&lt;DatabaseFixture&gt; is kept only to guarantee StormManager is
/// initialized before the first test runs.
/// </summary>
public class TopClauseSqlGenerationTests : IClassFixture<DatabaseFixture>
{
    private readonly TestStormContext _context;

    public TopClauseSqlGenerationTests(DatabaseFixture fixture)
    {
        _context = new TestStormContext(fixture.ConnectionString);
    }

    private static string Sql(ISqlGo command)
    {
        var batch = new List<SqlBatchCommand>();
        command.GenerateBatchCommands(batch);
        batch.Should().NotBeEmpty("GenerateBatchCommands must produce at least one command");
        return batch[0].CommandText;
    }

    [Fact]
    public void Delete_Where_WithTop_SqlContainsTopClause()
    {
        var sql = Sql(_context.DeleteFromUsersTable()
            .Where(x => x.UserId == 1)
            .Top(100));

        sql.Should().Contain("TOP (100)", "Top(100) on a DELETE must be reflected in the generated SQL");
    }

    [Fact]
    public void Delete_Where_WithTop_TopClauseAppearsRightAfterDeleteFrom()
    {
        var sql = Sql(_context.DeleteFromUsersTable()
            .Where(x => x.UserId == 1)
            .Top(100));

        var deletePos = sql.IndexOf("DELETE FROM", System.StringComparison.OrdinalIgnoreCase);
        var topPos = sql.IndexOf("TOP (100)", System.StringComparison.OrdinalIgnoreCase);

        deletePos.Should().BeGreaterThanOrEqualTo(0);
        topPos.Should().BeGreaterThan(deletePos, "TOP clause must come after DELETE FROM");
    }

    [Fact]
    public void Update_SetInstruction_WithTop_SqlContainsTopClause()
    {
        var sql = Sql(_context.UpdateUsersTable()
            .Set(x => x.FullName, "test")
            .Top(100));

        sql.Should().Contain("TOP (100)", "Top(100) on an UPDATE must be reflected in the generated SQL");
    }
}

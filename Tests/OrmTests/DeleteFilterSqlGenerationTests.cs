using System;
using System.Collections.Generic;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Verifies that DeleteFrom*() generates a DELETE command for every kind of filter:
/// LINQ Where, OData Where, both combined, and no filter at all.
/// Previously an OData-only filter (or no filter) generated no command and GoAsync silently returned 0.
///
/// Uses GenerateBatchCommands() so no database connection is opened.
/// </summary>
public class DeleteFilterSqlGenerationTests : IClassFixture<DatabaseFixture>
{
    private readonly TestStormContext _context;

    public DeleteFilterSqlGenerationTests(DatabaseFixture fixture)
    {
        _context = new TestStormContext(fixture.ConnectionString);
    }

    private static SqlBatchCommand Command(ISqlGo command)
    {
        var batch = new List<SqlBatchCommand>();
        command.GenerateBatchCommands(batch);
        batch.Should().ContainSingle("a DELETE must always produce exactly one batch command");
        return batch[0];
    }

    /// <summary>Returns the SQL of the main-table DELETE (the last statement, after SET NOCOUNT OFF).</summary>
    private static string MainDelete(string sql)
    {
        var idx = sql.LastIndexOf("SET NOCOUNT OFF;", StringComparison.Ordinal);
        idx.Should().BeGreaterThanOrEqualTo(0);
        return sql[idx..];
    }

    [Fact]
    public void Delete_WithODataFilterOnly_GeneratesWhereClause()
    {
        var command = Command(_context.DeleteFromUsersTable().Where("UserId eq 5"));

        var main = MainDelete(command.CommandText);
        main.Should().Contain("DELETE FROM");
        main.Should().Contain("WHERE");
        main.Should().Contain("[Id]", "UserId maps to the [Id] column");
        command.Parameters.Count.Should().Be(1);
        command.Parameters[0].Value.Should().Be(5);
    }

    [Fact]
    public void Delete_WithODataFilterOnly_FiltersDetailTablesToo()
    {
        var sql = Command(_context.DeleteFromUsersTable().Where("UserId eq 5")).CommandText;

        var detailPart = sql[..sql.LastIndexOf("SET NOCOUNT OFF;", StringComparison.Ordinal)];
        var detailDeletes = detailPart.Split("DELETE FROM").Length - 1;
        var detailWheres = detailPart.Split("WHERE").Length - 1;

        detailDeletes.Should().BeGreaterThan(0, "User has detail tables");
        detailWheres.Should().Be(detailDeletes, "every detail-table DELETE must carry the OData filter. SQL: {0}", sql);
    }

    [Fact]
    public void Delete_WithExpressionFilter_FiltersEveryDetailTable()
    {
        // Regression: only the first detail table used to get the WHERE clause,
        // so the remaining detail tables were wiped completely.
        var sql = Command(_context.DeleteFromUsersTable().Where(x => x.UserId == 5)).CommandText;

        var detailPart = sql[..sql.LastIndexOf("SET NOCOUNT OFF;", StringComparison.Ordinal)];
        var detailDeletes = detailPart.Split("DELETE FROM").Length - 1;
        var detailWheres = detailPart.Split("WHERE").Length - 1;

        detailDeletes.Should().BeGreaterThan(1, "User has several detail tables");
        detailWheres.Should().Be(detailDeletes, "every detail-table DELETE must carry the filter. SQL: {0}", sql);
    }

    [Fact]
    public void Delete_WithExpressionAndODataFilter_GeneratesBothConditions()
    {
        var command = Command(_context.DeleteFromUsersTable()
            .Where(x => x.BranchId == 7)
            .Where("UserId eq 5"));

        var main = MainDelete(command.CommandText);
        main.Should().Contain("WHERE");
        main.Should().Contain("[BranchId]");
        main.Should().Contain(" AND ");
        main.Should().Contain("[Id]", "UserId maps to the [Id] column");
    }

    [Fact]
    public void Delete_WithODataFilterAndTop_GeneratesTopAndWhere()
    {
        var main = MainDelete(Command(_context.DeleteFromUsersTable().Where("UserId ge 5").Top(3)).CommandText);

        main.Should().Contain("TOP (3)");
        main.Should().Contain("WHERE");
    }

    [Fact]
    public void Delete_WithoutFilter_GeneratesUnfilteredDelete()
    {
        var command = Command(_context.DeleteFromUsersTable());

        command.CommandText.Should().Contain("DELETE FROM");
        command.CommandText.Should().NotContain("WHERE");
        command.Parameters.Count.Should().Be(0);
    }
}

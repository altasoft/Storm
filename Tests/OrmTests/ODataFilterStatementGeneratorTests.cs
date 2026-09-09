using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Helpers;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;
using StormDbParameter = Microsoft.Data.SqlClient.SqlParameter;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Verifies that OData $filter expressions are translated to parameterized SQL correctly,
/// specifically for DateOnly and TimeOnly properties (Edm.Date / Edm.TimeOfDay).
///
/// Mirrors SqlStatementGeneratorTests, but drives the OData filter generator
/// (ODataFilterStatementGenerator.GenerateSql) instead of LINQ where-expressions.
/// No database connection is opened - only the SQL-generation pipeline runs.
/// </summary>
public class ODataFilterStatementGeneratorTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public ODataFilterStatementGeneratorTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);

        StormManager.SetLogger(logger);

        _context = new TestStormContext(fixture.ConnectionString);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync().ConfigureAwait(false);

    private sealed class FakeCommand : IVirtualStormDbCommand
    {
        public readonly List<(string Name, UnifiedDbType DbType, int Size, object? Value)> Params = [];
        public string CommandText { get; set; } = string.Empty;
        public CommandType CommandType { get; set; }

        public string AddDbParameter(int paramIdx, StormColumnDef column, object? value)
        {
            var name = "@p" + paramIdx.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Params.Add((name, column.DbType, column.Size, value));
            return name;
        }

        public StormDbParameter AddDbParameter(string paramName, UnifiedDbType dbType, int size, object? value)
        {
            Params.Add((paramName, dbType, size, value));
            return default!;
        }

        public void AddDbParameters(List<StormCallParameter> callParameters) { }

        public void SetStormCommandBaseParameters(SqlConnection connection, SqlTransaction? transaction, string sql,
            QueryParameters queryParameters, CommandType commandType = CommandType.Text) => throw new NotImplementedException();

        public void SetStormCommandBaseParameters(string sql, QueryParameters queryParameters,
            CommandType commandType = CommandType.Text) => throw new NotImplementedException();

        public void SetStormCommandBaseParameters(SqlConnection connection, SqlTransaction? transaction) => throw new NotImplementedException();

        public string? GenerateCallParameters(List<StormCallParameter>? queryParametersCallParameters, CallParameterType type) => null;
    }

    private static (string sql, FakeCommand cmd) RunODataFilter(string filter, StormColumnDef[] columns)
    {
        var sb = new StringBuilder();
        var cmd = new FakeCommand();
        var idx = 0;
        ODataFilterStatementGenerator.GenerateSql(cmd, filter, columns, null, ref idx, sb);
        return (sb.ToString(), cmd);
    }

    [Fact]
    public void DateOnly_Equal_ShouldGenerateParameterAndSql()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var (sql, cmd) = RunODataFilter("DateValue eq 2024-05-17", cols);

        sql.Should().Be("[DateValue] = @p0");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].DbType.Should().Be(UnifiedDbType.Date);
        cmd.Params[0].Value.Should().Be(new DateOnly(2024, 5, 17));
    }

    [Fact]
    public void DateOnly_GreaterThan_ShouldGenerateParameterAndSql()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var (sql, cmd) = RunODataFilter("DateValue gt 2024-01-01", cols);

        sql.Should().Be("[DateValue] > @p0");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].Value.Should().Be(new DateOnly(2024, 1, 1));
    }

    [Fact]
    public void NullableDateOnly_EqualNull_ShouldGenerateIsNull()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var (sql, cmd) = RunODataFilter("DateValueN eq null", cols);

        sql.Should().Be("[DateValueN] IS NULL");
        cmd.Params.Should().BeEmpty();
    }

    [Fact]
    public void NullableDateOnly_NotEqualNull_ShouldGenerateIsNotNull()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var (sql, cmd) = RunODataFilter("DateValueN ne null", cols);

        sql.Should().Be("[DateValueN] IS NOT NULL");
        cmd.Params.Should().BeEmpty();
    }

    [Fact]
    public void TimeOnly_Equal_ShouldGenerateParameterAndSql()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var (sql, cmd) = RunODataFilter("TimeValue eq 13:45:30", cols);

        sql.Should().Be("[TimeValue] = @p0");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].DbType.Should().Be(UnifiedDbType.Time);
        cmd.Params[0].Value.Should().Be(new TimeOnly(13, 45, 30));
    }

    [Fact]
    public void TimeOnly_LessThan_ShouldGenerateParameterAndSql()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var (sql, cmd) = RunODataFilter("TimeValue lt 08:30:00", cols);

        sql.Should().Be("[TimeValue] < @p0");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].Value.Should().Be(new TimeOnly(8, 30, 0));
    }

    [Fact]
    public void NullableTimeOnly_EqualNull_ShouldGenerateIsNull()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var (sql, cmd) = RunODataFilter("TimeValueN eq null", cols);

        sql.Should().Be("[TimeValueN] IS NULL");
        cmd.Params.Should().BeEmpty();
    }

    [Fact]
    public void NullableTimeOnly_NotEqualNull_ShouldGenerateIsNotNull()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var (sql, cmd) = RunODataFilter("TimeValueN ne null", cols);

        sql.Should().Be("[TimeValueN] IS NOT NULL");
        cmd.Params.Should().BeEmpty();
    }
}

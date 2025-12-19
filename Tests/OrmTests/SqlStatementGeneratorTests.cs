using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Helpers;
using AltaSoft.Storm.Interfaces;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using StormDbParameter = Microsoft.Data.SqlClient.SqlParameter;

namespace AltaSoft.Storm.Tests;

public class SqlStatementGeneratorTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public SqlStatementGeneratorTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);

        StormManager.SetLogger(logger);

        _context = new TestStormContext(fixture.ConnectionString);
    }

    public Task InitializeAsync() => _context.GetConnection().OpenAsync();

    public async Task DisposeAsync() => await _context.DisposeAsync().ConfigureAwait(false);

    private interface IMyTest { int CustomerId { get; } }

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

        public void SetStormCommandBaseParameters(StormContext context, string sql, QueryParameters queryParameters, CommandType commandType = CommandType.Text) { }

        public string? GenerateCallParameters(List<StormCallParameter>? queryParametersCallParameters, CallParameterType type) => null;
    }

    private static (string sql, FakeCommand cmd) RunWhere<T>(IList<System.Linq.Expressions.Expression<Func<T, bool>>> expressions, StormColumnDef[] columns) where T : IDataBindable
    {
        var sb = new StringBuilder();
        var cmd = new FakeCommand();
        var idx = 0;
        SqlStatementGenerator.GenerateWhereSql(cmd, expressions.ToList(), columns, null, ref idx, sb);
        return (sb.ToString(), cmd);
    }

    [Fact]
    public void CastedMemberAccess_ShouldGenerateParameterAndSql()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => ((IMyTest)x).CustomerId == 5;

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("([CustomerId] = @p0)");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].Value.Should().Be(5);
    }

    [Fact]
    public void StringContains_ShouldGenerateLikeAndParameter()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.StringName != null && x.StringName.Contains("xyz");

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([StringName] IS NOT NULL) AND [StringName] LIKE '%'+@p0+'%')");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].Value.Should().Be("xyz");
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForNonNullableProperty()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { 1, 2, 3 };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntValue.In(1, 2, 3);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[IntValue] IN (@p0,@p1,@p2)");
        cmd.Params.Should().HaveCount(values.Length);
        cmd.Params.Select(p => p.Value).Should().Contain(values.Select(v => (object)v));
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForNonNullableProperty2()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { (int?)null, 1, 2, 3 };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntValue.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[IntValue] IN (NULL,@p0,@p1,@p2)");
        cmd.Params.Should().HaveCount(values.Length - 1);
        cmd.Params.Select(p => p.Value).Should().Contain(values.Where(x => x is not null).Select(v => (object?)v));
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForNullableProperty()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { 1, 2, 3 };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntValueN.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[IntValueN] IN (@p0,@p1,@p2)");
        cmd.Params.Should().HaveCount(values.Length);
        cmd.Params.Select(p => p.Value).Should().Contain(values.Select(v => (object)v));
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForNullableProperty2()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { (int?)null, 1, 2, 3 };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntValueN.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[IntValueN] IN (NULL,@p0,@p1,@p2)");
        cmd.Params.Should().HaveCount(values.Length - 1);
        cmd.Params.Select(p => p.Value).Should().Contain(values.Where(x => x is not null).Select(v => (object?)v));
    }

    [Fact]
    public void NullComparison_ShouldGenerateIsNull()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.StringName == null;

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("([StringName] IS NULL)");
        cmd.Params.Should().BeEmpty();
    }

    [Fact]
    public void StringStartsWith_ShouldGenerateLikeAndParameter()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.StringName != null && x.StringName.StartsWith("ab");

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([StringName] IS NOT NULL) AND [StringName] LIKE @p0+'%')");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].Value.Should().Be("ab");
    }

    [Fact]
    public void StringEndsWith_ShouldGenerateLikeAndParameter()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.StringName != null && x.StringName.EndsWith("yz");

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([StringName] IS NOT NULL) AND [StringName] LIKE '%'+@p0)");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].Value.Should().Be("yz");
    }

    [Fact]
    public void GenerateValueSql_MemberAccess_ShouldGenerateColumnName()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var sb = new StringBuilder();
        var cmd = new FakeCommand();
        var idx = 0;

        SqlStatementGenerator.GenerateValueSql<SqlWhereTestEntity>(cmd, x => x.IntValue, cols, null, ref idx, sb);

        sb.ToString().Should().Be("[IntValue]");
        cmd.Params.Should().BeEmpty();
    }

    [Fact]
    public void DomainPrimitiveCastToInt_ShouldGenerateParameterAndSql()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => (int)x.CustomerId == 2;

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("([CustomerId] = @p0)");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].Value.Should().Be(2);
    }

    [Fact]
    public void NullableDomainPrimitiveValueAccess_ShouldGenerateIsNotNullAndComparison()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.CustomerIdN != null && x.CustomerIdN.Value == 2;

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([CustomerIdN] IS NOT NULL) AND ([CustomerIdN] = @p0))");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].Value.Should().Be(2);
    }

    [Fact]
    public void NullableEnumBitwiseWithoutValue_ShouldGenerateIsNotNullAndBitwiseComparison()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntColorN != null && ((int)x.IntColorN & (int)RgbColor.Red) != 0;

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([IntColorN] IS NOT NULL) AND (([IntColorN] & @p0) <> @p1))");
        cmd.Params.Should().HaveCount(2);
        cmd.Params[0].Value.Should().Be((int)RgbColor.Red);
        cmd.Params[1].Value.Should().Be(0);
    }

    [Fact]
    public void NullableEnumBitwiseWithoutValue_ShouldGenerateIsNotNullAndBitwiseComparison2()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntColorN.HasValue;

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("([IntColorN] IS NOT NULL)");
        cmd.Params.Should().HaveCount(2);
        cmd.Params[0].Value.Should().Be((int)RgbColor.Red);
        cmd.Params[1].Value.Should().Be(0);
    }

    [Fact]
    public void NullableEnumBitwiseWithoutValue_ShouldGenerateIsNotNullAndBitwiseComparison3()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntColorN.HasValue && (x.IntColorN.Value.HasFlag(RgbColor.Red));

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([IntColorN] IS NOT NULL) AND (([IntColorN] & @p0) <> @p1))");
        cmd.Params.Should().HaveCount(2);
        cmd.Params[0].Value.Should().Be((int)RgbColor.Red);
        cmd.Params[1].Value.Should().Be(0);
    }

    [Fact]
    public void NullableEnumBitwiseWithValueProperty_ShouldGenerateIsNotNullAndBitwiseComparison()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntColorN != null && ((int)x.IntColorN.Value & (int)RgbColor.Red) != 0;

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([IntColorN] IS NOT NULL) AND (([IntColorN] & @p0) <> @p1))");
        cmd.Params.Should().HaveCount(2);
        cmd.Params[0].Value.Should().Be((int)RgbColor.Red);
        cmd.Params[1].Value.Should().Be(0);
    }

    [Fact]
    public void InExtension_EmptyList_ShouldGenerateFalseCondition()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = Array.Empty<int>();
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntValue.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("1=0");
        cmd.Params.Should().BeEmpty();
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParameters()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { 1, 2, 3 };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.IntValue.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[IntValue] IN (@p0,@p1,@p2)");
        cmd.Params.Should().HaveCount(values.Length);
        cmd.Params.Select(p => p.Value).Should().Contain(values.Select(v => (object)v));
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForStringProperty()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { "a", "b" };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.StringName.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[StringName] IN (@p0,@p1)");
        cmd.Params.Should().HaveCount(values.Length);
        cmd.Params.Select(p => p.Value).Should().Contain(values);
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForStringPropertyWithNull()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { null, "a", "b" };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.StringName.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[StringName] IN (NULL,@p0,@p1)");
        cmd.Params.Should().HaveCount(values.Length - 1);
        cmd.Params.Select(p => p.Value).Should().Contain(values.Where(x => x is not null));
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForDomainPrimitiveStringProperty()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { "a", "b" };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.Ccy.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[Ccy] IN (@p0,@p1)");
        cmd.Params.Should().HaveCount(values.Length);
        cmd.Params.Select(p => p.Value).Should().Contain(values);
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForDomainPrimitiveStringPropertyWithNull()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { null, "a", "b" };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.CcyN.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[CcyN] IN (NULL,@p0,@p1)");
        cmd.Params.Should().HaveCount(values.Length - 1);
        cmd.Params.Select(p => p.Value).Should().Contain(values.Where(x => x is not null));
    }

    [Fact]
    public void StringEqualsConcatenationWithNullable_ShouldGenerateComparison()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.StringName == x.StringNameN + "A";

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("([StringName] = ([StringNameN] + @p0))");
        cmd.Params.Should().HaveCount(1);
        cmd.Params[0].Value.Should().Be("A");
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForStringColorProperty()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { "Red", "Blue" }; 
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.StringColor.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[StringColor] IN (@p0,@p1)");
        cmd.Params.Should().HaveCount(values.Length);
        cmd.Params.Select(p => p.Value).Should().Contain(values);
    }

    [Fact]
    public void InExtension_ShouldGenerateInAndParametersForStringColorPropertyWithNull()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        var values = new[] { null, "Red" };
        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.StringColor.In(values);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("[StringColor] IN (NULL,@p0)");
        cmd.Params.Should().HaveCount(values.Length - 1);
        cmd.Params.Select(p => p.Value).Should().Contain(values.Where(x => x is not null));
    }

    [Fact]
    public void BooleanLogicalAnd_ShouldGenerateAndOperation()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.BoolValue && x.BoolValue;

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([BoolValue] = @p0) AND ([BoolValue] = @p1))");
        cmd.Params.Should().HaveCount(2);
        cmd.Params[0].Value.Should().Be(true);
        cmd.Params[1].Value.Should().Be(true);
    }

    [Fact]
    public void BooleanLogicalOr_ShouldGenerateOrOperation()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.BoolValue || x.BoolValue;

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([BoolValue] = @p0) OR ([BoolValue] = @p1))");
        cmd.Params.Should().HaveCount(2);
        cmd.Params[0].Value.Should().Be(true);
        cmd.Params[1].Value.Should().Be(true);
    }

    [Fact]
    public void NullableBooleanLogicalAnd_ShouldGenerateAndOperation()
    {
        var ctrl = StormControllerCache.Get<SqlWhereTestEntity>(0);
        var cols = ctrl.ColumnDefs;

        System.Linq.Expressions.Expression<Func<SqlWhereTestEntity, bool>> expr = x => x.BoolValueN != null && (x.BoolValueN.Value && x.BoolValue);

        var (sql, cmd) = RunWhere([expr], cols);

        sql.Should().Be("(([BoolValueN] IS NOT NULL) AND (([BoolValueN] = @p0) AND ([BoolValue] = @p1)))");
        cmd.Params.Should().HaveCount(2);
        cmd.Params[0].Value.Should().Be(true);
        cmd.Params[1].Value.Should().Be(true);
    }
}

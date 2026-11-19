using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Helpers;
using AltaSoft.Storm.Interfaces;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;
using StormDbParameter = Microsoft.Data.SqlClient.SqlParameter;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Regression tests for LINQ predicate reduction and OData $filter translation bugs:
/// nullable HasValue negation, captured-value folding, boolean comparisons, reversed comparisons,
/// LIKE wildcard escaping, parameter size truncation, multiple properties of the same enum type,
/// bare boolean predicates in OData and flat-object sub-property paths.
/// </summary>
public class WhereAndODataFixTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public WhereAndODataFixTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        StormManager.SetLogger(new XunitLogger<DatabaseFixture>(output));
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
            var name = "@p" + paramIdx.ToString(CultureInfo.InvariantCulture);
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

    private static (string sql, FakeCommand cmd) Where(Expression<Func<SqlWhereTestEntity, bool>> expr)
    {
        var sb = new StringBuilder();
        var cmd = new FakeCommand();
        var idx = 0;
        SqlStatementGenerator.GenerateWhereSql(cmd, [expr], StormControllerCache.Get<SqlWhereTestEntity>(0).ColumnDefs, null, ref idx, sb);
        return (sb.ToString(), cmd);
    }

    private static (string sql, FakeCommand cmd) OData<T>(string filter) where T : IDataBindable
    {
        var sb = new StringBuilder();
        var cmd = new FakeCommand();
        var idx = 0;
        ODataFilterStatementGenerator.GenerateSql(cmd, filter, StormControllerCache.Get<T>(0).ColumnDefs, null, ref idx, sb);
        return (sb.ToString(), cmd);
    }

    private Task<List<User>> AllUsersAsync() => _context.SelectFromUsersTable().ListAsync();

    // ---------- #1 !x.Nullable.HasValue ----------

    [Fact]
    public void NotNullableHasValue_ShouldGenerateIsNull()
    {
        var (sql, cmd) = Where(x => !x.IntValueN.HasValue);

        sql.Should().Be("([IntValueN] IS NULL)");
        cmd.Params.Should().BeEmpty();
    }

    // ---------- #2 !captured.HasValue ----------

    [Fact]
    public void NotCapturedHasValue_WhenNull_ShouldFoldToTrue()
    {
        int? f = null;
        var (sql, _) = Where(x => !f.HasValue || x.IntValue == 1);

        sql.Should().Be("1=1");
    }

    [Fact]
    public void NotCapturedHasValue_WhenSet_ShouldFoldToFalse()
    {
        int? f = 5;
        var (sql, cmd) = Where(x => !f.HasValue || x.IntValue == 1);

        sql.Should().Be("([IntValue] = @p0)");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be(1);
    }

    // ---------- #3 constant folding uses value equality ----------

    [Fact]
    public void CapturedConstantsEqual_ShouldFoldByValue()
    {
        int a = 1000, b = 1000;

        Where(x => a == b).sql.Should().Be("1=1");
        Where(x => a != b).sql.Should().Be("1=0");

        var s1 = "abc";
        var s2 = new string("abc".ToCharArray());
        Where(x => s1 == s2 || x.IntValue == 1).sql.Should().Be("1=1");
    }

    [Fact]
    public void CapturedConstantsEqual_ShouldFoldWithTypedSemantics()
    {
        // NaN == NaN is false in C# (object.Equals would say true and fold the predicate to 1=1)
        double a = double.NaN, b = double.NaN;
        Where(x => a == b).sql.Should().Be("1=0");
        Where(x => a != b).sql.Should().Be("1=1");

        float fa = float.NaN, fb = float.NaN;
        Where(x => fa == fb).sql.Should().Be("1=0");

        double? na = double.NaN, nb = double.NaN;
        Where(x => na == nb).sql.Should().Be("1=0");

        // Nullable semantics are preserved: null == null is true, null == value is false
        int? n1 = null, n2 = null, n3 = 5;
        Where(x => n1 == n2).sql.Should().Be("1=1");
        Where(x => n1 == n3).sql.Should().Be("1=0");
        Where(x => n1 != n3).sql.Should().Be("1=1");
    }

    [Fact]
    public async Task Delete_WithCapturedNaNEquality_DeletesNothing()
    {
        double a = double.NaN, b = double.NaN;

        var countBefore = await _context.SelectFromUsersTable().CountAsync();

        // Before the fix this folded to 1=1 and deleted every row
        var deleted = await _context.DeleteFromUsersTable().Where(x => a == b).GoAsync();

        deleted.Should().Be(0);
        (await _context.SelectFromUsersTable().CountAsync()).Should().Be(countBefore);
    }

    // ---------- #4 boolean comparisons ----------

    [Fact]
    public void BoolColumnEqualsFalse_ShouldCompareRawColumn()
    {
        var (sql, cmd) = Where(x => x.BoolValue == false);

        sql.Should().Be("([BoolValue] = @p0)");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be(false);
    }

    [Fact]
    public void BoolColumnEqualsCapturedBool_ShouldCompareRawColumn()
    {
        var b = false;
        var (sql, cmd) = Where(x => x.BoolValue == b);

        sql.Should().Be("([BoolValue] = @p0)");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be(false);
    }

    [Fact]
    public void ConstantFirst_BoolColumnComparison_ShouldCompareRawColumn()
    {
        var (sql, cmd) = Where(x => false == x.BoolValue);

        sql.Should().Be("([BoolValue] = @p0)");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be(false);
    }

    [Fact]
    public void BoolColumnNotEqualTrue_ShouldCompareRawColumn()
    {
        var (sql, cmd) = Where(x => x.BoolValue != true);

        sql.Should().Be("([BoolValue] <> @p0)");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be(true);
    }

    [Fact]
    public void HasValueEqualsFalse_ShouldGenerateIsNull()
    {
        Where(x => x.IntValueN.HasValue == false).sql.Should().Be("([IntValueN] IS NULL)");
        Where(x => x.IntValueN.HasValue != true).sql.Should().Be("([IntValueN] IS NULL)");
        Where(x => x.IntValueN.HasValue == true).sql.Should().Be("([IntValueN] IS NOT NULL)");
        Where(x => x.IntValueN.HasValue != false).sql.Should().Be("([IntValueN] IS NOT NULL)");
    }

    [Fact]
    public void ComparisonEqualsFalse_ShouldNegateComparison()
    {
        var (sql, cmd) = Where(x => (x.IntValue == 1) == false);

        sql.Should().Be("([IntValue] <> @p0)");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be(1);
    }

    [Fact]
    public void BoolColumnEqualsBoolColumn_ShouldCompareRawColumns()
    {
        // ReSharper disable once EqualExpressionComparison
        Where(x => x.BoolValue == x.BoolValue).sql.Should().Be("([BoolValue] = [BoolValue])");
        Where(x => x.BoolValueN == x.BoolValue).sql.Should().Be("([BoolValueN] = [BoolValue])");
    }

    [Fact]
    public async Task BoolColumnComparisons_ShouldExecuteAgainstDatabase()
    {
        var b = false;
        // Previously generated "(([JoinFailed] = @p0) = @p1)" which SQL Server rejects.
        await _context.SelectFromSysAdmin().Where(x => x.JoinFailed == false).ListAsync();
        await _context.SelectFromSysAdmin().Where(x => x.JoinFailed == b).ListAsync();
        await _context.SelectFromSysAdmin().Where(x => x.JoinFailed != true).ListAsync();
        await _context.SelectFromSysAdmin().Where(x => x.CustomerId2.HasValue == false).ListAsync();
    }

    // ---------- #5 OData reversed comparison ----------

    [Fact]
    public void OData_ConstantOnLeft_ShouldMirrorComparison()
    {
        OData<SqlWhereTestEntity>("5 lt IntValue").sql.Should().Be("[IntValue] > @p0");
        OData<SqlWhereTestEntity>("5 le IntValue").sql.Should().Be("[IntValue] >= @p0");
        OData<SqlWhereTestEntity>("5 gt IntValue").sql.Should().Be("[IntValue] < @p0");
        OData<SqlWhereTestEntity>("5 ge IntValue").sql.Should().Be("[IntValue] <= @p0");
    }

    [Fact]
    public async Task OData_ConstantOnLeft_ShouldReturnSameRowsAsLinq()
    {
        var all = await AllUsersAsync();
        var pivot = all.Select(x => (int)x.UserId).Order().ElementAt(all.Count / 2);

        var odata = await _context.SelectFromUsersTable().Where($"{pivot} lt UserId").ListAsync();

        odata.Select(x => (int)x.UserId).Order().Should().Equal(all.Where(x => x.UserId > pivot).Select(x => (int)x.UserId).Order());
    }

    // ---------- #6 LIKE wildcard escaping ----------

    [Fact]
    public void Linq_LikeValues_ShouldEscapeWildcards()
    {
        var (sql, cmd) = Where(x => x.StringName!.Contains("a_b%c[d"));

        sql.Should().Be("[StringName] LIKE '%'+@p0+'%'");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be("a[_]b[%]c[[]d");

        Where(x => x.StringName!.StartsWith("_")).cmd.Params[0].Value.Should().Be("[_]");
        Where(x => x.StringName!.EndsWith("%")).cmd.Params[0].Value.Should().Be("[%]");
    }

    [Fact]
    public void OData_LikeValues_ShouldEscapeWildcards()
    {
        var (sql, cmd) = OData<SqlWhereTestEntity>("contains(StringName,'a_b%c[d')");

        sql.Should().Be("[StringName] LIKE @p0");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be("%a[_]b[%]c[[]d%");

        OData<SqlWhereTestEntity>("startswith(StringName,'_')").cmd.Params[0].Value.Should().Be("[_]%");
        OData<SqlWhereTestEntity>("endswith(StringName,'%')").cmd.Params[0].Value.Should().Be("%[%]");
    }

    [Fact]
    public async Task Like_UnderscoreAndPercent_ShouldMatchLiterally()
    {
        var all = await AllUsersAsync();
        all.Should().NotBeEmpty();
        all.Should().OnlyContain(x => x.FullName != null && !x.FullName.Contains('_') && !x.FullName.Contains('%'));

        (await _context.SelectFromUsersTable().Where(x => x.FullName!.Contains("_")).ListAsync()).Should().BeEmpty();
        (await _context.SelectFromUsersTable().Where(x => x.FullName!.StartsWith("%")).ListAsync()).Should().BeEmpty();
        (await _context.SelectFromUsersTable().Where("contains(FullName,'_')").ListAsync()).Should().BeEmpty();
        (await _context.SelectFromUsersTable().Where("endswith(FullName,'%')").ListAsync()).Should().BeEmpty();

        // guard: normal LIKE still works
        (await _context.SelectFromUsersTable().Where(x => x.FullName!.Contains("unicode")).ListAsync()).Should().HaveCount(all.Count);
        (await _context.SelectFromUsersTable().Where("contains(FullName,'(˿)')").ListAsync()).Should().HaveCount(all.Count);
        (await _context.SelectFromUsersTable().Where("contains(CurrencyId,'US')").ListAsync()).Should().HaveCount(all.Count(x => x.CurrencyId.ToString().Contains("US")));
        (await _context.SelectFromUsersTable().Where("contains(CurrencyId,'_')").ListAsync()).Should().BeEmpty();
    }

    [Fact]
    public void OData_EscapedLikeValueOnFixedLengthColumn_ShouldNotBeTruncated()
    {
        var (sql, cmd) = OData<User>("contains(CurrencyId,'_')");

        sql.Should().Be("[CurrencyId] LIKE @p0");
        cmd.Params.Should().ContainSingle();
        cmd.Params[0].Value.Should().Be("%[_]%");
        cmd.Params[0].Size.Should().BeGreaterThanOrEqualTo(5);
        cmd.Params[0].DbType.Should().Be(UnifiedDbType.AnsiString);
    }

    // ---------- #7 OData parameter size ----------

    [Fact]
    public void OData_StringParameterLongerThanColumn_ShouldNotBeTruncated()
    {
        var (sql, cmd) = OData<User>("CurrencyId eq 'USDX'");

        sql.Should().Be("[CurrencyId] = @p0");
        cmd.Params.Should().ContainSingle();
        cmd.Params[0].Size.Should().BeGreaterThanOrEqualTo(4);
        cmd.Params[0].Value.Should().Be("USDX");
    }

    [Fact]
    public async Task OData_StringParameterLongerThanColumn_ShouldNotMatchPrefix()
    {
        (await AllUsersAsync()).Should().Contain(x => x.CurrencyId == "USD");

        (await _context.SelectFromUsersTable().Where("CurrencyId eq 'USDX'").ListAsync()).Should().BeEmpty();
    }

    // ---------- #8 several properties of the same enum type ----------

    [Fact]
    public void OData_FirstEnumProperty_Guard()
    {
        OData<SqlWhereTestEntity>("IntColor eq 'Red'").sql.Should().Be("[IntColor] = @p0");
    }

    [Fact]
    public void OData_SecondPropertyOfSameEnumType_ShouldBeFilterable()
    {
        var (sql, cmd) = OData<SqlWhereTestEntity>("IntColorN eq 'Red'");

        sql.Should().Be("[IntColorN] = @p0");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be((int)RgbColor.Red);
    }

    [Fact]
    public async Task OData_SecondPropertyOfSameEnumType_ShouldQueryDatabase()
    {
        var all = await AllUsersAsync();

        var result = await _context.SelectFromUsersTable().Where("NullableUserStatus eq 'Ok'").ListAsync();

        result.Should().HaveCount(all.Count(x => x.NullableUserStatus == UserStatus.Ok));
    }

    // ---------- #9 bare boolean predicates in OData ----------

    [Fact]
    public void OData_BareBoolean_ShouldBeComparedToTrue()
    {
        OData<SqlWhereTestEntity>("BoolValue").sql.Should().Be("[BoolValue] = 1");
        OData<SqlWhereTestEntity>("not BoolValue").sql.Should().Be("NOT([BoolValue] = 1)");
        OData<SqlWhereTestEntity>("not not BoolValue").sql.Should().Be("[BoolValue] = 1");
        OData<SqlWhereTestEntity>("BoolValue and IntValue eq 1").sql.Should().Be("[BoolValue] = 1 AND [IntValue] = @p0");
        OData<SqlWhereTestEntity>("IntValue eq 1 or BoolValueN").sql.Should().Be("[IntValue] = @p0 OR [BoolValueN] = 1");
    }

    [Fact]
    public void OData_BooleanComparison_Guard()
    {
        var (sql, cmd) = OData<SqlWhereTestEntity>("BoolValue eq true");

        sql.Should().Be("[BoolValue] = @p0");
        cmd.Params.Should().ContainSingle().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task OData_BareBoolean_ShouldExecuteAgainstDatabase()
    {
        await _context.SelectFromSysAdmin().Where("JoinFailed").ListAsync();
        await _context.SelectFromSysAdmin().Where("not JoinFailed").ListAsync();
        await _context.SelectFromSysAdmin().Where("JoinFailed and Sid eq 1").ListAsync();
    }

    // ---------- #10 flat-object sub-property paths ----------

    [Theory]
    [InlineData("DatePair/Date1")]
    [InlineData("DatePair.Date1")]
    [InlineData("datepair/date1")]
    public async Task OData_FlatObjectSubProperty_ShouldBeFilterable(string path)
    {
        var all = await AllUsersAsync();
        var date = all[0].DatePair.Date1;

        var result = await _context.SelectFromUsersTable()
            .Where($"{path} eq {date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}")
            .ListAsync();

        result.Should().NotBeEmpty();
        result.Should().HaveCount(all.Count(x => x.DatePair.Date1 == date));
    }

    [Fact]
    public async Task OData_FlatObjectSubProperty_OnDerivedTable_ShouldBeFilterable()
    {
        await _context.SelectFromSysAdmin().Where("DatePair/Date1 eq 2024-01-01").ListAsync();
    }

    [Fact]
    public void OData_FlatObjectPathInsideStringLiteral_ShouldNotBeRewritten()
    {
        var (_, cmd) = OData<User>("LoginName eq 'DatePair/Date1' or LoginName eq 'it''s DatePair.Date1'");

        cmd.Params.Should().HaveCount(2);
        cmd.Params[0].Value.Should().Be("DatePair/Date1");
        cmd.Params[1].Value.Should().Be("it's DatePair.Date1");
    }
}

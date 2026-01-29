using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.TestModels;
using Xunit;

namespace AltaSoft.Storm.Tests;

public class QueryHintsTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public QueryHintsTests(DatabaseFixture fixture)
    {
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task ListAsync_WithQueryHints_DoesNotThrow()
    {
        var hints = new QueryHints { MaxDop = 1, Fast = 10, KeepPlan = true };
        var rows = await _context.SelectFromCustomerProperties()
            .ListAsync(hints, CancellationToken.None);
        Assert.NotNull(rows);
    }

    [Fact]
    public async Task GetAsync_WithQueryHints_DoesNotThrow()
    {
        var hints = new QueryHints { Plan = QueryPlanHint.Recompile };
        var row = await _context.SelectFromCustomerProperties()
            .Where(x => x.Id == 1)
            .GetAsync(hints, CancellationToken.None);
        // Should not throw, row may be null
    }

    [Fact]
    public async Task CountAsync_WithQueryHints_DoesNotThrow()
    {
        var hints = new QueryHints { QueryTraceOn = 9481 };
        var count = await _context.SelectFromCustomerProperties()
            .CountAsync(hints, CancellationToken.None);
        Assert.True(count >= 0);
    }

    [Fact]
    public async Task ExistsAsync_WithQueryHints_DoesNotThrow()
    {
        var hints = new QueryHints { Join = QueryJoinHint.Loop };
        var exists = await _context.SelectFromCustomerProperties()
            .ExistsAsync(hints, CancellationToken.None);
        Assert.IsType<bool>(exists);
    }

    [Fact]
    public async Task ListAsync_Column_WithQueryHints_DoesNotThrow()
    {
        var hints = new QueryHints { ForceOrder = true };
        var rows = await _context.SelectFromCustomerProperties()
            .ListAsync(x => x.Value, hints, CancellationToken.None);
        Assert.NotNull(rows);
    }

    [Fact]
    public async Task GetAsync_Column_WithQueryHints_DoesNotThrow()
    {
        var hints = new QueryHints { MaxDop = 2 };
        var value = await _context.SelectFromCustomerProperties()
            .Where(x => x.Id == 1)
            .GetAsync(x => x.Value, "default", hints, CancellationToken.None);
        Assert.NotNull(value);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class CustomerPropsTestsSelects : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;
    private readonly List<CustomerProperty> customerProps;

    public CustomerPropsTestsSelects(DatabaseFixture fixture, ITestOutputHelper output)
    {
        customerProps = fixture.CustomerProperties;

        var logger = new XunitLogger<DatabaseFixture>(output);
        StormManager.SetLogger(logger);

        _context = new TestStormContext(fixture.ConnectionString);
    }

    public Task InitializeAsync() => _context.GetConnection().OpenAsync();

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task CheckCustomerPropsRowCount_ShouldBe5()
    {
        var rows = await _context
            .SelectFromCustomerProperties()
            .Where(x => x.Id == 1)
            .ListAsync(CancellationToken.None);

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task CheckCustomerPropsRowCount2_ShouldBe5()
    {
        var rows = await _context
            .SelectFromCustomerProperties()
            .Where(x => x.Id == 1)
            .ListAsync(x => x.Value, CancellationToken.None);

        Assert.Equal(2, rows.Count);
    }
}

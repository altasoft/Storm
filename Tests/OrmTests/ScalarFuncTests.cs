using AltaSoft.Storm.TestModels;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class ScalarFuncTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public ScalarFuncTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);
        StormManager.SetLogger(logger);

        _context = new TestStormContext(fixture.ConnectionString);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task ExecuteScalarFunction_ShouldReturnExpectedValues()
    {
        // Arrange
        const int userId = 1;
        const int branchId = 7;

        // Act & Assert 
        var customerId = await _context.ExecuteScalarFunc(userId, branchId).GetAsync();
        customerId.Should().Be(77);
    }
}

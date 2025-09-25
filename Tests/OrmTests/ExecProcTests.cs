using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class ExecProcTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public ExecProcTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
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
    public async Task ExecuteProc_ShouldReturnExpectedValues()
    {
        // Arrange
        const int userId = 1;

        // Act & Assert
        var result = await _context.ExecuteInputOutputProc(userId, 0).ExecuteAsync();

        result.Should().NotBeNull();
        result.Exception.Should().BeNull();
        result.RowsAffected.Should().Be(-1);
        result.ReturnValue.Should().Be(1);
        result.ResultValue.Should().Be(userId);
        result.Io.Should().Be(77);
    }
}

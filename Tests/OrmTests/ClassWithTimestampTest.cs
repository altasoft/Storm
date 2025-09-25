using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class ClassWithTimestampTest : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public ClassWithTimestampTest(DatabaseFixture fixture, ITestOutputHelper output)
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
    public async Task InsertSingleObject_ShouldAddCorrectly()
    {
        // Arrange
        var obj = new ClassWithTimestamp { Name = "1" };

        // Act
        var insertionResult = await _context.InsertIntoClassWithTimestamp().Values(obj).GoAsync();

        // Assert
        insertionResult.Should().Be(1);

        // Act
        var list = await _context.SelectFromClassWithTimestamp().ListAsync();

        // Assert
        list.Count.Should().Be(1);
    }

    [Fact]
    public async Task Select_WhereShouldReturn()
    {
        var ts = SqlRowVersion.Zero;

        // Act
        var list = await _context
            .SelectFromClassWithTimestamp()
            .Where(x => x.EventId < ts)
            .ListAsync();

        // Assert
        list.Count.Should().Be(0);
    }
}

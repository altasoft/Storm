using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using AltaSoft.Storm.TestModels.VeryBadNamespace;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using UserId = AltaSoft.Storm.TestModels.DomainTypes.UserId;

namespace AltaSoft.Storm.Tests;

public class UsersProcTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;
    private readonly List<User> _users;

    public UsersProcTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        _users = fixture.Users;

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
    public async Task GetUserProperties_ShouldReturnExpectedValues()
    {
        // Arrange
        UserId userId = 1;
        CustomerId customerId = 1;

        // Act & Assert
        var user = await _context.ExecuteUserProc(userId, customerId).OutputResultInto(out var output).GetAsync();
        CheckUser(user, _users[0]);

        output.Should().NotBeNull();
        output.RowsAffected.Should().Be(-1);
        output.ReturnValue.Should().Be(0);
        output.Io.Should().Be((CustomerId)77);

        // Act & Assert - Checking multiple properties
        var users = await _context.ExecuteUserProc(userId, customerId).ListAsync();
        CheckUsers(users, _users.ToArray());
    }

    private static void CheckUser(User? actual, User expected)
    {
        actual.Should().NotBeNull();

        actual!.UserId.Should().Be(expected.UserId);
        actual.BranchId.Should().Be(expected.BranchId);
        actual.AutoInc.Should().Be(expected.AutoInc);
        actual.FullName.Should().Be(expected.FullName);
        actual.LoginName.Should().Be(expected.LoginName);
        actual.DatePair.Date1.Should().Be(expected.DatePair.Date1);
        actual.DatePair.Date2.Should().Be(expected.DatePair.Date2);
        actual.CurrencyId.Should().Be(expected.CurrencyId);
        actual.CustomerId.Should().Be(expected.CustomerId);
        actual.CustomerId2.Should().Be(expected.CustomerId2);

        actual.Roles.Should().BeEquivalentTo(expected.Roles);

        actual.TwoValues?.I1.Should().Be(expected.TwoValues?.I1);
        actual.TwoValues?.I2.Should().Be(expected.TwoValues?.I2);

        actual.ListOfStrings?.OrderBy(x => x).Should().BeEquivalentTo(expected.ListOfStrings?.OrderBy(x => x));

        actual.ListOfIntegers?.OrderBy(x => x).Should().BeEquivalentTo(expected.ListOfIntegers?.OrderBy(x => x));

        actual.Cars?.OrderBy(x => x.CarId).Should().BeEquivalentTo(expected.Cars?.OrderBy(x => x.CarId));
    }

    private static void CheckUsers(List<User>? actual, User[] expected)
    {
        actual.Should().NotBeNull();

        actual!.Count.Should().Be(expected.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            CheckUser(actual[i], expected[i]);
        }
    }
}

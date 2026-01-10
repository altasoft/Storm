using AltaSoft.Storm.TestModels;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class UserTestsMerges : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public UserTestsMerges(DatabaseFixture fixture, ITestOutputHelper output)
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
    public async Task MergeNonExistingSingleUserUsingUpdateOrInsert_ShouldMergeUserCorrectly()
    {
        // Arrange
        var newUser = DatabaseHelper.NewUser(100);

        // Act
        var mergeResult = await _context.MergeIntoUsersTable().UpdateOrInsert(newUser).GoAsync();

        // Assert
        mergeResult.Should().Be(1);
        await CheckUserAsync(100, newUser);
    }

    [Fact]
    public async Task MergeNonExistingSingleUserUsingInsertOrUpdate_ShouldMergeUserCorrectly()
    {
        // Arrange
        var newUser = DatabaseHelper.NewUser(101);

        // Act
        var mergeResult = await _context.MergeIntoUsersTable().InsertOrUpdate(newUser).GoAsync();

        // Assert
        mergeResult.Should().Be(1);
        await CheckUserAsync(101, newUser);
    }

    [Fact]
    public async Task MergeFirstBatchOfMultipleUsers_ShouldAddAllUsersCorrectly()
    {
        // Arrange
        var usersToInsert = new[]
        {
            DatabaseHelper.NewUser(200),
            DatabaseHelper.NewUser(201),
            DatabaseHelper.NewUser(202)
        };

        // Act
        var mergeResult = await _context.MergeIntoUsersTable().UpdateOrInsert(usersToInsert).GoAsync();

        // Assert
        mergeResult.Should().Be(usersToInsert.Length);
        foreach (var user in usersToInsert)
        {
            await CheckUserAsync(user.UserId, user);
        }
    }

    [Fact]
    public async Task MergeSecondBatchOfMultipleUsers_ShouldAddAllUsersCorrectly()
    {
        // Arrange
        var usersToInsert = new[]
        {
            DatabaseHelper.NewUser(300),
            DatabaseHelper.NewUser(301),
            DatabaseHelper.NewUser(302)
        };

        // Act
        var mergeResult = await _context.MergeIntoUsersTable().InsertOrUpdate(usersToInsert).GoAsync();

        // Assert
        mergeResult.Should().Be(usersToInsert.Length);
        foreach (var user in usersToInsert)
        {
            await CheckUserAsync(user.UserId, user);
        }
    }

    private async Task CheckUserAsync(int userId, User expected)
    {
        var actual = await _context.SelectFromUsersTable(userId, 7).GetAsync();
        CheckUser(actual, expected);
    }

    // CheckUser method remains the same

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
}

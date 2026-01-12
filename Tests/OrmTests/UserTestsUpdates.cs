using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class UserTestsUpdates : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;
    private readonly List<User> _users;

    public UserTestsUpdates(DatabaseFixture fixture, ITestOutputHelper output)
    {
        _users = fixture.Users;

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
    public async Task UpdateUser_ModifyFullName_ShouldReflectChangeAndReturnSuccess()
    {
        // Arrange
        var userToUpdate = _users.Last();
        userToUpdate.FullName = "UpdatedFirstName";

        // Act
        var updateResult = await _context.UpdateUsersTable().WithoutConcurrencyCheck().Set(userToUpdate).GoAsync();

        // Assert
        updateResult.Should().Be(1);
        await AssertUserUpdated(userToUpdate.UserId, userToUpdate);
    }

    [Fact]
    public async Task UpdateUser_ModifyFullName_WithTrackChanges_ShouldReflectChangeAndReturnSuccess()
    {
        // Arrange
        var userToUpdate = await _context.SelectFromUsersTable(9, 7).WithTracking().GetAsync();
        userToUpdate.Should().NotBeNull();

        userToUpdate!.FullName = "UpdatedFirstName";

        // Act
        var updateResult = await _context.UpdateUsersTable().WithConcurrencyCheck().Set(userToUpdate).GoAsync();

        // Assert
        updateResult.Should().Be(1);
        await AssertUserUpdated(userToUpdate.UserId, userToUpdate);
    }

    [Fact]
    public async Task UpdateBatchOfUsers_VerifyEachUserUpdateAndCount()
    {
        // Arrange
        var usersToUpdate = new[] { _users[2], _users[3], _users[4] };

        // Act
        var updateResult = await _context.UpdateUsersTable().WithoutConcurrencyCheck().Set(usersToUpdate).GoAsync();

        // Assert
        updateResult.Should().Be(usersToUpdate.Length);
        for (var i = 0; i < usersToUpdate.Length; i++)
        {
            var user = usersToUpdate[i];
            await AssertUserUpdated(user.UserId, usersToUpdate[i]);
        }
    }

    [Fact]
    public async Task UpdateUser_WhenUserIdMatches_ShouldUpdateFullNameAndReturnSuccess()
    {
        // Arrange
        const int userIdToUpdate = 6;
        var userToUpdate = _users[5];
        userToUpdate.FullName = "UpdatedFirstName";

        // Act
        var updateResult = await _context.UpdateUsersTable().Set(x => x.FullName, "UpdatedFirstName").Where(x => x.UserId == userIdToUpdate).GoAsync();

        // Assert
        updateResult.Should().Be(1);
        await AssertUserUpdated(userIdToUpdate, userToUpdate);
    }

    [Fact]
    public async Task UpdateUser_WithNonExistentUserId_ShouldNotUpdateAnyRecord()
    {
        // Arrange
        const int userIdToUpdate = 16;

        // Act
        var updateResult = await _context.UpdateUsersTable().Set(x => x.FullName, "UpdatedFirstName").Where(x => x.UserId == userIdToUpdate).GoAsync();

        // Assert
        updateResult.Should().Be(0);
    }

    [Fact]
    public async Task UpdateUser_ByIdWithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        const int userIdToUpdate = 6;
        var userToUpdate = _users[5];
        userToUpdate.FullName = "UpdatedFirstName";

        // Act
        var updateResult = await _context.UpdateUsersTable(userIdToUpdate, 7).Set(x => x.FullName, "UpdatedFirstName").GoAsync();

        // Assert
        updateResult.Should().Be(1);
        await AssertUserUpdated(userIdToUpdate, userToUpdate);
    }

    [Fact]
    public async Task UpdateUser_ByIdWithNonExistentId_ShouldNotUpdateAnyRecord()
    {
        // Arrange
        const int userIdToUpdate = 16;

        // Act
        var updateResult = await _context.UpdateUsersTable(userIdToUpdate, 7).Set(x => x.FullName, "UpdatedFirstName").GoAsync();

        // Assert
        updateResult.Should().Be(0);
    }


    [Fact]
    public async Task UpdateUser_ByIdValueExpression_ShouldUpdateRecord()
    {
        // Arrange
        const int userIdToUpdate = 6;
        var userToUpdate = _users[5];
        userToUpdate.FullName += "2";

        // Act
        var updateResult = await _context.UpdateUsersTable(userIdToUpdate, 7).Set(x => x.FullName, x => x.FullName + "2").GoAsync();

        // Assert
        updateResult.Should().Be(1);
        await AssertUserUpdated(userIdToUpdate, userToUpdate);
    }


    private async Task AssertUserUpdated(int userId, User expected)
    {
        var actual = await _context.SelectFromUsersTable(userId, 7).GetAsync();
        actual.Should().NotBeNull();
        CheckUser(actual, expected);
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
}

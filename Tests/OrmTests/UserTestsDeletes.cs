using AltaSoft.Storm.TestModels;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class UserTestsDeletes : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;
    private readonly List<User> _users;

    public UserTestsDeletes(DatabaseFixture fixture, ITestOutputHelper output)
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
    public async Task DeleteSingleUser_ShouldRemoveUserAndReturnOne()
    {
        // Arrange
        var userToDelete = _users.Last();

        // Act
        var deletionResult = await _context.DeleteFromUsersTable(userToDelete).GoAsync();

        // Assert
        deletionResult.Should().Be(1);
        await AssertUserDeleted(userToDelete.UserId);
    }

    [Fact]
    public async Task DeleteMultipleUsers_ShouldRemoveUsersAndReturnCorrectCount()
    {
        // Arrange
        var usersToDelete = new[] { _users[2], _users[3], _users[4] };

        // Act
        var deletionResult = await _context.DeleteFromUsersTable(usersToDelete).GoAsync();

        // Assert
        deletionResult.Should().Be(usersToDelete.Length);
        foreach (var user in usersToDelete)
        {
            await AssertUserDeleted(user.UserId);
        }
    }

    [Fact]
    public async Task DeleteUserByCondition_ShouldRemoveUserWhenConditionIsMet()
    {
        // Arrange
        const int userIdToDelete = 6;

        // Act
        var deletionResult = await _context.DeleteFromUsersTable().Where(static x => x.UserId == userIdToDelete).GoAsync();

        // Assert
        deletionResult.Should().Be(1);
        await AssertUserDeleted(userIdToDelete);
    }

    [Fact]
    public async Task DeleteUserByCondition_ShouldNotRemoveUserWhenConditionIsNotMet()
    {
        // Arrange
        const int nonExistentUserId = 16;

        // Act
        var deletionResult = await _context.DeleteFromUsersTable().Where(static x => x.UserId == nonExistentUserId).GoAsync();

        // Assert
        deletionResult.Should().Be(0);
    }

    [Fact]
    public async Task DeleteUserBySpecifiedPrimaryKey_ShouldRemoveUser()
    {
        // Arrange
        const int userIdToDelete = 1;

        // Act
        var deletionResult = await _context.DeleteFromUsersTable(userIdToDelete, 7).GoAsync();

        // Assert
        deletionResult.Should().Be(1);
        await AssertUserDeleted(userIdToDelete);
    }

    [Fact]
    public async Task DeleteUserByNonExistentPrimaryKey_ShouldNotRemoveUser()
    {
        // Arrange
        const int userIdToDelete = 16;

        // Act
        var deletionResult = await _context.DeleteFromUsersTable(userIdToDelete, 7).GoAsync();

        // Assert
        deletionResult.Should().Be(0);
    }

    private async Task AssertUserDeleted(int userId)
    {
        var user = await _context.SelectFromUsersTable(userId, 7).GetAsync();
        user.Should().BeNull();
    }
}

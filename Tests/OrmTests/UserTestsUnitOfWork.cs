using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class UserTestsUnitOfWork : IClassFixture<DatabaseFixture>
{
    private readonly List<User> _users;
    private readonly DatabaseFixture _fixture;

    public UserTestsUnitOfWork(DatabaseFixture fixture, ITestOutputHelper output)
    {
        _users = fixture.Users;

        var logger = new XunitLogger<DatabaseFixture>(output);

        StormManager.SetLogger(logger);
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateUser_ModifyFullName_ShouldReflectChangeAndReturnSuccess()
    {
        // Arrange
        var user1ToUpdate = _users.Last();
        user1ToUpdate.FullName = "UpdatedFirstName";

        var user2ToUpdate = _users.First();
        user2ToUpdate.FullName = "UpdatedFirstName";

        using var uow = UnitOfWork.Create();

        await using var tx = await uow.BeginAsync(_fixture.ConnectionString, CancellationToken.None);

        var context = new TestStormContext(_fixture.ConnectionString);

        var updateResult1 = await context.UpdateUsersTable().WithoutConcurrencyCheck().Set(user1ToUpdate).GoAsync();

        var updateResult2 = await context.UpdateUsersTable().WithoutConcurrencyCheck().Set(user2ToUpdate).GoAsync();

        await tx.CompleteAsync(CancellationToken.None);

        // Assert
        updateResult1.Should().Be(1);
        updateResult2.Should().Be(1);
        await AssertUserUpdated(user1ToUpdate.UserId, user1ToUpdate);
        await AssertUserUpdated(user2ToUpdate.UserId, user2ToUpdate);
    }

    [Fact]
    public async Task UpdateBatchOfUsers_VerifyEachUserUpdateAndCount()
    {
        // Arrange
        var usersToUpdate = new[] { _users[2], _users[3], _users[4] };

        using var uow = UnitOfWork.Create();

        await using var tx = await uow.BeginAsync(_fixture.ConnectionString, CancellationToken.None);

        var context = new TestStormContext(_fixture.ConnectionString);

        // Act
        var updateResult = await context.UpdateUsersTable().WithoutConcurrencyCheck().Set(usersToUpdate).GoAsync();

        await tx.CompleteAsync(CancellationToken.None);

        // Assert
        updateResult.Should().Be(usersToUpdate.Length);
        for (var i = 0; i < usersToUpdate.Length; i++)
        {
            var user = usersToUpdate[i];
            await AssertUserUpdated(user.UserId, usersToUpdate[i]);
        }
    }

    private async Task AssertUserUpdated(int userId, User expected)
    {
        var context = new TestStormContext(_fixture.ConnectionString);

        var actual = await context.SelectFromUsersTable(userId, 7).GetAsync();
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

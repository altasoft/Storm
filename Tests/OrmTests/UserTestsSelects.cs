using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using UserId = AltaSoft.Storm.TestModels.DomainTypes.UserId;

namespace AltaSoft.Storm.Tests;

public class UserTestsSelects : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;
    private readonly List<User> _users;

    public UserTestsSelects(DatabaseFixture fixture, ITestOutputHelper output)
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
    public async Task TrackableObject_VerifyPropertyIsChangedForAbstractType()
    {
        var user = await _context.SelectFromUsersTable().OrderBy(User.OrderByKey).GetAsync();
        user!.TrackableObject = new DerivedTrackableObject("x", null, 1, null, 7);
        user.StartChangeTracking();

        //user.TrackableObject.IntValue = 2;
        user.TrackableObject.CustomerIdValue = 8;
        user.TrackableObject.StrValue = "Z";
        var set = user.__GetChangedPropertyNames();
        set.Should().ContainSingle();
        set.First().Should().Be(nameof(User.TrackableObject));
    }

    [Fact]
    public async Task ChangeTrackingForDerivedType_ShouldTrackChanges()
    {
        //Arrange
        var newUser = DatabaseHelper.NewSysAdmin(100);

        //Act
        await _context.InsertIntoSysAdmin().Values(newUser).GoAsync();

        var sysAdmin = await _context.SelectFromSysAdmin().WithNoTracking().GetAsync();
        sysAdmin.Should().NotBeNull();

        sysAdmin.StartChangeTracking();
        sysAdmin.Sid = 321;
        sysAdmin.BranchId = 2222;
        var set = sysAdmin.__GetChangedPropertyNames().ToList();
        set.Should().HaveCount(2);
        set[0].Should().Be(nameof(SysAdmin.Sid));
        set[1].Should().Be(nameof(SysAdmin.BranchId));
    }

    [Fact]
    public async Task CountUsers_ShouldReturnCorrectCounts()
    {
        await AssertUserCountAsync(10);
        await AssertUserCountAsync(countCondition: x => x.UserId > 5, expectedCount: 5);
        await AssertUserCountAsync(countCondition: x => x.UserId < 5, expectedCount: 4);
        await AssertUserCountAsync(countCondition: x => x.UserId > 10, expectedCount: 0);
        await AssertUserCountAsync(userIds: [2, 7], expectedCount: 1);
        await AssertUserCountAsync(userIds: [3, 8], expectedCount: 0);
    }

    [Fact]
    public async Task CheckUserExistence_ShouldReturnCorrectExistence()
    {
        await AssertUserExistenceAsync(true);
        await AssertUserExistenceAsync(existenceCondition: x => x.UserId > 5, exists: true);
        await AssertUserExistenceAsync(existenceCondition: x => x.UserId < 5, exists: true);
        await AssertUserExistenceAsync(existenceCondition: x => x.UserId > 10, exists: false);
        await AssertUserExistenceAsync(userIds: [2, 7], exists: true);
        await AssertUserExistenceAsync(userIds: [3, 8], exists: false);
    }

    [Fact]
    public async Task RetrieveSpecificUsers_ShouldReturnCorrectUsers()
    {
        await AssertUserListAsync(x => x.UserId == 5, [_users[4]]);
        await AssertUserListAsync(x => x.UserId > 5, _users.Skip(5).ToArray());
        await AssertUserListAsync(x => x.UserId < 5, _users.Take(4).ToArray());
        await AssertUserListAsync(x => x.UserId > 10, []);
    }

    [Fact]
    public async Task SelectUsers_EnumInWhereCondition_ShouldNotThrowException()
    {
        _ = await _context.SelectFromUsersTable()
            .Where(x => x.UserStatus == UserStatus.Ok && x.NullableUserStatus != null).ListAsync();
    }

    [Fact]
    public async Task SelectUsers_WhereContainsNumbers_ShouldReturnExpectedResult()
    {
        var c = new List<UserId>() { 1, 2, 3, 4 };

        var users = await _context.SelectFromUsersTable()
            .Where(x => x.UserId.In(c)).ListAsync();

        users.Count.Should().Be(4);
    }

    [Fact]
    public async Task SelectUsers_WhereContainsNumbers_ShouldReturnExpectedResult2()
    {
        var c = new UserId[] { 1, 2, 3, 4 };

        var users = await _context.SelectFromUsersTable()
            .Where(x => x.UserId.In(c)).ListAsync();

        users.Count.Should().Be(4);
    }

    [Fact]
    public async Task SelectUsers_WithODataFilter_ShouldReturnExpectedResult()
    {
        var result = await _context.SelectFromUsersTable()
            .Where("UserId eq 2").ListAsync();
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetUserDetails_ShouldReturnExpectedUserOrNull()
    {
        var user = await _context.SelectFromUsersTable(5, 7).GetAsync();
        CheckUser(user, _users[4]);

        user = await _context.SelectFromUsersTable(3, 8).GetAsync();
        user.Should().BeNull();
    }

    [Fact]
    public async Task GetUserProperties_ShouldReturnExpectedValues()
    {
        //_context.SelectFromUsersTable().Prepare().GetAsync();
        // Arrange
        const int userId = 5;
        const int branchIdForUser = 7;
        const int nonExistentBranchId = 8;
        const string expectedCurrencyId = "USD";

        // Act & Assert - Checking Branch ID for existing user
        var (branchId, found) = await _context.SelectFromUsersTable(userId, branchIdForUser).WithTableHints(StormTableHints.NoLock).GetAsync(x => x.BranchId);
        found.Should().BeTrue("the user should be found in the database");
        branchId.Should().Be(branchIdForUser, "the branch ID should match the expected value for existing user");

        // Act & Assert - Checking Branch ID for non-existent user
        (branchId, found) = await _context.SelectFromUsersTable(userId, nonExistentBranchId).GetAsync(x => x.BranchId);
        found.Should().BeFalse("the user should not be found in the database");
        branchId.Should().Be(0, "the branch ID should be 0 for a non-existent user");

        // Act & Assert - Checking multiple properties
        var userProperties = await _context.SelectFromUsersTable(userId, branchIdForUser).GetAsync(x => x.BranchId, x => x.AutoInc);
        userProperties.Should().NotBeNull();
        userProperties.Value.Item1.Should().Be(branchIdForUser, "the first property (BranchId) should match the expected value");
        userProperties.Value.Item2.Should().Be(userId, "the second property (AutoInc) should match the user ID");

        // Act & Assert - Checking Currency ID
        var (currencyId, found2) = await _context.SelectFromUsersTable(userId, branchIdForUser).GetAsync(x => x.CurrencyId);
        found2.Should().BeTrue("the user should be found in the database");
        currencyId.Should().NotBeNull().And.Be(expectedCurrencyId, "the currency ID should match the expected value");
    }

    [Fact]
    public async Task ListUser_WithBasicPartialLoadFlags_ShouldLoadSpecificFields()
    {
        // Arrange
        const int expectedUserCount = 10;
        const User.PartialLoadFlags partialLoadFlag = User.PartialLoadFlags.Basic;

        // Act
        var userList = await _context.SelectFromUsersTable().Partially(partialLoadFlag).OrderBy(User.OrderByKey).ListAsync();

        // Assert
        userList.Should().HaveCount(expectedUserCount, "the user list should contain the expected number of users");
        foreach (var user in userList)
        {
            AssertUserWithBasicPartialLoad(user);
        }

        var singleUser = await _context.SelectFromUsersTable().Partially(partialLoadFlag).OrderBy(User.OrderByKey).GetAsync();
        singleUser.Should().NotBeNull();
        AssertUserWithBasicPartialLoad(singleUser);
    }

    private static void AssertUserWithBasicPartialLoad(User user)
    {
        user.LoginName.Should().NotBeNull("LoginName should be loaded with Basic partial load flags");
        user.DatePair.Should().NotBeNull("DatePair should be loaded with Basic partial load flags");

        user.Dates.Should().BeNull("Dates should not be loaded with Basic partial load flags");
        user.TwoValues.Should().BeNull("TwoValues should not be loaded with Basic partial load flags");
        user.FullName.Should().BeNull("FullName should not be loaded with Basic partial load flags");
        user.Roles.Should().BeNull("Roles should not be loaded with Basic partial load flags");
        user.Cars.Should().BeNull("Cars should not be loaded with Basic partial load flags");
        user.ListOfIntegers.Should().BeNull("ListOfIntegers should not be loaded with Basic partial load flags");
        user.ListOfStrings.Should().BeNull("ListOfStrings should not be loaded with Basic partial load flags");
    }

    [Fact]
    public async Task ListUser_WithSelectedPartialLoadFlags_ShouldLoadSpecifiedFields()
    {
        // Arrange
        const int expectedUserCount = 10;
        const User.PartialLoadFlags partialLoadFlags = User.PartialLoadFlags.FullName | User.PartialLoadFlags.Roles | User.PartialLoadFlags.Cars;

        // Act
        var userList = await _context.SelectFromUsersTable().Partially(partialLoadFlags).OrderBy(User.OrderByKey).ListAsync();

        // Assert
        userList.Should().HaveCount(expectedUserCount, "the user list should contain the expected number of users");
        foreach (var user in userList)
        {
            AssertUserWithSelectedPartialLoad(user);
        }

        var singleUser = await _context.SelectFromUsersTable().Partially(partialLoadFlags).OrderBy(User.OrderByKey).GetAsync();
        singleUser.Should().NotBeNull();
        AssertUserWithSelectedPartialLoad(singleUser);
    }

    private static void AssertUserWithSelectedPartialLoad(User user)
    {
        user.LoginName.Should().NotBeNull("LoginName should always be loaded");
        user.DatePair.Should().NotBeNull("DatePair should always be loaded");

        user.Dates.Should().BeNull("Dates should not be loaded with the selected partial load flags");
        user.TwoValues.Should().BeNull("TwoValues should not be loaded with the selected partial load flags");

        user.FullName.Should().NotBeNull("FullName should be loaded as part of the selected partial load flags");
        user.Roles.Should().NotBeNull("Roles should be loaded as part of the selected partial load flags");
        user.Cars.Should().NotBeNullOrEmpty("Cars should be loaded as part of the selected partial load flags");

        user.ListOfIntegers.Should().BeNull("ListOfIntegers should not be loaded with the selected partial load flags");
        user.ListOfStrings.Should().BeNull("ListOfStrings should not be loaded with the selected partial load flags");
    }

    private async Task AssertUserCountAsync(int expectedCount, Expression<Func<User, bool>>? countCondition = null, short[]? userIds = null)
    {
        int count;
        if (userIds is not null)
        {
            count = await _context.SelectFromUsersTable(userIds[0], userIds[1]).CountAsync();
        }
        else if (countCondition is not null)
        {
            count = await _context.SelectFromUsersTable().Where(countCondition).CountAsync();
        }
        else
        {
            count = await _context.SelectFromUsersTable().CountAsync();
        }

        count.Should().Be(expectedCount);
    }

    private async Task AssertUserExistenceAsync(bool exists, Expression<Func<User, bool>>? existenceCondition = null, short[]? userIds = null)
    {
        bool result;
        if (userIds != null)
        {
            result = await _context.SelectFromUsersTable(userIds[0], userIds[1]).ExistsAsync();
        }
        else if (existenceCondition != null)
        {
            result = await _context.SelectFromUsersTable().Where(existenceCondition).ExistsAsync();
        }
        else
        {
            result = await _context.SelectFromUsersTable().ExistsAsync();
        }

        result.Should().Be(exists);
    }

    private async Task AssertUserListAsync(Expression<Func<User, bool>> listCondition, User[] expectedUsers)
    {
        var userList = await _context.SelectFromUsersTable().Where(listCondition).ListAsync();
        userList.Should().HaveCount(expectedUsers.Length);
        CheckUsers(userList, expectedUsers);
    }

    private static void CheckUser(User? actual, User expected)
    {
        actual.Should().NotBeNull();

        actual.UserId.Should().Be(expected.UserId);
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

        actual.Count.Should().Be(expected.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            CheckUser(actual[i], expected[i]);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class UserVirtualViewTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;
    private readonly List<User> _users;

    public UserVirtualViewTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        _users = fixture.Users.Where(x => x.UserId > 5).ToList();

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
    public async Task CountUsers_ShouldReturnCorrectCounts()
    {
        await AssertUserCountAsync(expectedCount: 5);
        await AssertUserCountAsync(countCondition: x => x.UserId > 5, expectedCount: 5);
        await AssertUserCountAsync(countCondition: x => x.UserId < 5, expectedCount: 0);
        await AssertUserCountAsync(countCondition: x => x.UserId > 10, expectedCount: 0);
    }

    [Fact]
    public async Task CheckUserExistence_ShouldReturnCorrectExistence()
    {
        await AssertUserExistenceAsync(exists: true);
        await AssertUserExistenceAsync(existenceCondition: x => x.UserId > 5, exists: true);
        await AssertUserExistenceAsync(existenceCondition: x => x.UserId < 5, exists: false);
        await AssertUserExistenceAsync(existenceCondition: x => x.UserId > 10, exists: false);
    }

    [Fact]
    public async Task GetUser_Properties_ShouldReturnExpectedValues()
    {
        // Arrange
        const int userId = 6;
        const int branchIdForUser = 7;
        const string expectedCurrencyId = "USD";

        // Act & Assert - Checking Branch ID for existing user
        var r = await _context.SelectFromUsersVirtualView(userId, branchIdForUser).GetAsync(x => x.BranchId);
        r.RowFound.Should().BeTrue(because: "the user should be found in the database");
        r.Value.Should().Be(branchIdForUser, because: "the branch ID should match the expected value for existing user");

        // Act & Assert - Checking multiple properties
        var userProperties = await _context.SelectFromUsersVirtualView(userId, branchIdForUser).GetAsync(x => x.BranchId, x => x.AutoInc);
        userProperties.Should().NotBeNull();
        userProperties!.Value.Item1.Should().Be(branchIdForUser, because: "the first property (BranchId) should match the expected value");
        userProperties.Value.Item2.Should().Be(userId, because: "the second property (AutoInc) should match the user ID");

        // Act & Assert - Checking Currency ID
        var r2 = await _context.SelectFromUsersVirtualView(userId, branchIdForUser).GetAsync(x => x.CurrencyId);
        r2.RowFound.Should().BeTrue(because: "the user should be found in the database");
        r2.Value.Should().NotBeNull().And.Be(expectedCurrencyId, because: "the currency ID should match the expected value");
    }

    [Fact]
    public async Task GetUser2_Properties_ShouldReturnExpectedValues()
    {
        // Arrange
        const int userId = 6;
        const int branchIdForUser = 7;
        const string expectedCurrencyId = "USD";
        const string customSql = "SELECT * FROM dbo.Users WHERE Id > @min_user_id";

        var callParams = new List<StormCallParameter>(10)
        {
            new ("@min_user_id", UnifiedDbType.Int32, 5)
        };

        // Act & Assert - Checking Branch ID for existing user
        var r = await _context.SelectFromUsersCustomSql(userId, branchIdForUser, customSql, callParams).GetAsync(x => x.BranchId);
        r.RowFound.Should().BeTrue(because: "the user should be found in the database");
        r.Value.Should().Be(branchIdForUser, because: "the branch ID should match the expected value for existing user");

        // Act & Assert - Checking multiple properties
        var userProperties = await _context.SelectFromUsersCustomSql(userId, branchIdForUser, customSql, callParams).GetAsync(x => x.BranchId, x => x.AutoInc);
        userProperties.Should().NotBeNull();
        userProperties!.Value.Item1.Should().Be(branchIdForUser, because: "the first property (BranchId) should match the expected value");
        userProperties.Value.Item2.Should().Be(userId, because: "the second property (AutoInc) should match the user ID");

        // Act & Assert - Checking Currency ID
        var r2 = await _context.SelectFromUsersCustomSql(userId, branchIdForUser, customSql, callParams).GetAsync(x => x.CurrencyId);
        r2.RowFound.Should().BeTrue(because: "the user should be found in the database");
        r2.Value.Should().NotBeNull().And.Be(expectedCurrencyId, because: "the currency ID should match the expected value");
    }

    [Fact]
    public async Task ListUser_WithBasicPartialLoadFlags_ShouldLoadSpecificFields()
    {
        // Arrange
        const int expectedUserCount = 5;
        const User.PartialLoadFlags partialLoadFlag = User.PartialLoadFlags.Basic;

        // Act
        var userList = await _context.SelectFromUsersVirtualView().Partially(partialLoadFlag).OrderBy(User.OrderByKey).ListAsync();

        // Assert
        userList.Should().HaveCount(expectedUserCount, because: "the user list should contain the expected number of users");
        foreach (var user in userList)
        {
            AssertUserWithBasicPartialLoad(user);
        }

        var singleUser = await _context.SelectFromUsersVirtualView().Partially(partialLoadFlag).OrderBy(User.OrderByKey).GetAsync();
        singleUser.Should().NotBeNull();
        AssertUserWithBasicPartialLoad(singleUser!);
    }

    [Fact]
    public async Task ListUser2_WithBasicPartialLoadFlags_ShouldLoadSpecificFields()
    {
        // Arrange
        const int expectedUserCount = 5;
        const User.PartialLoadFlags partialLoadFlag = User.PartialLoadFlags.Basic;
        const string customSql = "SELECT * FROM dbo.Users WHERE Id > 5";

        // Act
        var userList = await _context.SelectFromUsersCustomSql(customSql, null).Partially(partialLoadFlag).OrderBy(User.OrderByKey).ListAsync();

        // Assert
        userList.Should().HaveCount(expectedUserCount, because: "the user list should contain the expected number of users");
        foreach (var user in userList)
        {
            AssertUserWithBasicPartialLoad(user);
        }

        var singleUser = await _context.SelectFromUsersCustomSql(customSql).Partially(partialLoadFlag).OrderBy(User.OrderByKey).GetAsync();
        singleUser.Should().NotBeNull();
        AssertUserWithBasicPartialLoad(singleUser!);
    }

    private static void AssertUserWithBasicPartialLoad(User user)
    {
        user.LoginName.Should().NotBeNull(because: "LoginName should be loaded with Basic partial load flags");
        user.DatePair.Should().NotBeNull(because: "DatePair should be loaded with Basic partial load flags");

        user.Dates.Should().BeNull(because: "Dates should not be loaded with Basic partial load flags");
        user.TwoValues.Should().BeNull(because: "TwoValues should not be loaded with Basic partial load flags");
        user.FullName.Should().BeNull(because: "FullName should not be loaded with Basic partial load flags");
        user.Roles.Should().BeNull(because: "Roles should not be loaded with Basic partial load flags");
        user.Cars.Should().BeNull(because: "Cars should not be loaded with Basic partial load flags");
        user.ListOfIntegers.Should().BeNull(because: "ListOfIntegers should not be loaded with Basic partial load flags");
        user.ListOfStrings.Should().BeNull(because: "ListOfStrings should not be loaded with Basic partial load flags");
    }

    [Fact]
    public async Task ListUser_WithSelectedPartialLoadFlags_ShouldLoadSpecifiedFields()
    {
        // Arrange
        const int expectedUserCount = 5;
        const User.PartialLoadFlags partialLoadFlags = User.PartialLoadFlags.FullName;

        // Act
        var userList = await _context.SelectFromUsersVirtualView().Partially(partialLoadFlags).OrderBy(User.OrderByKey).ListAsync();

        // Assert
        userList.Should().HaveCount(expectedUserCount, because: "the user list should contain the expected number of users");
        foreach (var user in userList)
        {
            AssertUserWithSelectedPartialLoad(user);
        }

        var singleUser = await _context.SelectFromUsersVirtualView().Partially(partialLoadFlags).OrderBy(User.OrderByKey).GetAsync();
        singleUser.Should().NotBeNull();
        AssertUserWithSelectedPartialLoad(singleUser!);
    }

    private static void AssertUserWithSelectedPartialLoad(User user)
    {
        user.LoginName.Should().NotBeNull(because: "LoginName should always be loaded");
        user.DatePair.Should().NotBeNull(because: "DatePair should always be loaded");

        user.Dates.Should().BeNull(because: "Dates should not be loaded with the selected partial load flags");
        user.TwoValues.Should().BeNull(because: "TwoValues should not be loaded with the selected partial load flags");

        user.FullName.Should().NotBeNull(because: "FullName should be loaded as part of the selected partial load flags");

        user.ListOfIntegers.Should().BeNull(because: "ListOfIntegers should not be loaded with the selected partial load flags");
        user.ListOfStrings.Should().BeNull(because: "ListOfStrings should not be loaded with the selected partial load flags");
    }

    private async Task AssertUserCountAsync(int expectedCount, Expression<Func<User, bool>>? countCondition = null)
    {
        int count;
        if (countCondition is not null)
        {
            count = await _context.SelectFromUsersVirtualView().Where(countCondition).CountAsync();
        }
        else
        {
            count = await _context.SelectFromUsersVirtualView().CountAsync();
        }
        count.Should().Be(expectedCount);
    }

    private async Task AssertUserExistenceAsync(bool exists, Expression<Func<User, bool>>? existenceCondition = null)
    {
        bool result;
        if (existenceCondition != null)
        {
            result = await _context.SelectFromUsersVirtualView().Where(existenceCondition).ExistsAsync();
        }
        else
        {
            result = await _context.SelectFromUsersVirtualView(6, 7).ExistsAsync();
        }
        result.Should().Be(exists);
    }

    [Fact]
    public async Task RetrieveUsers_ShouldReturnCorrectUsers()
    {
        var userList = await _context.SelectFromUsersVirtualView().ListAsync();
        userList.Should().HaveCount(_users.Count);
        CheckUsers(userList, _users.ToArray());

        userList = await _context.SelectFromUsersVirtualView().OrderBy(User.OrderBy.UserId_Desc).ListAsync();
        userList.Should().HaveCount(_users.Count);
        CheckUsers(userList, _users.OrderByDescending(x => x.UserId).ToArray());
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// End-to-end (real database) verification that DeleteFrom*().GoAsync() actually deletes rows when
/// filtered with an OData filter only, combined with a LINQ filter, or with no filter at all.
/// Previously these cases executed nothing and returned 0.
/// </summary>
public class DeleteFilterIntegrationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private const int FirstUserId = 2000;
    private const int TotalUsers = 10;

    private readonly TestStormContext _context;

    public DeleteFilterIntegrationTests(DatabaseFixture fixture)
    {
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        var users = new List<User>(TotalUsers);
        for (var i = 0; i < TotalUsers; i++)
            users.Add(DatabaseHelper.NewUser(FirstUserId + i));

        await _context.InsertIntoUsersTable().Values(users).GoAsync();
    }

    private async Task<List<int>> TestUserIdsAsync()
    {
        var users = await _context.SelectFromUsersTable()
            .Where(x => x.UserId >= FirstUserId)
            .ListAsync();
        return users.Select(x => (int)x.UserId).Order().ToList();
    }

    [Fact]
    public async Task Delete_WithODataFilterOnly_DeletesMatchingRows()
    {
        var deleted = await _context.DeleteFromUsersTable()
            .Where($"UserId ge {FirstUserId} and UserId lt {FirstUserId + 4}")
            .GoAsync();

        deleted.Should().Be(4);
        (await TestUserIdsAsync()).Should().Equal(Enumerable.Range(FirstUserId + 4, TotalUsers - 4));
    }

    [Fact]
    public async Task Delete_WithODataFilterAndTop_DeletesAtMostTopRows()
    {
        var deleted = await _context.DeleteFromUsersTable()
            .Where($"UserId ge {FirstUserId}")
            .Top(3)
            .GoAsync();

        deleted.Should().Be(3);
        (await TestUserIdsAsync()).Should().HaveCount(TotalUsers - 3);
    }

    [Fact]
    public async Task Delete_WithExpressionAndODataFilter_DeletesIntersection()
    {
        var deleted = await _context.DeleteFromUsersTable()
            .Where(x => x.UserId >= FirstUserId)
            .Where($"UserId eq {FirstUserId + 7}")
            .GoAsync();

        deleted.Should().Be(1);
        (await TestUserIdsAsync()).Should().NotContain(FirstUserId + 7).And.HaveCount(TotalUsers - 1);
    }

    [Fact]
    public async Task Delete_WithODataFilterMatchingNothing_ReturnsZero()
    {
        var deleted = await _context.DeleteFromUsersTable()
            .Where("UserId eq -1")
            .GoAsync();

        deleted.Should().Be(0);
        (await TestUserIdsAsync()).Should().HaveCount(TotalUsers);
    }

    [Fact]
    public async Task Delete_WithODataFilter_KeepsDetailRowsOfOtherUsers()
    {
        await _context.DeleteFromUsersTable().Where($"UserId eq {FirstUserId}").GoAsync();

        var survivor = await _context.SelectFromUsersTable()
            .Partially(User.PartialLoadFlags.All)
            .Where(x => x.UserId == FirstUserId + 1)
            .GetAsync();

        survivor.Should().NotBeNull();
        survivor!.Dates.Should().NotBeNullOrEmpty();
        survivor.Cars.Should().HaveCount(3);
        survivor.ListOfStrings.Should().HaveCount(5);
        survivor.ListOfIntegers.Should().HaveCount(5);
    }

    [Fact]
    public async Task Delete_WithoutFilter_DeletesAllRows()
    {
        var deleted = await _context.DeleteFromUsersTable().GoAsync();

        deleted.Should().BeGreaterThanOrEqualTo(TotalUsers);
        (await _context.SelectFromUsersTable().ListAsync()).Should().BeEmpty();
    }

    public async Task DisposeAsync()
    {
        // Leave the table empty so each test starts from exactly the rows InitializeAsync inserts.
        await _context.DeleteFromUsersTable().Where($"UserId ge {FirstUserId}").GoAsync();
        await _context.DisposeAsync().ConfigureAwait(false);
    }
}

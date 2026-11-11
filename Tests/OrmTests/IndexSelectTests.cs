using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Verifies the generated SelectFrom&lt;Entity&gt;(indexColumns) overloads filter on their own index columns,
/// and that an index filter combined with a LINQ/OData Where generates a valid "WHERE key=@p AND (...)" clause.
/// <para>
/// User declares: IX_USER_USER_ID (non-unique), IX_USER_BRANCH_ID (non-unique), IX_USER_AUTOINC (unique).
/// Previously __keyColumnDefs held only the PK and unique indexes, so SelectFromUsersTable(userId) filtered on AutoInc
/// and the branchId/autoInc overloads threw IndexOutOfRangeException.
/// </para>
/// </summary>
public class IndexSelectTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;
    private readonly List<User> _users;

    public IndexSelectTests(DatabaseFixture fixture)
    {
        _users = fixture.Users;
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public void KeyColumnDefs_HasOneSlotPerIndexInDeclarationOrder()
    {
        // Slot i + 1 must be the i-th StormIndex, because that is the keyId the generated overloads pass.
        var keyColumnDefs = StormControllerCache.Get<User>(0).KeyColumnDefs;

        keyColumnDefs.Select(slot => slot.Select(c => c.PropertyName).ToArray()).Should().BeEquivalentTo(
            new[]
            {
                new[] { nameof(User.UserId), nameof(User.BranchId) }, // PK
                new[] { nameof(User.UserId) },                        // IX_USER_USER_ID
                new[] { nameof(User.BranchId) },                      // IX_USER_BRANCH_ID
                new[] { nameof(User.AutoInc) }                        // IX_USER_AUTOINC
            },
            o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task Select_ByNonUniqueUserIdIndex_ReturnsRowsWithThatUserId()
    {
        var target = _users[^1];
        var expected = _users.Count(x => x.UserId == target.UserId);

        var result = await _context.SelectFromUsersTable(target.UserId).ListAsync();

        result.Should().HaveCount(expected);
        result.Should().OnlyContain(x => x.UserId == target.UserId);
    }

    [Fact]
    public async Task Select_ByNonUniqueBranchIdIndex_ReturnsRowsWithThatBranchId()
    {
        var branchId = _users[0].BranchId;
        var expected = _users.Count(x => x.BranchId == branchId);

        var result = await _context.SelectFromUsersTable(branchId).ListAsync();

        result.Should().HaveCount(expected);
        result.Should().OnlyContain(x => x.BranchId == branchId);
    }

    [Fact]
    public async Task Select_ByUniqueAutoIncIndex_ReturnsThatRow()
    {
        var all = await _context.SelectFromUsersTable().ListAsync();
        var target = all[^1];

        var result = await _context.SelectFromUsersTable(target.AutoInc).GetAsync();

        result.Should().NotBeNull();
        result!.AutoInc.Should().Be(target.AutoInc);
        result.UserId.Should().Be(target.UserId);
    }

    [Fact]
    public async Task Select_WithIndexKeyAndExpressionWhere_ReturnsIntersection()
    {
        var target = _users[0];
        var expected = _users.Count(x => x.BranchId == target.BranchId && x.UserId != target.UserId);

        var result = await _context.SelectFromUsersTable(target.BranchId)
            .Where(x => x.UserId != target.UserId)
            .ListAsync();

        result.Should().HaveCount(expected);
        result.Should().OnlyContain(x => x.BranchId == target.BranchId && x.UserId != target.UserId);
    }

    [Fact]
    public async Task Select_WithIndexKeyAndODataWhere_ReturnsIntersection()
    {
        var target = _users[0];
        var expected = _users.Count(x => x.BranchId == target.BranchId && x.UserId == target.UserId);

        var result = await _context.SelectFromUsersTable(target.BranchId)
            .Where($"UserId eq {target.UserId}")
            .ListAsync();

        result.Should().HaveCount(expected);
        result.Should().OnlyContain(x => x.BranchId == target.BranchId && x.UserId == target.UserId);
    }

    [Fact]
    public void KeyColumnDefs_NonUniqueIndexOnly_GetsItsOwnSlot()
    {
        // Post: PK Id + non-unique index on Text (previously the Text overload pointed past the end of the array)
        var keyColumnDefs = StormControllerCache.Get<Post>(0).KeyColumnDefs;

        keyColumnDefs.Select(slot => slot.Select(c => c.PropertyName).ToArray()).Should().BeEquivalentTo(
            new[] { new[] { nameof(Post.Id) }, new[] { nameof(Post.Text) } },
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void KeyColumnDefs_MultiColumnUniqueIndex_UsesPropertyDeclarationOrder()
    {
        // Account declares the index as [IbanAccount, Ccy]; columns are stored in property declaration order (Ccy first),
        // which is also the order the generated SelectFromAccount(ibanAccount, ccy) overload passes the values in.
        var account = StormControllerCache.Get<Account>(0).KeyColumnDefs;
        account.Select(slot => slot.Select(c => c.PropertyName).ToArray()).Should().BeEquivalentTo(
            new[] { new[] { nameof(Account.Id) }, new[] { nameof(Account.Ccy), nameof(Account.IbanAccount) } },
            o => o.WithStrictOrdering());

        var refIds = StormControllerCache.Get<RefIdentifiers>(0).KeyColumnDefs;
        refIds.Should().HaveCount(2);
        refIds[1].Select(c => c.PropertyName).Should().BeEquivalentTo(["Eid", "Id", "IdType", "Script"]);
    }

    [Fact]
    public async Task Select_ByNonUniqueIndex_WithOrderByAndTop_ReturnsOrderedSubset()
    {
        var branchId = _users[0].BranchId;
        var expected = _users.Where(x => x.BranchId == branchId).OrderByDescending(x => x.UserId).Take(2).Select(x => x.UserId).ToList();

        var result = await _context.SelectFromUsersTable(branchId)
            .OrderBy(User.OrderBy.UserId_Desc)
            .Top(2)
            .ListAsync();

        result.Select(x => x.UserId).Should().Equal(expected);
    }

    [Fact]
    public async Task Select_ByNonUniqueIndex_WithWhereOrderByAndTop_CombinesAllClauses()
    {
        var branchId = _users[0].BranchId;
        var minUserId = _users[0].UserId;
        var expected = _users.Where(x => x.BranchId == branchId && x.UserId > minUserId)
            .OrderBy(x => x.UserId).Take(3).Select(x => x.UserId).ToList();

        var result = await _context.SelectFromUsersTable(branchId)
            .Where(x => x.UserId > minUserId)
            .OrderBy(User.OrderBy.UserId)
            .Top(3)
            .ListAsync();

        result.Select(x => x.UserId).Should().Equal(expected);
    }

    [Fact]
    public async Task Select_ByNonUniqueIndex_WithMultipleWheres_AndsThemTogether()
    {
        var target = _users[0];

        var result = await _context.SelectFromUsersTable(target.BranchId)
            .Where(x => x.UserId == target.UserId)
            .Where($"UserId eq {target.UserId}")
            .ListAsync();

        result.Should().ContainSingle().Which.UserId.Should().Be(target.UserId);
    }

    [Fact]
    public async Task Exists_ByNonUniqueIndex_ReflectsWhetherRowsMatch()
    {
        var branchId = _users[0].BranchId;
        var missingBranchId = (short)(_users.Max(x => x.BranchId) + 1);

        (await _context.SelectFromUsersTable(branchId).ExistsAsync()).Should().BeTrue();
        (await _context.SelectFromUsersTable(missingBranchId).ExistsAsync()).Should().BeFalse();
        (await _context.SelectFromUsersTable(branchId).Where(x => x.UserId == -1).ExistsAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Select_ByIndex_NoMatch_ReturnsEmptyOrNull()
    {
        var missingBranchId = (short)(_users.Max(x => x.BranchId) + 1);

        (await _context.SelectFromUsersTable(missingBranchId).ListAsync()).Should().BeEmpty();
        (await _context.SelectFromUsersTable(missingBranchId).CountAsync()).Should().Be(0);
        (await _context.SelectFromUsersTable(int.MaxValue).GetAsync()).Should().BeNull();
    }

    [Fact]
    public async Task Select_ByNonUniqueIndex_OnView_MatchesTable()
    {
        var target = _users[0];

        var fromTable = await _context.SelectFromUsersTable(target.BranchId).Where(x => x.UserId != target.UserId).CountAsync();
        var fromView = await _context.SelectFromUsersView(target.BranchId).Where(x => x.UserId != target.UserId).CountAsync();

        fromView.Should().Be(fromTable);
        fromTable.Should().Be(_users.Count(x => x.BranchId == target.BranchId && x.UserId != target.UserId));
    }

    [Fact]
    public async Task Count_WithIndexKeyAndExpressionWhere_MatchesInMemoryCount()
    {
        var target = _users[0];
        var expected = _users.Count(x => x.BranchId == target.BranchId && x.UserId == target.UserId);

        var count = await _context.SelectFromUsersTable(target.BranchId)
            .Where(x => x.UserId == target.UserId)
            .CountAsync();

        count.Should().Be(expected);
    }
}

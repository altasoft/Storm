using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.TestModels;
using AltaSoft.Storm.TestModels.VeryBadNamespace;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace AltaSoft.Storm.Tests;

public class BatchTransactionRollbackTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public BatchTransactionRollbackTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Batch_WithPrimaryKeyViolation_ShouldThrowAndRollbackAllCommands()
    {
        var account1 = NewAccount(100_001, "US12345678901234560001");
        var account2 = NewAccount(100_001, "US12345678901234560002");
        var account3 = NewAccount(100_002, "US12345678901234560003");

        using (var _ = new StormTransactionScope())
        {
            await using var context = new TestStormContext(_fixture.ConnectionString);
            await using var batch = context.CreateBatch();

            batch.Add(context.InsertIntoAccount().Values(account1));
            batch.Add(context.InsertIntoAccount().Values(account2));
            batch.Add(context.InsertIntoAccount().Values(account3));

            var act = async () => await batch.ExecuteAsync(CancellationToken.None);
            await act.Should().ThrowAsync<StormPrimaryKeyViolationException>();
        }

        await using var verifyContext = new TestStormContext(_fixture.ConnectionString);

        var inserted1 = await verifyContext.SelectFromAccount(account1.IbanAccount, account1.Ccy).GetAsync();
        var inserted2 = await verifyContext.SelectFromAccount(account2.IbanAccount, account2.Ccy).GetAsync();
        var inserted3 = await verifyContext.SelectFromAccount(account3.IbanAccount, account3.Ccy).GetAsync();

        inserted1.Should().BeNull();
        inserted2.Should().BeNull();
        inserted3.Should().BeNull();
    }

    [Fact]
    public async Task Batch_WithUsersPrimaryKeyViolation_ShouldThrowAndRollbackAllCommands()
    {
        var user1 = DatabaseHelper.NewUser(100_100);
        var user2 = DatabaseHelper.NewUser(100_100); // same PK (UserId + BranchId)
        var user3 = DatabaseHelper.NewUser(100_101); // New PK, should not cause violation if executed alone


        using (var _ = new StormTransactionScope())
        {
            await using var context = new TestStormContext(_fixture.ConnectionString);
            await using var batch = context.CreateBatch();

            batch.Add(context.InsertIntoUsersTable().Values(user1));
            batch.Add(context.InsertIntoUsersTable().Values(user2));
            batch.Add(context.InsertIntoUsersTable().Values(user3));

            var act = async () => await batch.ExecuteAsync(CancellationToken.None);
            await act.Should().ThrowAsync<StormPrimaryKeyViolationException>();
        }

        await using var verifyContext = new TestStormContext(_fixture.ConnectionString);

        var inserted1 = await verifyContext.SelectFromUsersTable(user1.UserId, user1.BranchId).GetAsync();
        var inserted2 = await verifyContext.SelectFromUsersTable(user2.UserId, user2.BranchId).GetAsync();
        var inserted3 = await verifyContext.SelectFromUsersTable(user3.UserId, user3.BranchId).GetAsync();

        inserted1.Should().BeNull();
        inserted2.Should().BeNull();
        inserted3.Should().BeNull();
    }

    [Fact]
    public async Task Batch_WithNotNullViolation_HigherSeverity_ShouldThrowAndRollbackAllCommands()
    {
        var user1 = DatabaseHelper.NewUser(100_200);
        var user2 = DatabaseHelper.NewUser(100_201);
        var user3 = DatabaseHelper.NewUser(100_202);
        user2.LoginName = null!; // force NOT NULL violation (SqlException class 16)

        SqlException? thrown;

        using (var _ = new StormTransactionScope())
        {
            await using var context = new TestStormContext(_fixture.ConnectionString);
            await using var batch = context.CreateBatch();

            batch.Add(context.InsertIntoUsersTable().Values(user1));
            batch.Add(context.InsertIntoUsersTable().Values(user2));
            batch.Add(context.InsertIntoUsersTable().Values(user3));

            var act = async () => await batch.ExecuteAsync(CancellationToken.None);
            thrown = (await act.Should().ThrowAsync<SqlException>()).Which;
        }

        thrown.Should().NotBeNull();
        thrown.Class.Should().BeGreaterThanOrEqualTo(16);

        await using var verifyContext = new TestStormContext(_fixture.ConnectionString);

        var inserted1 = await verifyContext.SelectFromUsersTable(user1.UserId, user1.BranchId).GetAsync();
        var inserted2 = await verifyContext.SelectFromUsersTable(user2.UserId, user2.BranchId).GetAsync();
        var inserted3 = await verifyContext.SelectFromUsersTable(user3.UserId, user3.BranchId).GetAsync();

        inserted1.Should().BeNull();
        inserted2.Should().BeNull();
        inserted3.Should().BeNull();
    }


    [Fact]
    public async Task Batch_WithCommandTimeout_ShouldThrowAndRollbackAllCommands()
    {
        const int lockedUserId = 1;
        const short branchId = 7;

        await using var lockerConnection = new SqlConnection(_fixture.ConnectionString);
        await lockerConnection.OpenAsync();
        await using var lockerTransaction = (SqlTransaction)await lockerConnection.BeginTransactionAsync();

        await using (var lockCmd = lockerConnection.CreateCommand())
        {
            lockCmd.Transaction = lockerTransaction;
            lockCmd.CommandText = "UPDATE dbo.Users SET FullName = FullName WHERE Id = @id AND BranchId = @branchId";
            lockCmd.Parameters.AddWithValue("@id", lockedUserId);
            lockCmd.Parameters.AddWithValue("@branchId", branchId);
            await lockCmd.ExecuteNonQueryAsync();
        }

        var account = NewAccount(100_300, "US12345678901234560300");

        SqlException? thrown;

        using (var _ = new StormTransactionScope())
        {
            await using var context = new TestStormContext(_fixture.ConnectionString);
            await using var batch = context.CreateBatch();

            var insertCmd = context.InsertIntoAccount().Values(account);
            var blockedUpdateCmd = context.UpdateUsersTable(lockedUserId, branchId)
                .WithCommandTimeOut(1)
                .Set(x => x.FullName, "WillTimeout");

            batch.Add(insertCmd);
            batch.Add(blockedUpdateCmd);

            var act = async () => await batch.ExecuteAsync(CancellationToken.None);
            thrown = (await act.Should().ThrowAsync<SqlException>()).Which;
        }

        thrown.Should().NotBeNull();
        thrown.Number.Should().Be(-2);

        await lockerTransaction.RollbackAsync();

        await using var verifyContext = new TestStormContext(_fixture.ConnectionString);
        var inserted = await verifyContext.SelectFromAccount(account.IbanAccount, account.Ccy).GetAsync();
        inserted.Should().BeNull();
    }

    private static Account NewAccount(int id, string iban)
    {
        return new Account
        {
            Id = id,
            RelatedCustomerId = new CustomerId(1),
            Ccy = "USD",
            IbanAccount = iban,
            BbanAccount = 1234567890123456,
            BranchId = 1,
            Type = 1,
            Name = $"Test Account {id}",
            CanDebit = true,
            CanCredit = true
        };
    }
}

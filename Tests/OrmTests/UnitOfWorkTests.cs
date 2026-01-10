using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public sealed class UnitOfWorkTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UnitOfWorkTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);

        StormManager.SetLogger(logger);
        _fixture = fixture;
    }

    [Fact]
    public async Task Commit_CommitsTransactionAndDisposes()
    {
        var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        // act
        using (var uow = new StormTransactionScope(transaction))
        {
            Assert.True(uow.IsRoot);
            Assert.Equal(1, uow.Ambient.TransactionCount);

            await uow.CompleteAsync(CancellationToken.None);

            // assert
            Assert.Equal(0, uow.Ambient.TransactionCount);
        }

        // verify transaction was not committed and neither connection nor transaction were disposed
        Assert.Equal(ConnectionState.Open, connection.State);
        Assert.NotNull(transaction.Connection); // transaction should be not commited

        await transaction.CommitAsync(CancellationToken.None); // commit transaction manually
        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Rollback_WithoutCommit_RollsBackTransaction1()
    {
        var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        // act
        using (var uow = new StormTransactionScope(transaction))
        {
            // do not call CompleteAsync (commit)

            Assert.True(uow.IsRoot);
            Assert.Equal(1, uow.Ambient.TransactionCount);
        }

        // verify transaction was rollbacked
        Assert.Equal(ConnectionState.Open, connection.State);
        Assert.Null(transaction.Connection); // transaction should be rollbacked

        await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync(CancellationToken.None)); // cannot commit a rolled back transaction

        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Rollback_WithoutCommit_RollsBackTransaction2()
    {
        var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        // act
        using (var uow = new StormTransactionScope(transaction))
        {
            // await using var tx = is missing here, so the transaction is not disposed automatically

            // do not call CompleteAsync (commit)

            Assert.True(uow.IsRoot);
            Assert.Equal(1, uow.Ambient.TransactionCount);
        }

        // verify transaction was rollbacked
        Assert.Equal(ConnectionState.Open, connection.State);
        Assert.Null(transaction.Connection); // transaction should be rollbacked

        await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync(CancellationToken.None)); // cannot commit a rolled back transaction

        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Nested_UnitOfWork_OnlyRootDisposes()
    {
        SqlConnection? connection;
        SqlTransaction? transaction;

        // act
        using (var uow1 = new StormTransactionScope())
        {
            Assert.True(uow1.IsRoot);
            Assert.NotNull(uow1.Ambient);
            Assert.Equal(1, uow1.Ambient.TransactionCount);

            await using var usersContext = new TestStormContext(_fixture.ConnectionString);

            Assert.True(usersContext.IsInTransactionScope);
            Assert.False(usersContext.IsStandalone);

            (connection, transaction) = await usersContext.EnsureConnectionAndTransactionIsOpenAsync(CancellationToken.None);

            Assert.Same(uow1.Ambient.Connection, connection);
            Assert.Same(uow1.Ambient.Transaction, transaction);

            using (var uow2 = new StormTransactionScope())
            {
                Assert.Same(uow1.Ambient, uow2.Ambient);

                // assert
                Assert.False(uow2.IsRoot);
                Assert.Equal(2, uow2.Ambient.TransactionCount);

                Assert.NotNull(uow2.Ambient.Connection);
                Assert.NotNull(uow2.Ambient.Transaction);

                Assert.Same(connection, uow2.Ambient.Connection);
                Assert.Same(transaction, uow2.Ambient.Transaction);

                await uow2.CompleteAsync(CancellationToken.None);

                Assert.Equal(1, uow2.Ambient.TransactionCount);
            }

            await uow1.CompleteAsync(CancellationToken.None);

            // assert
            Assert.True(uow1.IsRoot);
            Assert.Equal(0, uow1.Ambient.TransactionCount);
        }

        // verify transaction was committed and disposed
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.NotNull(transaction);
        Assert.Null(transaction.Connection);
    }

    [Fact]
    public async Task RootUnitOfWork_OwnsConnection_AndDisposesIt()
    {
        SqlConnection? connection;
        SqlTransaction? transaction;

        // act
        var uow = new StormTransactionScope();
        try
        {
            await using var usersContext = new TestStormContext(_fixture.ConnectionString);

            Assert.True(usersContext.IsInTransactionScope);
            Assert.False(usersContext.IsStandalone);

            (connection, transaction) = await usersContext.EnsureConnectionAndTransactionIsOpenAsync(CancellationToken.None);

            Assert.Same(uow.Ambient.Connection, connection);
            Assert.Same(uow.Ambient.Transaction, transaction);

            Assert.False(uow.IsCompleted);
            Assert.Equal(ConnectionState.Open, connection.State);
            Assert.NotNull(transaction);
            Assert.NotNull(transaction.Connection);

            await uow.CompleteAsync(CancellationToken.None);

            Assert.True(uow.IsCompleted);
            Assert.Equal(ConnectionState.Open, connection.State);
            Assert.Null(transaction.Connection);
        }
        finally
        {
            uow.Dispose();
        }

        // verify connection was disposed
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Null(transaction.Connection);
    }


    [Fact]
    public async Task StandaloneUnitOfWork_StreamAndBatchUsers_Succeeds()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        const int batchSize = 3;
        var lastUserId = 0; // Use 0 to get all users

        await using var usersContext = new TestStormContext(_fixture.ConnectionString);

        Assert.False(usersContext.IsInTransactionScope);
        Assert.True(usersContext.IsStandalone);

        // Stream users with UserId > lastUserId, ordered by UserId, take batchSize
        var dbUsers = usersContext
            .SelectFromUsersTable()
            .Partially(User.PartialLoadFlags.Basic)
            .Where(x => x.UserId > lastUserId)
            .OrderBy(User.OrderBy.UserId)
            .Top(batchSize)
            .StreamAsync(cancellationToken);

        using var uow = new StormTransactionScope();

        Assert.False(usersContext.IsInTransactionScope);
        Assert.True(usersContext.IsStandalone);

        await using var batchContext = new TestStormContext(_fixture.ConnectionString);
        await using var batch = batchContext.CreateBatch();

        var count = 0;
        await foreach (var dbUser in dbUsers)
        {
            // For demonstration, update FullName
            dbUser.FullName = $"Updated_{dbUser.UserId}";
            var updateCmd = batchContext.UpdateUsersTable().WithoutConcurrencyCheck().Set(dbUser);
            batch.Add(updateCmd);
            count++;
        }

        if (count > 0)
        {
            var result = await batch.ExecuteAsync(cancellationToken);
            Assert.Equal(count, result); // All updates should succeed
        }
        else
        {
            Assert.Fail("No users streamed for update.");
        }

        await uow.CompleteAsync(CancellationToken.None);
    }
}

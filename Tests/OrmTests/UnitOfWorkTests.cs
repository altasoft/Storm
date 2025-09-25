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
        using (var uow = UnitOfWork.Create())
        {
            await using var tx = await uow.BeginAsync(connection, transaction, CancellationToken.None);

            Assert.Equal(1, uow.AmbientUow.TransactionCount);

            await tx.CompleteAsync(CancellationToken.None);

            // assert
            Assert.True(uow.IsRoot);
            Assert.False(uow.AmbientUow.IsRollBacked);
            Assert.Equal(0, uow.AmbientUow.TransactionCount);
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
        using (var uow = UnitOfWork.Create())
        {
            await using var tx = await uow.BeginAsync(connection, transaction, CancellationToken.None);

            // do not call CompleteAsync (commit)

            Assert.True(uow.IsRoot);
            Assert.False(uow.AmbientUow.IsRollBacked);
            Assert.Equal(1, uow.AmbientUow.TransactionCount);
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
        using (var uow = UnitOfWork.Create())
        {
            // await using var tx = is missing here, so the transaction is not disposed automatically
            await uow.BeginAsync(connection, transaction, CancellationToken.None);

            // do not call CompleteAsync (commit)

            Assert.True(uow.IsRoot);
            Assert.False(uow.AmbientUow.IsRollBacked);
            Assert.Equal(1, uow.AmbientUow.TransactionCount);
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
        SqlConnection connection;
        SqlTransaction transaction;

        // act
        using (var uow1 = UnitOfWork.Create())
        {
            await using var tx1 = await uow1.BeginAsync(_fixture.ConnectionString, CancellationToken.None);

            Assert.True(uow1.AmbientUow.IsInitialized);

            Assert.NotNull(uow1.AmbientUow.Connection);
            Assert.NotNull(uow1.AmbientUow.Transaction);

            connection = uow1.AmbientUow.Connection;
            transaction = uow1.AmbientUow.Transaction;

            using (var uow2 = UnitOfWork.Create())
            {
                await using var tx2 = await uow2.BeginAsync(_fixture.ConnectionString, CancellationToken.None);

                // assert
                Assert.False(uow2.IsRoot);
                Assert.False(uow2.AmbientUow.IsRollBacked);
                Assert.Equal(2, uow2.AmbientUow.TransactionCount);

                Assert.Same(uow1.AmbientUow, uow2.AmbientUow);

                Assert.NotNull(uow2.AmbientUow.Connection);
                Assert.NotNull(uow2.AmbientUow.Transaction);

                var connection2 = uow2.AmbientUow.Connection;
                var transaction2 = uow2.AmbientUow.Transaction;

                Assert.Same(connection, connection2);
                Assert.Same(transaction, transaction2);

                await tx2.CompleteAsync(CancellationToken.None);

                Assert.False(uow2.AmbientUow.IsRollBacked);
                Assert.Equal(1, uow2.AmbientUow.TransactionCount);
            }

            await tx1.CompleteAsync(CancellationToken.None);

            // assert
            Assert.True(uow1.IsRoot);
            Assert.False(uow1.AmbientUow.IsRollBacked);
            Assert.Equal(0, uow1.AmbientUow.TransactionCount);
        }

        // verify transaction was committed and disposed
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Null(transaction.Connection);
    }

    [Fact]
    public async Task RootUnitOfWork_OwnsConnection_AndDisposesIt()
    {
        // act
        var uow = UnitOfWork.Create();

        await using (var tx = await uow.BeginAsync(_fixture.ConnectionString, CancellationToken.None))
        {
            var connection = uow.AmbientUow.Connection;
            var transaction = uow.AmbientUow.Transaction;

            await tx.CompleteAsync(CancellationToken.None);

            // assert
            // verify transaction was committed and disposed
            Assert.NotNull(connection);
            Assert.NotNull(transaction);

            Assert.Equal(ConnectionState.Open, connection.State);
            Assert.Null(transaction.Connection);
        }

        uow.Dispose();
    }


    [Fact]
    public async Task StandaloneUnitOfWork_StreamAndBatchUsers_Succeeds()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        const int batchSize = 3;
        var lastUserId = 0; // Use 0 to get all users

        await using var usersContext = new TestStormContext(_fixture.ConnectionString);

        Assert.False(usersContext.IsInUnitOfWork);
        Assert.True(usersContext.IsStandalone);

        // Stream users with UserId > lastUserId, ordered by UserId, take batchSize
        var dbUsers = usersContext
            .SelectFromUsersTable()
            .Partially(User.PartialLoadFlags.Basic)
            .Where(x => x.UserId > lastUserId)
            .OrderBy(User.OrderBy.UserId)
            .Top(batchSize)
            .StreamAsync(cancellationToken);

        using var uow = UnitOfWork.CreateStandalone();

        Assert.False(usersContext.IsInUnitOfWork);
        Assert.True(usersContext.IsStandalone);

        await using var tx = await uow.BeginAsync(_fixture.ConnectionString, cancellationToken);

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

        await tx.CompleteAsync(cancellationToken);
    }
}

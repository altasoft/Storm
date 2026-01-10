using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.TestModels;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public sealed class TransactionScopeTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public TransactionScopeTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);

        StormManager.SetLogger(logger);
        _fixture = fixture;
    }

    [Fact]
    public async Task CompleteAsync_WithExternalTransaction_DoesNotDisposeConnectionOrTransaction()
    {
        var connection = new SqlConnection(_fixture.ConnectionString);
        try
        {
            await connection.OpenAsync();
            var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                // act
                using (var sts = new StormTransactionScope(transaction))
                {
                    Assert.True(sts.IsRoot);
                    Assert.Equal(1, sts.Ambient.TransactionCount);

                    await sts.CompleteAsync(CancellationToken.None);

                    // assert
                    Assert.Equal(0, sts.Ambient.TransactionCount);
                }

                // verify transaction was not committed and neither connection nor transaction were disposed
                Assert.Equal(ConnectionState.Open, connection.State);
                Assert.NotNull(transaction.Connection); // transaction should be not commited

                await transaction.CommitAsync(CancellationToken.None); // commit transaction manually
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Dispose_WithoutCompleteAsync_RollsBackExternalTransaction()
    {
        var connection = new SqlConnection(_fixture.ConnectionString);
        try
        {
            await connection.OpenAsync();
            var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                // act
                using (var sts = new StormTransactionScope(transaction))
                {
                    // do not call CompleteAsync (commit)

                    Assert.True(sts.IsRoot);
                    Assert.Equal(1, sts.Ambient.TransactionCount);
                }

                // verify transaction was rollbacked
                Assert.Equal(ConnectionState.Open, connection.State);
                Assert.Null(transaction.Connection); // transaction should be rollbacked

                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    transaction.CommitAsync(CancellationToken.None)); // cannot commit a rolled back transaction
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Dispose_WithoutCompleteAsync_RollsBackExternalTransaction_AlternatePath()
    {
        var connection = new SqlConnection(_fixture.ConnectionString);
        try
        {
            await connection.OpenAsync();
            var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                // act
                using (var sts = new StormTransactionScope(transaction))
                {
                    // await using var tx = is missing here, so the transaction is not disposed automatically

                    // do not call CompleteAsync (commit)

                    Assert.True(sts.IsRoot);
                    Assert.Equal(1, sts.Ambient.TransactionCount);
                }

                // verify transaction was rollbacked
                Assert.Equal(ConnectionState.Open, connection.State);
                Assert.Null(transaction.Connection); // transaction should be rollbacked

                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    transaction.CommitAsync(CancellationToken.None)); // cannot commit a rolled back transaction
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task NestedScopes_OnlyRootCommitsAndDisposesAmbientResources()
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
    public async Task RootScope_OwnsConnection_DisposesConnectionOnComplete()
    {
        SqlConnection? connection;
        SqlTransaction? transaction;

        // act
        using (var sts = new StormTransactionScope())
        {
            await using var usersContext = new TestStormContext(_fixture.ConnectionString);

            Assert.True(usersContext.IsInTransactionScope);
            Assert.False(usersContext.IsStandalone);

            (connection, transaction) = await usersContext.EnsureConnectionAndTransactionIsOpenAsync(CancellationToken.None);

            Assert.Same(sts.Ambient.Connection, connection);
            Assert.Same(sts.Ambient.Transaction, transaction);

            Assert.False(sts.IsCompleted);
            Assert.Equal(ConnectionState.Open, connection.State);
            Assert.NotNull(transaction);
            Assert.NotNull(transaction.Connection);

            await sts.CompleteAsync(CancellationToken.None);

            Assert.True(sts.IsCompleted);
            Assert.Equal(ConnectionState.Open, connection.State);
            Assert.Null(transaction.Connection);
        }

        // verify connection was disposed
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Null(transaction.Connection);
    }


    [Fact]
    public async Task StandaloneContext_StreamAndBatch_UpdatesUsers()
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

        using var sts = new StormTransactionScope();

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

        await sts.CompleteAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ScopeAmbient_IsPreservedAcrossAwait()
    {
        using var outer = new StormTransactionScope();

        // Current should be the outer scope
        Assert.Same(outer, StormTransactionScope.Current);

        // Await to force an async continuation
        await Task.Yield();

        // Ambient should still be preserved after await
        Assert.Same(outer, StormTransactionScope.Current);

        // mark completed and dispose
        await outer.CompleteAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CreateNewScope_RestoresPreviousScopeOnDispose()
    {
        using var outer = new StormTransactionScope();
        Assert.True(outer.IsRoot);

        // create a new nested ambient (CreateNew) which should have previous pointer to outer.Ambient
        using (var inner = new StormTransactionScope(StormTransactionScopeOption.CreateNew))
        {
            Assert.NotSame(outer.Ambient, inner.Ambient);
            Assert.Same(outer.Ambient, inner.Ambient.Previous);

            // Current inside nested scope should be inner
            Assert.Same(inner, StormTransactionScope.Current);

            await inner.CompleteAsync(CancellationToken.None);
        }

        // After inner disposed, current should be restored to outer
        Assert.Same(outer, StormTransactionScope.Current);

        await outer.CompleteAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ScopeCreatedInTask_RestoresAmbientAfterTask()
    {
        using var outer = new StormTransactionScope();

        var mainCurrent = StormTransactionScope.Current;
        Assert.Same(outer, mainCurrent);

        // Create nested scope in another task; ExecutionContext should flow so the task sees the ambient
        var t = Task.Run(async () =>
        {
            // the task should start with the same ambient
            Assert.Same(mainCurrent, StormTransactionScope.Current);

            using var inner = new StormTransactionScope(StormTransactionScopeOption.CreateNew);
            Assert.NotNull(StormTransactionScope.Current);

            // Await inside the task to ensure async continuation inside task keeps ambient
            await Task.Yield();

            await inner.CompleteAsync(CancellationToken.None);
        });

        await t;

        // After the task completes, the main context should still have the outer scope
        Assert.Same(outer, StormTransactionScope.Current);

        await outer.CompleteAsync(CancellationToken.None);
    }

    [Fact]
    public async Task MixedNesting_JoinAndCreateNew_MaintainsCorrectBehavior()
    {
        using var outer = new StormTransactionScope();

        // join existing nested scope - shares ambient with outer
        using var joinNested = new StormTransactionScope();
        Assert.False(joinNested.IsRoot);
        Assert.Same(outer.Ambient, joinNested.Ambient);

        // create new nested scope - should have its own ambient chained to previous
        using (var newNested = new StormTransactionScope(StormTransactionScopeOption.CreateNew))
        {
            Assert.True(newNested.IsRoot);
            Assert.Same(joinNested.Ambient, newNested.Ambient.Previous);

            // transaction counts
            Assert.Equal(2, outer.Ambient.TransactionCount); // outer + joinNested
            Assert.Equal(1, newNested.Ambient.TransactionCount);

            // complete inner new scope (commits its own ambient)
            await newNested.CompleteAsync(CancellationToken.None);
        }

        // After completing newNested, current should still be joinNested
        Assert.Same(joinNested, StormTransactionScope.Current);

        // complete joinNested (decrements shared ambient but does not commit yet)
        await joinNested.CompleteAsync(CancellationToken.None);
        Assert.Equal(1, outer.Ambient.TransactionCount);

        // complete outer (will commit)
        await outer.CompleteAsync(CancellationToken.None);
        Assert.Equal(0, outer.Ambient.TransactionCount);
    }

    [Fact]
    public async Task InnerJoin_DisposeWithoutComplete_RollsBackAmbient_PreventsOuterCommit()
    {
        using var outer = new StormTransactionScope();

        using (var innerJoin = new StormTransactionScope())
        {
            // inner join shares ambient
            Assert.Same(outer.Ambient, innerJoin.Ambient);
            // do not call CompleteAsync on innerJoin -> it will be disposed and cause rollback
        }

        // After disposing inner without complete the ambient should have been rolled back (TransactionCount reset)
        Assert.Equal(0, outer.Ambient.TransactionCount);

        // Attempting to complete outer now should fail because transaction was rolled back
        await Assert.ThrowsAsync<StormException>(() => outer.CompleteAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MultipleCreateNewChain_RestoresAmbientOrderOnDispose()
    {
        using var outer = new StormTransactionScope();

        using (var first = new StormTransactionScope(StormTransactionScopeOption.CreateNew))
        {
            Assert.Same(outer.Ambient, first.Ambient.Previous);

            using (var second = new StormTransactionScope(StormTransactionScopeOption.CreateNew))
            {
                Assert.Same(first.Ambient, second.Ambient.Previous);

                // inside second
                Assert.Same(second, StormTransactionScope.Current);

                await second.CompleteAsync(CancellationToken.None);
            }

            // after second disposed, current should be first
            Assert.Same(first, StormTransactionScope.Current);

            await first.CompleteAsync(CancellationToken.None);
        }

        // after all nested disposed, current should be outer
        Assert.Same(outer, StormTransactionScope.Current);

        await outer.CompleteAsync(CancellationToken.None);
    }

    // --- New concurrency / reentrancy tests ---

    [Fact]
    public async Task CompleteAsync_Idempotent_WhenCalledMultipleTimes()
    {
        using var scope = new StormTransactionScope();

        // first complete should commit (if outermost)
        await scope.CompleteAsync(CancellationToken.None);

        // second call should be ignored and not throw or decrement counts
        await scope.CompleteAsync(CancellationToken.None);

        Assert.True(scope.IsCompleted);
        Assert.Equal(0, scope.Ambient.TransactionCount);
    }

    [Fact]
    public async Task Dispose_CanBeCalledMultipleTimes_NoException()
    {
        var scope = new StormTransactionScope();

        // complete then dispose twice
        await scope.CompleteAsync(CancellationToken.None);
        scope.Dispose();
        scope.Dispose(); // should not throw
    }

    [Fact]
    public async Task Parallel_CreateNewScopes_IsolatedAndRestoreMainContext()
    {
        using var outer = new StormTransactionScope();

        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            // each parallel task should inherit ambient (outer) and then create its own ambient (CreateNew)
            // ReSharper disable once AccessToDisposedClosure
            Assert.Same(outer, StormTransactionScope.Current);

            using var s = new StormTransactionScope(StormTransactionScopeOption.CreateNew);
            // ReSharper disable once AccessToDisposedClosure
            Assert.NotSame(outer.Ambient, s.Ambient);
            // ReSharper disable once AccessToDisposedClosure
            Assert.Same(outer.Ambient, s.Ambient.Previous);

            await Task.Yield();

            await s.CompleteAsync(CancellationToken.None);
        })).ToArray();

        await Task.WhenAll(tasks);

        // main context should still have outer
        Assert.Same(outer, StormTransactionScope.Current);

        await outer.CompleteAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Parallel_JoinScopes_DoNotLeakAmbientToMainContext()
    {
        using var outer = new StormTransactionScope();

        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            // join existing ambient inside task
            // ReSharper disable once AccessToDisposedClosure
            Assert.Same(outer, StormTransactionScope.Current);

            using var s = new StormTransactionScope(); // JoinExisting
            // ReSharper disable once AccessToDisposedClosure
            Assert.Same(outer.Ambient, s.Ambient);

            await Task.Yield();

            await s.CompleteAsync(CancellationToken.None);
        })).ToArray();

        await Task.WhenAll(tasks);

        // main context should still have outer
        Assert.Same(outer, StormTransactionScope.Current);

        await outer.CompleteAsync(CancellationToken.None);
    }
}

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
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

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

    [Fact]
    public async Task Dispose_WithoutCompleteAsync_RollsBackExternalTransaction()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        // act
        using (var sts = new StormTransactionScope(transaction))
        {
            // do not call CompleteAsync (commit)

            Assert.True(sts.IsRoot);
            Assert.Equal(1, sts.Ambient.TransactionCount);
        }

        // verify transaction was rollbacked
        Assert.Equal(ConnectionState.Open, connection.State);
        Assert.Null(transaction.Connection);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transaction.CommitAsync(CancellationToken.None)); // cannot commit a rolled back transaction
    }

    [Fact]
    public async Task Dispose_WithoutCompleteAsync_RollsBackExternalTransaction_AlternatePath()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

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

            (connection, transaction) = await usersContext.EnsureConnectionAsync(CancellationToken.None);

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

            (connection, transaction) = await usersContext.EnsureConnectionAsync(CancellationToken.None);

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
        const int lastUserId = 0; // Use 0 to get all users

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
    public async Task RequiresNewScope_RestoresPreviousScopeOnDispose()
    {
        using var outer = new StormTransactionScope();
        Assert.True(outer.IsRoot);

        // create a new nested ambient (RequiresNew) which should have previous pointer to outer.Ambient
        using (var inner = new StormTransactionScope(StormTransactionScopeOption.RequiresNew))
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

            using var inner = new StormTransactionScope(StormTransactionScopeOption.RequiresNew);
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
    public async Task MixedNesting_JoinAndRequiresNew_MaintainsCorrectBehavior()
    {
        using var outer = new StormTransactionScope();

        // join existing nested scope - shares ambient with outer
        using var joinNested = new StormTransactionScope();
        Assert.False(joinNested.IsRoot);
        Assert.Same(outer.Ambient, joinNested.Ambient);

        // create new nested scope - should have its own ambient chained to previous
        using (var newNested = new StormTransactionScope(StormTransactionScopeOption.RequiresNew))
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
    public async Task MultipleRequiresNewChain_RestoresAmbientOrderOnDispose()
    {
        using var outer = new StormTransactionScope();

        using (var first = new StormTransactionScope(StormTransactionScopeOption.RequiresNew))
        {
            Assert.Same(outer.Ambient, first.Ambient.Previous);

            using (var second = new StormTransactionScope(StormTransactionScopeOption.RequiresNew))
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
    public async Task Parallel_RequiresNewScopes_IsolatedAndRestoreMainContext()
    {
        using var outer = new StormTransactionScope();

        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            // each parallel task should inherit ambient (outer) and then create its own ambient (RequiresNew)
            // ReSharper disable once AccessToDisposedClosure
            Assert.Same(outer, StormTransactionScope.Current);

            using var s = new StormTransactionScope(StormTransactionScopeOption.RequiresNew);
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

            using var s = new StormTransactionScope(); // Default is RequiresNew
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

    [Fact]
    public async Task OuterScope_WithProcedureAndStandaloneContexts_PersistsStandaloneChangesOnly()
    {
        const int transactionalUserId = 310_001;
        const int standaloneUser1Id = 310_002;
        const int standaloneUser2Id = 310_003;
        const int standaloneUser3Id = 310_004;
        const short branchId = 7;

        var transactionalUser = DatabaseHelper.NewUser(transactionalUserId);
        var standaloneUser1 = DatabaseHelper.NewUser(standaloneUser1Id);
        var standaloneUser2 = DatabaseHelper.NewUser(standaloneUser2Id);
        var standaloneUser3 = DatabaseHelper.NewUser(standaloneUser3Id);

        using (var outer = new StormTransactionScope())
        {
            Assert.True(outer.IsRoot);
            Assert.Equal(1, outer.Ambient.TransactionCount);
            Assert.Same(outer, StormTransactionScope.Current);

            await using var context1 = new TestStormContext(_fixture.ConnectionString, standalone: false);

            Assert.False(context1.IsStandalone);
            Assert.True(context1.IsInTransactionScope);

            var (outerConnection, outerTransaction) = await context1.EnsureConnectionAsync(CancellationToken.None);
            Assert.Same(outer.Ambient.Connection, outerConnection);
            Assert.Same(outer.Ambient.Transaction, outerTransaction);
            Assert.NotNull(outerTransaction);

            var procResult = await context1.ExecuteInputOutputProc(1, 0).ExecuteAsync();
            Assert.NotNull(procResult);
            Assert.Equal(1, procResult.ReturnValue);
            Assert.Equal(1, procResult.ResultValue);
            Assert.Equal(77, procResult.Io);

            await context1.InsertIntoUsersTable().Values(transactionalUser).GoAsync();

            var transactionalVisibleInsideOuter = await context1.SelectFromUsersTable(transactionalUserId, branchId).GetAsync();
            Assert.NotNull(transactionalVisibleInsideOuter);

            await using (var standaloneContext1 = new TestStormContext(_fixture.ConnectionString, standalone: true))
            {
                Assert.True(standaloneContext1.IsStandalone);
                Assert.False(standaloneContext1.IsInTransactionScope);

                var (standaloneConnection1, standaloneTransaction1) = await standaloneContext1.EnsureConnectionAsync(CancellationToken.None);
                Assert.NotNull(standaloneConnection1);
                Assert.Null(standaloneTransaction1);
                Assert.NotSame(outerConnection, standaloneConnection1);

                await standaloneContext1.InsertIntoUsersTable().Values(standaloneUser1).GoAsync();

                await standaloneContext1.UpdateUsersTable(standaloneUser1.UserId, standaloneUser1.BranchId)
                    .Set(x => x.FullName, "Standalone1_Updated")
                    .GoAsync();
            }

            await using (var outsideReadContext = new TestStormContext(_fixture.ConnectionString, standalone: true))
            {
                var standaloneVisibleOutside = await outsideReadContext.SelectFromUsersTable(standaloneUser1Id, branchId).GetAsync();

                Assert.NotNull(standaloneVisibleOutside);
                Assert.Equal("Standalone1_Updated", standaloneVisibleOutside!.FullName);
            }

            using (var inner = new StormTransactionScope(StormTransactionScopeOption.RequiresNew))
            {
                Assert.Same(inner, StormTransactionScope.Current);
                Assert.NotSame(outer.Ambient, inner.Ambient);
                Assert.Same(outer.Ambient, inner.Ambient.Previous);
                Assert.Equal(1, outer.Ambient.TransactionCount);
                Assert.Equal(1, inner.Ambient.TransactionCount);

                await using var context2 = new TestStormContext(_fixture.ConnectionString, standalone: true);

                Assert.True(context2.IsStandalone);
                Assert.False(context2.IsInTransactionScope);

                var (standaloneConnection2, standaloneTransaction2) = await context2.EnsureConnectionAsync(CancellationToken.None);
                Assert.NotNull(standaloneConnection2);
                Assert.Null(standaloneTransaction2);
                Assert.NotSame(outerConnection, standaloneConnection2);

                await context2.InsertIntoUsersTable().Values(standaloneUser2).GoAsync();
                await context2.InsertIntoUsersTable().Values(standaloneUser3).GoAsync();
                await context2.UpdateUsersTable(standaloneUser2.UserId, standaloneUser2.BranchId)
                    .Set(x => x.FullName, "Standalone2_Updated")
                    .GoAsync();

                await inner.CompleteAsync(CancellationToken.None);

                Assert.True(inner.IsCompleted);
                Assert.Equal(0, inner.Ambient.TransactionCount);
            }

            Assert.Same(outer, StormTransactionScope.Current);
            Assert.False(outer.IsCompleted);
            Assert.Equal(1, outer.Ambient.TransactionCount);

            // Intentionally do not complete outer scope so ambient transactional changes are rolled back.
        }

        await using var verifyContext = new TestStormContext(_fixture.ConnectionString);

        var transactionalInserted = await verifyContext.SelectFromUsersTable(transactionalUserId, branchId).GetAsync();
        var standaloneInserted1 = await verifyContext.SelectFromUsersTable(standaloneUser1Id, branchId).GetAsync();
        var standaloneInserted2 = await verifyContext.SelectFromUsersTable(standaloneUser2Id, branchId).GetAsync();
        var standaloneInserted3 = await verifyContext.SelectFromUsersTable(standaloneUser3Id, branchId).GetAsync();

        Assert.Null(transactionalInserted);

        Assert.NotNull(standaloneInserted1);
        Assert.Equal("Standalone1_Updated", standaloneInserted1!.FullName);

        Assert.NotNull(standaloneInserted2);
        Assert.Equal("Standalone2_Updated", standaloneInserted2!.FullName);

        Assert.NotNull(standaloneInserted3);
    }

    [Fact]
    public async Task SuppressScope_HidesAmbientForInnerOperations_AndRestoresOuterAfterDispose()
    {
        using var outer = new StormTransactionScope();

        Assert.Same(outer, StormTransactionScope.Current);
        Assert.Equal(1, outer.Ambient.TransactionCount);

        using (var suppressed = new StormTransactionScope(StormTransactionScopeOption.Suppress))
        {
            Assert.True(suppressed.IsSuppressed);
            Assert.Same(suppressed, StormTransactionScope.Current);

            await using var contextInSuppress = new TestStormContext(_fixture.ConnectionString, standalone: false);
            Assert.True(contextInSuppress.IsStandalone);
            Assert.False(contextInSuppress.IsInTransactionScope);

            var (_, tx) = await contextInSuppress.EnsureConnectionAsync(CancellationToken.None);
            Assert.Null(tx);

            await suppressed.CompleteAsync(CancellationToken.None);
            Assert.True(suppressed.IsCompleted);
        }

        Assert.Same(outer, StormTransactionScope.Current);
        Assert.Equal(1, outer.Ambient.TransactionCount);
        Assert.False(outer.IsCompleted);

        await outer.CompleteAsync(CancellationToken.None);
        Assert.Equal(0, outer.Ambient.TransactionCount);
    }

    [Fact]
    public async Task SuppressScope_DisposeWithoutComplete_DoesNotRollbackOuterAmbient()
    {
        const int userId = 310_050;
        const short branchId = 7;

        var user = DatabaseHelper.NewUser(userId);

        using (var outer = new StormTransactionScope())
        {
            await using var outerContext = new TestStormContext(_fixture.ConnectionString, standalone: false);
            await outerContext.InsertIntoUsersTable().Values(user).GoAsync();

            using (var _ = new StormTransactionScope(StormTransactionScopeOption.Suppress))
            {
                // Intentionally not completed.
            }

            var stillVisibleInOuter = await outerContext.SelectFromUsersTable(userId, branchId).GetAsync();
            Assert.NotNull(stillVisibleInOuter);

            await outer.CompleteAsync(CancellationToken.None);
        }

        await using var verifyContext = new TestStormContext(_fixture.ConnectionString);
        var persisted = await verifyContext.SelectFromUsersTable(userId, branchId).GetAsync();
        Assert.NotNull(persisted);
    }
}

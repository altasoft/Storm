using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using AltaSoft.Storm.Exceptions;
using Xunit;

namespace AltaSoft.Storm.Tests;

public sealed class StormTransactionScopeTests
{
    [Fact]
    public async Task AsyncFlow_PreservesAmbientAcrossAwait()
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
    public async Task Nested_CreateNewScope_RestoresPreviousOnDispose()
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
    public async Task NestedScope_CreatedInTask_RestoresPreviousOnCompletion()
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
    public async Task MixedNesting_JoinAndCreateNew_BehavesCorrectly()
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
    public async Task NestedJoin_DisposeWithoutComplete_RollsBackAmbient_AndOuterCompleteFails()
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
    public async Task MultipleCreateNewChain_RestoresPreviousCorrectly()
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
    public async Task CompleteAsync_CalledMultipleTimes_IsIdempotent()
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
    public async Task Dispose_CanBeCalledMultipleTimes_NoThrow()
    {
        var scope = new StormTransactionScope();

        // complete then dispose twice
        await scope.CompleteAsync(CancellationToken.None);
        scope.Dispose();
        scope.Dispose(); // should not throw
    }

    [Fact]
    public async Task Parallel_CreateNewScopes_AreIsolatedAndRestoreMainContext()
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

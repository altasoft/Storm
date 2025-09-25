//using System;
//using System.Runtime.CompilerServices;
//using System.Threading;
//using System.Threading.Tasks;
//using AltaSoft.Storm.Exceptions;

//namespace AltaSoft.Storm;

///// <summary>
///// Represents a unit of work for performing database operations within a transaction.
///// </summary>
//public sealed class UnitOfWorkOld : IAsyncDisposable, IDisposable
//{
//    internal readonly StormContext _context;
//    private readonly int _transactionCount;

//    /// <summary>
//    /// Creates a new UnitOfWorkOld instance with the specified StormContext.
//    /// </summary>
//    /// <param name="context">The StormContext to be used for the UnitOfWorkOld.</param>
//    /// <returns>A task representing the asynchronous operation. The task result contains the created UnitOfWorkOld instance.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static async Task<UnitOfWorkOld> CreateAsync(StormContext context)
//    {
//        var unitOfWork = new UnitOfWorkOld(context);
//        await context.BeginTransactionAsync().ConfigureAwait(false);
//        return unitOfWork;
//    }

//    /// <summary>
//    /// Constructor for creating a new instance of UnitOfWorkOld with the provided StormContext.
//    /// </summary>
//    /// <param name="context">The StormContext to be used for the UnitOfWorkOld.</param>
//    private UnitOfWorkOld(StormContext context)
//    {
//        _context = context;
//        _transactionCount = _context.GetTransactionCount() + 1;
//    }

//    /// <summary>
//    /// Completes the transaction and commits the changes to the database.
//    /// </summary>
//    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
//    /// <returns>A task representing the asynchronous operation.</returns>
//    /// <exception cref="StormException">Thrown when the transaction count does not match the expected value.</exception>
//    public async Task CompleteAsync(CancellationToken cancellationToken = default)
//    {
//        if (_context.GetTransactionCount() != _transactionCount)
//            throw new StormException("Starting and ending transaction count mismatch");

//        await _context.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
//        await _context.DisposeConnectionAsync().ConfigureAwait(false);
//    }

//    /// <summary>
//    /// Disposes the UnitOfWorkOld instance and rolls back the transaction if it was not committed.
//    /// </summary>
//    public void Dispose()
//    {
//        // If _context.GetTransactionCount() == _transactionCount, then Commit was not called, so we should roll back
//        if (_context.GetTransactionCount() != _transactionCount)
//            return;
//        _context.RollbackTransaction();
//        _context.DisposeConnection();
//    }

//    /// <summary>
//    /// Disposes the UnitOfWorkOld instance asynchronously and rolls back the transaction if it was not committed.
//    /// </summary>
//    /// <returns>A task representing the asynchronous operation.</returns>
//    public async ValueTask DisposeAsync()
//    {
//        // If _context.GetTransactionCount() == _transactionCount, then CommitTransaction was not called, so we should roll back
//        if (_context.GetTransactionCount() != _transactionCount)
//            return;
//        await _context.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
//        await _context.DisposeConnectionAsync().ConfigureAwait(false);
//    }

//    /// <summary>
//    /// Creates a batch operation for executing multiple database commands in a single context.
//    /// </summary>
//    public Batch CreateBatch() => new(_context);
//}

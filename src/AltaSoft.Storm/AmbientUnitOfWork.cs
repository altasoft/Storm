using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using Microsoft.Extensions.Logging;

namespace AltaSoft.Storm;

/// <summary>
/// Represents a unit of work for performing database operations within a transaction.
/// </summary>
internal sealed class AmbientUnitOfWork : IDisposable
{
    private static readonly AsyncLocal<AmbientUnitOfWork?> s_ambient = new();

    private readonly ILogger? _logger;

    private bool _disposed;
    private bool _ownsConnection;
    private bool _ownsTransaction;

    /// <summary>
    /// Gets a value indicating whether the unit of work is initialized.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Connection))]
    [MemberNotNullWhen(true, nameof(Transaction))]
    internal bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the current ambient unit of work for the async context.
    /// </summary>
    internal static AmbientUnitOfWork? Ambient => s_ambient.Value;
    /// <summary>
    /// Gets the database connection associated with this unit of work.
    /// </summary>
    internal StormDbConnection? Connection { get; private set; }
    /// <summary>
    /// Gets the database transaction associated with this unit of work.
    /// </summary>
    internal StormDbTransaction? Transaction { get; private set; }
    /// <summary>
    /// Gets the current transaction nesting count.
    /// </summary>
    internal int TransactionCount { get; private set; }
    /// <summary>
    /// Gets a value indicating whether the transaction has been rolled back.
    /// </summary>
    internal bool IsRollBacked { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AmbientUnitOfWork"/> class.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    internal AmbientUnitOfWork(ILogger? logger)
    {
        IsInitialized = false;
        TransactionCount = 0;
        _logger = logger;

        s_ambient.Value = this;
    }

    /// <summary>
    /// Initializes the unit of work with the specified connection and transaction.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="ownsConnection">Whether this unit of work owns the connection.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <param name="ownsTransaction">Whether this unit of work owns the transaction.</param>
    [MemberNotNull(nameof(Connection))]
    [MemberNotNull(nameof(Transaction))]
    internal void Initialize(
        StormDbConnection connection, bool ownsConnection,
        StormDbTransaction transaction, bool ownsTransaction)
    {
        Connection = connection;
        _ownsConnection = ownsConnection;

        Transaction = transaction;
        _ownsTransaction = ownsTransaction;

        IsInitialized = true;
    }

    /// <summary>
    /// Increments the transaction nesting count.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementTx() => TransactionCount++;

    /// <summary>
    /// Commits the transaction if this is the outermost transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="StormException">Thrown if already rolled back or no active transaction exists.</exception>
    internal async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (IsRollBacked)
            throw new StormException("Transaction has already been rolled back. Commit is not allowed.");

        if (TransactionCount == 0)
            throw new StormException("No active transaction to commit.");

        if (--TransactionCount == 0)
        {
            if (_ownsTransaction)
            {
                if (Transaction is not null)
                    await Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                _logger?.LogTrace("[AmbientUnitOfWork] Transaction committed successfully (outermost transaction).");
            }
            else
            {
                _logger?.LogTrace("[AmbientUnitOfWork] Internal commit: TransactionCount decremented to {TransactionCount} (not owner, no commit executed).", TransactionCount);
            }
        }
        else
        {
            _logger?.LogTrace("[AmbientUnitOfWork] Nested commit: TransactionCount decremented to {TransactionCount}.", TransactionCount);
        }
    }

    /// <summary>
    /// Rolls back the transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (IsRollBacked)
            return; // Already rolled back; nothing to do

        try
        {
            if (Transaction is not null)
                await Transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _logger?.LogWarning("[AmbientUnitOfWork] Transaction rolled back.");
        }
        finally
        {
            IsRollBacked = true;
            TransactionCount = 0; // Transaction is over; reset counter
        }
    }

    /// <summary>
    /// Disposes the unit of work, rolling back and disposing resources if owned.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (TransactionCount > 0 && !IsRollBacked)
            {
                try
                {
                    Transaction?.Rollback();
                    _logger?.LogWarning("[AmbientUnitOfWork] Transaction rolled back during Dispose due to incomplete commit.");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[AmbientUnitOfWork] Exception during rollback in Dispose.");
                }
            }
        }
        finally
        {
            if (_ownsTransaction)
            {
                try
                {
                    Transaction?.Dispose();
                    _logger?.LogTrace("[AmbientUnitOfWork] Transaction disposed.");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[AmbientUnitOfWork] Exception during transaction disposal.");
                }
            }

            if (_ownsConnection)
            {
                try
                {
                    Connection?.Dispose();
                    _logger?.LogTrace("[AmbientUnitOfWork] Connection disposed.");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[AmbientUnitOfWork] Exception during connection disposal.");
                }
            }

            s_ambient.Value = null;
            _disposed = true;
        }
    }
}

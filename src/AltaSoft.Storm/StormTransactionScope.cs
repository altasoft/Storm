using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using Microsoft.Extensions.Logging;

namespace AltaSoft.Storm;

/// <summary>
/// Lightweight transaction scope for Storm. Controls the ambient transaction state used by <see cref="StormContext"/>.
/// Typical usage:
/// <code>await using (var scope = new StormTransactionScope(StormTransactionScopeOption.JoinExisting)) { ... await scope.CompleteAsync(CancellationToken.None); }</code>
/// The scope either joins an existing ambient transaction or creates a new one depending on the option.
/// When disposed the scope will commit the ambient transaction if <see cref="CompleteAsync"/> was called on the
/// outermost scope; otherwise it will rollback.
/// </summary>
public sealed class StormTransactionScope : IDisposable
{
    private static readonly AsyncLocal<StormTransactionScope?> s_current = new();

    private bool _disposed;

    private readonly ILogger? _logger;

    // track previous scope so we can restore it when this scope is disposed
    private readonly StormTransactionScope? _previousScope;

    /// <summary>
    /// Gets the current scope for the logical async context, or <c>null</c> if there is none or the current scope is disposed.
    /// </summary>
    public static StormTransactionScope? Current => s_current.Value is { _disposed: false } current ? current : null;

    /// <summary>
    /// Gets a value indicating whether this scope was marked as successfully completed.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// The ambient transaction state associated with this scope.
    /// This is the shared state when joining existing scopes or a new state when creating a new scope.
    /// </summary>
    internal AmbientState Ambient { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this scope is the root scope that created the ambient state.
    /// When <c>true</c> the scope is responsible for disposing the ambient connection/transaction.
    /// </summary>
    public bool IsRoot { get; }

    ///// <summary>
    ///// Indicates whether ambient transactions are suppressed for this scope.
    ///// </summary>
    //public bool IsSuppressed => _suppressed;

    /// <summary>
    /// Creates a new instance of <see cref="StormTransactionScope"/> with the specified internal options.
    /// This private constructor is used by public overloads and accepts optional connection/transaction instances
    /// for scenarios where an existing connection or transaction should be used by the new scope.
    /// </summary>
    /// <param name="scopeOption">Controls whether to join existing ambient or create a new one.</param>
    /// <param name="connectionToUse">Optional existing connection to attach to the created ambient (for CreateNew).</param>
    /// <param name="transactionToUse">Optional existing transaction to attach to the created ambient (for CreateNew).</param>
    /// <param name="logger">Optional logger used for tracing and error messages.</param>
    private StormTransactionScope(StormTransactionScopeOption scopeOption, StormDbConnection? connectionToUse, StormDbTransaction? transactionToUse, ILogger? logger)
    {
        _logger = logger;

        // capture previous scope and its ambient (if any) so we can restore previous scope on dispose
        _previousScope = s_current.Value;
        var previousAmbient = _previousScope?.Ambient;

        switch (scopeOption)
        {
            case StormTransactionScopeOption.JoinExisting:
                if (connectionToUse is not null)
                    throw new ArgumentException("Cannot provide existing connection when joining existing scope.", nameof(connectionToUse));
                if (transactionToUse is not null)
                    throw new ArgumentException("Cannot provide existing transaction when joining existing scope.", nameof(transactionToUse));

                if (previousAmbient is not null)
                {
                    Ambient = previousAmbient; // join existing ambient from previous scope
                    IsRoot = false;
                    _logger?.LogTrace("[StormTransactionScope] Joined existing ambient transaction state.");
                }
                else
                {
                    Ambient = new AmbientState(null, _logger);
                    IsRoot = true;
                    _logger?.LogTrace("[StormTransactionScope] Created new ambient transaction state (JoinExisting - no existing).");
                }
                break;

            case StormTransactionScopeOption.CreateNew:
                var ambient = new AmbientState(previousAmbient, _logger);
                connectionToUse ??= transactionToUse?.Connection;
                if (connectionToUse is not null)
                {
                    ambient._connection = connectionToUse;
                    ambient._ownsConnection = false;

                    if (transactionToUse is not null)
                    {
                        ambient._transaction = transactionToUse;
                        ambient._ownsTransaction = false;
                    }
                }
                Ambient = ambient;
                IsRoot = true;

                if (logger is not null)
                {
                    if (transactionToUse is not null)
                        logger.LogTrace("[StormTransactionScope] Created new ambient transaction state (CreateNew) using provided transaction.");
                    else
                        logger.LogTrace("[StormTransactionScope] Created new ambient transaction state (CreateNew).");
                }
                break;

            default:
                throw new InvalidEnumArgumentException(nameof(scopeOption), (int)scopeOption, typeof(StormTransactionScopeOption));
        }

        s_current.Value = this;

        Ambient.TransactionCount++;
        _logger?.LogTrace("[StormTransactionScope] Scope created, TransactionCount={TransactionCount}", Ambient.TransactionCount);
    }

    /// <summary>
    /// Initializes a new scope that will join an existing ambient transaction if one exists; otherwise a new ambient is created.
    /// </summary>
    public StormTransactionScope() : this(StormTransactionScopeOption.JoinExisting)
    {
    }

    /// <summary>
    /// Initializes a new scope using the provided <paramref name="scopeOption"/>.
    /// </summary>
    /// <param name="scopeOption">Determines whether to join an existing ambient transaction or create a new one.</param>
    public StormTransactionScope(StormTransactionScopeOption scopeOption) : this(scopeOption, null, null, StormManager.Logger)
    {
    }

    /// <summary>
    /// Initializes a new scope that uses an existing transaction instance. The scope will create a new ambient
    /// state that references the provided transaction and (optionally) its connection.
    /// </summary>
    /// <param name="transactionToUse">Existing transaction which will be used by the scope.</param>
    public StormTransactionScope(StormDbTransaction transactionToUse) : this(StormTransactionScopeOption.CreateNew, transactionToUse.Connection, transactionToUse, StormManager.Logger)
    {
    }

    /// <summary>
    /// Initializes a new scope that uses an existing connection. A new transaction will be created on the provided connection.
    /// The scope will own the created transaction but will not own the provided connection instance.
    /// </summary>
    /// <param name="connectionToUse">Existing connection to use for the new scope.</param>
    public StormTransactionScope(StormDbConnection connectionToUse) : this(StormTransactionScopeOption.CreateNew, connectionToUse, null, StormManager.Logger)
    {
    }

    /// <summary>
    /// Determines whether there is an active transaction in the unit of work.
    /// </summary>
    public bool HasActiveTransaction() => !IsCompleted && Ambient is { IsRollBacked: false, TransactionCount: > 0 };

    /// <summary>
    /// Marks the scope as completed (successful) and attempts to commit the ambient transaction when this is the outermost scope.
    /// If the commit fails an attempt to rollback is made and the original exception is rethrown.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to observe while committing the transaction.</param>
    /// <returns>A <see cref="Task"/> that completes when commit (if required) has finished.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the scope has already been disposed.</exception>
    /// <exception cref="StormException">Thrown when transaction nesting is inconsistent.</exception>
    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        _logger?.LogTrace("[StormTransactionScope] CompleteAsync called.");

        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsCompleted)
            return;

        IsCompleted = true;

        if (--Ambient.TransactionCount < 0)
            throw new StormException("TransactionCount < 0.");

        if (Ambient.TransactionCount == 0)
        {
            try
            {
                await Ambient.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[StormTransactionScope] Commit failed.");
                try
                {
                    await Ambient.RollbackAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    _logger?.LogError(ex2, "[StormTransactionScope] Rollback after failed commit also failed.");
                }
                throw;
            }
        }
    }

    /// <summary>
    /// Returns the ambient state associated with the current scope or <c>null</c> when there is no current scope.
    /// </summary>
    /// <returns>The current <see cref="AmbientState"/> or <c>null</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static AmbientState? GetCurrentAmbient() => Current?.Ambient;

    /// <summary>
    /// Synchronously disposes the scope. If <see cref="CompleteAsync"/> was called and this is the outermost scope
    /// the ambient transaction will be committed; otherwise a rollback is executed. The previous scope in the
    /// async context is restored when the scope is disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // restore previous scope (if any)
        s_current.Value = _previousScope;

        try
        {
            if (!IsCompleted)
            {
                try
                {
                    Ambient.Rollback();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[StormTransactionScope] Rollback during Dispose failed.");
                }
            }
        }
        finally
        {
            // if we created ambient for this scope, dispose it
            if (IsRoot)
            {
                try
                {
                    Ambient.Dispose();
                    Ambient = null!;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[StormTransactionScope] Exception during ambient dispose.");
                }
            }

            _disposed = true;

            _logger?.LogTrace("[StormTransactionScope] Disposed scope.");
        }
    }
}

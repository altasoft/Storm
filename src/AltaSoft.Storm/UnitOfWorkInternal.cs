//using System;
//using System.Data;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Transactions;
//using AltaSoft.Storm.Exceptions;
//using AltaSoft.Storm.Extensions;
//using Microsoft.Extensions.Logging;

//namespace AltaSoft.Storm;

///// <summary>
///// Represents a unit of work for performing database operations within a transaction scope.
///// Handles transaction management, commit/rollback, and integration with an ambient unit of work context.
///// </summary>
//internal sealed class UnitOfWorkInternal : IUnitOfWork
//{
//    /// <summary>
//    /// The logger instance for this unit of work, or null if logging is disabled.
//    /// </summary>
//    private readonly ILogger? _logger;

//    /// <summary>
//    /// Indicates whether the unit of work has been committed.
//    /// </summary>
//    private bool _committed;

//    /// <summary>
//    /// Indicates whether the unit of work has been disposed.
//    /// </summary>
//    private bool _disposed;

//    /// <summary>
//    /// Gets a value indicating whether this instance is the root unit of work (i.e., it created the ambient unit of work).
//    /// Only the root unit of work disposes the ambient context and underlying resources.
//    /// </summary>
//    public bool IsRoot { get; }

//    /// <summary>
//    /// Gets the associated <see cref="AltaSoft.Storm.AmbientUnitOfWork"/> for advanced/test scenarios.
//    /// </summary>
//    public AmbientUnitOfWork AmbientUow { get; }

//    /// <summary>
//    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
//    /// Private; use <see cref="UnitOfWork.Create"/> or <see cref="UnitOfWork.CreateStandalone"/> to construct.
//    /// </summary>
//    /// <param name="ambientUow">The ambient unit of work context.</param>
//    /// <param name="isRoot">Indicates if this is the root unit of work.</param>
//    /// <param name="logger">The logger instance.</param>
//    internal UnitOfWorkInternal(AmbientUnitOfWork ambientUow, bool isRoot, ILogger? logger)
//    {
//        AmbientUow = ambientUow;
//        IsRoot = isRoot;
//        _logger = logger;

//        _logger?.LogTrace("[UnitOfWork] IsRoot={IsRoot}, TransactionCount={TransactionCount}", isRoot, AmbientUow.TransactionCount);
//    }

//    /// <summary>
//    /// Begins a new transaction using the specified connection string.
//    /// If this is the root unit of work, a new connection and transaction are created.
//    /// Otherwise, validates the connection string against the ambient context.
//    /// </summary>
//    /// <remarks>
//    /// The returned <see cref="UnitOfWorkTransaction"/> must be used with <c>await using</c>
//    /// to ensure proper disposal and rollback/commit semantics.
//    /// </remarks>
//    /// <param name="connectionString">The database connection string.</param>
//    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
//    /// <returns>A task representing the asynchronous operation that returns a <see cref="UnitOfWorkTransaction"/>.</returns>
//    /// <exception cref="StormException">
//    /// Thrown if the ambient unit of work is not initialized or the connection string does not match.
//    /// </exception>
//    public async Task<IUnitOfWorkTransaction> BeginAsync(string connectionString, CancellationToken cancellationToken)
//    {
//        _logger?.LogTrace("[UnitOfWork] BeginAsync: Starting with connection string. IsRoot={IsRoot}", IsRoot);
//        if (IsRoot)
//        {
//            StormDbTransaction? transaction = null;
//            var connection = new StormDbConnection(connectionString);
//            try
//            {
//                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

//                transaction = (StormDbTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

//                _logger?.LogTrace("[UnitOfWork] Root: Opened new connection and started transaction.");
//            }
//            catch
//            {
//                if (transaction is not null)
//                    await transaction.DisposeAsync().ConfigureAwait(false);
//                await connection.DisposeAsync().ConfigureAwait(false);
//                throw;
//            }

//            AmbientUow.Initialize(connection, true, transaction, true);
//        }
//        else
//        {
//            if (!AmbientUow.IsInitialized)
//            {
//                _logger?.LogError("[UnitOfWork] BeginAsync: AmbientUnitOfWork is not initialized.");
//                throw new StormException("AmbientUnitOfWork is not initialized.");
//            }

//            if (!AmbientUow.Connection.ConnectionString.IsSameConnectionString(connectionString))
//            {
//                _logger?.LogError("[UnitOfWork] BeginAsync: Provided connection string does not match the ambient context.");
//                throw new StormException("Provided connection string must match the ambient unit of work connection.");
//            }
//        }

//        AmbientUow.IncrementTx();
//        _logger?.LogTrace("[UnitOfWork] BeginAsync: Transaction count incremented to {TransactionCount}", AmbientUow.TransactionCount);

//        return new UnitOfWorkTransaction(this);
//    }

//    /// <summary>
//    /// Begins a new transaction using the specified connection and optional transaction.
//    /// If this is the root unit of work, a new transaction is created if not provided.
//    /// Otherwise, validates the connection string against the ambient context.
//    /// </summary>
//    /// <param name="connection">The database connection.</param>
//    /// <param name="transaction">The database transaction, or null to create a new one.</param>
//    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
//    /// <returns>A task representing the asynchronous operation that returns a <see cref="UnitOfWorkTransaction"/>.</returns>
//    /// <exception cref="StormException">
//    /// Thrown if the transaction is not associated with the connection, the ambient unit of work is not initialized, or the connection string does not match.
//    /// </exception>
//    public async Task<IUnitOfWorkTransaction> BeginAsync(StormDbConnection connection, StormDbTransaction? transaction, CancellationToken cancellationToken)
//    {
//        _logger?.LogTrace("[UnitOfWork] BeginAsync: Starting with provided connection and transaction. IsRoot={IsRoot}", IsRoot);
//        if (transaction is not null && transaction.Connection != connection)
//        {
//            _logger?.LogError("[UnitOfWork] BeginAsync: Provided transaction is not associated with the provided connection.");
//            throw new StormException("Provided transaction must be associated with the provided connection.");
//        }

//        if (IsRoot)
//        {
//            var ownsTransaction = false;
//            if (transaction is null)
//            {
//                transaction = (StormDbTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
//                ownsTransaction = true;
//                _logger?.LogTrace("[UnitOfWork] Root: Created new transaction for provided connection.");
//            }

//            AmbientUow.Initialize(connection, false, transaction, ownsTransaction);
//            _logger?.LogTrace("[UnitOfWork] Root: Initialized provided connection and transaction.");
//        }
//        else
//        {
//            if (!AmbientUow.IsInitialized)
//            {
//                _logger?.LogError("[UnitOfWork] BeginAsync: AmbientUnitOfWork is not initialized.");
//                throw new StormException("AmbientUnitOfWork is not initialized.");
//            }

//            if (!AmbientUow.Connection.ConnectionString.IsSameConnectionString(connection.ConnectionString))
//            {
//                _logger?.LogError("[UnitOfWork] BeginAsync: Provided connection string does not match the ambient context.");
//                throw new StormException("Provided connection string must match the ambient unit of work connection.");
//            }
//        }

//        if (connection.State != ConnectionState.Open)
//        {
//            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
//            _logger?.LogTrace("[UnitOfWork] BeginAsync: Opened provided connection.");
//        }

//        AmbientUow.IncrementTx();
//        _logger?.LogTrace("[UnitOfWork] BeginAsync: Transaction count incremented to {TransactionCount}", AmbientUow.TransactionCount);

//        return new UnitOfWorkTransaction(this);
//    }

//    /// <summary>
//    /// Determines whether there is an active transaction in the unit of work.
//    /// </summary>
//    public bool HasActiveTransaction() => !_committed && AmbientUow is { IsRollBacked: false, TransactionCount: > 0 };

//    ///// <summary>
//    ///// Gets the logger instance if logging is enabled at the Trace level; otherwise, returns null.
//    ///// </summary>
//    ///// <returns>The <see cref="ILogger"/> instance or null.</returns>
//    //private static ILogger? GetLogger()
//    //    => StormManager.Logger?.IsEnabled(LogLevel.Trace) == true ? StormManager.Logger : null;

//    /// <summary>
//    /// Synchronously disposes the unit of work. Rolls back the transaction if CompleteAsync was not called.
//    /// </summary>
//    public void Dispose()
//    {
//        if (_disposed)
//            return;

//        if (IsRoot)
//        {
//            AmbientUow.Dispose();
//        }
//        _disposed = true;
//    }

//    /// <summary>
//    /// Asynchronously rolls back the transaction if it was not committed.
//    /// </summary>
//    /// <returns>A task representing the asynchronous rollback operation.</returns>
//    private async Task RollbackIfNotCommitedAsync()
//    {
//        if (!HasActiveTransaction())
//            return;

//        try
//        {
//            await AmbientUow.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
//        }
//        catch (Exception ex)
//        {
//            _logger?.LogError(ex, "UnitOfWork rollback during DisposeAsync failed");
//        }
//    }

//    /// <summary>
//    /// Represents a transaction scope within a <see cref="UnitOfWork"/>. 
//    /// Handles commit and rollback logic for the transaction.
//    /// </summary>
//    private sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
//    {
//        /// <summary>
//        /// The parent <see cref="UnitOfWorkInternal"/> instance.
//        /// </summary>
//        private readonly UnitOfWorkInternal _uow;

//        /// <summary>
//        /// Indicates whether the transaction has been disposed.
//        /// </summary>
//        private bool _disposed;

//        /// <summary>
//        /// Initializes a new instance of the <see cref="UnitOfWorkTransaction"/> class.
//        /// </summary>
//        /// <param name="uow">The parent <see cref="UnitOfWork"/> instance.</param>
//        internal UnitOfWorkTransaction(UnitOfWorkInternal uow)
//        {
//            _uow = uow;
//        }

//        /// <summary>
//        /// Asynchronously completes (commits) the transaction.
//        /// </summary>
//        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
//        /// <returns>A task representing the asynchronous commit operation.</returns>
//        /// <exception cref="ObjectDisposedException">Thrown if the transaction has already been disposed.</exception>
//        public async Task CompleteAsync(CancellationToken cancellationToken)
//        {
//            ObjectDisposedException.ThrowIf(_disposed, nameof(UnitOfWork));

//            await _uow.AmbientUow.CommitAsync(cancellationToken).ConfigureAwait(false);

//            _uow._committed = true;
//        }

//        /// <summary>
//        /// Asynchronously disposes the transaction, rolling back if not committed.
//        /// </summary>
//        /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
//        public async ValueTask DisposeAsync()
//        {
//            if (_disposed)
//                return;

//            await _uow.RollbackIfNotCommitedAsync().ConfigureAwait(false);
//            _disposed = true;
//        }
//    }
//}

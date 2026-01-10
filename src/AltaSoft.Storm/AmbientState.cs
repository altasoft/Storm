using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using Microsoft.Extensions.Logging;

namespace AltaSoft.Storm;

/// <summary>
/// Ambient transaction state used by the scope chain.
/// </summary>
internal sealed class AmbientState : IDisposable
{
    internal AmbientState? Previous { get; }

    internal StormDbConnection? _connection;
    internal StormDbTransaction? _transaction;

    private bool _isRollBacked;
    internal bool _ownsConnection;
    internal bool _ownsTransaction;

    internal int TransactionCount { get; set; }

    private readonly ILogger? _logger;

    internal AmbientState(AmbientState? previous, ILogger? logger)
    {
        Previous = previous;
        _logger = logger;
        TransactionCount = 0;
        _isRollBacked = false;
    }

#pragma warning disable IDE0032
    internal StormDbConnection? Connection => _connection;

    internal StormDbTransaction? Transaction => _transaction;
#pragma warning restore IDE0032

    internal async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_isRollBacked)
            throw new StormException("Transaction has already been rolled back. Commit is not allowed.");

        if (TransactionCount != 0)
            throw new StormException("Cannot commit while nested transactions remain.");

        if (_ownsTransaction && _transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _transaction = null;
            _logger?.LogTrace("[StormTransactionScope] Transaction committed successfully.");
        }
    }

    internal async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_isRollBacked)
            return;

        try
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                _transaction = null;
                _logger?.LogWarning("[StormTransactionScope] Transaction rolled back.");
            }
        }
        finally
        {
            _isRollBacked = true;
            TransactionCount = 0;
        }
    }

    internal void Rollback()
    {
        if (_isRollBacked)
            return;

        try
        {
            if (_transaction is not null)
            {
                _transaction.Rollback();
                _transaction = null;
                _logger?.LogWarning("[StormTransactionScope] Transaction rolled back.");
            }
        }
        finally
        {
            _isRollBacked = true;
            TransactionCount = 0;
        }
    }

    internal StormDbConnection GetConnection(string connectionString)
    {
        if (_connection is null)
        {
            _connection = new StormDbConnection(connectionString);
            _ownsConnection = true;
            return _connection;
        }

        if (_connection.ConnectionString != connectionString)
            throw new StormException("Ambient connection string does not match the requested connection string.");
        return _connection;
    }

    internal async ValueTask<StormDbTransaction?> GetTransactionAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(_connection is not null);

        if (_transaction is not null)
            return _transaction;

        _transaction = (StormDbTransaction)await _connection!.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        _ownsTransaction = true;
        return _transaction;
    }

    public void Dispose()
    {
        if (_ownsTransaction && _transaction is not null)
        {
            try
            {
                _transaction.Dispose();
                _transaction = null;
                _logger?.LogTrace("[StormTransactionScope] Transaction disposed.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[StormTransactionScope] Exception during transaction disposal.");
            }
        }

        if (_ownsConnection && _connection is not null)
        {
            try
            {
                _connection.Dispose();
                _connection = null;
                _logger?.LogTrace("[StormTransactionScope] Connection disposed.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[StormTransactionScope] Exception during connection disposal.");
            }
        }
    }
}

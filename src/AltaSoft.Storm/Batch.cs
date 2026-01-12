using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;

namespace AltaSoft.Storm;

/// <summary>
/// Represents a batch operation that groups multiple SQL commands for execution.
/// </summary>
public sealed class Batch : IAsyncDisposable, IDisposable
{
    private readonly StormContext _context;
    private readonly StormDbBatch _batch;

    /// <summary>
    /// Initializes a new instance of the <see cref="Batch"/> class with the specified <see cref="StormContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="StormContext"/> used to manage the database connection.</param>
    internal Batch(StormContext context)
    {
        _context = context;
        _batch = new StormDbBatch();
    }

    /// <summary>
    /// Asynchronously disposes the resources used by the <see cref="Batch"/> instance.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync() => _batch.DisposeAsync();

    /// <summary>
    /// Disposes the resources used by the <see cref="Batch"/> instance.
    /// </summary>
    public void Dispose() => _batch.Dispose();

    /// <summary>
    /// Adds a Storm SQL statement to the batch for execution.
    /// </summary>
    /// <param name="sqlStatement">The <see cref="ISqlGo"/> command to add to the batch.</param>
    public void Add(ISqlGo sqlStatement)
    {
        sqlStatement.GenerateBatchCommands(_batch.Commands);
    }

    /// <summary>
    /// Adds a collection of Storm SQL statements to the batch for execution.
    /// </summary>
    /// <param name="sqlStatements">The collection of <see cref="ISqlGo"/> commands to add to the batch.</param>
    public void AddRange(IEnumerable<ISqlGo> sqlStatements)
    {
        foreach (var sqlStatement in sqlStatements)
        {
            sqlStatement.GenerateBatchCommands(_batch.Commands);
        }
    }

    /// <summary>
    /// Executes the batch of SQL commands asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the number of rows affected by the batch execution.
    /// </returns>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_batch.Commands.Count == 0)
            return 0;

        var (connection, transaction) = await _context.EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

        _batch.Connection = connection;
        _batch.Transaction = transaction;

        return await _batch.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }
}

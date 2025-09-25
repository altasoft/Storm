using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;

namespace AltaSoft.Storm;

/// <summary>
/// Defines a unit of work abstraction for managing database transactions and ambient context,
/// extending <see cref="IUnitOfWorkStandalone"/> with support for explicit connection and transaction management.
/// </summary>
public interface IUnitOfWork : IUnitOfWorkStandalone
{
    /// <summary>
    /// Begins a new transaction using the specified <paramref name="connection"/> and optional <paramref name="transaction"/>.
    /// <para>
    /// If this is the root unit of work, a new transaction is created if <paramref name="transaction"/> is <c>null</c>.
    /// Otherwise, validates the connection string against the ambient context.
    /// </para>
    /// </summary>
    /// <param name="connection">The database connection to use for the transaction.</param>
    /// <param name="transaction">The database transaction to use, or <c>null</c> to create a new one.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task representing the asynchronous operation that returns an <see cref="IUnitOfWorkTransaction"/>.
    /// </returns>
    /// <exception cref="StormException">
    /// Thrown if the transaction is not associated with the connection, the ambient unit of work is not initialized, or the connection string does not match.
    /// </exception>
    Task<IUnitOfWorkTransaction> BeginAsync(StormDbConnection connection, StormDbTransaction? transaction, CancellationToken cancellationToken);
}

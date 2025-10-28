using System;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;

namespace AltaSoft.Storm;

/// <summary>
/// Defines a standalone unit of work abstraction for managing database transactions and ambient context.
/// </summary>
public interface IUnitOfWorkStandalone : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether this instance is the root unit of work (i.e., it created the ambient unit of work).
    /// Only the root unit of work disposes the ambient context and underlying resources.
    /// </summary>
    bool IsRoot { get; }

    /// <summary>
    /// Begins a new transaction using the specified connection string.
    /// If this is the root unit of work, a new connection and transaction are created.
    /// Otherwise, validates the connection string against the ambient context.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="IUnitOfWorkTransaction"/> must be used with <c>await using</c>
    /// to ensure proper disposal and rollback/commit semantics.
    /// </remarks>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task representing the asynchronous operation that returns an <see cref="IUnitOfWorkTransaction"/>.
    /// </returns>
    /// <exception cref="StormException">
    /// Thrown if the ambient unit of work is not initialized or the connection string does not match.
    /// </exception>
    Task<IUnitOfWorkTransaction> BeginAsync(string connectionString, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether there is an active transaction in the unit of work.
    /// </summary>
    bool HasActiveTransaction();

    /// <summary>
    /// Gets the associated <see cref="AmbientUnitOfWork"/> for advanced or test scenarios.
    /// </summary>
    internal AmbientUnitOfWork AmbientUow { get; }
}

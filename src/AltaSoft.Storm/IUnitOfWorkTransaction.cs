using System;
using System.Threading;
using System.Threading.Tasks;

namespace AltaSoft.Storm;

/// <summary>
/// Represents an asynchronous transaction unit of work that can be completed (committed) or disposed.
/// </summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    /// <summary>
    /// Asynchronously completes (commits) the transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task representing the asynchronous commit operation.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the transaction has already been disposed.
    /// </exception>
    Task CompleteAsync(CancellationToken cancellationToken);
}

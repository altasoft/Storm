using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for a result that returns an integer asynchronously.
/// </summary>
public interface ISqlGo
{
    /// <summary>
    /// Asynchronously performs the 'Go' operation and returns an integer result.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which will return an integer result.</returns>
    Task<int> GoAsync(CancellationToken cancellationToken = default);

    internal void GenerateBatchCommands(List<StormDbBatchCommand> batchCommands);
}

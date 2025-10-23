using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Defines an interface for executing a database command or batch operation that returns an integer result.
/// Provides an asynchronous execution method and internal support for batch command generation.
/// </summary>
public interface ISqlGo
{
    /// <summary>
    /// Executes the operation and returns an integer result, such as the number of affected rows.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The integer result of the operation.</returns>
    Task<int> GoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the batch commands required for this operation.
    /// Intended for internal use.
    /// </summary>
    /// <param name="batchCommands">The list to which batch commands will be added.</param>
    internal void GenerateBatchCommands(List<StormDbBatchCommand> batchCommands);
}

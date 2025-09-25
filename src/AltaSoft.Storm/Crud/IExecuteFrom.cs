using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for classes that can execute a procedure from a specified data type T and return a result of type TOutput.
/// T must implement the IDataBindable interface.
/// TOutput must be a subclass of StormProcedureResult and have a parameterless constructor.
/// </summary>
public interface IExecuteFrom<T, TOutput>
    where T : IDataBindable
    where TOutput : StormProcedureResult, new()
{
    #region Builder

    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    IExecuteFrom<T, TOutput> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IExecuteFrom<T, TOutput> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Specifies to start automatic change tracking.
    /// </summary>
    IExecuteFrom<T, TOutput> WithTracking();

    /// <summary>
    /// Specifies not to start automatic change tracking.
    /// </summary>
    IExecuteFrom<T, TOutput> WithNoTracking();

    #endregion Builder

    /// <summary>
    /// Specifies instance, where to output results of Stored Procedure execution.
    /// </summary>
    IExecuteFrom<T, TOutput> OutputResultInto(out TOutput output);

    #region Get

    /// <summary>
    /// Asynchronously retrieves the first or default entity of type T from the database that matches the specified conditions.
    /// This method considers the partial load flags and any set query parameters.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain the first or default entity of type T.</returns>
    Task<T?> GetAsync(CancellationToken cancellationToken = default);

    #endregion Get

    #region List & Stream

    /// <summary>
    /// Asynchronously retrieves a list of entities of type T from the database based on the set query parameters.
    /// Considers partial load flags and order by column IDs for sorting.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain a list of entities matching the criteria.</returns>
    Task<List<T>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams data of type T asynchronously from the database using the provided connection and set query parameters.
    /// This method is useful for processing large datasets that should not be loaded into memory all at once.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>An IAsyncEnumerable of T representing the stream of data from the database.</returns>
    IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default);

    #endregion List & Stream
}

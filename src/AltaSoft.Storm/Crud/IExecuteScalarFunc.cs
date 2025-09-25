using System.Threading;
using System.Threading.Tasks;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for defining a method that executes a scalar function and returns a result of type TResult.
/// </summary>
public interface IExecuteScalarFunc<TResult>
{
    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    IExecuteScalarFunc<TResult> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IExecuteScalarFunc<TResult> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Asynchronously executes a function from the database that returns a single value.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain the result of the function.</returns>
    Task<TResult> GetAsync(CancellationToken cancellationToken = default);
}

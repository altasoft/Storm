using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for deleting rows from a table
/// </summary>
public interface IDeleteFromSingle<T> : ISqlGo where T : IDataBindable
{
    #region Builder

    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    IDeleteFromSingle<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IDeleteFromSingle<T> WithCommandTimeOut(int commandTimeout);

    #endregion Builder
}

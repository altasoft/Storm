using System.Collections.Generic;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for deleting rows from a table
/// </summary>
public interface IInsertInto<in T> : ISqlGo where T : IDataBindable
{
    #region Builder

    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    /// <returns>The interface with the connection closed after query.</returns>
    IInsertInto<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IInsertInto<T> WithCommandTimeOut(int commandTimeout);

    #endregion Builder

    /// <summary>
    /// Specifies the value to be inserted into the table.
    /// </summary>
    /// <param name="value">The single value to be inserted.</param>
    /// <returns>The interface with the specified value to be inserted.</returns>
    ISqlGo Values(T value);

    /// <summary>
    /// Specifies the values to be inserted into the table.
    /// </summary>
    /// <param name="values">The collection of values to be inserted.</param>
    /// <returns>The interface with the specified values to be inserted.</returns>
    ISqlGo Values(IEnumerable<T> values);
}

using System;
using System.Linq.Expressions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for deleting rows from a table
/// </summary>
public interface IDeleteFrom<T> : ISqlGo where T : IDataBindable
{
    #region Builder

    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    IDeleteFrom<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IDeleteFrom<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Specifies how many rows to return (top).
    /// </summary>
    /// <param name="rowCount">The number of rows to fetch from the data source.</param>
    IDeleteFrom<T> Top(int rowCount);

    /// <summary>
    /// Filters the elements of the interface based on a specified condition defined by the provided expression.
    /// </summary>
    IDeleteFrom<T> Where(Expression<Func<T, bool>> whereExpression);

    /// <summary>
    /// Filters the elements of the interface based on a specified OData filter.
    /// </summary>
    IDeleteFrom<T> Where(string oDataFilter);

    #endregion Builder
}

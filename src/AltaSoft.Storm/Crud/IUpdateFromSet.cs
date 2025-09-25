using System;
using System.Linq.Expressions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for updating data in a table
/// </summary>
public interface IUpdateFromSet<T> : ISqlGo where T : IDataBindable
{
    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    IUpdateFromSet<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IUpdateFromSet<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Filters the elements of the interface based on a specified condition defined by the provided expression.
    /// </summary>
    IUpdateFromSet<T> Where(Expression<Func<T, bool>> whereExpression);

    /// <summary>
    /// Filters the elements of the interface based on a specified OData filter.
    /// </summary>
    IUpdateFromSet<T> Where(string oDataFilter);

    /// <summary>
    /// Specifies how many rows to return (top).
    /// </summary>
    /// <param name="rowCount">The number of rows to fetch from the data source.</param>
    IUpdateFromSet<T> Top(int rowCount);

    /// <summary>
    /// Sets the value of a specified column for an entity of type T using the provided column selector expression.
    /// </summary>
    IUpdateFromSet<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, TValue value);

    /// <summary>
    /// Sets the value of a specified column for an entity of type T using the provided column selector expression.
    /// </summary>
    IUpdateFromSet<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, Expression<Func<T, TValue?>> valueSelector);
}

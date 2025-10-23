using System;
using System.Linq.Expressions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Defines a builder and execution interface for constructing and executing UPDATE queries
/// with column-level value assignments for entities of type <typeparamref name="T"/>.
/// Supports filtering, limiting, and additional column updates. Intended for use with
/// types that implement <see cref="IDataBindable"/>.
/// </summary>
/// <typeparam name="T">The entity type to update, which must implement <see cref="IDataBindable"/>.</typeparam>
public interface IUpdateFromSet<T> : ISqlGo where T : IDataBindable
{
    /// <summary>
    /// Specifies that the connection should be closed after the query is executed.
    /// </summary>
    IUpdateFromSet<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IUpdateFromSet<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Adds a filter to the update query using a LINQ expression.
    /// </summary>
    /// <param name="whereExpression">The expression used to filter entities to update.</param>
    IUpdateFromSet<T> Where(Expression<Func<T, bool>> whereExpression);

    /// <summary>
    /// Adds a filter to the update query using an OData filter string.
    /// </summary>
    /// <param name="oDataFilter">The OData filter string.</param>
    IUpdateFromSet<T> Where(string oDataFilter);

    /// <summary>
    /// Limits the number of rows affected by the update query.
    /// </summary>
    /// <param name="rowCount">The maximum number of rows to update.</param>
    IUpdateFromSet<T> Top(int rowCount);

    /// <summary>
    /// Sets the value of a specified column for the update operation using the provided column selector and value.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="columnSelector">The expression used to select the column.</param>
    /// <param name="value">The value to set for the column.</param>
    /// <returns>The interface for further configuration or execution.</returns>
    IUpdateFromSet<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, TValue value);

    /// <summary>
    /// Sets the value of a specified column for the update operation using the provided column selector and value selector expressions.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="columnSelector">The expression used to select the column.</param>
    /// <param name="valueSelector">The expression used to determine the value to set for the column.</param>
    /// <returns>The interface for further configuration or execution.</returns>
    IUpdateFromSet<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, Expression<Func<T, TValue?>> valueSelector);
}

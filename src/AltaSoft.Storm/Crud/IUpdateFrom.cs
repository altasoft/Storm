using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for updating data in a table
/// </summary>
public interface IUpdateFrom<T> : ISqlGo where T : IDataBindable
{
    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    IUpdateFrom<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IUpdateFrom<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Sets the value of the specified type T in an SQL query.
    /// </summary>
    /// <param name="value">The value to set in the SQL query.</param>
    /// <returns>The interface with the specified value set.</returns>
    ISqlGo Set(T value);

    /// <summary>
    /// Sets the values in the SQL database for the specified collection of objects.
    /// </summary>
    /// <param name="values">The collection of objects whose values should be set in the SQL database.</param>
    /// <returns>The interface with the specified values set.</returns>
    ISqlGo Set(IEnumerable<T> values);

    /// <summary>
    /// Sets the value of a specified column for an entity of type T using the provided column selector expression.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="columnSelector">The expression used to select the column.</param>
    /// <param name="value">The value to set for the column.</param>
    /// <returns>The interface with the specified column value set.</returns>
    IUpdateFromSet<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, TValue value);

    /// <summary>
    /// Sets the value of a specified column for an entity of type T using the provided column selector expression.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="columnSelector">The expression used to select the column.</param>
    /// <param name="valueSelector">The expression used to set the column value.</param>
    /// <returns>The interface with the specified column value set.</returns>
    IUpdateFromSet<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, Expression<Func<T, TValue?>> valueSelector);

    /// <summary>
    /// Enables concurrency check for the update operation.
    /// </summary>
    /// <returns>The interface with concurrency check enabled.</returns>
    IUpdateFrom<T> WithConcurrencyCheck();

    /// <summary>
    /// Disables concurrency check for the update operation.
    /// </summary>
    /// <returns>The interface with concurrency check disabled.</returns>
    IUpdateFrom<T> WithoutConcurrencyCheck();
}

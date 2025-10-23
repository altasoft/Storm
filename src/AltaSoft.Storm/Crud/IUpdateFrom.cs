using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Defines a builder and execution interface for constructing and executing UPDATE queries
/// against a table for entities of type <typeparamref name="T"/>. Supports setting values,
/// column updates, concurrency control, and command configuration. Intended for use with
/// types that implement <see cref="IDataBindable"/>.
/// </summary>
/// <typeparam name="T">The entity type to update, which must implement <see cref="IDataBindable"/>.</typeparam>
public interface IUpdateFrom<T> : ISqlGo where T : IDataBindable
{
    /// <summary>
    /// Specifies that the connection should be closed after the query is executed.
    /// </summary>
    IUpdateFrom<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IUpdateFrom<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Sets the values for the update operation using the provided entity instance.
    /// </summary>
    /// <param name="value">The entity instance whose values should be set in the update.</param>
    /// <returns>An <see cref="ISqlGo"/> interface for further configuration or execution.</returns>
    ISqlGo Set(T value);

    /// <summary>
    /// Sets the values for the update operation using the provided collection of entity instances.
    /// </summary>
    /// <param name="values">The collection of entities whose values should be set in the update.</param>
    /// <returns>An <see cref="ISqlGo"/> interface for further configuration or execution.</returns>
    ISqlGo Set(IEnumerable<T> values);

    /// <summary>
    /// Sets the value of a specified column for the update operation using the provided column selector and value.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="columnSelector">The expression used to select the column.</param>
    /// <param name="value">The value to set for the column.</param>
    /// <returns>An <see cref="IUpdateFromSet{T}"/> interface for further configuration or execution.</returns>
    IUpdateFromSet<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, TValue value);

    /// <summary>
    /// Sets the value of a specified column for the update operation using the provided column selector and value selector expressions.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="columnSelector">The expression used to select the column.</param>
    /// <param name="valueSelector">The expression used to determine the value to set for the column.</param>
    /// <returns>An <see cref="IUpdateFromSet{T}"/> interface for further configuration or execution.</returns>
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

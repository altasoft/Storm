using System;
using System.Linq.Expressions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Defines a builder and execution interface for constructing and executing UPDATE queries
/// that set column values for a single entity of type <typeparamref name="T"/>.
/// Supports command configuration and column-level value assignments. Intended for use with
/// types that implement <see cref="IDataBindable"/>.
/// </summary>
/// <typeparam name="T">The entity type to update, which must implement <see cref="IDataBindable"/>.</typeparam>
public interface IUpdateFromSetSingle<T> : ISqlGo where T : IDataBindable
{
    /// <summary>
    /// Specifies that the connection should be closed after the query is executed.
    /// </summary>
    IUpdateFromSetSingle<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IUpdateFromSetSingle<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Sets the value of a specified column for the update operation using the provided column selector and value.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="columnSelector">The expression used to select the column.</param>
    /// <param name="value">The value to set for the column.</param>
    /// <returns>The interface for further configuration or execution.</returns>
    IUpdateFromSetSingle<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, TValue value);

    /// <summary>
    /// Sets the value of a specified column for the update operation using the provided column selector and value selector expressions.
    /// </summary>
    /// <typeparam name="TValue">The type of the column value.</typeparam>
    /// <param name="columnSelector">The expression used to select the column.</param>
    /// <param name="valueSelector">The expression used to determine the value to set for the column.</param>
    /// <returns>The interface for further configuration or execution.</returns>
    IUpdateFromSetSingle<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, Expression<Func<T, TValue?>> valueSelector);
}

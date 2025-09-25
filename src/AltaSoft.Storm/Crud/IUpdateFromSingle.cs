using System;
using System.Linq.Expressions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for updating data in a table
/// </summary>
public interface IUpdateFromSingle<T> : ISqlGo where T : IDataBindable
{
    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    IUpdateFromSingle<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IUpdateFromSingle<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Sets the value of the specified type T in an SQL query.
    /// </summary>
    ISqlGo Set(T value);

    /// <summary>
    /// Sets the value of a specified column for an entity of type T using the provided column selector expression.
    /// </summary>
    IUpdateFromSetSingle<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, TValue value);

    /// <summary>
    /// Sets the value of a specified column for an entity of type T using the provided column selector expression.
    /// </summary>
    IUpdateFromSetSingle<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, Expression<Func<T, TValue?>> valueSelector);
}

using System;
using System.Linq.Expressions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for updating data in a table
/// </summary>
public interface IUpdateFromSetSingle<T> : ISqlGo where T : IDataBindable
{
    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    IUpdateFromSetSingle<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IUpdateFromSetSingle<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Sets the value of a specified column for an entity of type T using the provided column selector expression.
    /// </summary>
    IUpdateFromSetSingle<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, TValue value);

    /// <summary>
    /// Sets the value of a specified column for an entity of type T using the provided column selector expression.
    /// </summary>
    IUpdateFromSetSingle<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, Expression<Func<T, TValue?>> valueSelector);
}

using System.Collections.Generic;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for merging data into a table
/// </summary>
public interface IMergeInto<in T> : ISqlGo where T : IDataBindable
{
    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    IMergeInto<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    IMergeInto<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Sets the value of the specified type T in an SQL query.
    /// </summary>
    /// <param name="value">The value to be updated or inserted.</param>
    /// <returns>The interface with the updated or inserted value.</returns>
    ISqlGo UpdateOrInsert(T value);

    /// <summary>
    /// Sets the values in the SQL database for the specified collection of objects.
    /// </summary>
    /// <param name="values">The collection of values to be updated or inserted.</param>
    /// <returns>The interface with the updated or inserted values.</returns>
    ISqlGo UpdateOrInsert(IEnumerable<T> values);

    /// <summary>
    /// Sets the value of the specified type T in an SQL query.
    /// </summary>
    /// <param name="value">The value to be inserted or updated.</param>
    /// <returns>The interface with the inserted or updated value.</returns>
    ISqlGo InsertOrUpdate(T value);

    /// <summary>
    /// Sets the values in the SQL database for the specified collection of objects.
    /// </summary>
    /// <param name="values">The collection of values to be inserted or updated.</param>
    /// <returns>The interface with the inserted or updated values.</returns>
    ISqlGo InsertOrUpdate(IEnumerable<T> values);

    /// <summary>
    /// Specifies to perform a concurrency check when merging data into a table.
    /// </summary>
    /// <returns>The interface with the concurrency check enabled.</returns>
    IMergeInto<T> WithConcurrencyCheck();

    /// <summary>
    /// Specifies to skip the concurrency check when merging data into a table.
    /// </summary>
    /// <returns>The interface without the concurrency check.</returns>
    IMergeInto<T> WithoutConcurrencyCheck();
}

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using AltaSoft.Storm.Interfaces;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Defines a set of parameters for configuring bulk insert operations for a collection of <typeparamref name="T"/> objects.
/// Provides a fluent interface for specifying command timeout, batch size, and bulk copy options.
/// </summary>
/// <typeparam name="T">The type of objects to be inserted, which must implement <see cref="IDataBindable"/>.</typeparam>
public interface IBulkInsert<T> where T : IDataBindable
{
    #region Builder

    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    /// <returns>The interface with the connection closed after query.</returns>
    IBulkInsert<T> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the bulk insert operation.
    /// </summary>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <returns>The current <see cref="IBulkInsert{T}"/> instance for method chaining.</returns>
    IBulkInsert<T> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Sets the batch size for the bulk insert operation.
    /// </summary>
    /// <param name="batchSize">The number of rows in each batch. Default is 1,000.</param>
    /// <returns>The current <see cref="IBulkInsert{T}"/> instance for method chaining.</returns>
    IBulkInsert<T> WithBatchSize(int batchSize = 1_000);

    /// <summary>
    /// Sets the <see cref="SqlBulkCopyOptions"/> for the bulk insert operation.
    /// </summary>
    /// <param name="options">The bulk copy options to use. Default is <see cref="SqlBulkCopyOptions.Default"/>.</param>
    /// <returns>The current <see cref="IBulkInsert{T}"/> instance for method chaining.</returns>
    IBulkInsert<T> WithBulkCopyOptions(SqlBulkCopyOptions options = SqlBulkCopyOptions.Default);

    /// <summary>
    /// Configures progress notification for the bulk insert operation.
    /// </summary>
    /// <param name="notifyAfter">
    /// The number of rows to process before triggering the <paramref name="progressNotification"/> callback.
    /// </param>
    /// <param name="progressNotification">
    /// The callback action to invoke after every <paramref name="notifyAfter"/> rows have been processed. 
    /// The parameter to the action represents the total number of rows processed so far.
    /// </param>
    /// <returns>The current <see cref="IBulkInsert{T}"/> instance for method chaining.</returns>
    IBulkInsert<T> WithProgressNotification(int notifyAfter, Action<long> progressNotification);

    #endregion Builder

    /// <summary>
    /// Specifies the values to be inserted into the table.
    /// </summary>
    /// <param name="values">The collection of values to be inserted.</param>
    /// <returns>The interface with the specified values to be inserted.</returns>
    ISqlGo Values(IEnumerable<T> values);

    /// <summary>
    /// Specifies the values to be inserted into the table.
    /// </summary>
    /// <param name="values">The collection of values to be inserted.</param>
    /// <returns>The interface with the specified values to be inserted.</returns>
    ISqlGo Values(IAsyncEnumerable<T> values);

    /// <summary>
    /// Specifies the values to be inserted into the table.
    /// </summary>
    /// <param name="values">The collection of values to be inserted.</param>
    /// <returns>The interface with the specified values to be inserted.</returns>
    ISqlGo Values(ChannelReader<T> values);
}

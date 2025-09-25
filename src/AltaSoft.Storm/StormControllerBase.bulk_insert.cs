using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AltaSoft.Storm.BulkInsert;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Interfaces;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm;

/// <summary>
/// Abstract base class for ORM (Object-Relational Mapping) controllers, providing common functionality for database operations.
/// </summary>
public abstract partial class StormControllerBase
{
    internal async Task<int> BulkInsertAsync<T>(
        IEnumerable<T> values,
        BulkInsertQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        using var bulkCopy = await PrepareBulkCopyAsync(queryParameters, cancellationToken).ConfigureAwait(false);

        using var reader = new EnumerableDbDataReader<T>(values, GetFieldCount());

        await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
        return bulkCopy.GetRowsCopied();
    }

    internal async Task<int> BulkInsertAsync<T>(
        IAsyncEnumerable<T> values,
        BulkInsertQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        using var bulkCopy = await PrepareBulkCopyAsync(queryParameters, cancellationToken).ConfigureAwait(false);

        await using var reader = new AsyncEnumerableDbDataReader<T>(values, GetFieldCount());

        await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
        return bulkCopy.GetRowsCopied();
    }

    internal async Task<int> BulkInsertAsync<T>(
        ChannelReader<T> values,
        BulkInsertQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        using var bulkCopy = await PrepareBulkCopyAsync(queryParameters, cancellationToken).ConfigureAwait(false);

        await using var reader = new ChannelDbDataReader<T>(values, GetFieldCount());

        var rowsCopied = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!await values.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                break;

            await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
            rowsCopied += bulkCopy.GetRowsCopied();
        }
        return rowsCopied;
    }

    #region BulkInserts

    private int GetFieldCount() => ColumnDefs.Count(x => x.CanInsertColumn());

    private async Task<SqlBulkCopy> PrepareBulkCopyAsync<T>(BulkInsertQueryParameters<T> queryParameters, CancellationToken cancellationToken) where T : IDataBindable
    {
        var context = queryParameters.Context;

        var connection = context.GetConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var bulkCopy = new SqlBulkCopy(connection, queryParameters.BulkCopyOptions, context.GetTransaction());

        bulkCopy.DestinationTableName = QuotedObjectFullName;
        bulkCopy.BatchSize = queryParameters.BatchSize ?? 1_000;
        bulkCopy.BulkCopyTimeout = queryParameters.CommandTimeout ?? 60; // seconds
        bulkCopy.EnableStreaming = true;

        if (queryParameters.ProgressNotification is not null)
        {
            bulkCopy.NotifyAfter = queryParameters.NotifyAfter;
            bulkCopy.SqlRowsCopied += (sender, e) => { queryParameters.ProgressNotification(e.RowsCopied); };
        }

        // Mapping
        var bulkCopyColumnMappings = bulkCopy.ColumnMappings;
        var columns = ColumnDefs;
        for (var index = 0; index < columns.Length; index++)
        {
            var columnDef = columns[index];
            if (!columnDef.CanInsertColumn())
                continue;
            bulkCopyColumnMappings.Add(new SqlBulkCopyColumnMapping(index, columnDef.ColumnName));
        }
        return bulkCopy;
    }

    #endregion BulkInserts
}

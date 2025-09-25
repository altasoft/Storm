using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Interfaces;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm.Crud;

internal sealed class BulkInsert<T> : BulkInsertQueryParameters<T>, IBulkInsert<T>, ISqlGo
    where T : IDataBindable
{
    internal BulkInsert(StormContext context, int variant) : base(context, variant)
    {
    }

    #region Builder

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBulkInsert<T> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBulkInsert<T> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBulkInsert<T> WithBatchSize(int batchSize = 1_000)
    {
        BatchSize = batchSize;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBulkInsert<T> WithBulkCopyOptions(SqlBulkCopyOptions options = SqlBulkCopyOptions.Default)
    {
        BulkCopyOptions = options;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBulkInsert<T> WithProgressNotification(int notifyAfter, Action<long> progressNotification)
    {
        NotifyAfter = notifyAfter;
        ProgressNotification = progressNotification;
        return this;
    }

    #endregion Builder

    /// <inheritdoc/>
    public ISqlGo Values(IEnumerable<T> values)
    {
        RowValuesEnumerable = values;
        return this;
    }

    /// <inheritdoc/>
    public ISqlGo Values(IAsyncEnumerable<T> values)
    {
        RowValuesAsyncEnumerable = values;
        return this;
    }

    /// <inheritdoc/>
    public ISqlGo Values(ChannelReader<T> values)
    {
        RowValuesChannel = values;
        return this;
    }

    /// <inheritdoc/>
    public Task<int> GoAsync(CancellationToken cancellationToken = default)
    {
        if (RowValuesEnumerable is not null)
            return GetController().BulkInsertAsync(RowValuesEnumerable, this, cancellationToken);

        if (RowValuesAsyncEnumerable is not null)
            return GetController().BulkInsertAsync(RowValuesAsyncEnumerable, this, cancellationToken);

        if (RowValuesChannel is not null)
            return GetController().BulkInsertAsync(RowValuesChannel, this, cancellationToken);

        throw new StormException("No values to insert."); // This will never happen
    }

    public void GenerateBatchCommands(List<StormDbBatchCommand> batchCommands) => throw new NotImplementedException();
}

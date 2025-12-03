using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

internal sealed class MergeInto<T> : ModifyQueryParameters<T>, IMergeInto<T> where T : IDataBindable
{
    private bool _checkConcurrency = true;
    private bool _updateThenInsert = true;

    /// <summary>
    /// Constructor for initializing a MergeInto object with a given DbConnection.
    /// </summary>
    internal MergeInto(StormContext context, int variant) : base(context, variant)
    {
    }

    /// <summary>
    /// Constructor for initializing a MergeInto object with a given DbConnection.
    /// </summary>
    internal MergeInto(StormContext context, int variant, string customQuotedObjectFullName) : base(context, variant, customQuotedObjectFullName)
    {
    }

    #region Builder

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IMergeInto<T> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IMergeInto<T> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    /// <inheritdoc/>
    public IMergeInto<T> WithConcurrencyCheck()
    {
        _checkConcurrency = true;
        return this;
    }

    /// <inheritdoc/>
    public IMergeInto<T> WithoutConcurrencyCheck()
    {
        _checkConcurrency = false;
        return this;
    }

    #endregion Builder

    /// <inheritdoc/>
    public ISqlGo UpdateOrInsert(T value)
    {
        RowValue = value;
        _updateThenInsert = true;
        return this;
    }

    /// <inheritdoc/>
    public ISqlGo UpdateOrInsert(IEnumerable<T> values)
    {
        RowValues = values;
        _updateThenInsert = true;
        return this;
    }

    /// <inheritdoc/>
    public ISqlGo InsertOrUpdate(T value)
    {
        RowValue = value;
        _updateThenInsert = false;
        return this;
    }

    /// <inheritdoc/>
    public ISqlGo InsertOrUpdate(IEnumerable<T> values)
    {
        RowValues = values;
        _updateThenInsert = false;
        return this;
    }

    /// <inheritdoc/>
    public Task<int> GoAsync(CancellationToken cancellationToken = default)
    {
        if (RowValue is not null)
            return GetController().MergeAsync(RowValue, _checkConcurrency, _updateThenInsert, this, cancellationToken);

        if (RowValues is not null)
            return GetController().MergeAsync(RowValues, _checkConcurrency, _updateThenInsert, this, cancellationToken);

        return Task.FromResult(0);
    }

    /// <param name="batchCommands"></param>
    /// <inheritdoc />
    public void GenerateBatchCommands(List<StormDbBatchCommand> batchCommands)
    {
        if (RowValue is not null)
        {
            var command = StormManager.CreateBatchCommand(false);
            var vCommand = new StormVirtualDbBatchCommand(command);

            GetController().Merge(vCommand, RowValue, _checkConcurrency, _updateThenInsert, this);
            batchCommands.Add(command);
            return;
        }

        if (RowValues is not null)
        {
            var command = StormManager.CreateBatchCommand(false);
            var vCommand = new StormVirtualDbBatchCommand(command);

            GetController().Merge(vCommand, RowValues, _checkConcurrency, _updateThenInsert, this);
            batchCommands.Add(command);
        }
    }
}

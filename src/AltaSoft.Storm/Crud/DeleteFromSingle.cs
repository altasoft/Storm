using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Base class for deleting data from a database table
/// </summary>
internal sealed class DeleteFromSingle<T> : ModifyQueryParameters<T>, IDeleteFromSingle<T>, ISqlGo where T : IDataBindable
{
    internal DeleteFromSingle(StormContext context, int variant, object[] keyValues, int keyId) : base(context, variant)
    {
        KeyValues = keyValues;
        KeyId = keyId;
    }

    internal DeleteFromSingle(StormContext context, int variant, object[] keyValues, int keyId, string customQuotedObjectFullName) : base(context, variant, customQuotedObjectFullName)
    {
        KeyValues = keyValues;
        KeyId = keyId;
    }

    internal DeleteFromSingle(StormContext context, int variant, T value) : base(context, variant)
    {
        RowValue = value;
    }
    internal DeleteFromSingle(StormContext context, int variant, T value, string customQuotedObjectFullName) : base(context, variant, customQuotedObjectFullName)
    {
        RowValue = value;
    }

    internal DeleteFromSingle(StormContext context, int variant, IEnumerable<T> values) : base(context, variant)
    {
        RowValues = values;
    }

    internal DeleteFromSingle(StormContext context, int variant, IEnumerable<T> values, string customQuotedObjectFullName) : base(context, variant, customQuotedObjectFullName)
    {
        RowValues = values;
    }

    #region Builder

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDeleteFromSingle<T> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDeleteFromSingle<T> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    #endregion Builder

    /// <inheritdoc/>
    public Task<int> GoAsync(CancellationToken cancellationToken = default)
    {
        if (KeyValues is not null || WhereExpressions is not null)
            return GetController().DeleteAsync(this, cancellationToken);

        if (RowValue is not null)
            return GetController().DeleteAsync(RowValue, this, cancellationToken);

        if (RowValues is not null)
            return GetController().DeleteAsync(RowValues, this, cancellationToken);

        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public void GenerateBatchCommands(List<StormDbBatchCommand> batchCommands)
    {
        if (KeyValues is not null || WhereExpressions is not null)
        {
            var command = StormManager.CreateBatchCommand(false);
            var vCommand = new StormVirtualDbBatchCommand(command);

            GetController().Delete(vCommand, this);
            batchCommands.Add(command);
            return;
        }

        if (RowValue is not null)
        {
            var command = StormManager.CreateBatchCommand(false);
            var vCommand = new StormVirtualDbBatchCommand(command);

            GetController().Delete(vCommand, RowValue, this);
            batchCommands.Add(command);
            return;
        }

        if (RowValues is not null)
        {
            var command = StormManager.CreateBatchCommand(false);
            var vCommand = new StormVirtualDbBatchCommand(command);

            GetController().Delete(vCommand, RowValues, this);
            batchCommands.Add(command);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

internal sealed class UpdateFromSingle<T> : ModifyQueryParameters<T>, IUpdateFromSingle<T> where T : IDataBindable
{
    internal UpdateFromSingle(StormContext context, int variant, object[] keyValues, int keyId, string? customQuotedObjectFullName = null) : base(context, variant, customQuotedObjectFullName)
    {
        KeyValues = keyValues;
        KeyId = keyId;
    }

    #region Builder

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUpdateFromSingle<T> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUpdateFromSingle<T> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    #endregion Builder

    /// <inheritdoc />
    public IUpdateFromSetSingle<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, TValue value)
    {
        var propertyName = columnSelector.GetPropertyNameFromExpression();
        var columnDef = Array.Find(GetController().ColumnDefs, x => string.Equals(x.PropertyName, propertyName, StringComparison.Ordinal))
                        ?? throw new StormException($"Column '{propertyName}' not found in the column definitions.");

        return new UpdateFromSetSingle<T>(this, columnDef, value, KeyValues!, KeyId!.Value, CustomQuotedObjectFullName);
    }

    /// <inheritdoc />
    public IUpdateFromSetSingle<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, Expression<Func<T, TValue?>> valueSelector)
    {
        var propertyName = columnSelector.GetPropertyNameFromExpression();
        var columnDef = Array.Find(GetController().ColumnDefs, x => string.Equals(x.PropertyName, propertyName, StringComparison.Ordinal))
                        ?? throw new StormException($"Column '{propertyName}' not found in the column definitions.");

        return new UpdateFromSetSingle<T>(this, columnDef, valueSelector, KeyValues!, KeyId!.Value, CustomQuotedObjectFullName);
    }

    /// <inheritdoc />
    public ISqlGo Set(T value)
    {
        RowValue = value;
        return this;
    }

    /// <inheritdoc />
    public Task<int> GoAsync(CancellationToken cancellationToken = default)
    {
        if (RowValue is not null)
            return GetController().UpdateAsync(RowValue, false, this, cancellationToken);

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

            GetController().PrepareUpdate(vCommand, RowValue, false, this);
            batchCommands.Add(command);
        }
    }
}

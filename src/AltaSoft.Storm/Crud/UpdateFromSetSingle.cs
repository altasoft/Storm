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

internal sealed class UpdateFromSetSingle<T> : ModifyQueryParameters<T>, IUpdateFromSetSingle<T> where T : IDataBindable
{
    // Update with set instructions
    private readonly List<(StormColumnDef column, object? value)> _setInstructions;

    internal UpdateFromSetSingle(UpdateFromSingle<T> source, StormColumnDef columnSelector, object? value, object[] keyValues, int keyId) : base(source.Context, source.Variant)
    {
        CloseConnection = source.CloseConnection;
        CommandTimeout = source.CommandTimeout;

        KeyValues = keyValues;
        KeyId = keyId;

        _setInstructions = new List<(StormColumnDef column, object? value)>(4)
        {
            (columnSelector, value)
        };
    }

    internal UpdateFromSetSingle(UpdateFromSingle<T> source, StormColumnDef columnSelector, object? value, object[] keyValues, int keyId, string customQuotedObjectFullName) : base(source.Context, source.Variant, customQuotedObjectFullName)
    {
        CloseConnection = source.CloseConnection;
        CommandTimeout = source.CommandTimeout;

        KeyValues = keyValues;
        KeyId = keyId;

        _setInstructions = new List<(StormColumnDef column, object? value)>(4)
        {
            (columnSelector, value)
        };
    }

    #region Builder

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUpdateFromSetSingle<T> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUpdateFromSetSingle<T> WithCommandTimeOut(int commandTimeout)
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

        _setInstructions.Add((columnDef, value));
        return this;
    }

    /// <inheritdoc />
    public IUpdateFromSetSingle<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, Expression<Func<T, TValue?>> valueSelector)
    {
        var propertyName = columnSelector.GetPropertyNameFromExpression();
        var columnDef = Array.Find(GetController().ColumnDefs, x => string.Equals(x.PropertyName, propertyName, StringComparison.Ordinal))
                        ?? throw new StormException($"Column '{propertyName}' not found in the column definitions.");

        _setInstructions.Add((columnDef, valueSelector));
        return this;
    }

    /// <inheritdoc />
    public Task<int> GoAsync(CancellationToken cancellationToken = default)
    {
        return GetController().UpdateAsync(_setInstructions, this, cancellationToken);
    }

    /// <param name="batchCommands"></param>
    /// <inheritdoc />
    public void GenerateBatchCommands(List<StormDbBatchCommand> batchCommands)
    {
        var command = StormManager.CreateBatchCommand(false);
        var vCommand = new StormVirtualDbBatchCommand(command);

        GetController().Update(vCommand, _setInstructions, this);
        batchCommands.Add(command);
    }
}

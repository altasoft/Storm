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

internal sealed class UpdateFrom<T> : ModifyQueryParameters<T>, IUpdateFrom<T> where T : IDataBindable
{
    private bool _checkConcurrency = true;

    /// <summary>
    /// Constructor for initializing a SelectFromBase object with a given DbConnection.
    /// </summary>
    internal UpdateFrom(StormContext context, int variant) : base(context, variant)
    {
    }

    #region Builder

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUpdateFrom<T> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUpdateFrom<T> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    /// <inheritdoc />
    public IUpdateFrom<T> WithConcurrencyCheck()
    {
        _checkConcurrency = true;
        return this;
    }

    /// <inheritdoc />
    public IUpdateFrom<T> WithoutConcurrencyCheck()
    {
        _checkConcurrency = false;
        return this;
    }

    #endregion Builder

    /// <inheritdoc />
    public IUpdateFromSet<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, TValue value)
    {
        var propertyName = columnSelector.GetPropertyNameFromExpression();
        var columnDef = Array.Find(GetController().ColumnDefs, x => string.Equals(x.PropertyName, propertyName, StringComparison.Ordinal))
                        ?? throw new StormException($"Column '{propertyName}' not found in the column definitions.");

        return new UpdateFromSet<T>(this, columnDef, value);
    }

    /// <inheritdoc />
    public ISqlGo Set(T value)
    {
        RowValue = value;
        return this;
    }

    /// <inheritdoc />
    public ISqlGo Set(IEnumerable<T> values)
    {
        RowValues = values;
        return this;
    }

    /// <inheritdoc />
    public IUpdateFromSet<T> Set<TValue>(Expression<Func<T, TValue?>> columnSelector, Expression<Func<T, TValue?>> valueSelector)
    {
        var propertyName = columnSelector.GetPropertyNameFromExpression();
        var columnDef = Array.Find(GetController().ColumnDefs, x => string.Equals(x.PropertyName, propertyName, StringComparison.Ordinal))
                        ?? throw new StormException($"Column '{propertyName}' not found in the column definitions.");

        return new UpdateFromSet<T>(this, columnDef, valueSelector);
    }

    /// <inheritdoc />
    public Task<int> GoAsync(CancellationToken cancellationToken = default)
    {
        if (RowValue is not null)
            return GetController().UpdateAsync(RowValue, _checkConcurrency, this, cancellationToken);

        if (RowValues is not null)
            return GetController().UpdateAsync(RowValues, _checkConcurrency, this, cancellationToken);

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

            GetController().Update(vCommand, RowValue, _checkConcurrency, this);
            batchCommands.Add(command);
            return;
        }

        if (RowValues is not null)
        {
            var command = StormManager.CreateBatchCommand(false);
            var vCommand = new StormVirtualDbBatchCommand(command);

            GetController().Update(vCommand, RowValues, _checkConcurrency, this);
            batchCommands.Add(command);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Base class for deleting data from a database table
/// </summary>
internal sealed class DeleteFrom<T> : ModifyQueryParameters<T>, IDeleteFrom<T>, ISqlGo where T : IDataBindable
{
    /// <summary>
    /// Constructor for initializing a DeleteFrom object with a given DbConnection and custom quoted object name
    /// </summary>
    internal DeleteFrom(StormContext context, int variant, string? customQuotedObjectFullName = null) : base(context, variant, customQuotedObjectFullName)
    {
    }

    #region Builder

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDeleteFrom<T> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDeleteFrom<T> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDeleteFrom<T> Top(int rowCount)
    {
        TopRows = rowCount;
        return this;
    }

    /// <inheritdoc/>
    public IDeleteFrom<T> Where(Expression<Func<T, bool>> whereExpression)
    {
        WhereExpressions ??= new List<Expression<Func<T, bool>>>(1);
        WhereExpressions.Add(whereExpression);
        return this;
    }

    /// <inheritdoc/>
    public IDeleteFrom<T> Where(string oDataFilter)
    {
        ODataFilter = oDataFilter;
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

    /// <param name="batchCommands"></param>
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

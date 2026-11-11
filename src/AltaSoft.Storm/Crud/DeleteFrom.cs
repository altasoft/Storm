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

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDeleteFrom<T> WithTableHints(StormTableHints tableHints)
    {
        TableHints = tableHints;
        return this;
    }

    #endregion Builder

    /// <inheritdoc/>
    /// <remarks>
    /// Always executes a filtered DELETE. The filter is built from the key values, LINQ Where expressions
    /// and/or OData filter; when none of them are specified, all rows of the table are deleted.
    /// </remarks>
    public Task<int> GoAsync(CancellationToken cancellationToken = default)
    {
        return GetController().DeleteAsync(this, cancellationToken);
    }

    /// <param name="batchCommands"></param>
    /// <inheritdoc />
    public void GenerateBatchCommands(List<StormDbBatchCommand> batchCommands)
    {
        var command = StormManager.CreateBatchCommand(false);
        var vCommand = new StormVirtualDbBatchCommand(command);

        GetController().PrepareDelete(vCommand, this);
        batchCommands.Add(command);
    }
}

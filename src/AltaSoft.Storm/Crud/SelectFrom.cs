using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

internal class SelectFrom<T, TOrderBy, TPartialLoadFlags> : SelectQueryParameters<T>, ISelectFrom<T, TOrderBy, TPartialLoadFlags>
    where T : IDataBindable
    where TOrderBy : struct, Enum
    where TPartialLoadFlags : struct, Enum
{
    /// <summary>
    /// Constructor for QueryParameters class with a single parameter variant
    /// </summary>
    /// <param name="context">Database connection</param>
    /// <param name="variant">The variant value to be set</param>
    internal SelectFrom(StormContext context, int variant) : base(context, variant)
    {
    }

    /// <summary>
    /// Constructor for QueryParameters class with a single parameter variant
    /// </summary>
    /// <param name="context">Database connection</param>
    /// <param name="variant">The variant value to be set</param>
    /// <param name="keyValues">An array of objects containing key values.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    internal SelectFrom(StormContext context, int variant, object[] keyValues, int keyId) : base(context, variant)
    {
        KeyValues = keyValues;
        KeyId = keyId;
    }

    /// <summary>
    /// Constructor for creating a SelectFrom object with the provided StormContext, variant, and callParameters.
    /// </summary>
    /// <param name="context">The StormContext object to be used for the SelectFrom operation.</param>
    /// <param name="variant">An integer representing the variant of the SelectFrom operation.</param>
    /// <param name="callParameters">A list of StormCallParameter objects containing parameters for the SelectFrom operation.</param>
    internal SelectFrom(StormContext context, int variant, List<StormCallParameter> callParameters) : base(context, variant, callParameters)
    {
    }

    #region Builder

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithTracking()
    {
        AutoStartChangeTracking = true;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithNoTracking()
    {
        AutoStartChangeTracking = false;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> Partially(TPartialLoadFlags partialLoadFlags)
    {
        PartialLoadFlags = (uint)(object)partialLoadFlags;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> Top(int rowCount)
    {
        TopRows = rowCount;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> Skip(int skipRowCount)
    {
        SkipRows = skipRowCount;
        return this;
    }

    /// <inheritdoc />
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> Where(Expression<Func<T, bool>> whereExpression)
    {
        WhereExpressions ??= new List<Expression<Func<T, bool>>>(1);
        WhereExpressions.Add(whereExpression);
        return this;
    }

    /// <inheritdoc />
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> Where(string oDataFilter)
    {
        ODataFilter = oDataFilter;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> OrderBy(params TOrderBy[] orderBy)
    {
        OrderByColumnIds = orderBy.GetIntArrayFromOrderByEnum();
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithTableHints(StormTableHints tableHints)
    {
        TableHints = tableHints;
        return this;
    }

    #endregion Builder

    #region Misc

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => GetController().CountAsync(this, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => GetController().ExistsAsync(this, cancellationToken);

    #endregion Misc

    #region Get

    /// <inheritdoc />
    public Task<T?> GetAsync(CancellationToken cancellationToken = default)
        => GetController().FirstOrDefaultAsync(this, cancellationToken);

    /// <inheritdoc />
    public async Task<TColumn> GetAsync<TColumn>(Expression<Func<T, TColumn>> columnSelector,
        TColumn defaultWhenNotFound, CancellationToken cancellationToken = default)
    {
        var r = await GetAsync(columnSelector, cancellationToken).ConfigureAwait(false);
        return r is { RowFound: true, HasValue: true } ? r.Value! : defaultWhenNotFound;
    }

    /// <inheritdoc />
    public async Task<TColumn?> GetOrDefaultAsync<TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        CancellationToken cancellationToken = default)
    {
        var r = await GetAsync(columnSelector, cancellationToken).ConfigureAwait(false);
        return r is { RowFound: true, HasValue: true } ? r.Value : default;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<DbScalar<TColumn>> GetAsync<TColumn>(Expression<Func<T, TColumn>> columnSelector,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValueAsync(columnSelector, this, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<(TColumn1, TColumn2)?> GetAsync<TColumn1, TColumn2>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValuesAsync(columnSelector1, columnSelector2, this, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<(TColumn1, TColumn2, TColumn3)?> GetAsync<TColumn1, TColumn2, TColumn3>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        Expression<Func<T, TColumn3>> columnSelector3,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValuesAsync(columnSelector1, columnSelector2, columnSelector3, this, cancellationToken);

    #endregion Get

    #region List

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        var ctrl = GetController();
        var (partialLoadFlags, shouldLoadDetails) = ctrl.GetPartialLoadingData(PartialLoadFlags, this);

        return ctrl.ListAsync(partialLoadFlags, shouldLoadDetails, this, cancellationToken);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<List<TColumn>> ListAsync<TColumn>(Expression<Func<T, TColumn>> columnSelector, CancellationToken cancellationToken = default)
        => GetController().ListColumnValuesAsync(columnSelector, this, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<List<(TColumn1, TColumn2)>> ListAsync<TColumn1, TColumn2>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        CancellationToken cancellationToken = default)
        => GetController().ListColumnValuesAsync(columnSelector1, columnSelector2, this, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<List<(TColumn1, TColumn2, TColumn3)>> ListAsync<TColumn1, TColumn2, TColumn3>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        Expression<Func<T, TColumn3>> columnSelector3,
        CancellationToken cancellationToken = default)
        => GetController().ListColumnValuesAsync(columnSelector1, columnSelector2, columnSelector3, this, cancellationToken);

    #endregion List

    #region Stream

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default)
        => GetController().StreamAsync(this, cancellationToken);

    #endregion Stream
}

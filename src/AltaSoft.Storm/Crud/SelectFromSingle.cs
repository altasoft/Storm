using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

internal class SelectFromSingle<T, TOrderBy, TPartialLoadFlags> : SelectQueryParameters<T>, ISelectFromSingle<T, TOrderBy, TPartialLoadFlags>
    where T : IDataBindable
    where TOrderBy : struct, Enum
    where TPartialLoadFlags : struct, Enum
{
    /// <summary>
    /// Constructor for QueryParameters class with a single parameter variant
    /// </summary>
    /// <param name="context">Database connection</param>
    /// <param name="variant">The variant value to be set</param>
    /// <param name="keyValues">An array of objects containing key values.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    internal SelectFromSingle(StormContext context, int variant, object[] keyValues, int keyId) : base(context, variant)
    {
        KeyValues = keyValues;
        KeyId = keyId;
    }

    #region Builder

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithTracking()
    {
        AutoStartChangeTracking = true;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithNoTracking()
    {
        AutoStartChangeTracking = false;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> Partially(TPartialLoadFlags partialLoadFlags)
    {
        PartialLoadFlags = (uint)(object)partialLoadFlags;
        return this;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithTableHints(StormTableHints tableHints)
    {
        TableHints = tableHints;
        return this;
    }

    #endregion Builder

    #region Misc

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => GetController().CountAsync(this, null, cancellationToken);

    /// <summary>
    /// Counts the number of records in the data source that match the specified conditions using the provided query hints.
    /// </summary>
    /// <param name="queryHints">The query hints to apply to the query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The count of records matching the conditions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> CountAsync(QueryHints queryHints, CancellationToken cancellationToken = default)
        => GetController().CountAsync(this, queryHints, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
        => GetController().ExistsAsync(this, null, cancellationToken);

    /// <summary>
    /// Checks if any row exists in the data source that meets the specified conditions, applying the provided query hints.
    /// </summary>
    /// <param name="queryHints">The query hints to apply to the query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if any such row exists; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<bool> ExistsAsync(QueryHints queryHints, CancellationToken cancellationToken = default)
        => GetController().ExistsAsync(this, queryHints, cancellationToken);

    #endregion Misc

    #region Get

    /// <inheritdoc />
    public Task<T?> GetAsync(CancellationToken cancellationToken = default)
        => GetController().FirstOrDefaultAsync(this, null, cancellationToken);

    /// <inheritdoc />
    public Task<T?> GetAsync(QueryHints queryHints, CancellationToken cancellationToken = default)
        => GetController().FirstOrDefaultAsync(this, queryHints, cancellationToken);

    /// <inheritdoc />
    public async Task<TColumn> GetAsync<TColumn>(Expression<Func<T, TColumn>> columnSelector,
        TColumn defaultWhenNotFound, QueryHints queryHints, CancellationToken cancellationToken = default)
    {
        var r = await GetAsync(columnSelector, queryHints, cancellationToken).ConfigureAwait(false);
        return r is { RowFound: true, HasValue: true } ? r.Value! : defaultWhenNotFound;
    }

    /// <inheritdoc />
    public async Task<TColumn?> GetOrDefaultAsync<TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        QueryHints queryHints,
        CancellationToken cancellationToken = default)
    {
        var r = await GetAsync(columnSelector, queryHints, cancellationToken).ConfigureAwait(false);
        return r is { RowFound: true, HasValue: true } ? r.Value : default;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<DbScalar<TColumn>> GetAsync<TColumn>(Expression<Func<T, TColumn>> columnSelector,
        QueryHints queryHints, CancellationToken cancellationToken = default)
        => GetController().GetColumnValueAsync(columnSelector, this, queryHints, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<(TColumn1, TColumn2)?> GetAsync<TColumn1, TColumn2>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        QueryHints queryHints,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValuesAsync(columnSelector1, columnSelector2, this, queryHints, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<(TColumn1, TColumn2, TColumn3)?> GetAsync<TColumn1, TColumn2, TColumn3>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        Expression<Func<T, TColumn3>> columnSelector3,
        QueryHints queryHints,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValuesAsync(columnSelector1, columnSelector2, columnSelector3, this, queryHints, cancellationToken);

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
        => GetController().GetColumnValueAsync(columnSelector, this, null, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<(TColumn1, TColumn2)?> GetAsync<TColumn1, TColumn2>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValuesAsync(columnSelector1, columnSelector2, this, null, cancellationToken);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<(TColumn1, TColumn2, TColumn3)?> GetAsync<TColumn1, TColumn2, TColumn3>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        Expression<Func<T, TColumn3>> columnSelector3,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValuesAsync(columnSelector1, columnSelector2, columnSelector3, this, null, cancellationToken);

    #endregion Get
}

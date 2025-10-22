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

    /// <summary>
    /// Asynchronously counts the number of records in a database table that match the specified conditions set in the current instance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain the count of records matching the conditions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => GetController().CountAsync(this, cancellationToken);

    /// <summary>
    /// Asynchronously checks if any row in the database table exists that meets the specified conditions set in the current instance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will indicate whether any such row exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
        => GetController().ExistsAsync(this, cancellationToken);

    #endregion Misc

    #region Get

    /// <summary>
    /// Asynchronously retrieves the first or default entity of type T from the database that matches the specified conditions.
    /// This method considers the partial load flags and any set query parameters.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain the first or default entity of type T.</returns>
    public Task<T?> GetAsync(CancellationToken cancellationToken = default)
        => GetController().FirstOrDefaultAsync(this, cancellationToken);

    /// <summary>
    /// This method is deprecated. Please use another overload.
    /// </summary>
    [Obsolete("This method is deprecated. Please use another overload")]
    public async Task<TColumn?> GetAsync<TColumn>(Expression<Func<T, TColumn>> columnSelector,
        TColumn? defaultWhenNotFound, CancellationToken cancellationToken = default)
    {
        var (value, found) = await GetAsync(columnSelector, cancellationToken).ConfigureAwait(false);
        return found ? value : defaultWhenNotFound;
    }

    /// <summary>
    /// Asynchronously retrieves a single column value from the database based on the provided column selector and returns a tuple containing the value and a flag indicating whether a row was found.
    /// </summary>
    /// <typeparam name="TColumn">The type of the column to retrieve.</typeparam>
    /// <param name="columnSelector">An expression to select the column.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain the column value and whether row was found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<(TColumn? value, bool found)> GetAsync<TColumn>(Expression<Func<T, TColumn>> columnSelector,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValueAsync(columnSelector, this, cancellationToken);

    /// <summary>
    /// Asynchronously retrieves a tuple of two column values from the database based on the provided column selectors.
    /// </summary>
    /// <typeparam name="TColumn1">The type of the first column to retrieve.</typeparam>
    /// <typeparam name="TColumn2">The type of the second column to retrieve.</typeparam>
    /// <param name="columnSelector1">The expression representing the first column.</param>
    /// <param name="columnSelector2">The expression representing the second column.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain a tuple of the retrieved column values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<(TColumn1, TColumn2)?> GetAsync<TColumn1, TColumn2>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValuesAsync(columnSelector1, columnSelector2, this, cancellationToken);

    /// <summary>
    /// Asynchronously retrieves a tuple of three column values from the database based on the provided column selectors.
    /// </summary>
    /// <typeparam name="TColumn1">The type of the first column to retrieve.</typeparam>
    /// <typeparam name="TColumn2">The type of the second column to retrieve.</typeparam>
    /// <typeparam name="TColumn3">The type of the third column to retrieve.</typeparam>
    /// <param name="columnSelector1">The expression for selecting the first column.</param>
    /// <param name="columnSelector2">The expression for selecting the second column.</param>
    /// <param name="columnSelector3">The expression for selecting the third column.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain a tuple of the retrieved column values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<(TColumn1, TColumn2, TColumn3)?> GetAsync<TColumn1, TColumn2, TColumn3>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        Expression<Func<T, TColumn3>> columnSelector3,
        CancellationToken cancellationToken = default)
        => GetController().GetColumnValuesAsync(columnSelector1, columnSelector2, columnSelector3, this, cancellationToken);

    #endregion Get
}

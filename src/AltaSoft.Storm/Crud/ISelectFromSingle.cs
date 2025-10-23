using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Defines a builder and execution interface for constructing and executing SELECT queries
/// that return a single entity of type <typeparamref name="T"/> from a data source.
/// Supports filtering, ordering, partial loading, and table hints. Intended for use with
/// types that implement <see cref="IDataBindable"/> and enums for ordering and partial load flags.
/// </summary>
/// <typeparam name="T">The entity type to select, which must implement <see cref="IDataBindable"/>.</typeparam>
/// <typeparam name="TOrderBy">The enum type used for specifying order by columns.</typeparam>
/// <typeparam name="TPartialLoadFlags">The enum type used for specifying partial load flags.</typeparam>
public interface ISelectFromSingle<T, in TOrderBy, in TPartialLoadFlags>
    where T : IDataBindable
    where TOrderBy : struct, Enum, IConvertible
    where TPartialLoadFlags : struct, Enum, IConvertible
{
    #region Builder

    /// <summary>
    /// Specifies that the connection should be closed after the query is executed.
    /// </summary>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Enables automatic change tracking for the query results.
    /// </summary>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithTracking();

    /// <summary>
    /// Disables automatic change tracking for the query results.
    /// </summary>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithNoTracking();

    /// <summary>
    /// Specifies partial loading of data based on the provided flags.
    /// </summary>
    /// <param name="partialLoadFlags">Flags indicating which parts of the entity to load.</param>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> Partially(TPartialLoadFlags partialLoadFlags);

    /// <summary>
    /// Specifies table hints to be used in the SELECT query.
    /// </summary>
    /// <param name="tableHints">The table hints to apply.</param>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithTableHints(StormTableHints tableHints);

    #endregion Builder

    #region Misc

    /// <summary>
    /// Counts the number of records in the data source that match the specified conditions.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The count of records matching the conditions.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any row exists in the data source that meets the specified conditions.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if any such row exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);

    #endregion Misc

    #region Get

    /// <summary>
    /// Retrieves the first or default entity of type <typeparamref name="T"/> that matches the specified conditions.
    /// Considers partial load flags and any set query parameters.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The first or default entity of type <typeparamref name="T"/>.</returns>
    Task<T?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single column value from the data source based on the provided column selector.
    /// If no match is found, returns the specified default value.
    /// </summary>
    /// <typeparam name="TColumn">The type of the column to retrieve.</typeparam>
    /// <param name="columnSelector">An expression to select the column.</param>
    /// <param name="defaultWhenNotFound">The value to return if no matching row is found.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The column value if found; otherwise, the specified default value.</returns>
    Task<TColumn> GetAsync<TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        TColumn defaultWhenNotFound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single column value from the data source based on the provided column selector.
    /// Returns the value if found; otherwise, returns the default value for the column type.
    /// </summary>
    /// <typeparam name="TColumn">The type of the column to retrieve.</typeparam>
    /// <param name="columnSelector">An expression to select the column.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The column value if found; otherwise, the default value for the column type.</returns>
    Task<TColumn?> GetOrDefaultAsync<TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single column value from the data source based on the provided column selector.
    /// Returns a <see cref="DbScalar{TColumn}"/> containing the value and information about row existence and value presence.
    /// </summary>
    /// <typeparam name="TColumn">The type of the column to retrieve.</typeparam>
    /// <param name="columnSelector">An expression to select the column.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="DbScalar{TColumn}"/> with the column value and status flags.</returns>
    Task<DbScalar<TColumn>> GetAsync<TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a tuple of two column values from the data source based on the provided column selectors.
    /// </summary>
    /// <typeparam name="TColumn1">The type of the first column to retrieve.</typeparam>
    /// <typeparam name="TColumn2">The type of the second column to retrieve.</typeparam>
    /// <param name="columnSelector1">The expression representing the first column.</param>
    /// <param name="columnSelector2">The expression representing the second column.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A tuple of the retrieved column values, or null if not found.</returns>
    Task<(TColumn1, TColumn2)?> GetAsync<TColumn1, TColumn2>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a tuple of three column values from the data source based on the provided column selectors.
    /// </summary>
    /// <typeparam name="TColumn1">The type of the first column to retrieve.</typeparam>
    /// <typeparam name="TColumn2">The type of the second column to retrieve.</typeparam>
    /// <typeparam name="TColumn3">The type of the third column to retrieve.</typeparam>
    /// <param name="columnSelector1">The expression for selecting the first column.</param>
    /// <param name="columnSelector2">The expression for selecting the second column.</param>
    /// <param name="columnSelector3">The expression for selecting the third column.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A tuple of the retrieved column values, or null if not found.</returns>
    Task<(TColumn1, TColumn2, TColumn3)?> GetAsync<TColumn1, TColumn2, TColumn3>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        Expression<Func<T, TColumn3>> columnSelector3,
        CancellationToken cancellationToken = default);

    #endregion Get
}

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for selecting data from a data source with specified order by and partial load flags, where T must implement IDataBindable, TOrderBy must be a struct, Enum, and IConvertible, and TPartialLoadFlags must be a struct, Enum, and IConvertible.
/// </summary>
public interface ISelectFrom<T, in TOrderBy, in TPartialLoadFlags>
    where T : IDataBindable
    where TOrderBy : struct, Enum, IConvertible
    where TPartialLoadFlags : struct, Enum, IConvertible
{
    #region Builder

    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Specifies to start automatic change tracking.
    /// </summary>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithTracking();

    /// <summary>
    /// Specifies not to start automatic change tracking.
    /// </summary>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithNoTracking();

    /// <summary>
    /// Specifies partial loading of data based on the specified flags.
    /// </summary>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> Partially(TPartialLoadFlags partialLoadFlags);

    /// <summary>
    /// Specifies how many rows to return (top).
    /// </summary>
    /// <param name="rowCount">The number of rows to fetch from the data source.</param>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> Top(int rowCount);

    /// <summary>
    /// Specifies how many rows to offset (skip).
    /// </summary>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> Skip(int skipRowCount);

    /// <summary>
    /// Filters the elements of the interface based on a specified condition defined by the provided expression.
    /// </summary>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> Where(Expression<Func<T, bool>> whereExpression);

    /// <summary>
    /// Filters the elements of the interface based on a specified OData filter.
    /// </summary>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> Where(string oDataFilter);

    /// <summary>
    /// Specifies the order by parameters
    /// </summary>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> OrderBy(params TOrderBy[] orderBy);

    /// <summary>
    /// Specifies table hints to be used in the SELECT query.
    /// </summary>
    ISelectFrom<T, TOrderBy, TPartialLoadFlags> WithTableHints(StormTableHints tableHints);

    #endregion Builder

    #region Misc

    /// <summary>
    /// Asynchronously counts the number of records in a database table that match the specified conditions set in the current instance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain the count of records matching the conditions.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if any row in the database table exists that meets the specified conditions set in the current instance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will indicate whether any such row exists.</returns>
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);

    #endregion Misc

    #region Get

    /// <summary>
    /// Asynchronously retrieves the first or default entity of type T from the database that matches the specified conditions.
    /// This method considers the partial load flags and any set query parameters.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain the first or default entity of type T.</returns>
    Task<T?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a single column value from the database based on the provided column selector. If no match is found, returns a default value.
    /// </summary>
    /// <typeparam name="TColumn">The type of the column to retrieve.</typeparam>
    /// <param name="columnSelector">An expression to select the column.</param>
    /// <param name="defaultWhenNotFound">The default value to return if no match is found.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain the column value or the default value.</returns>
    Task<TColumn?> GetAsync<TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        TColumn? defaultWhenNotFound = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a tuple of two column values from the database based on the provided column selectors.
    /// </summary>
    /// <typeparam name="TColumn1">The type of the first column to retrieve.</typeparam>
    /// <typeparam name="TColumn2">The type of the second column to retrieve.</typeparam>
    /// <param name="columnSelector1">The expression representing the first column.</param>
    /// <param name="columnSelector2">The expression representing the second column.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain a tuple of the retrieved column values.</returns>
    Task<(TColumn1, TColumn2)?> GetAsync<TColumn1, TColumn2>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        CancellationToken cancellationToken = default);

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
    Task<(TColumn1, TColumn2, TColumn3)?> GetAsync<TColumn1, TColumn2, TColumn3>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        Expression<Func<T, TColumn3>> columnSelector3,
        CancellationToken cancellationToken = default);

    #endregion Get

    #region List & Stream

    /// <summary>
    /// Asynchronously retrieves a list of entities of type T from the database based on the set query parameters.
    /// Considers partial load flags and order by column IDs for sorting.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain a list of entities matching the criteria.</returns>
    Task<List<T>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a list of values for a specific column of type TColumn from the database based on the provided column selector and set query parameters.
    /// </summary>
    /// <typeparam name="TColumn">The type of the column to retrieve values from.</typeparam>
    /// <param name="columnSelector">An expression to select the column.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain a list of column values.</returns>
    Task<List<TColumn>> ListAsync<TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a list of tuples containing two column values of types TColumn1 and TColumn2 from the database based on the provided column selectors.
    /// </summary>
    /// <typeparam name="TColumn1">The type of the first column to retrieve values from.</typeparam>
    /// <typeparam name="TColumn2">The type of the second column to retrieve values from.</typeparam>
    /// <param name="columnSelector1">The expression for selecting the first column.</param>
    /// <param name="columnSelector2">The expression for selecting the second column.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain a list of tuples with the column values.</returns>
    Task<List<(TColumn1, TColumn2)>> ListAsync<TColumn1, TColumn2>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a list of tuples containing three column values of types TColumn1, TColumn2, and TColumn3 from the database based on the provided column selectors.
    /// </summary>
    /// <typeparam name="TColumn1">The type of the first column to retrieve values from.</typeparam>
    /// <typeparam name="TColumn2">The type of the second column to retrieve values from.</typeparam>
    /// <typeparam name="TColumn3">The type of the third column to retrieve values from.</typeparam>
    /// <param name="columnSelector1">The expression for selecting the first column.</param>
    /// <param name="columnSelector2">The expression for selecting the second column.</param>
    /// <param name="columnSelector3">The expression for selecting the third column.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain a list of tuples with the column values.</returns>
    Task<List<(TColumn1, TColumn2, TColumn3)>> ListAsync<TColumn1, TColumn2, TColumn3>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        Expression<Func<T, TColumn3>> columnSelector3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams data of type T asynchronously from the database using the provided connection and set query parameters.
    /// This method is useful for processing large datasets that should not be loaded into memory all at once.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>An IAsyncEnumerable of T representing the stream of data from the database.</returns>
    IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default);

    #endregion List & Stream
}

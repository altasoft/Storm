using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for selecting a single entity of type T from a data source, with options for ordering and partial loading.
/// T must implement the IDataBindable interface.
/// TOrderBy must be a struct, Enum, and IConvertible.
/// TPartialLoadFlags must be a struct, Enum, and IConvertible.
/// </summary>
public interface ISelectFromSingle<T, in TOrderBy, in TPartialLoadFlags>
    where T : IDataBindable
    where TOrderBy : struct, Enum, IConvertible
    where TPartialLoadFlags : struct, Enum, IConvertible
{
    #region Builder

    /// <summary>
    /// Specifies whether to close the connection after query.
    /// </summary>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithCloseConnection();

    /// <summary>
    /// Sets the command timeout for the query.
    /// </summary>
    /// <param name="commandTimeout">The timeout value for commands in seconds.</param>
    /// <returns>The interface with the specified command timeout.</returns>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithCommandTimeOut(int commandTimeout);

    /// <summary>
    /// Specifies to start automatic change tracking.
    /// </summary>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithTracking();

    /// <summary>
    /// Specifies not to start automatic change tracking.
    /// </summary>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithNoTracking();

    /// <summary>
    /// Specifies partial loading of data based on the specified flags.
    /// </summary>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> Partially(TPartialLoadFlags partialLoadFlags);

    /// <summary>
    /// Specifies table hints to be used in the SELECT query.
    /// </summary>
    ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> WithTableHints(StormTableHints tableHints);

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
    /// This method is deprecated. Please use another overload.
    /// </summary>
    [Obsolete("This method is deprecated. Please use another overload")]
    Task<TColumn?> GetAsync<TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        TColumn? defaultWhenNotFound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a single column value from the database based on the provided column selector, returning a tuple containing the value and a flag indicating whether a matching row was found.
    /// </summary>
    /// <typeparam name="TColumn">The type of the column to retrieve.</typeparam>
    /// <param name="columnSelector">An expression to select the column.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, which, upon completion, will contain the column value and whether row was found.</returns>
    Task<(TColumn? value, bool found)> GetAsync<TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
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
}

using System;
using System.Data;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace AltaSoft.Storm;

/// <summary>
/// Interface for managing database connections and transactions in Storm.
/// </summary>
internal interface IStormContext
{
    /// <summary>
    /// Gets the connection string for the database.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Gets the current <see cref="StormDbConnection"/> instance to be used for database operations.
    /// If <see cref="IsStandalone"/> is <c>true</c>, always returns an owned connection.
    /// Otherwise, returns the connection from the ambient unit of work if available; 
    /// falls back to an owned connection if not.
    /// </summary>
    /// <returns>The <see cref="StormDbConnection"/> to use.</returns>
    StormDbConnection GetConnection();

    /// <summary>
    /// Executes a SQL statement using the provided <see cref="StormDbConnection"/> and returns the number of rows affected.
    /// </summary>
    /// <param name="sqlStatement">The SQL statement to be executed.</param>
    /// <param name="commandType">The type of the command.</param>
    /// <param name="commandSetup">The action to set up the command.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// An asynchronous <see cref="Task{TResult}"/> representing the number of rows affected by the SQL statement execution.
    /// </returns>
    Task<int> ExecuteSqlStatementAsync(
        string sqlStatement, CommandType commandType = CommandType.Text, Action<StormDbCommand>? commandSetup = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the context is currently not within a unit of work.
    /// </summary>
    bool IsStandalone { get; }

    /// <summary>
    /// Gets a value indicating whether the context is currently within a unit of work.
    /// </summary>
    bool IsInUnitOfWork { get; }

    /// <summary>
    /// Creates a new <see cref="Batch"/> operation for grouping multiple SQL commands for execution.
    /// </summary>
    /// <returns>A new <see cref="Batch"/> instance.</returns>
    Batch CreateBatch();

    /// <summary>
    /// Retrieves the next value of a database sequence as a strongly-typed result.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the result, which must be a struct and implement the <see cref="IBinaryInteger{TResult}"/> interface.
    /// </typeparam>
    /// <param name="schemaName">The name of the schema containing the sequence. If null, the default schema name is used.</param>
    /// <param name="sequenceName">The name of the sequence from which to retrieve the next value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the next value of the sequence as a <typeparamref name="TResult"/>.
    /// </returns>
    Task<TResult> GetNextSequenceAsync<TResult>(string? schemaName, string sequenceName, CancellationToken cancellationToken = default)
        where TResult : struct, IBinaryInteger<TResult>;

    /// <summary>
    /// Retrieves the next range of values of a database sequence as a strongly-typed result.
    /// </summary>
    /// <param name="schemaName">The name of the schema containing the sequence. If null, the default schema name is used.</param>
    /// <param name="sequenceName">The name of the sequence from which to retrieve the next range of values.</param>
    /// <param name="rangeSize">The size of the range to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the first value of the retrieved range as a <see cref="long"/>.
    /// </returns>
    Task<long> GetNextSequenceRangeAsync(string? schemaName, string sequenceName, int rangeSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current value of a database sequence as a strongly-typed result.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the result, which must be a struct and implement the <see cref="IBinaryInteger{TResult}"/> interface.
    /// </typeparam>
    /// <param name="schemaName">The name of the schema containing the sequence. If null, the default schema name is used.</param>
    /// <param name="sequenceName">The name of the sequence from which to retrieve the next value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the current value of the sequence as a <typeparamref name="TResult"/>.
    /// </returns>
    Task<TResult> GetCurrentSequenceAsync<TResult>(string? schemaName, string sequenceName, CancellationToken cancellationToken = default)
        where TResult : struct, IBinaryInteger<TResult>;

    /// <summary>
    /// Asynchronously retrieves the last identity value generated for an identity column in the current session and scope.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the last identity value as an <see cref="int"/>.
    /// </returns>
    Task<int> GetScopeIdentityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the current database row version (timestamp) value.
    /// SELECT @@DBTS
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the current row version as a <see cref="SqlRowVersion"/>.
    /// </returns>
    Task<SqlRowVersion> GetCurrentRowVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the minimum active row version in the current database.
    /// SELECT MIN_ACTIVE_ROWVERSION()
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the minimum active row version as a <see cref="SqlRowVersion"/>.
    /// </returns>
    Task<SqlRowVersion> GetMinActiveRowVersionAsync(CancellationToken cancellationToken = default);

    ///<summary>
    /// Retrieves both the current database row version (timestamp) value and the minimum active row version in the current database.
    /// SELECT @@DBTS, MIN_ACTIVE_ROWVERSION()
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a tuple with the current row version as <see cref="SqlRowVersion"/> and the minimum active row version as <see cref="SqlRowVersion"/>.
    /// </returns>
    Task<(SqlRowVersion currentRowVersion, SqlRowVersion minActiveRowVersion)> GetCurrentAndMinActiveRowVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the last log sequence number (LSN) in the current database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the last log sequence number as a <see cref="SqlLogSequenceNumber"/>.
    /// </returns>
    Task<SqlLogSequenceNumber> GetLastLogSequenceNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the last log sequence number (LSN) for a specific capture instance.
    /// </summary>
    /// <param name="captureInstance">The name of the capture instance.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the last log sequence number as a <see cref="SqlLogSequenceNumber"/>.
    /// </returns>
    Task<SqlLogSequenceNumber> GetLastLogSequenceNumberAsync(string captureInstance, CancellationToken cancellationToken = default);
}

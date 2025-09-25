using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Extensions;
using Microsoft.Extensions.Logging;

namespace AltaSoft.Storm;

/// <summary>
/// Represents the primary database context for executing queries and commands.
/// Can participate in a unit of work (transaction) or operate standalone.
/// </summary>
public abstract class StormContext : IAsyncDisposable, IDisposable, IStormContext
{
    private readonly ILogger? _logger;

    private StormDbConnection? _ownedConnection; // only when not in UoW
    private bool _disposed;

    // Should be set at app startup.
    private static readonly Dictionary<Type, string> s_connectionStrings = new(4);
    private static readonly Dictionary<Type, string> s_dbSchemas = new(4);
    private readonly bool _standalone;

    /// <summary>
    /// Gets the connection string associated with this context.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StormContext"/> class using the connection string from the static map.
    /// The context will use its own connection if <paramref name="standalone"/> is <c>true</c>;
    /// otherwise, it will use the connection and transaction of the ambient unit of work if available.
    /// </summary>
    /// <param name="standalone">
    /// If <c>true</c>, the context will always use its own connection and never participate in any ambient unit of work.
    /// If <c>false</c> (default), the context will participate in a unit of work if present.
    /// </param>
    protected StormContext(bool standalone = false)
    {
        ConnectionString = GetConnectionString(GetType());
        _standalone = standalone;
        _logger = GetLogger();

        if (!standalone)
            _standalone = CheckConnectionString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StormContext"/> class with a specific connection string.
    /// The context will use its own connection if <paramref name="standalone"/> is <c>true</c>;
    /// otherwise, it will use the connection and transaction of the ambient unit of work if available.
    /// </summary>
    /// <param name="connectionString">The connection string to use for this context.</param>
    /// <param name="standalone">
    /// If <c>true</c>, the context will always use its own connection and never participate in any ambient unit of work.
    /// If <c>false</c> (default), the context will participate in a unit of work if present.
    /// </param>
    protected StormContext(string connectionString, bool standalone = false)
    {
        ConnectionString = connectionString;
        _standalone = standalone;
        _logger = GetLogger();

        if (!standalone)
            _standalone = CheckConnectionString();
    }

    /// <summary>
    /// Gets the current <see cref="StormDbConnection"/> instance to be used for database operations.
    /// If <see cref="_standalone"/> is <c>true</c>, always returns an owned connection.
    /// Otherwise, returns the connection from the ambient unit of work if available; 
    /// falls back to an owned connection if not.
    /// </summary>
    /// <returns>The <see cref="StormDbConnection"/> to use.</returns>
    public StormDbConnection GetConnection()
    {
        if (_standalone)
        {
            // Standalone context: always use an owned connection.
            return _ownedConnection ??= new StormDbConnection(ConnectionString);
        }

        var uow = AmbientUnitOfWork.Ambient;
        if (uow is not null)
        {
            if (!uow.IsInitialized)
                throw new StormException("StormContext: The ambient UnitOfWork is not initialized.");

            // Use the connection from the ambient unit of work.
            return uow.Connection;
        }

        // Not in a unit of work: use an owned connection.
        return _ownedConnection ??= new StormDbConnection(ConnectionString);
    }

    /// <summary>
    /// Gets a value indicating whether the context is currently not within a unit of work.
    /// </summary>
    public bool IsStandalone => _standalone || AmbientUnitOfWork.Ambient is null;

    /// <summary>
    /// Gets a value indicating whether the context is currently within a unit of work.
    /// </summary>
    public bool IsInUnitOfWork => !_standalone && AmbientUnitOfWork.Ambient is not null;

    /// <summary>
    /// Gets the current <see cref="StormDbTransaction"/>, or null if none is active.
    /// </summary>
    /// <returns>The current <see cref="StormDbTransaction"/>, or null if not in a unit of work.</returns>
    internal StormDbTransaction? GetTransaction()
    {
        if (_standalone)
            return null; // Standalone context does not use transactions.

        var uow = AmbientUnitOfWork.Ambient;
        return uow?.Transaction; // Null if not in UoW.
    }

    /// <summary>
    /// Gets the connection string for a specific <see cref="StormContext"/> type.
    /// </summary>
    /// <typeparam name="TStormContext">The type of the <see cref="StormContext"/>.</typeparam>
    /// <returns>The connection string for the specified context type.</returns>
    public static string GetConnectionString<TStormContext>() where TStormContext : StormContext
    {
        return GetConnectionString(typeof(TStormContext));
    }

    /// <summary>
    /// Gets the connection string for a specific <see cref="StormContext"/> type.
    /// </summary>
    /// <param name="stormContextType">The type of the <see cref="StormContext"/>.</param>
    /// <returns>The connection string for the specified context type.</returns>
    /// <exception cref="StormException">Thrown if the connection string is not set for the specified type.</exception>
    public static string GetConnectionString(Type stormContextType)
    {
        return s_connectionStrings.TryGetValue(stormContextType, out var cs)
            ? cs
            : throw new StormException($"Connection string is not set for {stormContextType.Name}.");
    }

    /// <summary>
    /// Gets the quoted schema name for a specific <see cref="StormContext"/> type.
    /// </summary>
    /// <typeparam name="TStormContext">The type of the <see cref="StormContext"/>.</typeparam>
    /// <returns>The quoted schema name for the specified context type, or "[dbo]" if not set.</returns>
    public static string GetQuotedSchema<TStormContext>() where TStormContext : StormContext
    {
        return GetQuotedSchema(typeof(TStormContext));
    }

    /// <summary>
    /// Gets the quoted schema name for a specific <see cref="StormContext"/> type.
    /// </summary>
    /// <param name="stormContextType">The type of the <see cref="StormContext"/>.</param>
    /// <returns>The quoted schema name for the specified context type, or "[dbo]" if not set.</returns>
    public static string GetQuotedSchema(Type stormContextType)
    {
        return s_dbSchemas.GetValueOrDefault(stormContextType, "[dbo]");
    }

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
    public async Task<int> ExecuteSqlStatementAsync(
        string sqlStatement, CommandType commandType = CommandType.Text, Action<StormDbCommand>? commandSetup = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await EnsureConnectionIsOpenAsync(cancellationToken).ConfigureAwait(false);

        return await connection.ExecuteSqlStatementAsync(sqlStatement, commandType,
            command =>
            {
                command.Transaction = GetTransaction();
                commandSetup?.Invoke(command);
            }, cancellationToken).ConfigureAwait(false);
    }

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
    public Task<TResult> GetNextSequenceAsync<TResult>(string? schemaName, string sequenceName, CancellationToken cancellationToken = default)
        where TResult : struct, IBinaryInteger<TResult>
    {
        var statement = $"SELECT NEXT VALUE FOR {schemaName?.QuoteSqlName() ?? GetQuotedSchema(GetType())}.{sequenceName.QuoteSqlName()}";
        var queryParameters = new QueryParameters(this);
        return this.ExecuteScalarAsync<TResult>(statement, queryParameters, cancellationToken);
    }

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
    public Task<TResult> GetCurrentSequenceAsync<TResult>(string? schemaName, string sequenceName, CancellationToken cancellationToken = default) where TResult : struct, IBinaryInteger<TResult>
    {
        sequenceName = sequenceName.UnquoteSqlName().QuoteName('\'');
        schemaName = schemaName?.UnquoteSqlName().QuoteName('\'') ?? GetQuotedSchema(GetType()).QuoteName('\'');

        var statement =
            $"""
             SELECT [current_value] 
             FROM sys.sequences seq
             	INNER JOIN sys.schemas sc ON sc.[schema_id] = seq.[schema_id]
             WHERE seq.[name] = {sequenceName} AND sc.[name] = {schemaName};
             """;
        var queryParameters = new QueryParameters(this);
        return this.ExecuteScalarAsync<TResult>(statement, queryParameters, cancellationToken);
    }

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
    public Task<long> GetNextSequenceRangeAsync(string? schemaName, string sequenceName, int rangeSize, CancellationToken cancellationToken = default)
    {
        var sequenceFullName = $"{schemaName?.QuoteSqlName() ?? GetQuotedSchema(GetType())}.{sequenceName.QuoteSqlName()}".QuoteSqlName().QuoteName('\'');
        var statement =
            $"""
                 DECLARE @first_value sql_variant;
                 EXEC sys.sp_sequence_get_range @sequence_name={sequenceFullName},@range_size={rangeSize.ToString(CultureInfo.InvariantCulture)},@range_first_value=@first_value OUTPUT;
                 SELECT CAST(@first_value AS bigint);
                 """;

        var queryParameters = new QueryParameters(this);
        return this.ExecuteScalarAsync<long>(statement, queryParameters, cancellationToken);
    }

    /// <summary>
    /// Asynchronously retrieves the last identity value generated for an identity column in the current session and scope.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the last identity value as an <see cref="int"/>.
    /// </returns>
    public Task<int> GetScopeIdentityAsync(CancellationToken cancellationToken = default)
    {
        const string statement = "SELECT SCOPE_IDENTITY()";

        var queryParameters = new QueryParameters(this);
        return this.ExecuteScalarAsync<int>(statement, queryParameters, cancellationToken);
    }

    /// <summary>
    /// Asynchronously retrieves the current database row version (timestamp) value.
    /// SELECT @@DBTS
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the current row version as a <see cref="SqlRowVersion"/>.
    /// </returns>
    public async Task<SqlRowVersion> GetCurrentRowVersionAsync(CancellationToken cancellationToken = default)
    {
        const string statement = "SELECT @@DBTS";

        var queryParameters = new QueryParameters(this);
        var bytes = await this.ExecuteScalarAsync<byte[]>(statement, queryParameters, cancellationToken).ConfigureAwait(false);
        return new SqlRowVersion(bytes);
    }

    /// <summary>
    /// Asynchronously retrieves the minimum active row version in the current database.
    /// SELECT MIN_ACTIVE_ROWVERSION()
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the minimum active row version as a <see cref="SqlRowVersion"/>.
    /// </returns>
    public async Task<SqlRowVersion> GetMinActiveRowVersionAsync(CancellationToken cancellationToken = default)
    {
        const string statement = "SELECT MIN_ACTIVE_ROWVERSION()";

        var queryParameters = new QueryParameters(this);
        var bytes = await this.ExecuteScalarAsync<byte[]>(statement, queryParameters, cancellationToken).ConfigureAwait(false);
        return new SqlRowVersion(bytes);
    }

    ///<summary>
    /// Retrieves both the current database row version (timestamp) value and the minimum active row version in the current database.
    /// SELECT @@DBTS, MIN_ACTIVE_ROWVERSION()
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a tuple with the current row version as <see cref="SqlRowVersion"/> and the minimum active row version as <see cref="SqlRowVersion"/>.
    /// </returns>
    public async Task<(SqlRowVersion currentRowVersion, SqlRowVersion minActiveRowVersion)> GetCurrentAndMinActiveRowVersionsAsync(CancellationToken cancellationToken = default)
    {
        const string statement = "SELECT @@DBTS, MIN_ACTIVE_ROWVERSION()";

        var queryParameters = new QueryParameters(this);

        var (bytes1, bytes2) = await this.ExecuteScalarAsync<(byte[] dbts, byte[] minActiveRowVersion)>(statement, queryParameters,
            dr => (dr.GetBinary(0), dr.GetBinary(1)), cancellationToken).ConfigureAwait(false);

        return (new SqlRowVersion(bytes1), new SqlRowVersion(bytes2));
    }

    /// <summary>
    /// Asynchronously retrieves the last log sequence number (LSN) in the current database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the last log sequence number as a <see cref="SqlLogSequenceNumber"/>.
    /// </returns>
    public async Task<SqlLogSequenceNumber> GetLastLogSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        const string statement = "SELECT sys.fn_cdc_get_max_lsn()";

        var queryParameters = new QueryParameters(this);
        var bytes = await this.ExecuteScalarAsync<byte[]>(statement, queryParameters, cancellationToken).ConfigureAwait(false);
        return new SqlLogSequenceNumber(bytes);
    }

    /// <summary>
    /// Asynchronously retrieves the last log sequence number (LSN) for a specific capture instance.
    /// </summary>
    /// <param name="captureInstance">The name of the capture instance.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the last log sequence number as a <see cref="SqlLogSequenceNumber"/>.
    /// </returns>
    public async Task<SqlLogSequenceNumber> GetLastLogSequenceNumberAsync(string captureInstance, CancellationToken cancellationToken = default)
    {
        var statement = $"SELECT sys.fn_cdc_get_max_lsn('{captureInstance.QuoteSqlName()}')";

        var queryParameters = new QueryParameters(this);
        var bytes = await this.ExecuteScalarAsync<byte[]>(statement, queryParameters, cancellationToken).ConfigureAwait(false);
        return new SqlLogSequenceNumber(bytes);
    }

    /// <summary>
    /// Ensures that the current connection is open asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, with the open <see cref="StormDbConnection"/>.
    /// </returns>
    private async ValueTask<StormDbConnection> EnsureConnectionIsOpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <summary>
    /// Creates a new <see cref="Batch"/> operation for grouping multiple SQL commands for execution.
    /// </summary>
    /// <returns>A new <see cref="Batch"/> instance.</returns>
    public Batch CreateBatch() => new(this);

    /// <summary>
    /// Sets the default connection string for a specific <see cref="StormContext"/> type.
    /// </summary>
    /// <param name="stormContextType">The type of the <see cref="StormContext"/>.</param>
    /// <param name="connectionString">The connection string to associate with the context type.</param>
    internal static void SetDefaultConnectionString(Type stormContextType, string connectionString)
    {
        s_connectionStrings[stormContextType] = connectionString;
    }

    internal static void SetDefaultSchema(Type stormContextType, string schema)
    {
        s_dbSchemas[stormContextType] = schema.QuoteSqlName();
    }

    /// <summary>
    /// Gets the logger instance if logging is enabled at the Trace level; otherwise, returns null.
    /// </summary>
    /// <returns>The <see cref="ILogger"/> instance or null.</returns>
    private static ILogger? GetLogger() => StormManager.Logger?.IsEnabled(LogLevel.Trace) == true ? StormManager.Logger : null;

    /// <summary>
    /// Checks that the context's connection string matches the active unit of work's connection string, if present.
    /// </summary>
    /// <exception cref="StormException">
    /// Thrown if the context connection string does not match the active unit of work connection string.
    /// </exception>
    private bool CheckConnectionString()
    {
        var uow = AmbientUnitOfWork.Ambient;
        if (uow is null)
        {
            return true; // No active unit of work, no check needed.
        }

        if (!uow.IsInitialized)
        {
            _logger?.LogError("StormContext: The ambient UnitOfWork is not initialized.");
            throw new StormException("StormContext: The ambient UnitOfWork is not initialized.");
        }

        // If the context is not standalone, ensure the connection strings match.
        if (!ConnectionString.IsSameConnectionString(uow.Connection.ConnectionString))
        {
            _logger?.LogError("StormContext: The context connection string '{ConnectionString1}' does not match the active UnitOfWork connection string '{ConnectionString2}'.", ConnectionString, uow.Connection.ConnectionString);
            throw new StormException($"StormContext: The context connection string '{ConnectionString}' does not match the active UnitOfWork connection string '{uow.Connection.ConnectionString}'.");
        }

        return false;
    }

    /// <summary>
    /// Asynchronously disposes the resources used by the <see cref="StormContext"/> instance.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the resources used by the <see cref="StormContext"/> instance.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from <see cref="DisposeAsync()"/>.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing && _ownedConnection is not null)
        {
            await _ownedConnection.DisposeAsync().ConfigureAwait(false);
            _ownedConnection = null;
        }
        _disposed = true;
    }

    /// <summary>
    /// Synchronously disposes the resources used by the <see cref="StormContext"/> instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Synchronously disposes the resources used by the <see cref="StormContext"/> instance.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing && _ownedConnection is not null)
        {
            _ownedConnection.Dispose();
            _ownedConnection = null;
        }
        _disposed = true;
    }
}

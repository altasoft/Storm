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

    private StormDbConnection? _ownedConnection; // only when not in ambient
    private bool _disposed;

    // Should be set at app startup.
    private static readonly Dictionary<Type, string> s_connectionStrings = new(4);
    private static readonly Dictionary<Type, string> s_dbSchemas = new(4);

    /// <summary>
    /// Gets the connection string associated with this context.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Gets a value indicating whether the context is currently not within a transaction scope.
    /// </summary>
    public bool IsStandalone { get; }

    /// <summary>
    /// Gets a value indicating whether the context is currently within a transaction scope.
    /// </summary>
    public bool IsInTransactionScope => !IsStandalone;

    /// <summary>
    /// Initializes a new instance of the <see cref="StormContext"/> class using the connection string from the static map.
    /// The context will use its own connection if <paramref name="standalone"/> is <c>true</c>;
    /// otherwise, it will use the connection and transaction of the ambient transaction scope if available.
    /// </summary>
    /// <param name="standalone">
    /// If <c>true</c>, the context will always use its own connection and never participate in any ambient transaction.
    /// If <c>false</c> (default), the context will participate in a transaction scope if present.
    /// </param>
    protected StormContext(bool standalone = false)
    {
        ConnectionString = GetConnectionString(GetType());
        IsStandalone = standalone || StormTransactionScope.GetCurrentAmbient() is null;
        _logger = GetLogger();

        if (!IsStandalone)
            CheckConnectionString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StormContext"/> class with a specific connection string.
    /// The context will use its own connection if <paramref name="standalone"/> is <c>true</c>;
    /// otherwise, it will use the connection and transaction of the ambient transaction scope if available.
    /// </summary>
    /// <param name="connectionString">The connection string to use for this context.</param>
    /// <param name="standalone">
    /// If <c>true</c>, the context will always use its own connection and never participate in any ambient transaction.
    /// If <c>false</c> (default), the context will participate in a transaction scope if present.
    /// </param>
    protected StormContext(string connectionString, bool standalone = false)
    {
        ConnectionString = connectionString;
        IsStandalone = standalone || StormTransactionScope.GetCurrentAmbient() is null;
        _logger = GetLogger();

        if (!IsStandalone)
            CheckConnectionString();
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
            : throw new StormException($"Connection string is not set for type '{stormContextType.Name}'.");
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
        var (connection, transaction) = await EnsureConnectionAndTransactionIsOpenAsync(cancellationToken).ConfigureAwait(false);

        return await connection.ExecuteSqlStatementAsync(sqlStatement, commandType,
            command =>
            {
                command.Transaction = transaction;
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

    internal async ValueTask<(StormDbConnection connection, StormDbTransaction? transaction)> EnsureConnectionAndTransactionIsOpenAsync(CancellationToken cancellationToken)
    {
        StormDbConnection connection;
        StormDbTransaction? transaction = null;

        var ambient = StormTransactionScope.GetCurrentAmbient();

        if (IsStandalone || ambient is null)
        {
            // Standalone context: always use an owned connection.
            connection = _ownedConnection ??= new StormDbConnection(ConnectionString);
        }
        else
        {
            // Use the connection from the ambient.
            connection = ambient.GetConnection(ConnectionString);
        }

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!IsStandalone && ambient is not null)
        {
            transaction = await ambient.GetTransactionAsync(cancellationToken).ConfigureAwait(false);
        }

        return (connection, transaction);
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
    /// Thrown if the context connection string does not match the active UnitOfWork connection string.
    /// </exception>
    private void CheckConnectionString()
    {
        var ambient = StormTransactionScope.GetCurrentAmbient();

        if (ambient?.Connection is null)
        {
            return; // No active ambient, no check needed.
        }

        // If the context is not standalone, ensure the connection strings match.
        if (!ConnectionString.IsSameConnectionString(ambient.Connection.ConnectionString))
        {
            _logger?.LogError("StormContext: The context connection string '{ConnectionString1}' does not match the active ambient connection string '{ConnectionString2}'.", ConnectionString, ambient.Connection.ConnectionString);
            throw new StormException($"StormContext: The context connection string '{ConnectionString}' does not match the active ambient connection string '{ambient.Connection.ConnectionString}'.");
        }
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

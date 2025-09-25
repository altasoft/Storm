using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Contains extension methods for working with database connections.
/// </summary>
public static class DbConnectionExt
{
    /// <summary>
    /// Executes a SQL statement using the provided SqlConnection and returns the number of rows affected.
    /// </summary>
    /// <param name="connection">The extension method for SqlConnection.</param>
    /// <param name="sqlStatement">The SQL statement to be executed.</param>
    /// <param name="commandType">The type of the command.</param>
    /// <param name="commandSetup">The action to set up the command.</param>
    /// <param name="cancellationToken">A CancellationToken to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous Task&lt;int&gt; representing the number of rows affected by the SQL statement execution.</returns>
    public static async Task<int> ExecuteSqlStatementAsync(this StormDbConnection connection,
        string sqlStatement, CommandType commandType = CommandType.Text, Action<StormDbCommand>? commandSetup = null, CancellationToken cancellationToken = default)
    {
        await using var command = StormManager.CreateCommand(false);
        command.Connection = connection;
        command.CommandText = sqlStatement;
        command.CommandType = commandType;

        commandSetup?.Invoke(command);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<TResult> ExecuteScalarAsync<TResult>(
       this StormContext context,
       ScalarParameters queryParameters,
       Func<StormDbDataReader, TResult> resultReader,
       CancellationToken cancellationToken = default)
    {
        var command = StormManager.CreateCommand(false);

        await using (command.ConfigureAwait(false))
        {
            var callParamStr = command.GenerateCallParameters(queryParameters.CallParameters, CallParameterType.Function);

            var sql = $"SELECT {queryParameters.SchemaName?.QuoteSqlName() ?? StormContext.GetQuotedSchema(context.GetType())}.{queryParameters.ObjectName.QuoteSqlName()} ({callParamStr})";

            command.SetStormCommandBaseParameters(context, sql, queryParameters);

            var commandBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;
            if (queryParameters.CloseConnection)
                commandBehavior |= CommandBehavior.CloseConnection;

            var connection = context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return default!;

                return resultReader(reader);
            }
        }
    }

    internal static async Task<TResult> ExecuteScalarAsync<TResult>(
        this StormContext context,
        string sqlStatement,
        QueryParameters queryParameters,
        Func<StormDbDataReader, TResult> resultReader,
        CancellationToken cancellationToken = default)
    {
        var command = StormManager.CreateCommand(false);

        await using (command.ConfigureAwait(false))
        {
            command.SetStormCommandBaseParameters(context, sqlStatement, queryParameters);

            var commandBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;
            if (queryParameters.CloseConnection)
                commandBehavior |= CommandBehavior.CloseConnection;

            var connection = context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return default!;

                return resultReader(reader);
            }
        }
    }

    internal static async Task<TResult> ExecuteScalarAsync<TResult>(
        this StormContext context,
        string sqlStatement,
        QueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var command = StormManager.CreateCommand(false);

        await using (command.ConfigureAwait(false))
        {
            command.SetStormCommandBaseParameters(context, sqlStatement, queryParameters);

            var connection = context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return (TResult)value;
        }
    }

    internal static async Task<TResult> ExecuteProcedureAsync<TResult>(
        this StormContext context,
        ScalarParameters queryParameters,
        Func<int, StormDbParameterCollection, Exception?, TResult> resultReader,
        CancellationToken cancellationToken = default)
        where TResult : StormProcedureResult
    {
        var command = StormManager.CreateCommand(true);
        await using (command.ConfigureAwait(false))
        {
            var sql = $"{queryParameters.SchemaName?.QuoteSqlName() ?? StormContext.GetQuotedSchema(context.GetType())}.{queryParameters.ObjectName.QuoteSqlName()}";

            command.GenerateCallParameters(queryParameters.CallParameters, CallParameterType.StoreProcedure);
            command.SetStormCommandBaseParameters(queryParameters.Context, sql, queryParameters, CommandType.StoredProcedure);

            var connection = context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var (rowsAffected, ex) = await command.ExecuteCommand2Async(cancellationToken).ConfigureAwait(false);

            if (queryParameters.CloseConnection)
                await connection.CloseAsync().ConfigureAwait(false);

            return resultReader(rowsAffected, command.Parameters, ex);
        }
    }
}

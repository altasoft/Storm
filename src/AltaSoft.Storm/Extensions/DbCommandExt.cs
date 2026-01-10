using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using Microsoft.Extensions.Logging;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Extension class for <see cref="StormDbCommand" />.
/// </summary>
internal static class DbCommandExt
{
    /// <summary>
    /// Adds a database parameter to the given StormDbCommand object.
    /// </summary>
    /// <param name="command">The DbCommand object to which the parameter will be added.</param>
    /// <param name="parameter">The StormCallParameter object containing information about the parameter.</param>
    /// <returns>
    /// The name of the added parameter.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string AddDbParameter(this StormDbCommand command, StormCallParameter parameter)
    {
        var p = StormManager.AddDbParameter(command, parameter.ParameterName, parameter.DbType, parameter.Size, parameter.Value, parameter.Direction);
        return p.ParameterName;
    }

    /// <summary>
    /// Adds a database parameter to the specified DbCommand object.
    /// </summary>
    /// <param name="command">The DbCommand object to add the parameter to.</param>
    /// <param name="paramIdx">The index of the parameter.</param>
    /// <param name="column">The StormColumnDef object representing the column.</param>
    /// <param name="value">The value to be assigned to the parameter. Can be null.</param>
    /// <returns>The name of the added parameter.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string AddDbParameter(this StormDbCommand command, int paramIdx, StormColumnDef column, object? value)
    {
        var p = StormManager.AddDbParameter(command, "@p" + paramIdx.ToString(System.Globalization.CultureInfo.InvariantCulture), column.DbType, column.Size, value);
        return p.ParameterName;
    }

    /// <summary>
    /// Generates call parameters for a DbCommand based on the provided list of StormCallParameter objects.
    /// </summary>
    internal static string? GenerateCallParameters(this StormDbCommand command, List<StormCallParameter>? callParameters, CallParameterType type)
    {
        if (type == CallParameterType.StoreProcedure)
        {
            StormManager.AddDbParameter(command, "ReturnValue", UnifiedDbType.Int32, 0, 0, ParameterDirection.ReturnValue);
        }

        if (callParameters is null)
        {
            return null;
        }

        if (type != CallParameterType.CustomSqlStatement)
        {
            var sb = StormManager.GetStringBuilderFromPool();
            sb.AppendJoinFast(callParameters, ',', (builder, x) =>
            {
                builder.Append(x.ParameterName);
                if (x.Direction != ParameterDirection.Input)
                    builder.Append(" OUTPUT");
                command.AddDbParameter(x);
            });

            return sb.ToStringAndReturnToPool();
        }

        // For CustomSqlStatement, we just add parameters without generating a string
        foreach (var p in callParameters)
        {
            command.AddDbParameter(p);
        }
        return null;
    }
   
    /// <summary>
    /// Sets the parameters for a database command, including the command text, connection, transaction, command timeout, and command type.
    /// </summary>
    /// <param name="command">The database command to set the parameters for.</param>
    /// <param name="context">The Storm context.</param>
    /// <param name="commandText">The command text to set for the command.</param>
    /// <param name="queryParameters">The query parameters.</param>
    /// <param name="commandType">The command type to set for the command. Optional.</param>
    internal static void SetStormCommandBaseParameters(this StormDbCommand command, StormDbConnection connection, StormDbTransaction? transaction, string commandText, QueryParameters queryParameters, CommandType commandType = CommandType.Text)
    {
        command.Connection = connection;
        command.Transaction = transaction;

        command.CommandText = commandText;
        command.CommandType = commandType;

        if (queryParameters.CommandTimeout.HasValue)
            command.CommandTimeout = queryParameters.CommandTimeout.Value;
    }

    internal static void SetStormCommandBaseParameters(this StormDbCommand command, string commandText, QueryParameters queryParameters, CommandType commandType = CommandType.Text)
    {
        command.CommandText = commandText;
        command.CommandType = commandType;

        if (queryParameters.CommandTimeout.HasValue)
            command.CommandTimeout = queryParameters.CommandTimeout.Value;
    }

    internal static void SetStormCommandBaseParameters(this StormDbCommand command, StormDbConnection connection, StormDbTransaction? transaction)
    {
        command.Connection = connection;
        command.Transaction = transaction;
    }

    /// <summary>
    /// Executes a database command asynchronously and returns the number of rows affected.
    /// </summary>
    /// <param name="command">The database command to execute.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an integer representing the number of rows affected by the command execution.
    /// </returns>
    /// <exception cref="StormDbException">Thrown when there is an error executing the database command.</exception>
    internal static async Task<int> ExecuteCommandAsync(this StormDbCommand command, CancellationToken cancellationToken)
    {
        command.Log();
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (StormDbException ex)
        {
            if (StormManager.HandleDbException(ex) is { } handledException)
                throw handledException;
            throw;
        }
    }

    /// <summary>
    /// Executes a database command asynchronously and returns the number of rows affected along with any exception that occurred.
    /// </summary>
    /// <param name="command">The database command to execute.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a tuple with an integer representing the number of rows affected by the command execution and an exception if one occurred.
    /// </returns>
    internal static async Task<(int returnValue, Exception? ex)> ExecuteCommand2Async(this StormDbCommand command, CancellationToken cancellationToken)
    {
        command.Log();

        try
        {
            return (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), null);
        }
        catch (StormDbException ex)
        {
            if (StormManager.HandleDbException(ex) is { } handledException)
                return (-1, handledException);
            return (-1, ex);
        }
    }

    /// <summary>
    /// Asynchronously executes the command and returns a data reader for reading the results, with the specified behavior and cancellation token.
    /// </summary>
    /// <param name="command">The StormDbCommand to execute.</param>
    /// <param name="behavior">One of the CommandBehavior values that determines the behavior of the command execution.</param>
    /// <param name="cancellationToken">The CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A Task representing the asynchronous operation. The task result is a StormDbDataReader for reading the results of the command.
    /// </returns>
    internal static async Task<StormDbDataReader> ExecuteCommandReaderAsync(this StormDbCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
    {
        command.Log();
        try
        {
            return await command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
        }
        catch (StormDbException ex)
        {
            if (StormManager.HandleDbException(ex) is { } handledException)
                throw handledException;
            throw;
        }
    }

    /// <summary>
    /// Executes a scalar command asynchronously using the provided StormDbCommand and cancellation token.
    /// </summary>
    /// <param name="command">The StormDbCommand to execute.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// An object representing the result of the scalar command execution.
    /// </returns>
    internal static async Task<object> ExecuteScalarCommandAsync(this StormDbCommand command, CancellationToken cancellationToken)
    {
        command.Log();

        try
        {
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (StormDbException ex)
        {
            if (StormManager.HandleDbException(ex) is { } handledException)
                throw handledException;
            throw;
        }
    }

    /// <summary>
    /// Sets up logging for a StormDbCommand by checking if logging is enabled at the Trace level, and then logging the command type, command text, and parameters.
    /// </summary>
    /// <param name="command">The StormDbCommand to set up logging for.</param>
    private static void Log(this StormDbCommand command)
    {
        var logger = StormManager.Logger;
        if (logger?.IsEnabled(LogLevel.Trace) != true)
            return;

        // Logging
        var sb = StormManager.GetStringBuilderFromPool();
        for (var i = 0; i < command.Parameters.Count; i++)
        {
            var p = command.Parameters[i];
            var sqlType = p.SqlDbType.ToSqlDbTypeText(p.Size, p.Precision, p.Scale);

            sb.Append("declare ").Append(p.ParameterName).Append(' ').Append(sqlType).Append(" = ")
                .Append(p.Value is string s ? ('\'' + s + '\'') : (p.Value == DBNull.Value ? "null" : p.Value)).Append(';').AppendLine();
        }

        logger.LogTrace("Storm sql query: (CommandType.{CommandType})\n{CommandParams}\n{CommandText}",
            command.CommandType, sb.ToStringAndReturnToPool(), command.CommandText);
    }
}

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
/// Extension class for <see cref="StormDbBatchCommand" />.
/// </summary>
internal static class DbBatchCommandExt
{
    /// <summary>
    /// Adds a database parameter to the given StormDbBatchCommand object.
    /// </summary>
    /// <param name="command">The DbCommand object to which the parameter will be added.</param>
    /// <param name="parameter">The StormCallParameter object containing information about the parameter.</param>
    /// <returns>
    /// The name of the added parameter.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string AddDbParameter(this StormDbBatchCommand command, StormCallParameter parameter)
    {
        var p = StormManager.AddDbBatchParameter(command, parameter.ParameterName, parameter.DbType, parameter.Size, parameter.Value, parameter.Direction);
        return p.ParameterName;
    }

    /// <summary>
    /// Adds a database parameter to the specified DbBatchCommand object.
    /// </summary>
    /// <param name="command">The DbBatchCommand object to add the parameter to.</param>
    /// <param name="paramIdx">The index of the parameter.</param>
    /// <param name="column">The StormColumnDef object representing the column.</param>
    /// <param name="value">The value to be assigned to the parameter. Can be null.</param>
    /// <returns>The name of the added parameter.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string AddDbParameter(this StormDbBatchCommand command, int paramIdx, StormColumnDef column, object? value)
    {
        var p = StormManager.AddDbBatchParameter(command, "@p" + paramIdx.ToString(System.Globalization.CultureInfo.InvariantCulture), column.DbType, column.Size, value);
        return p.ParameterName;
    }

    /// <summary>
    /// Generates call parameters for a DbCommand based on the provided list of StormCallParameter objects.
    /// </summary>
    internal static string? GenerateCallParameters(this StormDbBatchCommand command, List<StormCallParameter>? callParameters, CallParameterType type)
    {
        if (type == CallParameterType.StoreProcedure)
        {
            StormManager.AddDbBatchParameter(command, "ReturnValue", UnifiedDbType.Int32, 0, 0, ParameterDirection.ReturnValue);
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
    internal static void SetStormCommandBaseParameters(this StormDbBatchCommand command, StormContext context, string commandText, QueryParameters queryParameters, CommandType commandType = CommandType.Text)
    {
        command.CommandText = commandText;
        command.CommandType = commandType;
    }

    /// <summary>
    /// Executes a database command asynchronously and returns the number of rows affected.
    /// </summary>
    /// <param name="batch">The database batch to execute.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an integer representing the number of rows affected by the command execution.
    /// </returns>
    /// <exception cref="StormDbException">Thrown when there is an error executing the database command.</exception>
    internal static async Task<int> ExecuteCommandAsync(this StormDbBatch batch, CancellationToken cancellationToken)
    {
        batch.Log();
        try
        {
            return await batch.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (StormDbException ex)
        {
            if (StormManager.HandleDbException(ex) is { } handledException)
                throw handledException;
            throw;
        }
    }

    /// <summary>
    /// Sets up logging for a StormDbBatchCommand by checking if logging is enabled at the Trace level, and then logging the command type, command text, and parameters.
    /// </summary>
    /// <param name="batch">The Batch to set up logging for.</param>
    private static void Log(this StormDbBatch batch)
    {
        var logger = StormManager.Logger;
        if (logger?.IsEnabled(LogLevel.Trace) != true)
            return;

        // Logging
        logger.LogTrace("Storm sql batch start. {Count}", batch.Commands.Count);

        foreach (var command in batch.Commands)
        {
            var sb = StormManager.GetStringBuilderFromPool();

            for (var i = 0; i < command.Parameters.Count; i++)
            {
                var p = command.Parameters[i];
                var sqlType = p.SqlDbType.ToSqlDbTypeText(p.Size, p.Precision, p.Scale);

                sb.Append("declare ").Append(p.ParameterName).Append(' ').Append(sqlType).Append(" = ")
                    .Append(p.Value is string s ? ('\'' + s + '\'') : (p.Value == DBNull.Value ? "null" : p.Value)).Append(';').AppendLine();
            }

            logger.LogTrace("Storm batch sql query: (CommandType.{CommandType})\n{CommandParams}\n{CommandText}",
                command.CommandType, sb.ToStringAndReturnToPool(), command.CommandText);
        }

        logger.LogTrace("Storm sql batch end.");
    }
}

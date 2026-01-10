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
    /// Adds a database parameter to the given <see cref="StormDbCommand"/> using a <see cref="StormCallParameter"/> descriptor.
    /// </summary>
    /// <param name="command">The command to which the parameter will be added.</param>
    /// <param name="parameter">The parameter descriptor containing name, type, size, value and direction.</param>
    /// <returns>The name of the added parameter.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string AddDbParameter(this StormDbCommand command, StormCallParameter parameter)
    {
        var p = StormManager.AddDbParameter(command, parameter.ParameterName, parameter.DbType, parameter.Size, parameter.Value, parameter.Direction);
        return p.ParameterName;
    }

    /// <summary>
    /// Adds a database parameter to the given <see cref="StormDbCommand"/> for a column with a generated parameter name.
    /// </summary>
    /// <param name="command">The command to which the parameter will be added.</param>
    /// <param name="paramIdx">Index used to generate the parameter name (parameter name will be of the form <c>"@p{paramIdx}"</c>).</param>
    /// <param name="column">Column metadata describing database type and size.</param>
    /// <param name="value">The value to assign to the parameter. May be <c>null</c>.</param>
    /// <returns>The name of the added parameter.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string AddDbParameter(this StormDbCommand command, int paramIdx, StormColumnDef column, object? value)
    {
        var p = StormManager.AddDbParameter(command, "@p" + paramIdx.ToString(System.Globalization.CultureInfo.InvariantCulture), column.DbType, column.Size, value);
        return p.ParameterName;
    }

    /// <summary>
    /// Generates call parameters for a <see cref="StormDbCommand"/> from a list of <see cref="StormCallParameter"/> values.
    /// When <paramref name="type"/> is <see cref="CallParameterType.StoreProcedure"/>, a return value parameter is added.
    /// For <see cref="CallParameterType.CustomSqlStatement"/>, parameters are added to the command but no parameter string is returned.
    /// </summary>
    /// <param name="command">The command to which parameters will be added.</param>
    /// <param name="callParameters">List of call parameters to add. If <c>null</c>, no parameters are added and <c>null</c> is returned.</param>
    /// <param name="type">The type of call that determines how parameters are generated.</param>
    /// <returns>
    /// A comma-separated parameter list suitable for inclusion in a call statement, or <c>null</c> when <paramref name="callParameters"/> is <c>null</c> or when <paramref name="type"/> is <see cref="CallParameterType.CustomSqlStatement"/>.
    /// </returns>
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
    /// Sets base parameters on the specified <see cref="StormDbCommand"/>, including connection, transaction, command text, command type and optional timeout.
    /// </summary>
    /// <param name="command">The command to configure.</param>
    /// <param name="connection">The connection to assign to the command.</param>
    /// <param name="transaction">Optional transaction to assign to the command.</param>
    /// <param name="commandText">SQL text or procedure name to set on the command.</param>
    /// <param name="queryParameters">Query parameters that may contain a command timeout value.</param>
    /// <param name="commandType">The type of the command (Text or StoredProcedure). Defaults to <see cref="CommandType.Text"/>.</param>
    internal static void SetStormCommandBaseParameters(this StormDbCommand command, StormDbConnection connection, StormDbTransaction? transaction, string commandText, QueryParameters queryParameters, CommandType commandType = CommandType.Text)
    {
        command.Connection = connection;
        command.Transaction = transaction;

        command.CommandText = commandText;
        command.CommandType = commandType;

        if (queryParameters.CommandTimeout.HasValue)
            command.CommandTimeout = queryParameters.CommandTimeout.Value;
    }

    /// <summary>
    /// Sets base parameters on the specified <see cref="StormDbCommand"/>, including command text, command type and optional timeout.
    /// </summary>
    /// <param name="command">The command to configure.</param>
    /// <param name="commandText">SQL text or procedure name to set on the command.</param>
    /// <param name="queryParameters">Query parameters that may contain a command timeout value.</param>
    /// <param name="commandType">The type of the command (Text or StoredProcedure). Defaults to <see cref="CommandType.Text"/>.</param>
    internal static void SetStormCommandBaseParameters(this StormDbCommand command, string commandText, QueryParameters queryParameters, CommandType commandType = CommandType.Text)
    {
        command.CommandText = commandText;
        command.CommandType = commandType;

        if (queryParameters.CommandTimeout.HasValue)
            command.CommandTimeout = queryParameters.CommandTimeout.Value;
    }

    /// <summary>
    /// Sets the connection and optional transaction on the specified <see cref="StormDbCommand"/>.
    /// </summary>
    /// <param name="command">The command to configure.</param>
    /// <param name="connection">The connection to assign to the command.</param>
    /// <param name="transaction">Optional transaction to assign to the command.</param>
    internal static void SetStormCommandBaseParameters(this StormDbCommand command, StormDbConnection connection, StormDbTransaction? transaction)
    {
        command.Connection = connection;
        command.Transaction = transaction;
    }

    /// <summary>
    /// Executes a database command asynchronously and returns the number of rows affected.
    /// Any provider-specific <see cref="StormDbException"/> is handled by <see cref="StormManager.HandleDbException"/> and re-thrown if converted.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>Number of rows affected.</returns>
    /// <exception cref="StormDbException">If the provider throws and the exception is not converted to another exception by <see cref="StormManager.HandleDbException"/>.</exception>
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
    /// Executes a database command asynchronously and returns the number of rows affected and any exception that occurred.
    /// This method never throws; instead it returns a tuple where <c>ex</c> contains the exception if one occurred.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A tuple where <c>returnValue</c> is the number of rows affected (or -1 on error) and <c>ex</c> is the exception that occurred, if any.
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
    /// Asynchronously executes the command and returns a <see cref="StormDbDataReader"/> for reading the results.
    /// Any provider-specific <see cref="StormDbException"/> will be converted by <see cref="StormManager.HandleDbException"/> when applicable.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values that determines how the command is executed and how the reader behaves.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>A <see cref="StormDbDataReader"/> that can be used to read results.</returns>
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
    /// Executes a scalar command asynchronously and returns the first column of the first row in the result set.
    /// Any provider-specific <see cref="StormDbException"/> will be converted by <see cref="StormManager.HandleDbException"/> when applicable.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>The value of the first column of the first row in the result set.</returns>
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
    /// Logs the command and its parameters at <see cref="LogLevel.Trace"/>. No-op when the logger is not enabled for trace.
    /// </summary>
    /// <param name="command">The command whose details will be logged.</param>
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

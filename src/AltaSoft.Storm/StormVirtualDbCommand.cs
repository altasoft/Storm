using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;

namespace AltaSoft.Storm;

internal readonly struct StormVirtualDbCommand : IVirtualStormDbCommand
{
    private readonly StormDbCommand _command;

    /// <inheritdoc/>
    public string CommandText
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _command.CommandText;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _command.CommandText = value;
    }

    /// <inheritdoc/>
    public CommandType CommandType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _command.CommandType;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _command.CommandType = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string AddDbParameter(int paramIdx, StormColumnDef column, object? value) => _command.AddDbParameter(paramIdx, column, value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StormDbParameter AddDbParameter(string paramName, UnifiedDbType dbType, int size, object? value) =>
        StormManager.AddDbParameter(_command, paramName, dbType, size, value);

    /// <inheritdoc/>
    public void AddDbParameters(List<StormCallParameter> callParameters)
    {
        foreach (var p in callParameters)
        {
            _command.AddDbParameter(p);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStormCommandBaseParameters(StormDbConnection connection, StormDbTransaction? transaction, string sql, QueryParameters queryParameters, CommandType commandType = CommandType.Text)
    {
        _command.SetStormCommandBaseParameters(connection, transaction, sql, queryParameters, commandType);
    }

    /// <inheritdoc/>
    public void SetStormCommandBaseParameters(string sql, QueryParameters queryParameters, CommandType commandType = CommandType.Text)
    {
        _command.SetStormCommandBaseParameters(sql, queryParameters, commandType);
    }

    /// <inheritdoc/>
    public void SetStormCommandBaseParameters(StormDbConnection connection, StormDbTransaction? transaction)
    {
        _command.SetStormCommandBaseParameters(connection, transaction);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GenerateCallParameters(List<StormCallParameter>? queryParametersCallParameters, CallParameterType type) =>
        _command.GenerateCallParameters(queryParametersCallParameters, type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StormVirtualDbCommand(StormDbCommand command)
    {
        _command = command;
    }
}

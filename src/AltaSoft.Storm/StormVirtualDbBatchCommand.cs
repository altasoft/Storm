using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;

namespace AltaSoft.Storm;

internal sealed class StormVirtualDbBatchCommand : IVirtualStormDbCommand
{
    private readonly StormDbBatchCommand _command;

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
        StormManager.AddDbBatchParameter(_command, paramName, dbType, size, value);

    /// <inheritdoc/>
    public void AddDbParameters(List<StormCallParameter> callParameters)
    {
        foreach (var p in callParameters)
        {
            _command.AddDbParameter(p);
        }
    }

    public void SetStormCommandBaseParameters(StormContext context, string sql, QueryParameters queryParameters, CommandType commandType = CommandType.Text) =>
        _command.SetStormCommandBaseParameters(context, sql, queryParameters, commandType);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GenerateCallParameters(List<StormCallParameter>? queryParametersCallParameters, CallParameterType type) =>
        _command.GenerateCallParameters(queryParametersCallParameters, type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StormVirtualDbBatchCommand(StormDbBatchCommand command)
    {
        _command = command;
    }
}

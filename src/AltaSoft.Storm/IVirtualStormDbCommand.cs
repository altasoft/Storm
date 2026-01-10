using System.Collections.Generic;
using System.Data;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;

// ReSharper disable UnusedMember.Global

namespace AltaSoft.Storm;

/// <summary>
/// Contract for a lightweight/virtual Storm database command abstraction used by the library to build and execute
/// parameterized commands without depending on a concrete ADO.NET provider type.
/// </summary>
internal interface IVirtualStormDbCommand
{
    /// <summary>
    /// Gets or sets the command text to execute (SQL statement or stored procedure name).
    /// </summary>
    string CommandText { get; set; }

    /// <summary>
    /// Gets or sets how the <see cref="CommandText"/> is interpreted by the database provider.
    /// </summary>
    CommandType CommandType { get; set; }

    /// <summary>
    /// Adds a parameter to the command using column metadata. The parameter name is generated from the provided index.
    /// </summary>
    /// <param name="paramIdx">Index used to generate the parameter name (for example, <c>"@p0"</c>, <c>"@p1"</c>).</param>
    /// <param name="column">Column definition that provides the database type and size for the parameter.</param>
    /// <param name="value">Value to assign to the parameter. May be <c>null</c>.</param>
    /// <returns>The actual parameter name added to the command.</returns>
    string AddDbParameter(int paramIdx, StormColumnDef column, object? value);

    /// <summary>
    /// Adds a parameter to the command with the specified name, unified database type, size and value.
    /// </summary>
    /// <param name="paramName">The name of the parameter to add (including prefix, e.g. <c>"@p0"</c>).</param>
    /// <param name="dbType">Unified database type describing the underlying provider type.</param>
    /// <param name="size">Size of the parameter (used for string/binary types); provider-dependent.</param>
    /// <param name="value">The value to assign to the parameter. May be <c>null</c>.</param>
    /// <returns>The created <see cref="StormDbParameter"/> instance.</returns>
    StormDbParameter AddDbParameter(string paramName, UnifiedDbType dbType, int size, object? value);

    /// <summary>
    /// Adds multiple call parameters to the command.
    /// </summary>
    /// <param name="callParameters">List of <see cref="StormCallParameter"/> instances to add to the command. If empty, no parameters are added.</param>
    void AddDbParameters(List<StormCallParameter> callParameters);

    /// <summary>
    /// Configures the command with connection, optional transaction, command text, command type and optional timeout from <see cref="QueryParameters"/>
    /// </summary>
    /// <param name="connection">The connection to associate with the command.</param>
    /// <param name="transaction">Optional transaction to associate with the command.</param>
    /// <param name="sql">SQL text or stored procedure name to set on the command.</param>
    /// <param name="queryParameters">Query parameters that may contain additional settings such as command timeout.</param>
    /// <param name="commandType">Command type indicating whether <paramref name="sql"/> is text or stored procedure. Defaults to <see cref="CommandType.Text"/>.</param>
    void SetStormCommandBaseParameters(StormDbConnection connection, StormDbTransaction? transaction, string sql, QueryParameters queryParameters, CommandType commandType = CommandType.Text);

    /// <summary>
    /// Configures the command with command text, command type and optional timeout from <see cref="QueryParameters"/>
    /// </summary>
    /// <param name="sql">SQL text or stored procedure name to set on the command.</param>
    /// <param name="queryParameters">Query parameters that may contain additional settings such as command timeout.</param>
    /// <param name="commandType">Command type indicating whether <paramref name="sql"/> is text or stored procedure. Defaults to <see cref="CommandType.Text"/>.</param>
    void SetStormCommandBaseParameters(string sql, QueryParameters queryParameters, CommandType commandType = CommandType.Text);

    /// <summary>
    /// Configures the command with the specified connection and optional transaction.
    /// </summary>
    /// <param name="connection">The connection to associate with the command.</param>
    /// <param name="transaction">Optional transaction to associate with the command.</param>
    void SetStormCommandBaseParameters(StormDbConnection connection, StormDbTransaction? transaction);

    /// <summary>
    /// Generates and/or registers call parameters for the command based on the provided list and the parameterization <paramref name="type"/>.
    /// For custom SQL statements parameters are added to the command and <c>null</c> is returned; otherwise a comma-separated parameter list string is returned.
    /// </summary>
    /// <param name="queryParametersCallParameters">List of call parameters to add, or <c>null</c> if no parameters should be generated.</param>
    /// <param name="type">The call parameterization type which affects how parameters are generated and whether a parameter list string is returned.</param>
    /// <returns>
    /// A comma-separated list of parameter names suitable for inclusion in a call expression, or <c>null</c> if no list is applicable (for example when <paramref name="queryParametersCallParameters"/> is <c>null</c> or when <paramref name="type"/> is <see cref="CallParameterType.CustomSqlStatement"/>).
    /// </returns>
    string? GenerateCallParameters(List<StormCallParameter>? queryParametersCallParameters, CallParameterType type);
}

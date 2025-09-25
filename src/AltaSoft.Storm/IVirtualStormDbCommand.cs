using System.Collections.Generic;
using System.Data;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;

namespace AltaSoft.Storm;

/// <summary>
/// Defines the contract for a virtual Storm database command, supporting parameterized queries and command configuration.
/// </summary>
internal interface IVirtualStormDbCommand
{
    /// <summary>
    /// Gets or sets the text command to run against the database.
    /// </summary>
    string CommandText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating how the <see cref="CommandText"/> property is to be interpreted.
    /// </summary>
    CommandType CommandType { get; set; }

    /// <summary>
    /// Adds a database parameter using the specified column definition and value.
    /// </summary>
    /// <param name="paramIdx">The index of the parameter.</param>
    /// <param name="column">The column definition for the parameter.</param>
    /// <param name="value">The value to assign to the parameter.</param>
    /// <returns>The name of the added parameter.</returns>
    string AddDbParameter(int paramIdx, StormColumnDef column, object? value);

    /// <summary>
    /// Adds a database parameter with the specified name, type, size, and value.
    /// </summary>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="dbType">The unified database type of the parameter.</param>
    /// <param name="size">The size of the parameter.</param>
    /// <param name="value">The value to assign to the parameter.</param>
    /// <returns>The created <see cref="StormDbParameter"/> instance.</returns>
    StormDbParameter AddDbParameter(string paramName, UnifiedDbType dbType, int size, object? value);

    /// <summary>
    /// Adds a collection of call parameters to the command.
    /// </summary>
    /// <param name="callParameters">The list of <see cref="StormCallParameter"/> objects to add.</param>
    void AddDbParameters(List<StormCallParameter> callParameters);

    /// <summary>
    /// Sets the base parameters for the Storm command, including context, SQL, query parameters, and command type.
    /// </summary>
    /// <param name="context">The <see cref="StormContext"/> for the command.</param>
    /// <param name="sql">The SQL statement to execute.</param>
    /// <param name="queryParameters">The query parameters to use.</param>
    /// <param name="commandType">The type of the command. Defaults to <see cref="CommandType.Text"/>.</param>
    void SetStormCommandBaseParameters(StormContext context, string sql, QueryParameters queryParameters, CommandType commandType = CommandType.Text);

    /// <summary>
    /// Generates a string representation of the call parameters for the command.
    /// </summary>
    /// <param name="queryParametersCallParameters">The list of call parameters to include, or null.</param>
    /// <param name="type">Type of parameterization to use.</param>
    /// <returns>A string representing the call parameters, or null if not applicable.</returns>
    string? GenerateCallParameters(List<StormCallParameter>? queryParametersCallParameters, CallParameterType type);
}

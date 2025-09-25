using System.Collections.Generic;

namespace AltaSoft.Storm.Crud;

internal class ScalarParameters : QueryParameters
{
    /// <summary>
    /// Gets the name of the database schema associated with the controller.
    /// If null, a default schema may be used.
    /// </summary>
    public string? SchemaName { get; }

    /// <summary>
    /// Gets the name of the table in the database that this controller manages.
    /// </summary>
    public string ObjectName { get; }

    /// <summary>
    /// List of DbParameter objects used for making a call.
    /// Can be null if no parameters are provided.
    /// </summary>
    protected internal List<StormCallParameter> CallParameters { get; set; }

    /// <summary>
    /// Constructor for ScalarParameters class with a single parameter variant
    /// </summary>
    public ScalarParameters(StormContext context, List<StormCallParameter> callParameters, string? schemaName, string objectName) : base(context)
    {
        CallParameters = callParameters;
        SchemaName = schemaName;
        ObjectName = objectName;
    }
}

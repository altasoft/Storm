using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Represents a custom attribute used to mark a method as a Storm database procedure.
/// This attribute can be applied to methods to signify their association with a Storm database operation.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StormProcedureAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the Storm database procedure.
    /// The procedure name is optional and if not specified, the default naming convention is used.
    /// </summary>
    public string? ObjectName { get; set; }

    /// <summary>
    /// Gets or sets the database schema where the procedure is defined.
    /// Specifying a schema is optional. If not set, the default schema is used.
    /// </summary>
    public string? SchemaName { get; set; }
}

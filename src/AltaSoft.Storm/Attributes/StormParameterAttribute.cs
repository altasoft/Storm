using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Attribute class to define metadata for a database function and procedure parameter in Storm ORM.
/// This class can be used to decorate properties in template methods that represent database function or procedure,
/// providing additional information about how the .net parameter maps to a database parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class StormParameterAttribute : StormDbTypeMappingBaseAttribute
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// If not set, the field name is used as the parameter name.
    /// </summary>
    public string? ParameterName { get; set; }
}

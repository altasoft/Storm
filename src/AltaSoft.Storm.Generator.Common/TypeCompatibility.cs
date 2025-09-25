namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Enum representing the compatibility between two types.
/// </summary>
public enum TypeCompatibility
{
    /// <summary>
    /// Represents that two types are exactly compatible
    /// </summary>
    ExactlyCompatible,

    /// <summary>
    /// Represents that two types are partially compatible
    /// </summary>
    PartiallyCompatible,

    /// <summary>
    /// Represents that two types are not compatible
    /// </summary>
    NotCompatible
}

namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Represents the different kinds of classes in the system.
/// </summary>
public enum ClassKind
{
    /// <summary>
    /// Represents a general object class.
    /// </summary>
    Object,

    /// <summary>
    /// Represents a known type class.
    /// </summary>
    KnownType,

    /// <summary>
    /// Represents an enumeration class.
    /// </summary>
    Enum,

    /// <summary>
    /// Represents a domain primitive class.
    /// </summary>
    DomainPrimitive,

    /// <summary>
    /// Represents a SQL rowversion (timestamp) class.
    /// </summary>
    SqlRowVersion,

    /// <summary>
    /// Represents a SQL log sequence number class.
    /// </summary>
    SqlLogSequenceNumber,

    /// <summary>
    /// Represents a list class.
    /// </summary>
    List,

    /// <summary>
    /// Represents a dictionary class.
    /// </summary>
    Dictionary
}

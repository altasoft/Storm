using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Base class for mapping database column/parameter and column types in Storm.
/// </summary>
public abstract class StormDbTypeMappingBaseAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the database type of the database column/parameter.
    /// The default is <see cref="UnifiedDbType.Default"/>.
    /// </summary>
    public UnifiedDbType DbType { get; set; } = UnifiedDbType.Default;

    /// <summary>
    /// Gets or sets the size of the database column/parameter.
    /// This is typically used for specifying the size of string parameters.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the precision of the database column/parameter.
    /// This is typically used for decimal parameters.
    /// </summary>
    public int Precision { get; set; }

    /// <summary>
    /// Gets or sets the scale of the database column/parameter.
    /// This is typically used for decimal parameters to specify the number of digits to the right of the decimal point.
    /// </summary>
    public int Scale { get; set; }
}

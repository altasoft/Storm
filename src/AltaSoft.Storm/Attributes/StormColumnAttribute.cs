using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Attribute class to define metadata for a database column in Storm ORM.
/// This class can be used to decorate properties in classes that represent database entities,
/// providing additional information about how the property maps to a database column.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class StormColumnAttribute : StormDbTypeMappingBaseAttribute
{
    /// <summary>
    /// Gets or sets the name of the database column.
    /// If not set, the property name is used as the column name.
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets the save behavior of the column in the database.
    /// The default is <see cref="SaveAs.Default"/>.
    /// </summary>
    public SaveAs SaveAs { get; set; } = SaveAs.Default;

    /// <summary>
    /// Gets or sets a value indicating whether to load this column with flags.
    /// </summary>
    public bool LoadWithFlags { get; set; }

    /// <summary>
    /// Gets or sets the type of the column.
    /// The default is <see cref="ColumnType.Default"/>.
    /// </summary>
    public ColumnType ColumnType { get; set; } = ColumnType.Default;

    /// <summary>
    /// Gets or sets the name of the detail table if this property is a reference to another table.
    /// This is typically used for foreign key relationships.
    /// </summary>
    public string? DetailTableName { get; set; }
}

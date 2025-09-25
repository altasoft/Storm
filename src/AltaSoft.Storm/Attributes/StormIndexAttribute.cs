using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Specifies an index for a Storm Table.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class StormIndexAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the columns included in the index.
    /// </summary>
    public string[] Columns { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the index is unique.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Gets or sets the name of the index.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StormIndexAttribute"/> class.
    /// </summary>
    /// <param name="columns">The columns included in the index.</param>
    /// <param name="isUnique">A value indicating whether the index is unique.</param>
    /// <param name="indexName">The name of the index.</param>
    public StormIndexAttribute(string[] columns, bool isUnique, string? indexName = null)
    {
        Columns = columns;
        IsUnique = isUnique;
        IndexName = indexName;
    }
}

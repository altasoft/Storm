namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Specifies the type of save action being performed on a data entity.
/// </summary>
public enum SaveAction
{
    /// <summary>
    /// Represents an insert operation, where a new data entity is being added.
    /// </summary>
    Insert,

    /// <summary>
    /// Represents an update operation, where an existing data entity is being modified.
    /// </summary>
    Update,

    /// <summary>
    /// Represents a delete operation, where an existing data entity is being removed.
    /// </summary>
    Delete,

    /// <summary>
    /// Represents a merge operation, where an existing data entity is being merged.
    /// </summary>
    Merge
}

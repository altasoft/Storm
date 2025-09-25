namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Represents the update mode options for updating an entity.
/// This enum defines how an entity will be updated in the database.
/// </summary>
public enum UpdateMode
{
    /// <summary>
    /// Indicates that only changed fields of the entity should be updated.
    /// This mode tracks changes to the entity's properties and only updates the modified ones.
    /// </summary>
    ChangeTracking = 0,

    /// <summary>
    /// Indicates that the entire entity should be updated.
    /// This mode does not check for changes and updates all fields of the entity, regardless of whether they were modified or not.
    /// It is simpler but can be less efficient, especially for entities with many properties or large data fields.
    /// </summary>
    UpdateAll = 1,

    /// <summary>
    /// Indicates that update will not be supported
    /// </summary>
    NoUpdates = 99
}

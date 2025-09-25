namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Same as in AltaSoft.Storm
/// </summary>
public enum DupUpdateMode
{
    /// <summary>
    /// Indicates that only changed fields of the entity should be updated.
    /// This mode tracks changes to the entity's properties and only updates the modified ones.
    /// </summary>
    ChangeTracking = 0,

    /// <summary>
    /// Indicates that the entire entity should be updated.
    /// This mode does not check for changes and updates all fields.
    /// </summary>
    UpdateAll = 1,

    /// <summary>
    /// Indicates that update will not be supported
    /// </summary>
    NoUpdates = 99
}

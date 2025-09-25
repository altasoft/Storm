namespace AltaSoft.Storm;

/// <summary>
/// Represents the state of an object in terms of change tracking.
/// </summary>
internal enum ChangeTrackingState
{
    /// <summary>
    /// Indicates that the object has not been modified since it was retrieved or last saved.
    /// </summary>
    Unchanged,

    /// <summary>
    /// Indicates that the object has been modified since it was retrieved or last saved.
    /// </summary>
    Updated,

    /// <summary>
    /// Indicates that the object has been newly inserted and is yet to be saved.
    /// </summary>
    Inserted,

    /// <summary>
    /// Indicates that the object has been marked for deletion.
    /// </summary>
    Deleted
}

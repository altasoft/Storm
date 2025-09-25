namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Defines an interface for objects that support change tracking, used in ORM (Object-Relational Mapping) scenarios.
/// </summary>
public interface IChangeTrackable
{
    /// <summary>
    /// Determines whether change tracking is currently active.
    /// </summary>
    /// <returns>True if change tracking is active, otherwise false.</returns>
    bool IsChangeTrackingActive();

    /// <summary>
    /// Starts tracking changes on the object.
    /// </summary>
    void StartChangeTracking();

    /// <summary>
    /// Accepts the changes made to the object and optionally stops tracking further changes.
    /// </summary>
    /// <param name="stopTracking">If true, stops change tracking after accepting the changes. Default is true.</param>
    void AcceptChanges(bool stopTracking = true);

    /// <summary>
    /// Determines if the object has been modified since the last change tracking reset.
    /// </summary>
    /// <returns>True if the object is dirty (modified), otherwise false.</returns>
    bool IsDirty();
}

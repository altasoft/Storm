using System;
using System.Collections.Generic;
using AltaSoft.Storm.Interfaces;

#pragma warning disable IDE1006

namespace AltaSoft.Storm;

/// <summary>
/// Represents a state machine for change tracking.
/// </summary>
public sealed class ChangeTrackingStateMachine
{
    /// <summary>
    /// Represents an empty Set of strings.
    /// </summary>
    public static readonly IReadOnlySet<string> EmptyStringSet = new HashSet<string>(0, StringComparer.Ordinal);

    private HashSet<string>? _changedPropertyNames;
    private ChangeTrackingState _internalTrackState;

    private readonly ITrackingObject _trackedObject;
    private (string propertyName, IChangeTrackable? value)[]? _trackableProperties;

    /// <summary>
    /// Initializes a new instance of the ChangeTrackingStateMachine class.
    /// </summary>
    /// <param name="trackedObject">The object to be tracked for changes.</param>
    public ChangeTrackingStateMachine(ITrackingObject trackedObject)
    {
        _trackedObject = trackedObject;
    }

    /// <summary>
    /// Starts change tracking by accepting changes, initializing the list of changed property names, and recursively starting change tracking on all trackable objects.
    /// </summary>
    public void StartChangeTracking()
    {
        AcceptChanges(true);

        var trackableMembers = GetTrackableMembers();
        for (var i = 0; i < trackableMembers.Length; i++)
        {
            trackableMembers[i].value?.StartChangeTracking();
        }
    }

    /// <summary>
    /// Accepts changes made to the object and its trackable properties.
    /// </summary>
    /// <param name="stopTracking">Indicates whether to stop tracking changes after accepting them.</param>
    public void AcceptChanges(bool stopTracking)
    {
        var trackableMembers = GetTrackableMembers();
        for (var i = 0; i < trackableMembers.Length; i++)
        {
            trackableMembers[i].value?.AcceptChanges(stopTracking);
        }

        _internalTrackState = ChangeTrackingState.Unchanged;
        _changedPropertyNames = null;
    }

    /// <summary>
    /// Checks if the object or any of its trackable properties is dirty.
    /// </summary>
    /// <returns>
    /// Returns true if the object or any of its trackable properties is dirty, otherwise returns false.
    /// </returns>
    public bool IsDirty()
    {
        if (_internalTrackState != ChangeTrackingState.Unchanged)
            return true;

        var trackableMembers = GetTrackableMembers();
        return trackableMembers.Length != 0 && Array.Exists(trackableMembers, x => x.value?.IsDirty() == true);
    }

    /// <summary>
    /// Returns a IReadOnlySet of updated property names and their values.
    /// </summary>
    /// <returns>Set of strings or null</returns>
    public IReadOnlySet<string> __GetChangedPropertyNames()
    {
        var trackableMembers = GetTrackableMembers();

        var changed = _changedPropertyNames is not null ? new HashSet<string>(_changedPropertyNames, StringComparer.Ordinal) : new HashSet<string>(trackableMembers.Length, StringComparer.Ordinal);

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < trackableMembers.Length; i++)
        {
            var (propertyName, value) = trackableMembers[i];
            if (value is null)
                continue;

            if (value.IsDirty())
                changed.Add(propertyName);
        }
        return changed;
    }

    /// <summary>
    /// Retrieves the trackable properties and their current values from the tracked object.
    /// </summary>
    /// <returns>
    /// An array of tuples containing the property name and the corresponding trackable value.
    /// </returns>
    private (string propertyName, IChangeTrackable? value)[] GetTrackableMembers()
    {
        _trackableProperties ??= _trackedObject.__TrackableMembers();
        return _trackableProperties;
    }

    /// <summary>
    /// Notifies that a property has changed and updates the change tracking state.
    /// </summary>
    /// <param name="propertyName">The name of the property that has changed.</param>
    /// <param name="value">The new value of the property.</param>
    public void PropertyChanged(string propertyName, object? value)
    {
        _trackableProperties = null;

        if (_internalTrackState == ChangeTrackingState.Unchanged)
        {
            _internalTrackState = ChangeTrackingState.Updated;
        }

        _changedPropertyNames ??= new HashSet<string>(16, StringComparer.Ordinal);
        _changedPropertyNames.Add(propertyName);

        if (value is ITrackingObject itValue && !itValue.IsChangeTrackingActive())
        {
            itValue.StartChangeTracking();
        }
    }
}

using System.Collections.Generic;
using System.ComponentModel;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Defines an interface for objects that support change tracking, used in ORM (Object-Relational Mapping) scenarios.
/// </summary>
public interface ITrackingObject : IChangeTrackable
{
    /// <summary>
    /// <c>For Storm internal use only !!!</c>
    /// <br/>
    /// Gets a set of names of the properties that have been modified.
    /// </summary>
    /// <returns>A IReadOnlySet of property names that have been changed.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    IReadOnlySet<string> __GetChangedPropertyNames();

    /// <summary>
    /// <c>For Storm internal use only !!!</c>
    /// <br/>
    /// Returns an array of properties whose types implement the <see cref="ITrackingObject" /> interface.
    /// </summary>
    /// <returns>
    /// An array of tuples containing the property name and the corresponding value that implements the <see cref="ITrackingObject" /> interface.
    /// </returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    (string propertyName, IChangeTrackable? value)[] __TrackableMembers();
}

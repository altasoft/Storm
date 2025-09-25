using System;
using System.Collections.Generic;

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Represents a generic interface for a list that tracks changes to entities and implements the IEntityComparer interface.
/// </summary>
/// <typeparam name="T">The type of entities in the list.</typeparam>
public interface IEntityTrackingList<T> : ITrackingList<T>, IEntityTrackingList where T : IEquatable<T>, IEntityComparer<T>, IDataBindable;

/// <summary>
/// Represents an interface for a list that tracks changes to entities.
/// </summary>
public interface IEntityTrackingList : ITrackingList
{
    /// <summary>
    /// Retrieves a collection of items that have been inserted.
    /// </summary>
    /// <returns>An IEnumerable of the inserted items.</returns>
    IEnumerable<IDataBindable> GetInsertedEntities();

    /// <summary>
    /// Retrieves a collection of deleted items of type T.
    /// </summary>
    /// <returns>An IEnumerable collection of deleted items.</returns>
    IEnumerable<IDataBindable> GetDeletedEntities();

    /// <summary>
    /// Returns a collection of updated items.
    /// </summary>
    /// <returns>A collection of updated items.</returns>
    IEnumerable<IDataBindable> GetUpdatedEntities();
}

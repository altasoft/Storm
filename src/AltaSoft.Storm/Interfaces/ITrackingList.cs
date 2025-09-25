using System.Collections.Generic;

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Represents a tracking list that tracks changes made to its items.
/// </summary>
/// <seealso cref="IList{T}" />
/// <seealso cref="IChangeTrackable" />
public interface ITrackingList<T> : IList<T>, ITrackingList
{
    /// <summary>
    /// Retrieves a collection of items that have been inserted.
    /// </summary>
    /// <returns>An IEnumerable of the inserted items.</returns>
    IEnumerable<T> GetInsertedItems();

    /// <summary>
    /// Retrieves a collection of deleted items of type T.
    /// </summary>
    /// <returns>An IEnumerable collection of deleted items.</returns>
    IEnumerable<T> GetDeletedItems();

    /// <summary>
    /// Returns a collection of updated items.
    /// </summary>
    /// <returns>A collection of updated items.</returns>
    public IEnumerable<T> GetUpdatedItems();
}

/// <summary>
/// Represents a list that tracks changes made to its elements.
/// </summary>
public interface ITrackingList : IChangeTrackable
{
    /// <summary>
    /// Retrieves a collection of items that have been inserted.
    /// </summary>
    /// <returns>An IEnumerable of the inserted items.</returns>
    IEnumerable<object> GetInsertedObjects();

    /// <summary>
    /// Retrieves a collection of deleted items of type T.
    /// </summary>
    /// <returns>An IEnumerable collection of deleted items.</returns>
    IEnumerable<object> GetDeletedObjects();

    /// <summary>
    /// Returns a collection of updated items.
    /// </summary>
    /// <returns>A collection of updated items.</returns>
    public IEnumerable<object> GetUpdatedObjects();
};

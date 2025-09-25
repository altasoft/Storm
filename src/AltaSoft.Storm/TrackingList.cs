using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

/// <summary>
/// Represents a generic list that tracks changes made to its items.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
/// <seealso cref="IList{T}"/>
/// <seealso cref="ITrackingObject"/>
public class TrackingList<T> : ITrackingList<T> where T : IEquatable<T>
{
    /// <summary>
    /// Is type T implementing ITrackingObject?
    /// </summary>
    protected static readonly bool IsTiTrackingObject = typeof(ITrackingObject).IsAssignableFrom(typeof(T));

    /// <summary>
    /// The internal list that holds the items.
    /// </summary>
    protected readonly List<T> InternalList;

    /// <summary>
    /// The set of items that have been inserted into the list.
    /// </summary>
    protected readonly HashSet<T> InsertedItems = new(16);

    /// <summary>
    /// The set of items that have been deleted from the list.
    /// </summary>
    protected readonly HashSet<T> DeletedItems = new(16);

    /// <summary>
    /// Indicates whether change tracking is active for the list.
    /// </summary>
    protected bool TrackChanges;

    #region Constructors and implicit operators

    /// <summary>
    /// Initializes a new instance of the TrackingList class.
    /// This constructor creates an empty TrackingList. It provides the option to start change tracking immediately upon creation.
    /// </summary>
    /// <param name="autoTrack">A boolean value indicating whether to automatically start change tracking.
    /// If set to true, change tracking is activated from the outset, recording any additions, deletions, or modifications to the list items from the moment the list is created.
    /// If false, the list will not track changes until StartChangeTracking is explicitly called.</param>
    /// <remarks>
    /// The change tracking mechanism is crucial for monitoring modifications to the list after its instantiation.
    /// Enabling autoTrack is useful when you need to ensure that all changes from the point of creation are recorded.
    /// </remarks>
    public TrackingList(bool autoTrack = false)
    {
        InternalList = new List<T>();
        if (autoTrack)
        {
            StartChangeTracking();
        }
    }

    /// <summary>
    /// Initializes a new instance of the TrackingList class with the specified collection and autoTrack option.
    /// This constructor creates a new TrackingList from an existing collection. If the provided collection is already a <see cref="List{T}"/>
    /// it is used directly; otherwise, a new <see cref="List{T}"/> is created from the elements of the provided collection.
    /// </summary>
    /// <param name="collection">The collection to initialize the TrackingList with. If this collection is a <see cref="List{T}"/>
    /// the TrackingList will use the same underlying list (shallow copy). If it is any other <see cref="IEnumerable{T}"/>, a new <see cref="List{T}"/> is created from its elements.</param>
    /// <param name="autoTrack">A boolean value indicating whether to automatically start change tracking.
    /// If set to true, change tracking starts immediately, keeping track of modifications to the list after the TrackingList is created.</param>
    /// <remarks>
    /// The autoTrack parameter controls whether changes are tracked from the moment the list is created. If false,
    /// changes made to the list after creation won't be tracked until StartChangeTracking is called.
    /// </remarks>
    public TrackingList(IEnumerable<T> collection, bool autoTrack = false)
    {
        InternalList = collection as List<T> ?? new List<T>(collection);
        if (autoTrack)
        {
            StartChangeTracking();
        }
    }

    /// <summary>
    /// Implicitly converts a nullable <see cref="List{T}" /> to a nullable <see cref="TrackingList{T}"/>.
    /// This operator creates a new <see cref="TrackingList{T}"/> instance containing a shallow copy of the items from the input <see cref="List{T}"/>.
    /// A shallow copy means that the elements themselves are not cloned; the new list will contain references to the same elements as the original list.
    /// If the input collection is null, the conversion returns null. Otherwise, it returns a new <see cref="TrackingList{T}"/> instance with its elements copied from the input list.
    /// Note: The change tracking status for the new <see cref="TrackingList{T}"/> will be reset, and will need to be started explicitly if desired.
    /// </summary>
    /// <param name="collection">The nullable <see cref="List{T}"/> to convert.</param>
    /// <returns>A nullable <see cref="TrackingList{T}"/> instance or null. If not null, the returned <see cref="TrackingList{T}"/> contains a shallow copy of the elements from the input <see cref="List{T}"/>.</returns>
    [return: NotNullIfNotNull(nameof(collection))]
    public static implicit operator TrackingList<T>?(List<T>? collection) => collection != null ? new TrackingList<T>(collection) : null;

    /// <summary>
    /// Implicitly converts a <see cref="TrackingList{T}"/> to <see cref="List{T}"/>.
    /// This conversion returns the underlying <see cref="List{T}"/> from the <see cref="TrackingList{T}"/>.
    /// Note: The returned <see cref="List{T}"/> is a reference to the internal list managed by the <see cref="TrackingList{T}"/>.
    /// Any modifications made to this list will directly affect the contents of the <see cref="TrackingList{T}"/>.
    /// If the input <see cref="TrackingList{T}"/> is null, the conversion returns null.
    /// </summary>
    /// <param name="collection">The <see cref="TrackingList{T}"/> to convert.</param>
    /// <returns>The underlying <see cref="List{T}"/> from the <see cref="TrackingList{T}"/>, or null if the input collection is null.
    /// Modifications to this list will reflect in the <see cref="TrackingList{T}"/>.</returns>
    [return: NotNullIfNotNull(nameof(collection))]
    public static implicit operator List<T>?(TrackingList<T>? collection) => collection?.InternalList;

    #endregion Constructors and implicit operators

    #region Custom IList<T> methods

    /// <inheritdoc />
    public virtual void Add(T item)
    {
        InternalList.Add(item);
        if (!TrackChanges)
            return;

        OnAdded(item);
    }

    /// <inheritdoc />
    public virtual void Insert(int index, T item)
    {
        InternalList.Insert(index, item);
        if (!TrackChanges)
            return;

        OnAdded(item);
    }

    /// <inheritdoc />
    public virtual T this[int index]
    {
        get => InternalList[index];
        set
        {
            var oldValue = InternalList[index];

            if (oldValue.Equals(value)) // If the new value is the same as the old value, return
                return;

            InternalList[index] = value; // Set the new value

            if (!TrackChanges)
                return;

            OnRemoved(oldValue);
            OnAdded(value);
        }
    }

    /// <inheritdoc />
    public virtual bool Remove(T item)
    {
        var removed = InternalList.Remove(item);
        if (!removed || !TrackChanges)
            return removed;

        OnRemoved(item);

        return removed;
    }

    /// <inheritdoc />
    public virtual void RemoveAt(int index)
    {
        var item = InternalList[index];

        InternalList.RemoveAt(index);
        if (!TrackChanges)
            return;

        OnRemoved(item);
    }

    /// <inheritdoc />
    public virtual void Clear()
    {
        if (TrackChanges)
        {
            foreach (var item in InternalList)
            {
                // If the item was added and then cleared, we remove it from the inserted list
                if (!InsertedItems.Remove(item))
                {
                    // if the item was not added in the same session, then add it to the deleted items
                    DeletedItems.Add(item);
                }
            }
        }

        InternalList.Clear();
    }

    #endregion Custom IList<T> methods

    #region IList<T> => InternalList

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => InternalList.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Contains(T item) => InternalList.Contains(item);

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex) => InternalList.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public int Count => InternalList.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public int IndexOf(T item) => InternalList.IndexOf(item);

    #endregion IList<T> => InternalList

    #region IChangeTrackable

    /// <inheritdoc />
    public bool IsChangeTrackingActive() => TrackChanges;

    /// <inheritdoc />
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public void StartChangeTracking()
    {
        AcceptChanges();

        if (IsTiTrackingObject)
        {
            InternalList.ForEach(x => ((ITrackingObject)x).StartChangeTracking());
        }

        TrackChanges = true;
    }

    /// <inheritdoc />
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public void AcceptChanges(bool stopTracking = true)
    {
        InsertedItems.Clear();
        DeletedItems.Clear();

        if (IsTiTrackingObject)
        {
            InternalList.ForEach(x => ((ITrackingObject)x).AcceptChanges());
        }

        TrackChanges = !stopTracking;
    }

    /// <summary>
    /// Determines if the object has been modified since the last change tracking reset.
    /// </summary>
    /// <returns>True if the object is dirty (modified), otherwise false.</returns>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public bool IsDirty()
    {
        var isDirty = InsertedItems.Count > 0 || DeletedItems.Count > 0;
        if (isDirty || !IsTiTrackingObject)
            return isDirty;

        return InternalList.Exists(x => ((ITrackingObject)x).IsDirty());
    }

    #endregion IChangeTrackable

    #region ITrackingList<T>

    /// <inheritdoc />
    public IEnumerable<T> GetInsertedItems() => InsertedItems;

    /// <inheritdoc />
    public IEnumerable<T> GetDeletedItems() => DeletedItems;

    /// <inheritdoc />
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public virtual IEnumerable<T> GetUpdatedItems() => IsTiTrackingObject ? InternalList.Where(x => ((ITrackingObject)x).IsDirty()) : Enumerable.Empty<T>();

    #endregion ITrackingList<T>

    #region ITrackingList

    /// <inheritdoc />
    public IEnumerable<object> GetInsertedObjects() => GetInsertedItems().Cast<object>();

    /// <inheritdoc />
    public IEnumerable<object> GetDeletedObjects() => GetDeletedItems().Cast<object>();

    /// <inheritdoc />
    public IEnumerable<object> GetUpdatedObjects() => GetUpdatedItems().Cast<object>();

    #endregion ITrackingList

    #region Helper methods

    /// <summary>
    /// Starts change tracking for the given object if it is a tracking object and change tracking is not already active.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    protected void StartTacking(T item)
    {
        if (!IsTiTrackingObject || IsChangeTrackingActive())
            return;

        ((ITrackingObject)item).StartChangeTracking();
    }

    //protected void StopTracking(T item)
    //{
    //    if (!s_isItemChangeTrackable || !IsChangeTrackingActive())
    //        return;

    //    ((ITrackingObject)item).AcceptChanges();
    //}

    /// <summary>
    /// Adds the item to the collection and starts tracking it. If the item was previously marked as deleted, it removes it from the deleted items and adds it to the inserted items.
    /// </summary>
    /// <param name="item">The item to be added and tracked</param>
    private void OnAdded(T item)
    {
        StartTacking(item);

        // If item was deleted before, remove from deleted items and do not add to InsertedItems
        if (DeletedItems.Remove(item))
            return;

        InsertedItems.Add(item);
    }

    /// <summary>
    /// Handles the removal of an item.
    /// If the item was added in the same session, it is removed from the inserted items.
    /// If the item was not added in the same session, it is added to the deleted items.
    /// </summary>
    /// <param name="item">The item to be removed</param>
    private void OnRemoved(T item)
    {
        // If item was added in same session, then remove from inserted items
        if (!InsertedItems.Remove(item))
        {
            // If item was not added in same session, then add to deleted items
            DeletedItems.Add(item);
        }
    }

    #endregion Helper methods
}

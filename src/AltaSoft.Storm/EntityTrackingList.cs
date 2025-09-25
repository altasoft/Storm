using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

/// <summary>
/// Represents a generic list that tracks changes made to its items.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
/// <seealso cref="IList{T}"/>
/// <seealso cref="ITrackingObject"/>
public class EntityTrackingList<T> : TrackingList<T>, IEntityTrackingList<T> where T : IEquatable<T>, IEntityComparer<T>, IDataBindable
{
    /// <summary>
    /// A read-only HashSet that stores items of type T and is initialized with a capacity of 16.
    /// </summary>
    protected readonly HashSet<T> UpdatedItems = new(16);

    #region Constructors and implicit operators

    /// <inheritdoc/>
    public EntityTrackingList(bool autoTrack = false) : base(autoTrack)
    {
    }

    /// <inheritdoc/>
    public EntityTrackingList(IEnumerable<T> collection, bool autoTrack = false) : base(collection, autoTrack)
    {
    }

    /// <summary>
    /// Implicitly converts a nullable <see cref="List{T}" /> to a nullable <see cref="EntityTrackingList{T}"/>.
    /// This operator creates a new <see cref="EntityTrackingList{T}"/> instance containing a shallow copy of the items from the input <see cref="List{T}"/>.
    /// A shallow copy means that the elements themselves are not cloned; the new list will contain references to the same elements as the original list.
    /// If the input collection is null, the conversion returns null. Otherwise, it returns a new <see cref="EntityTrackingList{T}"/> instance with its elements copied from the input list.
    /// Note: The change tracking status for the new <see cref="EntityTrackingList{T}"/> will be reset, and will need to be started explicitly if desired.
    /// </summary>
    /// <param name="collection">The nullable <see cref="List{T}"/> to convert.</param>
    /// <returns>A nullable <see cref="EntityTrackingList{T}"/> instance or null. If not null, the returned <see cref="EntityTrackingList{T}"/> contains a shallow copy of the elements from the input <see cref="List{T}"/>.</returns>
    [return: NotNullIfNotNull(nameof(collection))]
    public static implicit operator EntityTrackingList<T>?(List<T>? collection) => collection != null ? new EntityTrackingList<T>(collection) : null;

    /// <summary>
    /// Implicitly converts a <see cref="EntityTrackingList{T}"/> to <see cref="List{T}"/>.
    /// This conversion returns the underlying <see cref="List{T}"/> from the <see cref="EntityTrackingList{T}"/>.
    /// Note: The returned <see cref="List{T}"/> is a reference to the internal list managed by the <see cref="EntityTrackingList{T}"/>.
    /// Any modifications made to this list will directly affect the contents of the <see cref="EntityTrackingList{T}"/>.
    /// If the input <see cref="EntityTrackingList{T}"/> is null, the conversion returns null.
    /// </summary>
    /// <param name="collection">The <see cref="EntityTrackingList{T}"/> to convert.</param>
    /// <returns>The underlying <see cref="List{T}"/> from the <see cref="EntityTrackingList{T}"/>, or null if the input collection is null.
    /// Modifications to this list will reflect in the <see cref="EntityTrackingList{T}"/>.</returns>
    [return: NotNullIfNotNull(nameof(collection))]
    public static implicit operator List<T>?(EntityTrackingList<T>? collection) => collection?.InternalList;

    #endregion Constructors and implicit operators

    #region Custom IList<T> methods

    /// <inheritdoc />
    public override void Add(T item)
    {
        if (ContainsKey(item))
        {
            throw new InvalidOperationException("An item with the same key already exists.");
        }

        InternalList.Add(item);
        if (!TrackChanges)
            return;

        OnAdded(item);
    }

    /// <summary>
    /// Inserts an element into the <see cref="EntityTrackingList{T}"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the element should be inserted.</param>
    /// <param name="item">The element to insert.</param>
    /// <exception cref="InvalidOperationException">Thrown when an item with the same key already exists.</exception>
    public override void Insert(int index, T item)
    {
        if (ContainsKey(item))
        {
            throw new InvalidOperationException("An item with the same key already exists.");
        }

        InternalList.Insert(index, item);
        if (!TrackChanges)
            return;

        OnAdded(item);
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    public override T this[int index]
    {
        get => base[index];
        set
        {
            var oldValue = base[index];

            if (oldValue.Equals(value)) // If the new value is the same as the old value, return
                return;

            if (!TrackChanges) // If change tracking is disabled, just set the new value and return
            {
                base[index] = value;
                return;
            }

            if (oldValue.KeyEquals(value))
            {
                InternalList[index] = value; // Set the new value

                OnUpdated(oldValue, value);
            }
            else
            {
                if (ContainsKey(value))
                {
                    throw new InvalidOperationException("An item with the same key already exists.");
                }

                Remove(base[index]);
                base.Insert(index, value);
            }
        }
    }

    /// <inheritdoc />
    public override bool Remove(T item)
    {
        var removed = InternalList.Remove(item);
        if (!removed || !TrackChanges)
            return removed;

        OnRemoved(item);

        return removed;
    }

    /// <inheritdoc />
    public override void RemoveAt(int index)
    {
        var item = InternalList[index];

        InternalList.RemoveAt(index);
        if (!TrackChanges)
            return;

        OnRemoved(item);
    }

    #endregion Custom IList<T> methods

    #region ITrackingList<T>

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override IEnumerable<T> GetUpdatedItems() => IsTiTrackingObject ? UpdatedItems.Concat(base.GetUpdatedItems()) : UpdatedItems;

    #endregion ITrackingList<T>

    #region IEntityTrackingList

    /// <inheritdoc />
    public IEnumerable<IDataBindable> GetInsertedEntities() => GetInsertedItems().Cast<IDataBindable>();

    /// <inheritdoc />
    public IEnumerable<IDataBindable> GetDeletedEntities() => GetDeletedItems().Cast<IDataBindable>();

    /// <inheritdoc />
    public IEnumerable<IDataBindable> GetUpdatedEntities() => GetUpdatedItems().Cast<IDataBindable>();

    #endregion IEntityTrackingList

    #region Helper methods

    /// <summary>
    /// Checks if the collection contains a key that is equal to the specified item.
    /// </summary>
    /// <param name="item">The item to check for.</param>
    /// <returns>True if a key equal to the item is found, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsKey(T item) => this.Any(x => x.KeyEquals(item));

    private void OnAdded(T item)
    {
        StartTacking(item);

        // ContainsKey(item) returned false, so the item is neither in InternalList nor in InsertedItems nor in UpdatedItems

        if (DeletedItems.Remove(item)) // If item was deleted before, just remove from deleted items
        {
            return;
        }

        // Check for deleted item with IEntityComparer.KeyEquals
        var deleted = DeletedItems.FirstOrDefault(x => x.KeyEquals(item));
        if (deleted is not null) // If item with the same key was deleted before, it means that entity was updated, so remove it from the deleted items and add it to the updated items
        {
            DeletedItems.Remove(deleted);

            UpdatedItems.Add(item);
        }
        else
        {
            // If the item was not in the deleted items, it means that it is a new item
            InsertedItems.Add(item);
        }
    }

    private void OnRemoved(T item)
    {
        // If item was added in same session, then remove from inserted items
        if (InsertedItems.Remove(item))
            return;

        if (UpdatedItems.Remove(item))
            return;

        // If item was not added in same session, then add to deleted items
        DeletedItems.Add(item);
    }

    private void OnUpdated(T oldValue, T newValue)
    {
        if (InsertedItems.Remove(oldValue)) // If item was added in same session, remove old value and add new value
        {
            InsertedItems.Add(newValue);
            return;
        }

        UpdatedItems.Add(newValue);
    }

    #endregion Helper methods
}

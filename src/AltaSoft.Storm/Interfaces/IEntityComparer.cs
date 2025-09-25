using System;

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Provides a mechanism to compare two entities of type <typeparamref name="T"/> based on their entity keys.
/// </summary>
/// <typeparam name="T">The type of entities to be compared. This type must implement <see cref="IEquatable{T}"/>.</typeparam>
public interface IEntityComparer<in T> where T : IEquatable<T>
{
    /// <summary>
    /// Determines whether the key of the current object is equal to the key of another object of the same type.
    /// </summary>
    /// <remarks>
    /// This method is primarily used for comparing entities to ascertain if they represent the same record or data item.
    /// The comparison is based on the entity's key, which could be composed of one or several properties that uniquely identify an entity.
    /// Implementations of this method should ensure that the comparison is efficient and conforms to the requirements of the entity's key structure.
    /// </remarks>
    /// <param name="other">An object of type <typeparamref name="T"/> to compare with the current object.</param>
    /// <returns>
    /// <see langword="true"/> if the key of the current object is equal to the key of the <paramref name="other"/> object;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool KeyEquals(T other);
}

using System.Collections.Generic;
using System.Linq;
using AltaSoft.DomainPrimitives;

namespace AltaSoft.Storm;

/// <summary>
/// Provides extension methods for building SQL-like predicates in expression trees.
/// These helpers are used by the SQL WHERE generator to translate calls such as
/// <c>x => x.Prop.In(collection)</c> into SQL <c>IN</c> clauses.
/// </summary>
public static class SqlWhereExt
{
    /// <summary>
    /// Returns true if the specified value is contained in the provided collections.
    /// This overload is intended for reference types and nullable domain primitives when called
    /// from expression trees.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="self">The value to locate in the collections.</param>
    /// <param name="values">One or more collections to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is found in any of the <paramref name="values"/> collections; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, params IEnumerable<T?> values) => values.Contains(self);

    /// <summary>
    /// Determines whether the domain primitive string representation is contained in the provided string values.
    /// This is a convenience overload that calls <c>ToString()</c> on the domain primitive when it's not null.
    /// </summary>
    /// <typeparam name="T">Domain primitive type implementing <see cref="IDomainValue{TUnderlying}"/> for <see cref="string"/>.</typeparam>
    /// <param name="self">The domain primitive value to check.</param>
    /// <param name="values">The collection of string values to search.</param>
    /// <returns><c>true</c> if the textual representation of <paramref name="self"/> is found in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, params IEnumerable<string?> values) where T : IDomainValue<string> => values.Contains(self?.ToString());

    /// <summary>
    /// Returns true if the specified non-nullable value is contained in the provided collection.
    /// Intended for value types such as numbers and enums.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="self">The value to locate.</param>
    /// <param name="values">The collection to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T self, params IEnumerable<T?> values) where T : struct => values.Contains(self);

    /// <summary>
    /// Returns true if the nullable value is contained in the provided collections.
    /// Intended for usage with nullable value types where collection items are nullable.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="self">The nullable value to locate.</param>
    /// <param name="values">One or more collections to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in any of the <paramref name="values"/> collections; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, params IEnumerable<T?> values) where T : struct => values.Contains(self);

    /// <summary>
    /// Returns true if the nullable value has a value and that value is contained in the provided collection.
    /// This overload is intended for nullable value types where the collection contains non-nullable items.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="self">The nullable value to check.</param>
    /// <param name="values">The collection of non-nullable items to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> has a value and that value is present in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, params IEnumerable<T> values) where T : struct => self is not null && values.Contains(self.Value);
}

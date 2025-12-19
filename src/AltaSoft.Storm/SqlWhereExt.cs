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
    /// Returns true if the specified reference-type value is contained in the provided collections.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="self">The value to locate in the collections.</param>
    /// <param name="values">One or more collections to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is found in any of the <paramref name="values"/> collections; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<T?> values) => values.Contains(self);

    /// <summary>
    /// Returns true if the specified reference-type value is contained in the provided array of values.
    /// Convenience overload for call-sites that pass an array.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="self">The value to locate.</param>
    /// <param name="values">Array of values to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> exists in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, params T?[] values) => values.Contains(self);

    /// <summary>
    /// Determines whether the textual representation of a domain primitive is contained in the provided string values.
    /// Calls <see cref="object.ToString"/> on the domain primitive when non-null.
    /// </summary>
    /// <typeparam name="T">Domain primitive type implementing <see cref="IDomainValue{TUnderlying}"/> for <see cref="string"/>.</typeparam>
    /// <param name="self">The domain primitive value to check.</param>
    /// <param name="values">The collection of string values to search.</param>
    /// <returns><c>true</c> if the textual representation of <paramref name="self"/> is found in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<string?> values) where T : IDomainValue<string> => values.Contains(self?.ToString());

    /// <summary>
    /// Determines whether the textual representation of a domain primitive is contained in the provided string array.
    /// Convenience overload for call-sites that pass an array.
    /// </summary>
    /// <typeparam name="T">Domain primitive type implementing <see cref="IDomainValue{TUnderlying}"/> for <see cref="string"/>.</typeparam>
    /// <param name="self">The domain primitive value to check.</param>
    /// <param name="values">Array of string values to search.</param>
    /// <returns><c>true</c> if the textual representation of <paramref name="self"/> is found in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, params string?[] values) where T : IDomainValue<string> => values.Contains(self?.ToString());

    /// <summary>
    /// Returns true if the specified non-nullable value type is contained in the provided collection.
    /// Intended for value types such as numbers and enums.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="self">The value to locate.</param>
    /// <param name="values">The collection to search (items may be nullable).</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T self, IEnumerable<T?> values) where T : struct => values.Contains(self);

    /// <summary>
    /// Returns true if the specified non-nullable value type is contained in the provided array of (nullable) values.
    /// Convenience overload for call-sites that pass an array.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="self">The value to locate.</param>
    /// <param name="values">Array of nullable values to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T self, params T?[] values) where T : struct => values.Contains(self);

    /// <summary>
    /// Returns true if the nullable value is contained in the provided collection of nullable items.
    /// Intended for nullable value types where collection items are nullable as well.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="self">The nullable value to locate.</param>
    /// <param name="values">Collection of nullable items to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<T?> values) where T : struct => values.Contains(self);

    /// <summary>
    /// Returns true if the nullable value is contained in the provided array of nullable items.
    /// Convenience overload for call-sites that pass an array.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="self">The nullable value to locate.</param>
    /// <param name="values">Array of nullable values to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, params T?[] values) where T : struct => values.Contains(self);

    /// <summary>
    /// Returns true if the nullable value has a value and that value is contained in the provided collection.
    /// This overload is intended for nullable value types where the collection contains non-nullable items.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="self">The nullable value to check.</param>
    /// <param name="values">The collection of non-nullable items to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> has a value and that value is present in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<T> values) where T : struct => self is not null && values.Contains(self.Value);

    /// <summary>
    /// Returns true if the nullable value has a value and that value is contained in the provided array.
    /// Convenience overload for call-sites that pass an array.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="self">The nullable value to check.</param>
    /// <param name="values">Array of non-nullable items to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> has a value and that value is present in <paramref name="values"/>; otherwise, <c>false</c>.</returns>
    public static bool In<T>(this T? self, params T[] values) where T : struct => self is not null && values.Contains(self.Value);
}

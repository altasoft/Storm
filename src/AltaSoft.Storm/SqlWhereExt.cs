using System.Collections.Generic;
using System.Linq;
using AltaSoft.DomainPrimitives;

namespace AltaSoft.Storm;

/// <summary>
/// Provides extension helpers used by the SQL WHERE generator to translate
/// expressions like <c>x => x.Prop.In(collection)</c> into SQL <c>IN</c> clauses.
/// </summary>
public static class SqlWhereExt
{
    /// <summary>
    /// Returns true if the specified reference-type value is contained in the provided collection.
    /// </summary>
    /// <typeparam name="T">Element type of the collection.</typeparam>
    /// <param name="self">Value to locate in the collection.</param>
    /// <param name="values">Collection to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<T?> values) => values.Contains(self);

    /// <summary>
    /// Convenience overload: checks presence of a reference-type value in an array.
    /// </summary>
    /// <typeparam name="T">Element type of the array.</typeparam>
    /// <param name="self">Value to locate in the array.</param>
    /// <param name="values">Array to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, params T?[] values) => values.Contains(self);

    /// <summary>
    /// Returns true if the specified reference-type value is contained in the provided string collection.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <param name="self">Value to locate in the collection.</param>
    /// <param name="values">Collection to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<string?> values) => values.Contains(self?.ToString());

    /// <summary>
    /// Convenience overload: checks presence of a reference-type value in a string array.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <param name="self">Value to locate in the array.</param>
    /// <param name="values">Array to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, params string?[] values) => values.Contains(self?.ToString());

    /// <summary>
    /// Returns true if the specified non-nullable value type is contained in the provided collection.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="self">Value to locate.</param>
    /// <param name="values">Collection to search (items may be nullable).</param>
    /// <returns><c>true</c> when <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T self, IEnumerable<T?> values) where T : struct => values.Contains(self);

    /// <summary>
    /// Convenience overload: checks presence of a non-nullable value in an array of nullable items.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="self">Value to locate.</param>
    /// <param name="values">Array of nullable items to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T self, params T?[] values) where T : struct => values.Contains(self);

    /// <summary>
    /// Returns true if the nullable value is contained in the provided collection of nullable items.
    /// </summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="self">Nullable value to locate.</param>
    /// <param name="values">Collection of nullable items to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<T?> values) where T : struct => values.Contains(self);

    /// <summary>
    /// Convenience overload: checks presence of a nullable value in an array of nullable items.
    /// </summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="self">Nullable value to locate.</param>
    /// <param name="values">Array of nullable items to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, params T?[] values) where T : struct => values.Contains(self);

    /// <summary>
    /// Returns true if the nullable value has a value and that value is contained in the provided collection
    /// of non-nullable items.
    /// </summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="self">Nullable value to check.</param>
    /// <param name="values">Collection of non-nullable items to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> has a value and it is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<T> values) where T : struct => self is not null && values.Contains(self.Value);

    /// <summary>
    /// Convenience overload: checks presence of a nullable value in an array of non-nullable items.
    /// </summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="self">Nullable value to check.</param>
    /// <param name="values">Array of non-nullable items to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/> has a value and it is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, params T[] values) where T : struct => self is not null && values.Contains(self.Value);
}

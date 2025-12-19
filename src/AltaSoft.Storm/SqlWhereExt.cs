using System.Collections.Generic;
using System.Linq;

namespace AltaSoft.Storm;

/// <summary>
/// Provides extension helpers used by the SQL WHERE generator.
/// These helpers allow expression trees to use a readable <c>In</c> method
/// which the SQL generator recognizes and translates into SQL <c>IN</c> clauses.
/// <para>
/// Example usage: <c>x => x.Prop.In(1, 2, 3)</c>
/// </para>
/// </summary>
public static class SqlWhereExt
{
    /// <summary>
    /// Determines whether the specified value is contained in the supplied collection.
    /// This overload is intended for use from expression trees.
    /// </summary>
    /// <typeparam name="T">Type of elements in <paramref name="values"/>.</typeparam>
    /// <param name="self">Value to locate in the collection.</param>
    /// <param name="values">Collection to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is found in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<T?> values) => values.Contains(self);

    /// <summary>
    /// Determines whether the specified value is contained in the supplied array.
    /// Convenience overload for call-sites that pass an array literal.
    /// </summary>
    /// <typeparam name="T">Element type of <paramref name="values"/>.</typeparam>
    /// <param name="self">Value to locate in the array.</param>
    /// <param name="values">Array to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is found in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, params T?[] values) => values.Contains(self);

    /// <summary>
    /// Determines whether the textual representation of a value (e.g. a domain primitive) is
    /// contained in the supplied collection of strings. The value's <c>ToString()</c> result is used.
    /// </summary>
    /// <typeparam name="T">Type of <paramref name="self"/>. Typically, a domain primitive.</typeparam>
    /// <param name="self">Value to locate (its string form is used).</param>
    /// <param name="values">Collection of string values to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/>'s string form is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<string?> values) => values.Contains(self?.ToString());

    /// <summary>
    /// Determines whether the textual representation of a value is contained in the supplied string array.
    /// Convenience overload for call-sites that pass an array literal.
    /// </summary>
    /// <typeparam name="T">Type of <paramref name="self"/>. Typically, a domain primitive.</typeparam>
    /// <param name="self">Value to locate (its string form is used).</param>
    /// <param name="values">Array of string values to search.</param>
    /// <returns><c>true</c> when <paramref name="self"/>'s string form is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, params string?[] values) => values.Contains(self?.ToString());

    /// <summary>
    /// Determines whether the specified non-nullable value type is contained in the supplied collection.
    /// Items in <paramref name="values"/> may be nullable; comparison uses value equality.
    /// </summary>
    /// <typeparam name="T">Value-type element type.</typeparam>
    /// <param name="self">Value to locate.</param>
    /// <param name="values">Collection to search (items may be nullable).</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T self, IEnumerable<T?> values) where T : struct => values.Contains(self);

    /// <summary>
    /// Determines whether the specified non-nullable value type is contained in the supplied array of nullable items.
    /// Convenience overload for call-sites that pass an array literal.
    /// </summary>
    /// <typeparam name="T">Value-type element type.</typeparam>
    /// <param name="self">Value to locate.</param>
    /// <param name="values">Array of nullable items to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T self, params T?[] values) where T : struct => values.Contains(self);

    /// <summary>
    /// Determines whether the specified nullable value is contained in the supplied collection of nullable items.
    /// </summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="self">Nullable value to locate.</param>
    /// <param name="values">Collection of nullable items to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<T?> values) where T : struct => values.Contains(self);

    /// <summary>
    /// Determines whether the specified nullable value is contained in the supplied array of nullable items.
    /// Convenience overload for call-sites that pass an array literal.
    /// </summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="self">Nullable value to locate.</param>
    /// <param name="values">Array of nullable items to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, params T?[] values) where T : struct => values.Contains(self);

    /// <summary>
    /// Determines whether the specified nullable value has a value and that value is contained
    /// in the supplied collection of non-nullable items.
    /// </summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="self">Nullable value to check.</param>
    /// <param name="values">Collection of non-nullable items to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> has a value and it is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, IEnumerable<T> values) where T : struct => self is not null && values.Contains(self.Value);

    /// <summary>
    /// Determines whether the specified nullable value has a value and that value is contained
    /// in the supplied array of non-nullable items. Convenience overload for array literals.
    /// </summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="self">Nullable value to check.</param>
    /// <param name="values">Array of non-nullable items to search.</param>
    /// <returns><c>true</c> if <paramref name="self"/> has a value and it is present in <paramref name="values"/>; otherwise <c>false</c>.</returns>
    public static bool In<T>(this T? self, params T[] values) where T : struct => self is not null && values.Contains(self.Value);
}

using System.Collections.Generic;
using System.Linq;

namespace AltaSoft.Storm;

/// <summary>
/// Provides extension methods for SQL-like operations.
/// </summary>
public static class SqlWhereExt
{
    /// <summary>
    /// Determines whether the specified value exists within the provided collection.
    /// </summary>
    /// <typeparam name="T">The type of the value and the collection elements.</typeparam>
    /// <param name="self">The value to locate in the collection.</param>
    /// <param name="values">The collection in which to search for the value.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="self"/> is found in <paramref name="values"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool In<T>(this T self, IEnumerable<T> values) => values.Contains(self);
}

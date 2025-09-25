using System;
using System.ComponentModel;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Extension class for working with OrderBy enums.
/// </summary>
public static class EnumExt
{
    /// <summary>
    /// Converts an array of int enum values to an array of integers based on their order.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="orderBy">The array of enum values.</param>
    /// <returns>An array of integers representing the order of the enum values.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static int[]? GetIntArrayFromOrderByEnum<T>(this T[]? orderBy) where T : struct, Enum
    {
        if (orderBy is null)
            return null;

        ref var result = ref System.Runtime.CompilerServices.Unsafe.As<T[], int[]>(ref orderBy);
        return result;
    }
}

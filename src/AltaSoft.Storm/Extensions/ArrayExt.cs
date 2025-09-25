using System;
using System.Collections.Generic;
using System.Linq;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// This class provides high performance extension methods for arrays.
/// </summary>
internal static class ArrayExt
{
    /// <summary>
    /// Filters and selects elements from an input array based on the provided filter predicate and select function.
    /// </summary>
    /// <typeparam name="TInput">The type of elements in the input array.</typeparam>
    /// <typeparam name="TOutput">The type of elements in the output array.</typeparam>
    /// <param name="array">The input array.</param>
    /// <param name="filterPredicate">The predicate used to filter elements from the input array.</param>
    /// <param name="selectFunction">The function used to select elements from the input array.</param>
    /// <returns>An array containing the selected elements from the input array that satisfy the filter predicate.</returns>
    public static List<TOutput> FilterAndSelectList<TInput, TOutput>(this TInput[] array, Func<TInput, bool> filterPredicate, Func<TInput, TOutput> selectFunction)
    {
        var result = new List<TOutput>(array.Length);

        // ReSharper disable once ForCanBeConvertedToForeach
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < array.Length; i++)
        {
            var item = array[i];
            if (filterPredicate(item))
            {
                result.Add(selectFunction(item));
            }
        }

        return result;
    }

    /// <summary>
    /// Filters and selects elements from an array based on the provided filter predicate and select function.
    /// </summary>
    /// <typeparam name="TInput">The type of elements in the input array.</typeparam>
    /// <typeparam name="TOutput">The type of elements in the output enumerable.</typeparam>
    /// <param name="array">The input array.</param>
    /// <param name="filterPredicate">The predicate used to filter elements from the input array.</param>
    /// <param name="selectFunction">The function used to select elements from the input array.</param>
    /// <returns>An enumerable of elements that pass the filter predicate and are selected by the select function.</returns>
    public static IEnumerable<TOutput> FilterAndSelect<TInput, TOutput>(this TInput[] array, Func<TInput, bool> filterPredicate, Func<TInput, TOutput> selectFunction)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < array.Length; i++)
        {
            var item = array[i];
            if (filterPredicate(item))
            {
                yield return selectFunction(item);
            }
        }
    }

    /// <summary>
    /// Converts an IEnumerable to an ICollection. If the source is already an ICollection, it is returned as is. Otherwise, it is converted to an array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the IEnumerable</typeparam>
    /// <param name="source">The source IEnumerable to convert</param>
    /// <returns>
    /// An ICollection of type T
    /// </returns>
    internal static ICollection<T> AsCollection<T>(this IEnumerable<T> source)
    {
        return source switch
        {
            ICollection<T> collection => collection,
            _ => source.ToArray()
        };
    }

    internal static IList<T> AsIList<T>(this IEnumerable<T> source)
    {
        return source switch
        {
            IList<T> list => list,
            _ => source.ToArray()
        };
    }
}

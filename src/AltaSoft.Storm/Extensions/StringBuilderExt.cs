using System;
using System.Collections.Generic;
using System.Text;
// ReSharper disable ForCanBeConvertedToForeach

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Provides extension methods for StringBuilder.
/// </summary>
internal static class StringBuilderExt
{
    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, char separator, TInput[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            var value = array[i];
            if (i > 0)
            {
                sb.Append(separator);
            }
            sb.Append(value);
        }
        return sb;
    }

    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, char separator, List<TInput> array)
    {
        for (var i = 0; i < array.Count; i++)
        {
            var value = array[i];
            if (i > 0)
            {
                sb.Append(separator);
            }
            sb.Append(value);
        }
        return sb;
    }
    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, string separator, TInput[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            var value = array[i];
            if (i > 0)
            {
                sb.Append(separator);
            }
            sb.Append(value);
        }
        return sb;
    }

    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, string separator, List<TInput> array)
    {
        for (var i = 0; i < array.Count; i++)
        {
            var value = array[i];
            if (i > 0)
            {
                sb.Append(separator);
            }
            sb.Append(value);
        }
        return sb;
    }

    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, string separator, List<TInput> array1, List<TInput> array2)
    {
        if (array1.Count == 0)
            return sb.AppendJoin(separator, array2);

        sb.AppendJoinFast(separator, array1);

        for (var i = 0; i < array2.Count; i++)
        {
            var value = array2[i];
            sb.Append(separator);
            sb.Append(value);
        }
        return sb;
    }

    /// <summary>
    /// Appends the elements of an array to a StringBuilder, using a specified separator and a function to select the string representation of each element.
    /// </summary>
    /// <typeparam name="TInput">The type of the elements in the array.</typeparam>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="array">The array of elements to append.</param>
    /// <param name="separator">The separator to use between elements.</param>
    /// <param name="selectFunction">A function that selects the string representation of each element.</param>
    /// <returns>The StringBuilder with the appended elements.</returns>
    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, TInput[] array, string separator, Func<TInput, string> selectFunction)
    {
        for (var i = 0; i < array.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(separator);
            }

            sb.Append(selectFunction(array[i]));
        }
        return sb;
    }

    /// <summary>
    /// Appends each element of the input array to the StringBuilder, separated by the specified separator, and applies the specified action to each element.
    /// </summary>
    /// <typeparam name="TInput">The type of the elements in the input array.</typeparam>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="array">The input array.</param>
    /// <param name="separator">The separator to use between elements.</param>
    /// <param name="action">The action to apply to each element.</param>
    /// <returns>The StringBuilder after appending all elements.</returns>
    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, TInput[] array, string separator, Action<StringBuilder, TInput> action)
    {
        for (var i = 0; i < array.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(separator);
            }

            action(sb, array[i]);
        }
        return sb;
    }

    /// <summary>
    /// Appends each element of the input array to the StringBuilder, separated by the specified separator character, and applies the specified action to each element.
    /// </summary>
    /// <typeparam name="TInput">The type of the elements in the input array.</typeparam>
    /// <param name="sb">The StringBuilder to append the elements to.</param>
    /// <param name="array">The input array containing the elements to append.</param>
    /// <param name="separator">The separator character to use between elements.</param>
    /// <param name="action">The action to apply to each element.</param>
    /// <returns>The StringBuilder with the appended elements.</returns>
    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, TInput[] array, char separator, Action<StringBuilder, TInput> action)
    {
        for (var i = 0; i < array.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(separator);
            }

            action(sb, array[i]);
        }
        return sb;
    }

    /// <summary>
    /// Appends the filtered elements of an array to a StringBuilder, separated by a specified character, and performs a custom action on each element.
    /// </summary>
    /// <typeparam name="TInput">The type of the elements in the array.</typeparam>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="array">The array of elements.</param>
    /// <param name="separator">The character used to separate the elements.</param>
    /// <param name="filterPredicate">A function that determines whether an element should be included in the result.</param>
    /// <param name="action">An action to perform on each included element.</param>
    /// <returns>The StringBuilder with the appended elements.</returns>
    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, TInput[] array, char separator, Func<TInput, bool> filterPredicate, Action<StringBuilder, TInput> action)
    {
        for (var i = 0; i < array.Length; i++)
        {
            var value = array[i];
            if (!filterPredicate(value))
                continue;

            if (i > 0)
            {
                sb.Append(separator);
            }

            action(sb, value);
        }
        return sb;
    }

    /// <summary>
    /// Appends the filtered elements of an array to a StringBuilder, separated by a specified separator, and performs a custom action on each element.
    /// </summary>
    /// <typeparam name="TInput">The type of the elements in the array.</typeparam>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="array">The array of elements.</param>
    /// <param name="separator">The separator to use between elements.</param>
    /// <param name="filterPredicate">A function that determines whether an element should be included in the result.</param>
    /// <param name="action">An action to perform on each included element.</param>
    /// <returns>The StringBuilder with the appended elements.</returns>
    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, TInput[] array, string separator, Func<TInput, bool> filterPredicate, Action<StringBuilder, TInput> action)
    {
        for (var i = 0; i < array.Length; i++)
        {
            var value = array[i];
            if (!filterPredicate(value))
                continue;

            if (i > 0)
            {
                sb.Append(separator);
            }

            action(sb, value);
        }
        return sb;
    }

    /// <summary>
    /// Appends the elements of a list to a StringBuilder, separated by a specified separator, and applies a specified action to each element.
    /// </summary>
    /// <typeparam name="TInput">The type of elements in the list.</typeparam>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="array">The list of elements to append.</param>
    /// <param name="separator">The separator to use between elements.</param>
    /// <param name="action">The action to apply to each element.</param>
    /// <returns>The StringBuilder after appending the elements.</returns>
    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, IReadOnlyList<TInput> array, string separator, Action<StringBuilder, TInput> action)
    {
        for (var i = 0; i < array.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(separator);
            }

            action(sb, array[i]);
        }
        return sb;
    }

    /// <summary>
    /// Appends the elements of a list to a StringBuilder, separated by a specified character, using a custom action to format each element.
    /// </summary>
    /// <typeparam name="TInput">The type of elements in the list.</typeparam>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="array">The list of elements to append.</param>
    /// <param name="separator">The character used to separate the elements.</param>
    /// <param name="action">The custom action used to format each element.</param>
    /// <returns>The StringBuilder with the appended elements.</returns>
    public static StringBuilder AppendJoinFast<TInput>(this StringBuilder sb, IReadOnlyList<TInput> array, char separator, Action<StringBuilder, TInput> action)
    {
        for (var i = 0; i < array.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(separator);
            }

            action(sb, array[i]);
        }
        return sb;
    }

    /// <summary>
    /// Wraps the contents of the specified StringBuilder in SQL transaction statements by inserting a BEGIN TRAN at the
    /// start and appending a COMMIT TRAN at the end.
    /// </summary>
    /// <remarks>This method modifies the provided StringBuilder in place. Use this method to ensure that a
    /// batch of SQL statements is executed within a single transaction block.</remarks>
    /// <param name="sb">The StringBuilder instance containing the SQL statements to be wrapped in a transaction. Cannot be null.</param>
    /// <returns>The same StringBuilder instance with BEGIN TRAN prepended and COMMIT TRAN appended.</returns>
    public static StringBuilder WrapIntoBeginTranCommit(this StringBuilder sb)
    {
        sb.Insert(0, $"BEGIN TRAN;{Environment.NewLine}");
        sb.AppendLine("COMMIT TRAN");
        return sb;
    }
}

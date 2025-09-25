#if !NET8_0_OR_GREATER

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Collections.Frozen;

/// <summary>
/// Represents a read-only dictionary where the keys are not nullable.
/// </summary>
public sealed class FrozenDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull;

/// <summary>
/// Extension method to convert a mutable Dictionary to an immutable Dictionary.
/// </summary>
public static class FrozenDictionaryExt
{
    /// <summary>
    /// Extension method that returns the input dictionary as a frozen dictionary.
    /// </summary>
    public static Dictionary<TKey, TValue> ToFrozenDictionary<TKey, TValue>(this Dictionary<TKey, TValue> source) where TKey : notnull => source;
}

#endif

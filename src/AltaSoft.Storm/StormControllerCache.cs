using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Exceptions;

namespace AltaSoft.Storm;

/// <summary>
/// Provides a cache for storing and retrieving instances of <see cref="StormControllerBase"/>.
/// This static class is used to manage ORM controller objects associated with specific types.
/// </summary>
public static class StormControllerCache
{
#if NET8_0_OR_GREATER
    private static FrozenDictionary<(Type type, int variant), StormControllerBase>? s_dict;
    private static FrozenDictionary<(Type type, int variant), Func<StormControllerBase>>? s_dict2;
#else
    private static Dictionary<(Type type, int variant), StormControllerBase>? s_dict;
    private static Dictionary<(Type type, int variant), Func<StormControllerBase>>? s_dict2;
#endif
    private static readonly Dictionary<(Type type, int variant), StormControllerBase> s_dictTemporary = new();
    private static readonly Dictionary<(Type type, int variant), Func<StormControllerBase>> s_dictTemporary2 = new();

    /// <summary>
    /// Adds a new ORM controller instance to the cache.
    /// </summary>
    /// <param name="type">The type associated with the controller to be added.</param>
    /// <param name="variant">The variant of the controller to be added.</param>
    /// <param name="controller">The ORM controller instance to add to the cache.</param>
    public static void Add(Type type, int variant, StormControllerBase controller) => s_dictTemporary.Add((type, variant), controller);

    /// <summary>
    /// Adds a new ORM controller instance to the cache using a factory method.
    /// </summary>
    /// <param name="type">The type associated with the controller to be added.</param>
    /// <param name="variant">The variant of the controller to be added.</param>
    /// <param name="ctor">A factory method that creates a new instance of <see cref="StormControllerBase"/>.</param>
    public static void Add(Type type, int variant, Func<StormControllerBase> ctor) => s_dictTemporary2.Add((type, variant), ctor);

    /// <summary>
    /// Retrieves an ORM controller instance from the cache.
    /// </summary>
    /// <param name="type">The type associated with the controller to retrieve.</param>
    /// <param name="variant">The variant associated with the controller to retrieve.</param>
    /// <returns>The ORM controller instance for the specified type.</returns>
    /// <exception cref="StormException">Thrown when there is no controller registered for the given type.</exception>
    internal static StormControllerBase Get(Type type, int variant)
    {
        if (s_dict is null)
            throw new StormException($"{nameof(StormManager)}.{nameof(StormManager.Initialize)} must be called before using the ORM system.");

        if (s_dict.TryGetValue((type, variant), out var ctrl))
        {
            if (ctrl.QuotedSchemaName.Length == 0)
                ctrl.SetSchemaName("dbo");
            return ctrl;
        }

        // For pure Virtual views, we need to create a new instance
        if (s_dict2 is null)
            throw new StormException($"{nameof(StormManager)}.{nameof(StormManager.Initialize)} must be called before using the ORM system.");

        if (s_dict2.TryGetValue((type, variant), out var ctor))
            return ctor();

        throw new StormException($"OrmController is not registered for type '{type}'");
    }

    /// <summary>
    /// Retrieves an ORM controller instance from the cache.
    /// </summary>
    /// <returns>The ORM controller instance for the specified type.</returns>
    /// <exception cref="StormException">Thrown when there is no controller registered for the given type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static StormControllerBase Get<T>(int variant) => Get(typeof(T), variant);

    /// <summary>
    /// Returns all StormControllerBase instances from the cache
    /// </summary>
    internal static IEnumerable<StormControllerBase> GetAllControllers() => s_dictTemporary.Values;

    /// <summary>
    /// Method to finish the initialization process by assigning the temporary dictionary to a frozen dictionary.
    /// </summary>
    internal static void FinishInitialization()
    {
        s_dict = s_dictTemporary.ToFrozenDictionary();
        s_dictTemporary.Clear();

        s_dict2 = s_dictTemporary2.ToFrozenDictionary();
        s_dictTemporary2.Clear();
    }
}

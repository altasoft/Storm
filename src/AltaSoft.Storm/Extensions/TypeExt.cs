using System;

#if NET8_0_OR_GREATER

using AltaSoft.DomainPrimitives;

#endif

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Provides extension methods for Type.
/// </summary>
internal static class TypeExt
{
    private const string SqlRowVersionTypeFullName = "AltaSoft.Storm.SqlRowVersion";
    private const string SqlLogSequenceNumberTypeFullName = "AltaSoft.Storm.SqlLogSequenceNumber";

#if NET8_0_OR_GREATER
    private static readonly Type? s_interfaceDomainValue = typeof(IDomainValue);
#else
    private const string DomainValueInterfaceFullName = "AltaSoft.DomainPrimitives.IDomainValue";
    private static readonly Type? s_interfaceDomainValue = Type.GetType(DomainValueInterfaceFullName);
#endif
    private static readonly Type? s_sqlTimestampType = Type.GetType(SqlRowVersionTypeFullName);
    private static readonly Type? s_sqlLogSequenceNumberType = Type.GetType(SqlLogSequenceNumberTypeFullName);

    /// <summary>
    /// Determines if the given type is a primitive, enum, AltaSoft.IDomainPrimitive or SqlRowVersion or SqlLogSequenceNumber value.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a primitive, enum, or domain value; otherwise, false.</returns>
    public static bool IsPrimitiveEnumOrDomainValue(this Type type)
    {
        // The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
        if (type.IsPrimitive || type.IsEnum)
            return true;

        if (type == typeof(string) || type == typeof(Guid) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
            return true;

        if (type == typeof(DateOnly) || type == typeof(TimeOnly))
            return true;

        return s_interfaceDomainValue?.IsAssignableFrom(type) == true || s_sqlTimestampType == type || s_sqlLogSequenceNumberType == type;
    }
}

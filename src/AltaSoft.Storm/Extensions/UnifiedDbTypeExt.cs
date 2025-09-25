using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Exceptions;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Provides extension methods for UnifiedDbType, primarily for mapping between .NET types and database types.
/// </summary>
public static partial class UnifiedDbTypeExt
{
    private static readonly Dictionary<Type, UnifiedDbType> s_dotNetTypeUnifiedDbTypeMap = new()
    {
        { typeof(bool), UnifiedDbType.Boolean },
        { typeof(byte), UnifiedDbType.UInt8 },
        { typeof(sbyte), UnifiedDbType.Int8},
        { typeof(ushort), UnifiedDbType.UInt16 },
        { typeof(short), UnifiedDbType.Int16 },
        { typeof(uint), UnifiedDbType.UInt32 },
        { typeof(int), UnifiedDbType.Int32 },
        { typeof(ulong), UnifiedDbType.UInt64 },
        { typeof(long), UnifiedDbType.Int64 },

        { typeof(char), UnifiedDbType.StringFixedLength },
        { typeof(string), UnifiedDbType.String },

        { typeof(float), UnifiedDbType.Single },
        { typeof(double), UnifiedDbType.Double },
        { typeof(decimal), UnifiedDbType.Decimal },

        { typeof(DateTime), UnifiedDbType.DateTime2 },
        { typeof(DateTimeOffset), UnifiedDbType.DateTimeOffset },
#if NET6_0_OR_GREATER
        { typeof(DateOnly), UnifiedDbType.Date },
        { typeof(TimeOnly), UnifiedDbType.Time },
#endif
        { typeof(TimeSpan), UnifiedDbType.Time },
        { typeof(Guid), UnifiedDbType.Guid },
        { typeof(byte[]), UnifiedDbType.VarBinary },
        { typeof(object), UnifiedDbType.Blob }
    };

    /// <summary>
    /// Dictionary that maps UnifiedDbType to corresponding .NET type name.
    /// </summary>
    /// <returns>
    /// Dictionary containing the mapping.
    /// </returns>
#if NET8_0_OR_GREATER
    private static readonly FrozenDictionary<UnifiedDbType, string> s_dbTypeDotNetTypeNameMap = new Dictionary<UnifiedDbType, string>
#else

    private static readonly IReadOnlyDictionary<UnifiedDbType, string> s_dbTypeDotNetTypeNameMap = new Dictionary<UnifiedDbType, string>
#endif
    {
        { UnifiedDbType.Boolean, "bool" },
        { UnifiedDbType.UInt8, "byte" },
        { UnifiedDbType.Int8, "sbyte" },
        { UnifiedDbType.UInt16, "ushort" },
        { UnifiedDbType.Int16, "short" },
        { UnifiedDbType.UInt32, "uint" },
        { UnifiedDbType.Int32, "int" },
        { UnifiedDbType.UInt64, "ulong" },
        { UnifiedDbType.Int64, "long" },

        { UnifiedDbType.AnsiString, "string" },
        { UnifiedDbType.AnsiStringFixedLength, "string" },
        { UnifiedDbType.String, "string" },
        { UnifiedDbType.StringFixedLength, "string" },
        //{ UnifiedDbType.CompressedString, "string" },

        { UnifiedDbType.Currency, "decimal" },
        { UnifiedDbType.Single, "float" },
        { UnifiedDbType.Double, "double" },
        { UnifiedDbType.Decimal, "decimal" },
        //{ UnifiedDbType.VarNumeric, "decimal" },

        { UnifiedDbType.SmallDateTime, "DateTime" },
        { UnifiedDbType.DateTime, "DateTime" },
        { UnifiedDbType.DateTime2, "DateTime" },
        { UnifiedDbType.DateTimeOffset, "DateTimeOffset" },
        { UnifiedDbType.Date, "DateOnly" },
        { UnifiedDbType.Time, "TimeOnly" },

        { UnifiedDbType.Guid, "Guid" },

        { UnifiedDbType.AnsiXml, "object" },
        { UnifiedDbType.Xml, "object" },
        { UnifiedDbType.AnsiJson, "object" },
        { UnifiedDbType.Json, "object" },
        { UnifiedDbType.AnsiText, "string" },
        { UnifiedDbType.Text, "string" },
        { UnifiedDbType.VarBinary, "byte[]" },
        { UnifiedDbType.Binary, "byte[]" },
        { UnifiedDbType.Blob, "byte[]" }
    }.ToFrozenDictionary();

    /// <summary>
    /// Convert UnifiedDbType type to .Net data type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? ToDotNetTypeName(this UnifiedDbType dbType) => s_dbTypeDotNetTypeNameMap.TryGetValue(dbType, out var value) ? value : null;

    /// <summary>
    /// Converts a .NET type to the corresponding UnifiedDbType.
    /// </summary>
    /// <param name="type">The .NET type to be converted.</param>
    /// <returns>The corresponding UnifiedDbType for the given .NET type.</returns>
    /// <exception cref="StormException">Thrown when the .NET type is unsupported.</exception>
    public static UnifiedDbType ToUnifiedDbType(this Type type)
    {
        if (s_dotNetTypeUnifiedDbTypeMap.TryGetValue(type, out var unifiedDbType))
            return unifiedDbType;

        if (type.FullName is "AltaSoft.Storm.SqlRowVersion" or "AltaSoft.Storm.SqlLogSequenceNumber")
            return UnifiedDbType.Binary;

        throw new StormException($"Unsupported Type: {type}");
    }

    /// <summary>
    /// Determines whether the UnifiedDbType is a string type.
    /// </summary>
    /// <param name="self">The UnifiedDbType instance.</param>
    /// <returns>True if the UnifiedDbType is a string type; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsString(this UnifiedDbType self) => self.IsAnsiString() || self.IsUnicodeString();

    /// <summary>
    /// Determines whether the UnifiedDbType is an ANSI string type.
    /// </summary>
    /// <param name="self">The UnifiedDbType instance.</param>
    /// <returns>True if the UnifiedDbType is an ANSI string type; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAnsiString(this UnifiedDbType self)
        => self is UnifiedDbType.AnsiString or UnifiedDbType.AnsiStringFixedLength or UnifiedDbType.AnsiText or UnifiedDbType.AnsiJson or UnifiedDbType.AnsiXml;

    /// <summary>
    /// Determines whether the UnifiedDbType is a Unicode string type.
    /// </summary>
    /// <param name="self">The UnifiedDbType instance.</param>
    /// <returns>True if the UnifiedDbType is a Unicode string type; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUnicodeString(this UnifiedDbType self)
        => self is UnifiedDbType.String or UnifiedDbType.StringFixedLength or UnifiedDbType.Text or UnifiedDbType.Json or UnifiedDbType.Xml;

    /// <summary>
    /// Checks if the UnifiedDbType has a maximum size, either for string types or binary types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasMaxSize(this UnifiedDbType self) => IsString(self) || self is UnifiedDbType.Binary or UnifiedDbType.VarBinary;

    /// <summary>
    /// Checks if the UnifiedDbType has a precision.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasPrecision(this UnifiedDbType self) => self is UnifiedDbType.DateTime2 or UnifiedDbType.Time or UnifiedDbType.DateTimeOffset
        or UnifiedDbType.Decimal or UnifiedDbType.Double;

    /// <summary>
    /// Checks if the UnifiedDbType has a scale.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasScale(this UnifiedDbType self) => self == UnifiedDbType.Decimal;
}

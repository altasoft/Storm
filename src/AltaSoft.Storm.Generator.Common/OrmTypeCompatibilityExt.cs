using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

public static class OrmTypeCompatibilityExt
{
    /// <summary>
    /// Dictionary that maps SpecialType to a list of compatible DbTypes.
    /// </summary>
    private static readonly Dictionary<SpecialType, List<UnifiedDbType>> s_exactCompatibilityMap = new()
    {
        {SpecialType.System_Boolean, [UnifiedDbType.Boolean]},

        {SpecialType.System_Byte,    [UnifiedDbType.UInt8]},
        {SpecialType.System_SByte,   [UnifiedDbType.Int8]},
        {SpecialType.System_UInt16,  [UnifiedDbType.UInt16]},
        {SpecialType.System_Int16,   [UnifiedDbType.Int16]},
        {SpecialType.System_UInt32,  [UnifiedDbType.UInt32]},
        {SpecialType.System_Int32,   [UnifiedDbType.Int32]},
        {SpecialType.System_UInt64,  [UnifiedDbType.UInt64]},
        {SpecialType.System_Int64,   [UnifiedDbType.Int64]},

        {SpecialType.System_Decimal, [UnifiedDbType.Decimal, UnifiedDbType.Currency /*, UnifiedDbType.VarNumeric */]},
        {SpecialType.System_Double,  [UnifiedDbType.Double]},
        {SpecialType.System_Single,  [UnifiedDbType.Single]},

        {SpecialType.System_DateTime,[UnifiedDbType.DateTime2, UnifiedDbType.DateTime, UnifiedDbType.SmallDateTime]},

        {SpecialType.System_String,  [UnifiedDbType.String, UnifiedDbType.AnsiString, UnifiedDbType.StringFixedLength, UnifiedDbType.AnsiStringFixedLength, UnifiedDbType.AnsiText, UnifiedDbType.Text, UnifiedDbType.AnsiJson, UnifiedDbType.Json, UnifiedDbType.AnsiXml, UnifiedDbType.Xml]},
        {SpecialType.System_Char,    [UnifiedDbType.StringFixedLength, UnifiedDbType.AnsiStringFixedLength]},
        {SpecialType.System_Object,  [UnifiedDbType.Blob]}
    };

    /// <summary>
    /// Dictionary mapping SpecialType to a list of partially compatible DbTypes.
    /// </summary>
    private static readonly Dictionary<SpecialType, List<UnifiedDbType>> s_partiallyCompatibleTypesMap = new()
    {
        {SpecialType.System_Boolean, [UnifiedDbType.Int8, UnifiedDbType.Int16, UnifiedDbType.UInt16, UnifiedDbType.Int32, UnifiedDbType.UInt32, UnifiedDbType.Int64, UnifiedDbType.UInt64]},
        {SpecialType.System_SByte,   [UnifiedDbType.UInt8, UnifiedDbType.Int16, UnifiedDbType.UInt16, UnifiedDbType.Int32, UnifiedDbType.UInt32, UnifiedDbType.Int64, UnifiedDbType.UInt64]},
        {SpecialType.System_UInt16,  [UnifiedDbType.UInt8, UnifiedDbType.Int8, UnifiedDbType.Int16, UnifiedDbType.Int32, UnifiedDbType.UInt32, UnifiedDbType.Int64, UnifiedDbType.UInt64]},
        {SpecialType.System_Int16,   [UnifiedDbType.UInt8, UnifiedDbType.Int8, UnifiedDbType.UInt16, UnifiedDbType.Int32, UnifiedDbType.UInt32, UnifiedDbType.Int64, UnifiedDbType.UInt64]},
        {SpecialType.System_UInt32,  [UnifiedDbType.UInt8, UnifiedDbType.Int8, UnifiedDbType.Int16, UnifiedDbType.UInt16, UnifiedDbType.Int32, UnifiedDbType.Int64, UnifiedDbType.UInt64]},
        {SpecialType.System_Int32,   [UnifiedDbType.UInt8, UnifiedDbType.Int8, UnifiedDbType.Int16, UnifiedDbType.UInt16, UnifiedDbType.UInt32, UnifiedDbType.Int64, UnifiedDbType.UInt64]},
        {SpecialType.System_UInt64,  [UnifiedDbType.UInt8, UnifiedDbType.Int8, UnifiedDbType.Int16, UnifiedDbType.UInt16, UnifiedDbType.Int32, UnifiedDbType.UInt32, UnifiedDbType.Int64]},
        {SpecialType.System_Int64,   [UnifiedDbType.UInt8, UnifiedDbType.Int8, UnifiedDbType.Int16, UnifiedDbType.UInt16, UnifiedDbType.Int32, UnifiedDbType.UInt32, UnifiedDbType.UInt64]},

        {SpecialType.System_Decimal, [UnifiedDbType.Single, UnifiedDbType.Double]},
        {SpecialType.System_Double,  [UnifiedDbType.Currency, UnifiedDbType.Single, UnifiedDbType.Decimal /*, UnifiedDbType.VarNumeric */]},
        {SpecialType.System_Single,  [UnifiedDbType.Currency, UnifiedDbType.Double, UnifiedDbType.Decimal /*, UnifiedDbType.VarNumeric */]},

        {SpecialType.System_DateTime,[UnifiedDbType.Date, UnifiedDbType.Time, UnifiedDbType.DateTimeOffset]},

        {SpecialType.System_String,  []},
        {SpecialType.System_Char,    [UnifiedDbType.String, UnifiedDbType.AnsiString, UnifiedDbType.Xml]},
        {SpecialType.System_Object,  []}
    };

    /// <summary>
    /// Retrieves the default UnifiedDbType for a given ITypeSymbol.
    /// </summary>
    /// <param name="typeSymbol">The INamedTypeSymbol to retrieve the default UnifiedDbType for.</param>
    /// <returns>The default UnifiedDbType for the given INamedTypeSymbol, or null if no default UnifiedDbType is found.</returns>
    public static UnifiedDbType? GetDefaultCompatibleDbType(this ITypeSymbol typeSymbol, DupSaveAs saveAs)
    {
        var mapping = GetExactMapping(typeSymbol, saveAs);
        return mapping is null || mapping.Count == 0 ? null : mapping[0];
    }

    /// <summary>
    /// Checks the compatibility of a type symbol with a UnifiedDbType.
    /// </summary>
    /// <returns>The compatibility status between the type symbol and the UnifiedDbType.</returns>
    public static TypeCompatibility CheckDbTypeCompatibility(this ITypeSymbol typeSymbol, UnifiedDbType dbType, DupSaveAs? saveAs)
    {
        var mapping = GetExactMapping(typeSymbol, saveAs);
        if (mapping is null)
        {
            // Some object that we are going to save as text?
            if (saveAs is DupSaveAs.String or DupSaveAs.Json or DupSaveAs.Xml && s_exactCompatibilityMap[SpecialType.System_String].Contains(dbType))
                return TypeCompatibility.ExactlyCompatible;
            if (saveAs is DupSaveAs.CompressedString or DupSaveAs.CompressedJson or DupSaveAs.CompressedXml && s_exactCompatibilityMap[SpecialType.System_Object].Contains(dbType))
                return TypeCompatibility.ExactlyCompatible;
            return TypeCompatibility.NotCompatible;
        }

        if (mapping.Contains(dbType))
            return TypeCompatibility.ExactlyCompatible;

        mapping = GetPartialMapping(typeSymbol, saveAs);
        if (mapping is null)
            return TypeCompatibility.NotCompatible;

        if (mapping.Contains(dbType))
            return TypeCompatibility.PartiallyCompatible;

        return TypeCompatibility.NotCompatible;
    }

    /// <summary>
    /// Gets the exact mapping of a given type symbol to a list of compatible database types.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to get the mapping for.</param>
    /// <returns>A list of compatible database types for the given type symbol.</returns>
    private static List<UnifiedDbType>? GetExactMapping(ITypeSymbol typeSymbol, DupSaveAs? saveAs)
    {
        if (saveAs.HasValue)
        {
            switch (saveAs)
            {
                case DupSaveAs.String:
                    return [UnifiedDbType.String, UnifiedDbType.AnsiString, UnifiedDbType.StringFixedLength, UnifiedDbType.AnsiStringFixedLength];
                case DupSaveAs.CompressedString:
                    return [UnifiedDbType.VarBinary, UnifiedDbType.Binary];
                case DupSaveAs.Json:
                    return [UnifiedDbType.Json, UnifiedDbType.AnsiJson];
                case DupSaveAs.CompressedJson:
                    return [UnifiedDbType.VarBinary, UnifiedDbType.Binary];
                case DupSaveAs.Xml:
                    return [UnifiedDbType.Xml, UnifiedDbType.AnsiXml];
                case DupSaveAs.CompressedXml:
                    return [UnifiedDbType.VarBinary, UnifiedDbType.Binary];
            }
        }

        if (s_exactCompatibilityMap.TryGetValue(typeSymbol.SpecialType, out var compatibleDbTypes))
            return compatibleDbTypes;

        var typeName = typeSymbol.ToDisplayString();

        return typeName switch
        {
            "System.Guid" => [UnifiedDbType.Guid],
            "System.DateOnly" => [UnifiedDbType.Date],
            "System.TimeOnly" => [UnifiedDbType.Time],
            "System.TimeSpan" => [UnifiedDbType.Time],
            "System.DateTimeOffset" => [UnifiedDbType.DateTimeOffset],
            Constants.SqlRowVersionTypeFullName => [UnifiedDbType.Binary],
            Constants.SqlLogSequenceNumberTypeFullName => [UnifiedDbType.Binary],
            "byte[]" => [UnifiedDbType.VarBinary, UnifiedDbType.Binary],
            "byte[]?" => [UnifiedDbType.VarBinary, UnifiedDbType.Binary],
            _ => null
        };
    }

    /// <summary>
    /// Gets the partial mapping of a given type symbol to a list of compatible database types.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to get the partial mapping for.</param>
    /// <returns>A list of compatible database types for the given type symbol, or null if no mapping is found.</returns>
    private static List<UnifiedDbType>? GetPartialMapping(ITypeSymbol typeSymbol, DupSaveAs? saveAs)
    {
        if (saveAs.HasValue)
        {
            switch (saveAs)
            {
                case DupSaveAs.String:
                    return [UnifiedDbType.AnsiText, UnifiedDbType.Text];
                case DupSaveAs.CompressedString:
                    return [UnifiedDbType.Blob];
                case DupSaveAs.Json:
                    return [UnifiedDbType.String, UnifiedDbType.AnsiString, UnifiedDbType.StringFixedLength, UnifiedDbType.AnsiStringFixedLength, UnifiedDbType.AnsiText, UnifiedDbType.Text];
                case DupSaveAs.CompressedJson:
                    return [UnifiedDbType.Blob];
                case DupSaveAs.Xml:
                    return [UnifiedDbType.String, UnifiedDbType.AnsiString, UnifiedDbType.StringFixedLength, UnifiedDbType.AnsiStringFixedLength, UnifiedDbType.AnsiText, UnifiedDbType.Text];
                case DupSaveAs.CompressedXml:
                    return [UnifiedDbType.Blob];
            }
        }

        if (s_partiallyCompatibleTypesMap.TryGetValue(typeSymbol.SpecialType, out var compatibleDbTypes))
            return compatibleDbTypes;

        var typeName = typeSymbol.ToDisplayString();

        return typeName switch
        {
            "System.Guid" => [],
            "System.DateOnly" => [UnifiedDbType.DateTime2, UnifiedDbType.DateTime, UnifiedDbType.SmallDateTime, UnifiedDbType.DateTimeOffset],
            "System.TimeOnly" => [UnifiedDbType.DateTime2, UnifiedDbType.DateTime, UnifiedDbType.SmallDateTime, UnifiedDbType.DateTimeOffset],
            "System.TimeSpan" => [UnifiedDbType.DateTime2, UnifiedDbType.DateTime, UnifiedDbType.SmallDateTime, UnifiedDbType.DateTimeOffset],
            "System.DateTimeOffset" => [UnifiedDbType.Date, UnifiedDbType.Time, UnifiedDbType.DateTime2, UnifiedDbType.DateTime, UnifiedDbType.SmallDateTime],
            Constants.SqlRowVersionTypeFullName => [UnifiedDbType.VarBinary],
            Constants.SqlLogSequenceNumberTypeFullName => [UnifiedDbType.VarBinary],
            "byte[]" => [UnifiedDbType.Blob],
            "byte[]?" => [UnifiedDbType.Blob],
            _ => null
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

public static class SymbolExt
{
    /// <summary>
    /// Dictionary containing type aliases for common C# types.
    /// </summary>
    /// <returns>
    /// Dictionary with type aliases as key-value pairs.
    /// </returns>
    private static readonly Dictionary<string, string> s_typeAliases = new(StringComparer.Ordinal)
    {
        { typeof(byte).FullName, "byte" },
        { typeof(sbyte).FullName, "sbyte" },
        { typeof(short).FullName, "short" },
        { typeof(ushort).FullName, "ushort" },
        { typeof(int).FullName, "int" },
        { typeof(uint).FullName, "uint" },
        { typeof(long).FullName, "long" },
        { typeof(ulong).FullName, "ulong" },
        { typeof(float).FullName, "float" },
        { typeof(double).FullName, "double" },
        { typeof(decimal).FullName, "decimal" },
        { typeof(object).FullName, "object" },
        { typeof(bool).FullName, "bool" },
        { typeof(char).FullName, "char" },
        { typeof(string).FullName, "string" },
        { typeof(void).FullName, "void" }
    };

    ///// <summary>
    ///// Gets a 'friendly' name for the specified named type symbol, which is a more readable string representation of the type.
    ///// It simplifies the display of generic types and handles nullable value types.
    ///// </summary>
    ///// <param name="type">The named type symbol for which to get the friendly name.</param>
    ///// <returns>A string representing the 'friendly' name of the type.</returns>
    //public static string GetFriendlyName(this ITypeSymbol type)
    //{
    //    var ns = type.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining))!;

    //    if (s_typeAliases.TryGetValue(ns + "." + type.MetadataName, out var result))
    //    {
    //        return result;
    //    }

    //    var friendlyName = type.MetadataName;
    //    if (friendlyName.Length == 0)
    //        friendlyName = type.OriginalDefinition.ToDisplayString();

    //    if (type is not INamedTypeSymbol namedType)
    //        return friendlyName;

    //    if (!namedType.IsGenericType)
    //        return friendlyName;

    //    if (namedType.IsNullableValueType(out var underlyingType))
    //        return underlyingType!.GetFriendlyName();

    //    var iBacktick = friendlyName.IndexOf('`');
    //    if (iBacktick > 0)
    //        friendlyName = friendlyName.Remove(iBacktick);
    //    friendlyName += "<";

    //    var typeParameters = namedType.TypeArguments;
    //    for (var i = 0; i < typeParameters.Length; ++i)
    //    {
    //        var typeParamName = typeParameters[i].ToString();
    //        friendlyName += i == 0 ? typeParamName : "," + typeParamName;
    //    }
    //    friendlyName += ">";
    //    return friendlyName;
    //}

    /// <summary>
    /// Gets a 'full' name for the specified named type symbol, which includes namespace.
    /// It simplifies the display of generic types and handles nullable value types.
    /// </summary>
    /// <param name="type">The named type symbol for which to get the full name.</param>
    /// <returns>A string representing the 'full' name of the type.</returns>
    public static string GetFullName(this ITypeSymbol type)
    {
        var ns = type.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining))!;

        if (s_typeAliases.TryGetValue(ns + "." + type.MetadataName, out var result))
        {
            return result;
        }

        var friendlyName = type.MetadataName;
        if (friendlyName.Length == 0)
            friendlyName = type.OriginalDefinition.ToDisplayString();

        if (type is not INamedTypeSymbol namedType)
            return friendlyName;

        if (!namedType.IsGenericType)
            return ns + '.' + friendlyName;

        if (namedType.IsNullableValueType(out var underlyingType))
            return underlyingType!.GetFullName();

        var iBacktick = friendlyName.IndexOf('`');
        if (iBacktick > 0)
            friendlyName = friendlyName.Remove(iBacktick);
        friendlyName += "<";

        var typeParameters = namedType.TypeArguments;
        for (var i = 0; i < typeParameters.Length; ++i)
        {
            var typeParamName = typeParameters[i].ToString();
            friendlyName += i == 0 ? typeParamName : "," + typeParamName;
        }
        friendlyName += ">";
        return ns + '.' + friendlyName;
    }

    /// <summary>
    /// Retrieves the XTypeInfo for a given INamedTypeSymbol and SaveAs option.
    /// </summary>
    /// <returns>The XTypeInfo for the given typeSymbol and saveAs.</returns>
    internal static XTypeInfo GetXTypeInfo(this ITypeSymbol typeSymbol, DupSaveAs? saveAs)
    {
        var derivedTypeSymbol = typeSymbol;

        var isNullable = derivedTypeSymbol.NullableAnnotation != NullableAnnotation.NotAnnotated;
        var kind = GetKind(derivedTypeSymbol, saveAs);

        XTypeInfo? listItemTypeInfo = null;
        XTypeInfo? keyItemTypeInfo = null;
        ITypeSymbol? dbStorageTypeInfo = null;

        if (derivedTypeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsNullableValueType(out var underlyingTypeSymbol)) // Is Nullable<T>
        {
            derivedTypeSymbol = underlyingTypeSymbol!;
            isNullable = true;
            kind = GetKind(derivedTypeSymbol, saveAs);

            dbStorageTypeInfo = GetXTypeInfo(derivedTypeSymbol, saveAs).DbStorageTypeSymbol;
        }

        if (kind == ClassKind.Enum && saveAs is not (DupSaveAs.String or DupSaveAs.CompressedString)) // Is IEnumerable<T>
        {
            if (derivedTypeSymbol is INamedTypeSymbol { EnumUnderlyingType: not null } enumTypeSymbol)
            {
                dbStorageTypeInfo = GetXTypeInfo(enumTypeSymbol.EnumUnderlyingType, saveAs).DbStorageTypeSymbol;
            }
            else
            {
                throw new Exception("Property type is not Enum");
            }
        }
        else
        if (kind == ClassKind.List)
        {
            if (derivedTypeSymbol.AllInterfaces.FirstOrDefault(x => x.ConstructedFrom.IsList()) is { } iList
                && iList.TypeArguments[0] is INamedTypeSymbol genericType)
            {
                listItemTypeInfo = GetXTypeInfo(genericType, saveAs);
                dbStorageTypeInfo = listItemTypeInfo.DbStorageTypeSymbol;
            }
            else
            {
                throw new Exception("Property type is not IEnumerable<T>");
            }
        }
        else
        if (kind == ClassKind.Dictionary)
        {
            if (derivedTypeSymbol.AllInterfaces.FirstOrDefault(x => x.ConstructedFrom.IsDictionary()) is { } iDict &&
                iDict.TypeArguments[0] is INamedTypeSymbol keyGenericType && iDict.TypeArguments[1] is INamedTypeSymbol valueGenericType)
            {
                keyItemTypeInfo = GetXTypeInfo(keyGenericType, saveAs);
                listItemTypeInfo = GetXTypeInfo(valueGenericType, saveAs);
                dbStorageTypeInfo = listItemTypeInfo.DbStorageTypeSymbol;
            }
            else
            {
                throw new Exception("Property type is not IDictionary<TKey, TValue>");
            }
        }
        else
        if (kind == ClassKind.DomainPrimitive) // Is IDomainValue<T>
        {
            if (derivedTypeSymbol.AllInterfaces.FirstOrDefault(x => string.Equals(x.ConstructedFrom.ToString(), Constants.DomainValueInterfaceFullName, StringComparison.Ordinal)) is { } intf)
            {
                dbStorageTypeInfo = GetXTypeInfo(intf.TypeArguments[0], saveAs).DbStorageTypeSymbol;
            }
            else
            {
                throw new Exception("Property type is not IDomainValue<T>");
            }
        }
        else
        if (kind is ClassKind.SqlRowVersion or ClassKind.SqlLogSequenceNumber)
        {
            isNullable = false;
        }

        return new XTypeInfo(typeSymbol, kind, isNullable, dbStorageTypeInfo,
            listItemTypeInfo?.Kind, listItemTypeInfo?.TypeSymbol,
            keyItemTypeInfo?.Kind, keyItemTypeInfo?.TypeSymbol);

        static ClassKind GetKind(ITypeSymbol typeSymbol, DupSaveAs? saveAs)
        {
            switch (typeSymbol.SpecialType)
            {
                //case SpecialType.System_Object:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_DateTime:
                    return ClassKind.KnownType;
            }

            switch (typeSymbol.ToDisplayString())
            {
                case "System.Guid":
                case "System.DateOnly":
                case "System.TimeOnly":
                case "System.TimeSpan":
                case "System.DateTimeOffset":
                    return ClassKind.KnownType;

                case Constants.SqlRowVersionTypeFullName:
                    return ClassKind.SqlRowVersion;

                case Constants.SqlLogSequenceNumberTypeFullName:
                    return ClassKind.SqlLogSequenceNumber;

                case "byte[]":
                case "byte[]?":
                    return ClassKind.KnownType;
            }

            if (typeSymbol is INamedTypeSymbol { EnumUnderlyingType: not null })
            {
                return ClassKind.Enum;
            }

            if (typeSymbol.AllInterfaces.Any(x => string.Equals(x.ConstructedFrom.ToString(), Constants.DomainValueInterfaceFullName, StringComparison.Ordinal)))
            {
                return ClassKind.DomainPrimitive;
            }

            if (saveAs == DupSaveAs.DetailTable && typeSymbol.AllInterfaces.Any(x => x.ConstructedFrom.IsList()))
            {
                return ClassKind.List;
            }

            if (saveAs == DupSaveAs.DetailTable && typeSymbol.AllInterfaces.Any(x => x.ConstructedFrom.IsDictionary()))
            {
                return ClassKind.Dictionary;
            }

            return ClassKind.Object;
        }
    }

    /// <summary>
    /// Determines whether the specified type symbol is a Nullable&lt;T&gt; value type and, if so, provides the underlying value type symbol.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <param name="underlyingTypeSymbol">When this method returns, contains the type symbol of the underlying value type if <paramref name="type"/> is a nullable value type; otherwise, null. This parameter is passed uninitialized.</param>
    /// <returns><c>true</c> if <paramref name="type"/> is a nullable value type; otherwise, <c>false</c>.</returns>
    public static bool IsNullableValueType(this INamedTypeSymbol type, out ITypeSymbol? underlyingTypeSymbol)
    {
        if (type is { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T })
        {
            underlyingTypeSymbol = type.TypeArguments[0];
            return true;
        }

        underlyingTypeSymbol = null;
        return false;
    }

    /// <summary>
    /// Extension method that checks if the given ITypeSymbol represents a list type.
    /// </summary>
    /// <param name="self">The ITypeSymbol to check.</param>
    /// <returns>True if the ITypeSymbol represents a list type, false otherwise.</returns>
    public static bool IsList(this ITypeSymbol self)
    {
        return self.SpecialType is
            SpecialType.System_Collections_Generic_IEnumerable_T or
            SpecialType.System_Collections_Generic_IList_T or
            SpecialType.System_Collections_Generic_ICollection_T or
            SpecialType.System_Collections_Generic_IReadOnlyList_T or
            SpecialType.System_Collections_Generic_IReadOnlyCollection_T;
    }

    public static bool IsDictionary(this ITypeSymbol self)
    {
        return self.ToDisplayString().Equals("System.Collections.Generic.IDictionary<TKey, TValue>", StringComparison.Ordinal);
    }

    public static bool HasStormDbObjectAttribute(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
            return false;

        while (typeSymbol is not null)
        {
            if (typeSymbol.GetStormDbObjectAttributes().Any())
                return true;

            typeSymbol = typeSymbol.BaseType;
        }
        return false;
    }

    public static IEnumerable<AttributeData> GetStormDbObjectAttributes(this ITypeSymbol type)
    {
        foreach (var attributeData in type.GetAttributes())
        {
            var attrClass = attributeData.AttributeClass;
            if (attrClass is { Name: Constants.StormDbObjectAttributeName, Arity: 1 } &&
                attrClass.ContainingNamespace.ToDisplayString() == Constants.StormAttributeNamespace)
            {
                yield return attributeData;
            }
        }
    }
    public static (string? ConverterTypeFullName, int? MaxLength) GetStormStringEnumAttributeData(this ITypeSymbol type)
    {
        foreach (var attributeData in type.GetAttributes())
        {
            var attrClass = attributeData.AttributeClass;
            if (attrClass is { Name: Constants.StormStringEnumAttributeName, Arity: 2 } &&
                attrClass.ContainingNamespace.ToDisplayString() == Constants.StormAttributeNamespace)
            {
                // Get generic type argument's full name
                var typeArg = attrClass.TypeArguments[1];
                var fullName = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Get MaxLength from constructor argument
                var ctorArg = attributeData.ConstructorArguments.Length > 0 ? attributeData.ConstructorArguments[0] : default;
                var maxLength = ctorArg is { Kind: TypedConstantKind.Primitive, Value: int i } ? i : (int?)null;

                return (fullName, maxLength);
            }
        }
        return (null, null);
    }
}

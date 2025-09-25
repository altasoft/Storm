using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

public class ReadGenerationSpec
{
    public ISymbol Symbol { get; set; }

    public ITypeSymbol TypeSymbol { get; set; }

    /// <summary>
    /// Gets the database type of the property.
    /// </summary>
    public UnifiedDbType DbType { get; }

    /// <summary>
    /// Gets the method of saving this property (e.g., as a detail table or flat object).
    /// </summary>
    public DupSaveAs SaveAs { get; }

    /// <summary>
    /// Gets a value indicating whether the property is nullable.
    /// </summary>
    /// <returns>True if the property is nullable; otherwise, false.</returns>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the kind of the class.
    /// </summary>
    public ClassKind Kind { get; }

    /// <summary>
    /// Gets the type information for the database storage, or null if not available.
    /// Nullable{T}, enum, AltaSoft.DomainPrimitives
    /// </summary>
    public ITypeSymbol DbStorageTypeSymbol { get; }

    /// <summary>
    /// Gets the type information of the list items, or null if not list.
    /// </summary>
    public ITypeSymbol? ListItemTypeSymbol { get; }

    public ClassKind? ListItemKind { get; }

    ///// <summary>
    ///// Gets the friendly name of the property's type. Does not include nullability '?'
    ///// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetTypeAlias() => GetTypeFullName();//TypeSymbol.GetFriendlyName();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetTypeFullName() => TypeSymbol.GetFullName();

    public ReadGenerationSpec(ISymbol symbol, ITypeSymbol typeSymbol, UnifiedDbType dbType, DupSaveAs saveAs, bool isNullable, ClassKind kind, ITypeSymbol dbStorageTypeSymbol, ITypeSymbol? listItemTypeSymbol, ClassKind? listItemKind)
    {
        Symbol = symbol;
        TypeSymbol = typeSymbol;
        DbType = dbType;
        SaveAs = saveAs;
        IsNullable = isNullable;
        Kind = kind;
        DbStorageTypeSymbol = dbStorageTypeSymbol;
        ListItemTypeSymbol = listItemTypeSymbol;
        ListItemKind = listItemKind;
        Symbol = symbol;
    }
}

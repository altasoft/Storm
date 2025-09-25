using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Represents type information for a class.
/// </summary>
internal sealed class XTypeInfo
{
    /// <summary>
    /// Gets the symbol representing the type of the class.
    /// </summary>
    public ITypeSymbol TypeSymbol { get; }

    /// <summary>
    /// Gets the type information for the database storage, or null if not available.
    /// Nullable{T}, enum, AltaSoft.DomainPrimitives
    /// </summary>
    public ITypeSymbol DbStorageTypeSymbol { get; }

    /// <summary>
    /// Gets the kind of the class.
    /// </summary>
    public ClassKind Kind { get; }

    /// <summary>
    /// Gets a value indicating whether the property is nullable.
    /// </summary>
    /// <returns>True if the property is nullable; otherwise, false.</returns>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the kind of the list item as a nullable ClassKind enum. (For lists and dictionaries)
    /// </summary>
    public ClassKind? ListItemKind { get; }

    /// <summary>
    /// Gets the type information of the list items, or null if the list is empty. (For lists and dictionaries)
    /// </summary>
    public ITypeSymbol? ListItemTypeSymbol { get; }

    /// <summary>
    /// Gets the kind of the list item as a nullable ClassKind enum. (For dictionaries)
    /// </summary>
    public ClassKind? KeyItemKind { get; }

    /// <summary>
    /// Gets the type information of the list items, or null if the list is empty. (For dictionaries)
    /// </summary>
    public ITypeSymbol? KeyItemTypeSymbol { get; }

    /// <summary>
    /// Initializes a new instance of the XTypeInfo class.
    /// </summary>
    public XTypeInfo(ITypeSymbol typeSymbol, ClassKind kind, bool isNullable, ITypeSymbol? dbStorageTypeSymbol,
        ClassKind? listItemKind, ITypeSymbol? listItemTypeSymbol,
        ClassKind? keyItemKind, ITypeSymbol? keyItemTypeSymbol)
    {
        TypeSymbol = typeSymbol;
        DbStorageTypeSymbol = dbStorageTypeSymbol ?? typeSymbol;
        Kind = kind;
        IsNullable = isNullable;
        ListItemKind = listItemKind;
        ListItemTypeSymbol = listItemTypeSymbol;
        KeyItemKind = keyItemKind;
        KeyItemTypeSymbol = keyItemTypeSymbol;
    }
}

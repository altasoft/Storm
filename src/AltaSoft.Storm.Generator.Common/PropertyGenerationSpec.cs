using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Represents a specification for generating a property, encapsulating various characteristics and configurations used in the generation process.
/// </summary>
[DebuggerDisplay("Name={PropertyName}")]
public sealed class PropertyGenerationSpec : ReadGenerationSpec
{
    /// <summary>
    /// Gets the property symbol representing a property declaration within a class.
    /// </summary>
    public IPropertySymbol Property { get; }

    /// <summary>
    /// Generation specification for the property's type
    /// In case of ordinary column, this is the simple type of the property
    /// In case of FlatObject, this is the type of the flat object
    /// In case of DetailTable, this is the type of the detail object
    /// </summary>
    public TypeGenerationSpec? TypeGenerationSpec { get; }

    /// <summary>
    /// Gets or sets the BindColumnData object for binding column data.
    /// </summary>
    public BindColumnData BindColumnData { get; }

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the corresponding column name in the database for this property.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Gets or sets flags indicating how this property is partially loaded.
    /// </summary>
    public uint PartialLoadFlags { get; internal set; }

    /// <summary>
    /// Gets the type of the column associated with this property in the database.
    /// </summary>
    public DupColumnType ColumnType { get; }

    /// <summary>
    /// Returns a boolean value indicating whether the property is a key.
    /// </summary>
    public bool IsKey { get; }

    /// <summary>
    /// Gets a value indicating whether the property is marked with the ConcurrencyCheck attribute.
    /// </summary>
    public bool IsConcurrencyCheck { get; }

    /// <summary>
    /// Gets a value indicating whether this property is of type AltaSoft.Storm.Interfaces.ITrackingList
    /// </summary>
    public bool IsTrackingList { get; }

    /// <summary>
    /// Gets a value indicating whether this property is read-only.
    /// </summary>
    public bool IsReadOnly { get; }

    /// <summary>
    /// Gets the size of the property, relevant only for string types.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets the precision of the property, relevant only for numeric types.
    /// </summary>
    public int Precision { get; }

    /// <summary>
    /// Gets the scale of the property, relevant only for numeric types.
    /// </summary>
    public int Scale { get; }

    /// <summary>
    /// Gets or sets the name of the detail table, if this property is saved in a separate table.
    /// </summary>
    public string? DetailTableName { get; internal set; }

    /// <summary>
    /// Returns the name of the detail table based on the provided master type friendly name.
    /// If the DetailTableName property is null, the detail table name is constructed by appending the PropertyName to the master type friendly name.
    /// Otherwise, the DetailTableName is quoted using the QuoteSqlName method.
    /// </summary>
    /// <param name="masterTypeFriendlyName">The friendly name of the master type.</param>
    /// <returns>The name of the detail table.</returns>
    public string GetDetailTableName(string masterTypeFriendlyName) => DetailTableName is null ? masterTypeFriendlyName + PropertyName : DetailTableName.QuoteSqlName();

    /// <summary>
    /// Gets the friendly name of the underlying type for List&lt;T&gt;
    /// </summary>
    public string? GetListItemTypeFullName() => ListItemTypeSymbol?.GetFullName();

    public PropertyGenerationSpec(IPropertySymbol property, TypeGenerationSpec? typeGenerationSpec,
        BindColumnData bindColumnData, uint partialLoadFlags, DupSaveAs saveAs, bool isNullable, ClassKind kind,
        ITypeSymbol dbStorageTypeSymbol, ITypeSymbol? listItemTypeSymbol, ClassKind? listItemKind, UnifiedDbType dbType, int size, int precision, int scale)
        : base(property, property.Type, dbType, saveAs, isNullable, kind, dbStorageTypeSymbol, listItemTypeSymbol, listItemKind)
    {
        Property = property;
        TypeGenerationSpec = typeGenerationSpec;

        PropertyName = property.Name;

        BindColumnData = bindColumnData;
        PartialLoadFlags = partialLoadFlags;

        Size = size;
        Precision = precision;
        Scale = scale;

        ColumnName = bindColumnData.ColumnName ??= property.Name;
        ColumnType = bindColumnData.ColumnType ?? DupColumnType.Default;

        IsKey = BindColumnData.IsKey;
        IsConcurrencyCheck = BindColumnData.IsConcurrencyCheck;

        IsTrackingList = property.Type.AllInterfaces.Any(x => string.Equals(x.ToString(), Constants.TrackingListInterfaceFullName, StringComparison.Ordinal));
        IsReadOnly = property.IsReadOnly;

        DetailTableName = bindColumnData.DetailTableName;
    }
}

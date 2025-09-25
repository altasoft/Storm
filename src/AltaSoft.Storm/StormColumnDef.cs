using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Helpers;

namespace AltaSoft.Storm;

/// <summary>
/// Represents a definition of a column in an ORM (Object-Relational Mapping) context,
/// including its properties and behaviors.
/// </summary>
[DebuggerDisplay("Name={PropertyName}.{SubPropertyName}, SaveAs={SaveAs}")]
public sealed class StormColumnDef
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StormColumnDef"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="subPropertyName">The sub-property name, if any.</param>
    /// <param name="columnName">The name of the column in the database.</param>
    /// <param name="flags">The ORM column flags.</param>
    /// <param name="dbType">The database type of the column.</param>
    /// <param name="size">The size of the column.</param>
    /// <param name="precision">The precision of the column.</param>
    /// <param name="scale">The scale of the column.</param>
    /// <param name="saveAs">The save behavior of the column.</param>
    /// <param name="partialLoadFlags">Flags for partial loading.</param>
    /// <param name="isNullable">Indicates whether the column is nullable.</param>
    /// <param name="detailType">The detail type, if any.</param>
    /// <param name="detailTableName">The detail table name, if any.</param>
    /// <param name="propertyType">The actual type of property</param>
    /// <param name="propertySerializationType">The actual type of property if saved as Json or Xml</param>
    public StormColumnDef(string propertyName, string? subPropertyName, string columnName, StormColumnFlags flags,
        UnifiedDbType dbType, int size, int precision, int scale, SaveAs saveAs, uint partialLoadFlags, bool isNullable, Type? detailType, string? detailTableName,
        Type propertyType, Type? propertySerializationType)
    {
        PropertyName = propertyName;
        SubPropertyName = subPropertyName;
        ColumnName = columnName.QuoteSqlName();
        Flags = flags;
        DbType = dbType;
        Size = size;
        Precision = precision;
        Scale = scale;
        SaveAs = saveAs;
        PartialLoadFlags = partialLoadFlags;
        IsNullable = isNullable;
        DetailType = detailType;
        PropertyType = propertyType;
        PropertySerializationType = propertySerializationType;

        if (detailTableName is null)
            return;
        UnquotedDetailTableName = detailTableName.UnquoteSqlName();
        QuotedDetailTableName = UnquotedDetailTableName.QuoteSqlName();
    }

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the sub-property name, if any.
    /// </summary>
    public string? SubPropertyName { get; }

    /// <summary>
    /// Gets the quoted column name.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Gets or sets the ORM column flags.
    /// </summary>
    public StormColumnFlags Flags { get; set; }

    /// <summary>
    /// Gets the database type of the column.
    /// </summary>
    public UnifiedDbType DbType { get; }

    /// <summary>
    /// Gets the size of the column. For non-string types, this value is 0.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets the precision of the column. For non-numeric types, this value is 0.
    /// </summary>
    public int Precision { get; }

    /// <summary>
    /// Gets the scale of the column. For non-numeric types, this value is 0.
    /// </summary>
    public int Scale { get; }

    /// <summary>
    /// Gets the save behavior of the column.
    /// </summary>
    public SaveAs SaveAs { get; }

    /// <summary>
    /// Gets the flags for partial loading.
    /// </summary>
    public uint PartialLoadFlags { get; }

    /// <summary>
    /// Gets a value indicating whether the column is nullable.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the detail type, if any.
    /// </summary>
    public Type? DetailType { get; }

    /// <summary>
    /// Gets the Actual type of property
    /// </summary>
    public Type PropertyType { get; }
    /// <summary>
    /// Gets the Actual type of property if saved as Json or Xml
    /// </summary>
    public Type? PropertySerializationType { get; }

    /// <summary>
    /// Gets the quoted name of the detail table associated with this column definition, if applicable.
    /// This property can be used to specify a related table in a database schema where detail records are stored.
    /// The value can be null if no detail table is associated.
    /// </summary>
    public string? QuotedDetailTableName { get; }

    /// <summary>
    /// Gets the unquoted name of the detail table associated with this column definition, if applicable.
    /// This property can be used to specify a related table in a database schema where detail records are stored.
    /// The value can be null if no detail table is associated.
    /// </summary>
    public string? UnquotedDetailTableName { get; }

    /// <summary>
    /// Determines if a column can be selected based on the SaveAs property and the StormColumnFlags.
    /// </summary>
    /// <returns>True if the column can be selected, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanSelectColumn() => SaveAs != SaveAs.DetailTable && (Flags & StormColumnFlags.CanSelect) != StormColumnFlags.None;

    /// <summary>
    /// Checks if the column can be updated based on the SaveAs property and the CanUpdate flag.
    /// </summary>
    /// <returns>True if the column can be updated, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanUpdateColumn() => SaveAs != SaveAs.DetailTable && (Flags & StormColumnFlags.CanUpdate) != StormColumnFlags.None;

    /// <summary>
    /// Checks if a column can be inserted based on the SaveAs property and the CanInsert flag.
    /// </summary>
    /// <returns>True if a column can be inserted, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanInsertColumn() => SaveAs != SaveAs.DetailTable && (Flags & StormColumnFlags.CanInsert) != StormColumnFlags.None;

    /// <summary>
    /// Checks if the column has the Key flag set.
    /// </summary>
    /// <returns>True if the column is a key, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKey() => (Flags & StormColumnFlags.Key) != StormColumnFlags.None;

    /// <summary>
    /// Checks if the column has the AutoIncrement flag set.
    /// </summary>
    /// <returns>True if the column has the AutoIncrement flag set, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAutoInc() => (Flags & StormColumnFlags.AutoIncrement) != StormColumnFlags.None;

    /// <summary>
    /// Checks if the column has the RowVersion flag set.
    /// </summary>
    /// <returns>True if the column has the RowVersion flag set, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsRowVersion() => (Flags & StormColumnFlags.RowVersion) != StormColumnFlags.None;

    /// <summary>
    /// Checks if the detail column should be loaded during partial loading based on the save as type and partial load flags.
    /// </summary>
    /// <param name="partialLoadFlags">The partial load flags.</param>
    /// <returns>True if the detail column should be loaded during partial loading, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool LoadThisDetailColumnInPartialLoading(uint partialLoadFlags) => SaveAs == SaveAs.DetailTable && (PartialLoadFlags == 0 || (PartialLoadFlags & partialLoadFlags) != 0);

    /// <summary>
    /// Converts the input value to JSON or XML format based on the SaveAs enum value.
    /// If the input value is null, it returns null.
    /// </summary>
    /// <param name="value">The input value to be converted.</param>
    /// <param name="typeToSerialize">Type to serialize object</param>
    /// <returns>
    /// Returns the input value converted to JSON or XML format based on the SaveAs enum value.
    /// If the input value is null, it returns null.
    /// </returns>
    internal object? GetValueForDbParameter(object? value, Type? typeToSerialize)
    {
        if (value is null)
            return value;

        switch (SaveAs)
        {
            case SaveAs.Json:
                return StormManager.ToJson(value, typeToSerialize);

            case SaveAs.String:
                return value?.ToString();

            case SaveAs.CompressedString:
                var s = value?.ToString();
                if (s is null)
                    return null;
                return SqlCompression.Compress(s);

            case SaveAs.CompressedJson:
                return SqlCompression.Compress(StormManager.ToJson(value, typeToSerialize));

            case SaveAs.Xml:
                return StormManager.ToXml(value, typeToSerialize);

            case SaveAs.CompressedXml:
                return SqlCompression.Compress(StormManager.ToXml(value, typeToSerialize));

            default:
                if (value is not string strValue)
                    return value;
                return strValue;
        }
    }
}

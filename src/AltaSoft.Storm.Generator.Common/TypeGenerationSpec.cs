using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Represents a specification for generating a type.
/// </summary>
[DebuggerDisplay("TypeFullName={TypeFullName}")]
public sealed class TypeGenerationSpec
{
    private DupUpdateMode? _updateMode;

    /// <summary>
    /// Gets the type symbol.
    /// </summary>
    public ITypeSymbol TypeSymbol { get; }

    public string TypeFullName => TypeSymbol.GetFullName();

    /// <summary>
    /// Gets the list of property generation specifications.
    /// </summary>
    public List<PropertyGenerationSpec>? PropertyGenSpecList { get; internal set; }

    /// <summary>
    /// Gets or sets the list of ObjectVariant objects.
    /// </summary>
    public List<ObjectVariant> ObjectVariants { get; set; } = default!;

    /// <summary>
    /// Gets or sets the list of IndexObjectData objects.
    /// </summary>
    public List<IndexObjectData> IndexObjects { get; set; } = default!;

    /// <summary>
    /// Gets the minimum UpdateMode value from a collection of ObjectVariants using LINQ Min method.
    /// </summary>
    public DupUpdateMode UpdateMode() => _updateMode ?? (ObjectVariants.Count == 0 ? DupUpdateMode.NoUpdates : ObjectVariants.Min(x => x.UpdateMode));

    public List<string>? TemplateMethods { get; set; } //TODO

    public string? EnumConverterFullName { get; set; }
    public int? EnumConverterColumnSize { get; set; }

    /// <summary>
    /// Initializes a new instance of the TypeGenerationSpec class with the specified INamedTypeSymbol.
    /// </summary>
    /// <param name="type">The INamedTypeSymbol representing the type.</param>
    public TypeGenerationSpec(ITypeSymbol type)
    {
        TypeSymbol = type;
    }

    /// <summary>
    /// Initializes the TypeGenerationSpec instance.
    /// </summary>
    /// <param name="propertyGenSpecList">The list of property generation specifications.</param>
    /// <param name="bindObjects">BindObjectData object list used to bind data to a table.</param>
    /// <param name="indexObjects"></param>
    public void Initialize(List<PropertyGenerationSpec>? propertyGenSpecList, List<BindObjectData> bindObjects, List<IndexObjectData> indexObjects)
    {
        PropertyGenSpecList = propertyGenSpecList;
        ObjectVariants = bindObjects.ConvertAll(x =>
            new ObjectVariant(x.ObjectName ?? TypeSymbol.Name.Pluralize(), x.UpdateMode ?? DupUpdateMode.ChangeTracking, x.VirtualViewSql, x));
        IndexObjects = indexObjects;
    }

    public void Initialize()
    {
        var bindObjectData =
            new BindObjectData("yyy")
            {
                ObjectType = DupDbObjectType.Table,
                UpdateMode = DupUpdateMode.NoUpdates,
                ObjectName = TypeSymbol.Name.Pluralize()
            };

        ObjectVariants = [new ObjectVariant(bindObjectData.ObjectName, DupUpdateMode.NoUpdates, null, bindObjectData)];
    }

    internal void SetUpdateMode(DupUpdateMode updateMode)
    {
        _updateMode = updateMode;
    }
}

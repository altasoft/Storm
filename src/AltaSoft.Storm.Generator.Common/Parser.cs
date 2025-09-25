using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using AltaSoft.Storm.Extensions;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// The Parser class is responsible for generating specifications for types and properties based on attributes and inferred settings.
/// It maintains a cache of type generation specifications to prevent stack overflow when the same type is found elsewhere in the object graph.
/// </summary>
public sealed class Parser
{
    private readonly SourceProductionContext? _context;

    /// <summary>
    /// Type information for member types in input object graphs.
    /// </summary>
    private readonly Dictionary<ITypeSymbol, TypeGenerationSpec> _typeGenerationSpecCache = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Type information for member types in input object graphs.
    /// </summary>
    private readonly Dictionary<ITypeSymbol, TypeGenerationSpec> _objectTypeGenerationSpecCache = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Initializes a new instance of the Parser class with the specified SourceProductionContext.
    /// </summary>
    /// <param name="context">The SourceProductionContext to use for parsing.</param>
    public Parser(SourceProductionContext? context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves the TypeGenerationSpec for a given INamedTypeSymbol.
    /// </summary>
    /// <param name="type">The INamedTypeSymbol representing the type.</param>
    /// <returns>The TypeGenerationSpec for the given type.</returns>
    public TypeGenerationSpec GetStormTypeGenerationSpec(ITypeSymbol type)
    {
        if (_typeGenerationSpecCache.TryGetValue(type, out var typeSpec))
        {
            return typeSpec!;
        }

        // Add metadata to cache now to prevent stack overflow when the same type is found somewhere else in the object graph.
        typeSpec = new TypeGenerationSpec(type);
        _typeGenerationSpecCache[type] = typeSpec;

        var bindObjects = new List<BindObjectData>(4);
        var indexObjects = new List<IndexObjectData>(4);

        foreach (var attributeData in type.GetStormDbObjectAttributes())
        {
            var attributeType = attributeData.AttributeClass!.TypeArguments[0];
            var typeName = attributeType.Name;
            if (typeName == "StormContext" && attributeType.ContainingNamespace.ToDisplayString() == Constants.StormContextNamespace)
            {
                throw new InvalidStormAttributeParams("AL0010", "Invalid type for StormContext", "\"StormContext\" cannot be used as a generic argument type", attributeData.GetGenericArgumentLocation());
            }

            var bindObjectData = new BindObjectData(typeName);

            foreach (var arg in attributeData.NamedArguments)
            {
                if (arg.Is(Constants.StormDbObjectObjectNamePropertyName, "string"))
                {
                    bindObjectData.ObjectName = (string?)ArgValue();
                }
                else
                if (arg.Is(Constants.StormDbObjectSchemaNamePropertyName, "string"))
                {
                    bindObjectData.SchemaName = (string?)ArgValue();
                }
                else
                if (arg.Is(Constants.StormDbObjectObjectTypePropertyName, Constants.StormDbObjectObjectTypeFullName))
                {
                    bindObjectData.ObjectType = (DupDbObjectType)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.StormDbObjectDisplayNamePropertyName, "string"))
                {
                    bindObjectData.DisplayName = (string?)ArgValue();
                }
                else
                if (arg.Is(Constants.StormDbObjectUpdateModePropertyName, Constants.StormDbObjectUpdateModeFullName))
                {
                    bindObjectData.UpdateMode = (DupUpdateMode)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.StormDbObjectVirtualViewSqlPropertyName, "string"))
                {
                    bindObjectData.VirtualViewSql = (string?)ArgValue();
                }
                else
                if (arg.Is(Constants.StormDbObjectBulkCopyPropertyName, "bool"))
                {
                    bindObjectData.BulkInsert = (bool?)ArgValue() ?? false;
                }
                continue;

                object? ArgValue() => arg.Value.Value;
                object NotNullArgValue() => ArgValue() ?? new Exception("Argument Value is null");
            }

            bindObjectData.UpdateMode ??= bindObjectData.ObjectType == DupDbObjectType.Table ? DupUpdateMode.ChangeTracking : DupUpdateMode.NoUpdates;

            if (bindObjectData.ObjectType is DupDbObjectType.StoredProcedure or DupDbObjectType.TableValuedFunction)
            {
                bindObjectData.Parameters = GetParametersSpec(type, bindObjectData.DisplayName ?? bindObjectData.ObjectName);
            }

            if (bindObjectData.ObjectType is DupDbObjectType.VirtualView && (bindObjectData.VirtualViewSql is null || bindObjectData.VirtualViewSql.Length == 0))
            {
                throw new InvalidStormAttributeParams("AL0011", "VirtualViewSql not specified", "VirtualViewSql value is required for VirtualView", attributeData.GetGenericArgumentLocation());
            }

            if (bindObjectData.ObjectType is not DupDbObjectType.VirtualView && bindObjectData.VirtualViewSql is not null)
            {
                throw new InvalidStormAttributeParams("AL0012", "VirtualViewSql is specified", "VirtualViewSql value should be specified only for VirtualView-s", attributeData.GetGenericArgumentLocation());
            }

            bindObjects.Add(bindObjectData);
        }

        foreach (var attributeData in type.GetAttributes().Where(x => x.IsOfType(0, Constants.StormIndexAttributeName)))
        {
            var indexObjectData = new IndexObjectData();

            if (attributeData.ConstructorArguments.Length < 2)
            {
                continue; // Invalid constructor
            }

            var arg = attributeData.ConstructorArguments[0];
            indexObjectData.IndexColumns = ((ImmutableArray<TypedConstant>)NotNullArgValues())
                .Select(x => x.Value?.ToString() ?? "").ToArray();

            arg = attributeData.ConstructorArguments[1];
            indexObjectData.IsUnique = (bool)NotNullArgValue();

            if (attributeData.ConstructorArguments.Length >= 2)
            {
                arg = attributeData.ConstructorArguments[2];
                indexObjectData.IndexName = (string?)ArgValue();
            }

            indexObjects.Add(indexObjectData);
            continue;

            object? ArgValue() => arg.Value;
            object? ArgValues() => arg.Values;
            object NotNullArgValue() => ArgValue() ?? new Exception("Argument Value is null");
            object NotNullArgValues() => ArgValues() ?? new Exception("Argument Value is null");
        }

        if (type.GetAttributes().Any(x => x.IsOfType(0, Constants.StormTrackableObjectAttributeName)))
        {
            typeSpec.SetUpdateMode(DupUpdateMode.ChangeTracking);
        }

        var (enumConverterFullName, enumConverterSize) = type.GetStormStringEnumAttributeData();

        var propGenSpecList = new List<PropertyGenerationSpec>(16);
        var partialLoadFlagsCount = 0;

        for (var currentType = type; currentType != null; currentType = currentType.BaseType)
        {
            var idx = 0;

            //if (currentType.ContainingNamespace.ToDisplayString().StartsWith("System."))
            //    break;

            foreach (var propertySymbol in currentType.GetStormCompatibleProperties())
            {
                var spec = GetPropertyGenerationSpec(propertySymbol, ref partialLoadFlagsCount);

                if (spec is null || spec.SaveAs == DupSaveAs.Ignore)
                {
                    continue;
                }

                propGenSpecList.Insert(idx++, spec);
            }
        }

        typeSpec.Initialize(propGenSpecList, bindObjects, indexObjects);
        typeSpec.EnumConverterFullName = enumConverterFullName;
        typeSpec.EnumConverterColumnSize = enumConverterSize;
        return typeSpec;
    }

    /// <summary>
    /// Retrieves the TypeGenerationSpec for a given INamedTypeSymbol.
    /// </summary>
    /// <param name="type">The INamedTypeSymbol representing the type.</param>
    /// <returns>The TypeGenerationSpec for the given type.</returns>
    public TypeGenerationSpec GetObjectStormTypeGenerationSpec(ITypeSymbol type)
    {
        if (_objectTypeGenerationSpecCache.TryGetValue(type, out var typeSpec))
        {
            return typeSpec!;
        }

        // Add metadata to cache now to prevent stack overflow when the same type is found somewhere else in the object graph.
        typeSpec = new TypeGenerationSpec(type);
        _objectTypeGenerationSpecCache[type] = typeSpec;

        var bindObjects = new List<BindObjectData>(4);

        foreach (var attributeData in type.GetStormDbObjectAttributes())
        {
            var bindObjectData = new BindObjectData("xxx");

            foreach (var arg in attributeData.NamedArguments)
            {
                if (arg.Is(Constants.StormDbObjectUpdateModePropertyName, Constants.StormDbObjectUpdateModeFullName))
                {
                    bindObjectData.UpdateMode = (DupUpdateMode)NotNullArgValue();
                    break;
                }
                continue;

                object? ArgValue() => arg.Value.Value;
                object NotNullArgValue() => ArgValue() ?? new Exception("Argument Value is null");
            }

            bindObjectData.UpdateMode ??= bindObjectData.ObjectType == DupDbObjectType.Table ? DupUpdateMode.ChangeTracking : DupUpdateMode.NoUpdates;
            bindObjects.Add(bindObjectData);
        }

        if (type.GetAttributes().Any(x => x.IsOfType(0, Constants.StormTrackableObjectAttributeName)))
        {
            typeSpec.SetUpdateMode(DupUpdateMode.ChangeTracking);
        }

        var propGenSpecList = new List<PropertyGenerationSpec>(0);
        typeSpec.Initialize(propGenSpecList, bindObjects, []);
        return typeSpec;
    }

    private List<ParameterGenerationSpec>? GetParametersSpec(ITypeSymbol typeSymbol, string? objectName)
    {
        var method = typeSymbol.GetMembersOfType<IMethodSymbol>().FirstOrDefault(x => string.Equals(x.Name, objectName, StringComparison.Ordinal));
        if (method is null)
        {
            _context.ReportDiagnosticError("AL0007", "Template method not found",
                $"private void {objectName}(...) method not found in class {typeSymbol.Name}. Please specify template method with parameters.",
                typeSymbol.Locations.FirstOrDefault());
            return null;
        }

        if (method.Parameters.Length == 0)
        {
            return [];
        }

        return method.Parameters.Select(GetParameterGenerationSpec).ToList();
    }

    /// <summary>
    /// Retrieves the TypeGenerationSpec for a given named type symbol and class kind.
    /// </summary>
    /// <param name="type">The named type symbol.</param>
    /// <returns>The TypeGenerationSpec for the given type and class kind.</returns>
    private TypeGenerationSpec GetSimpleStormTypeGenerationSpec(ITypeSymbol type)
    {
        if (_typeGenerationSpecCache.TryGetValue(type, out var typeMetadata))
        {
            return typeMetadata!;
        }

        // Add metadata to cache now to prevent stack overflow when the same type is found somewhere else in the object graph.
        typeMetadata = new TypeGenerationSpec(type);
        typeMetadata.Initialize();

        _typeGenerationSpecCache[type] = typeMetadata;
        return typeMetadata;
    }

    /// <summary>
    /// Generates a specification for a property, extracting metadata from attributes and inferring additional settings.
    /// This specification is used for various purposes, including database mapping and code generation.
    /// </summary>
    /// <param name="propertySymbol">The symbol representing the property to generate the specification for.</param>
    /// <param name="partialLoadFlagsCount">A reference to a count of partial load flags, used for tracking state across multiple properties.</param>
    /// <returns>A <see cref="PropertyGenerationSpec"/> object encapsulating the metadata and settings of the property.</returns>
    private PropertyGenerationSpec? GetPropertyGenerationSpec(IPropertySymbol propertySymbol, ref int partialLoadFlagsCount)
    {
        if (propertySymbol.Type.TypeKind == TypeKind.Interface)
        {
            //TODO warning
            return null;
        }

        var bindColumnData = new BindColumnData();

        var attributeData = propertySymbol.GetAttributes().FirstOrDefault(x => x.IsOfType(0, Constants.StormColumnAttributeName));
        if (attributeData is not null)
        {
            foreach (var arg in attributeData.NamedArguments)
            {
                if (arg.Is(Constants.SaveAsPropertyName, Constants.StormColumnSaveAsTypeFullName))
                {
                    bindColumnData.SaveAs = (DupSaveAs)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.ColumnTypePropertyName, Constants.StormColumnColumnTypeFullName))
                {
                    bindColumnData.ColumnType = (DupColumnType)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.DbTypePropertyName, Constants.UnifiedDbTypeFullName))
                {
                    bindColumnData.DbType = (UnifiedDbType)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.LoadWithFlagsPropertyName, SpecialType.System_Boolean))
                {
                    bindColumnData.LoadWithFlags = (bool)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.ColumnNamePropertyName, SpecialType.System_String))
                {
                    bindColumnData.ColumnName = (string?)ArgValue();
                }
                else
                if (arg.Is(Constants.DbSizePropertyName, SpecialType.System_Int32))
                {
                    bindColumnData.Size = (int)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.DbPrecisionPropertyName, SpecialType.System_Int32))
                {
                    bindColumnData.Precision = (int)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.DbScalePropertyName, SpecialType.System_Int32))
                {
                    bindColumnData.Scale = (int)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.DetailTableNamePropertyName, SpecialType.System_String))
                {
                    bindColumnData.DetailTableName = (string?)ArgValue();
                }

                continue;

                object? ArgValue() => arg.Value.Value;
                object NotNullArgValue() => ArgValue() ?? new Exception("Argument Value is null");
            }
        }

        if (bindColumnData.SaveAs == DupSaveAs.Ignore)
        {
            return null;
        }
        var xTypeInfo = (propertySymbol.Type).GetXTypeInfo(bindColumnData.SaveAs);

        if (_context.HasValue)
        {
            if (xTypeInfo.Kind == ClassKind.Object && bindColumnData.SaveAs == DupSaveAs.Default)
            {
                _context.ReportDiagnosticError("AL0001", "Invalid use of SaveAs property",
                    $"SaveAs.{nameof(DupSaveAs.Default)} is specified for object property. Please specify SaveAs.{nameof(DupSaveAs.Json)}, SaveAs.{nameof(DupSaveAs.Xml)}, SaveAs.{nameof(DupSaveAs.CompressedJson)}, SaveAs.{nameof(DupSaveAs.CompressedXml)} or SaveAs.{nameof(DupSaveAs.FlatObject)}",
                    GetAttributeArgumentLocation(attributeData, propertySymbol, Constants.SaveAsPropertyName));
            }

            if (bindColumnData.SaveAs != DupSaveAs.DetailTable && bindColumnData.DetailTableName is not null)
            {
                _context.ReportDiagnosticError("AL0005", "Invalid use of DetailTableName property",
                    $"DetailTableName can only be specified for SaveAs = SaveAs.{nameof(DupSaveAs.DetailTable)}",
                    GetAttributeArgumentLocation(attributeData, propertySymbol, Constants.DetailTableNamePropertyName));
            }
        }

        var saveAs = bindColumnData.SaveAs ??= DupSaveAs.Default;
        var dbType = bindColumnData.DbType;

        // In case of Nullable<T>, Enum, DomainValue<T>, SqlRowVersion or SqlLogSequenceNumber we need to use the underlying type

        TypeGenerationSpec? typeGenerationSpec = null;

        switch (xTypeInfo.Kind)
        {
            case ClassKind.Object:
                switch (saveAs)
                {
                    case DupSaveAs.FlatObject:
                        dbType ??= UnifiedDbType.Default; // If saving as FlatObject, we don't care about the type here
                        typeGenerationSpec = GetStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                        break;

                    case DupSaveAs.String:
                        // Here we are interested only if the property is a IChangeTrackable
                        dbType ??= UnifiedDbType.VarBinary;
                        typeGenerationSpec = GetObjectStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                        saveAs = DupSaveAs.String;
                        break;

                    case DupSaveAs.CompressedString:
                        // Here we are interested only if the property is a IChangeTrackable
                        dbType ??= UnifiedDbType.VarBinary;
                        typeGenerationSpec = GetObjectStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                        saveAs = DupSaveAs.CompressedString;
                        break;

                    case DupSaveAs.CompressedJson:
                        // Here we are interested only if the property is a IChangeTrackable
                        dbType ??= UnifiedDbType.VarBinary;
                        typeGenerationSpec = GetObjectStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                        saveAs = DupSaveAs.CompressedJson;
                        break;

                    case DupSaveAs.Xml:
                        // Here we are interested only if the property is a IChangeTrackable
                        dbType ??= UnifiedDbType.Xml;
                        typeGenerationSpec = GetObjectStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                        saveAs = DupSaveAs.Xml;
                        break;

                    case DupSaveAs.CompressedXml:
                        // Here we are interested only if the property is a IChangeTrackable
                        dbType ??= UnifiedDbType.VarBinary;
                        typeGenerationSpec = GetObjectStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                        saveAs = DupSaveAs.CompressedXml;
                        break;

                    default:
                        // Here we are interested only if the property is a IChangeTrackable
                        dbType ??= UnifiedDbType.Json; // If not saving as FlatObject, CompressedJson, Xml or CompressedXml, then save as Json (by default)
                        typeGenerationSpec = GetObjectStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                        saveAs = DupSaveAs.Json;
                        break;
                }
                break;

            case ClassKind.List:
            case ClassKind.Dictionary:
                if (saveAs == DupSaveAs.DetailTable)
                {
                    typeGenerationSpec = GetStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                }
                else
                if (saveAs == DupSaveAs.Xml)
                {
                    dbType ??= UnifiedDbType.Xml;
                    saveAs = DupSaveAs.Xml;
                }
                else
                if (saveAs == DupSaveAs.CompressedXml)
                {
                    dbType ??= UnifiedDbType.VarBinary;
                    saveAs = DupSaveAs.CompressedXml;
                }
                else
                if (saveAs == DupSaveAs.CompressedJson)
                {
                    dbType ??= UnifiedDbType.VarBinary;
                    saveAs = DupSaveAs.CompressedJson;
                }
                else
                {
                    dbType ??= UnifiedDbType.Json; // If not saving as DetailTable or Xml, then save as Json (by default)
                    saveAs = DupSaveAs.Json;
                }
                break;

            case ClassKind.SqlRowVersion:
                dbType ??= UnifiedDbType.Binary;
                saveAs = DupSaveAs.Default;
                bindColumnData.Size ??= 8;
                typeGenerationSpec = GetSimpleStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                break;

            case ClassKind.SqlLogSequenceNumber:
                dbType ??= UnifiedDbType.Binary;
                saveAs = DupSaveAs.Default;
                bindColumnData.Size ??= 10;
                typeGenerationSpec = GetSimpleStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                break;

            case ClassKind.Enum:
                typeGenerationSpec = GetStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                break;

            default:
                typeGenerationSpec = GetSimpleStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                break;
        }

        if (saveAs == DupSaveAs.DetailTable && dbType != UnifiedDbType.Default)
        {
            if (attributeData is not null && dbType.HasValue)
            {
                VerifyTypeCompatibility(attributeData, dbType.Value, saveAs, xTypeInfo.DbStorageTypeSymbol, propertySymbol);
            }

            dbType ??= UnifiedDbType.Default;
        }
        else
        {
            dbType ??= xTypeInfo.DbStorageTypeSymbol.GetDefaultCompatibleDbType(saveAs) ?? UnifiedDbType.String;

            if (attributeData is not null && dbType != UnifiedDbType.Default)
            {
                VerifyTypeCompatibility(attributeData, dbType.Value, saveAs, xTypeInfo.DbStorageTypeSymbol, propertySymbol);
            }
        }

        uint partialLoadFlags = 0;
        var lwf = saveAs is DupSaveAs.DetailTable ||
            (xTypeInfo.IsNullable && saveAs is DupSaveAs.Json or DupSaveAs.Xml or DupSaveAs.CompressedJson or DupSaveAs.CompressedXml);
        if (bindColumnData.LoadWithFlags ?? lwf)
        {
            if (!xTypeInfo.IsNullable)
            {
                _context.ReportDiagnosticError("AL0003", "Invalid use of LoadWithFlags property",
                    "LoadWithFlags = true is specified for not nullable property. Cannot partially load not nullable property",
                    GetAttributeArgumentLocation(attributeData, propertySymbol, Constants.LoadWithFlagsPropertyName));
            }

            partialLoadFlags = (uint)1 << partialLoadFlagsCount;
            partialLoadFlagsCount++;
        }

        return new PropertyGenerationSpec(propertySymbol, typeGenerationSpec, bindColumnData, partialLoadFlags, saveAs,
            xTypeInfo.IsNullable, xTypeInfo.Kind, xTypeInfo.DbStorageTypeSymbol, xTypeInfo.ListItemTypeSymbol, xTypeInfo.ListItemKind,
            dbType.Value,
            bindColumnData.Size ?? typeGenerationSpec?.EnumConverterColumnSize ?? (dbType.Value.HasMaxSize() ? -1 : 0),
            bindColumnData.Precision ?? 0, bindColumnData.Scale ?? 0);
    }

    public ParameterGenerationSpec GetParameterGenerationSpec(IParameterSymbol paramSymbol)
    {
        var bindParameterData = new BindParameterData();

        var attributeData = paramSymbol.GetAttributes().FirstOrDefault(x => x.IsOfType(0, Constants.StormParameterAttributeName));
        if (attributeData is not null)
        {
            foreach (var arg in attributeData.NamedArguments)
            {
                if (arg.Is(Constants.DbTypePropertyName, Constants.UnifiedDbTypeFullName))
                {
                    bindParameterData.DbType = (UnifiedDbType)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.ParameterNamePropertyName, SpecialType.System_String))
                {
                    bindParameterData.ParameterName = (string?)ArgValue();
                }
                else
                if (arg.Is(Constants.DbSizePropertyName, SpecialType.System_Int32))
                {
                    bindParameterData.Size = (int)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.DbPrecisionPropertyName, SpecialType.System_Int32))
                {
                    bindParameterData.Precision = (int)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.DbScalePropertyName, SpecialType.System_Int32))
                {
                    bindParameterData.Scale = (int)NotNullArgValue();
                }

                continue;

                object? ArgValue() => arg.Value.Value;
                object NotNullArgValue() => ArgValue() ?? new Exception("Argument Value is null");
            }
        }

        bindParameterData.Direction = paramSymbol.RefKind switch
        {
            RefKind.Ref => ParameterDirection.InputOutput,
            RefKind.Out => ParameterDirection.Output,
            _ => ParameterDirection.Input
        };

        var xTypeInfo = (paramSymbol.Type).GetXTypeInfo(DupSaveAs.Default);

        if (_context.HasValue)
        {
            if (xTypeInfo.Kind is ClassKind.Object or ClassKind.List or ClassKind.Dictionary)
            {
                _context.ReportDiagnosticError("AL0008", "Unsupported parameter type",
                    $"{xTypeInfo.Kind} is not supported in parameters",
                    paramSymbol.Locations.FirstOrDefault());
            }
        }

        var dbType = bindParameterData.DbType;
        TypeGenerationSpec? typeGenerationSpec = null;

        switch (xTypeInfo.Kind)
        {
            case ClassKind.Object:
            case ClassKind.List:
            case ClassKind.Dictionary:
                break;

            case ClassKind.SqlRowVersion:
                dbType ??= UnifiedDbType.Binary;
                bindParameterData.Size ??= 8;
                typeGenerationSpec = GetSimpleStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                break;

            case ClassKind.SqlLogSequenceNumber:
                dbType ??= UnifiedDbType.Binary;
                bindParameterData.Size ??= 10;
                typeGenerationSpec = GetSimpleStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                break;

            default:
                typeGenerationSpec = GetSimpleStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                break;
        }

        dbType ??= xTypeInfo.DbStorageTypeSymbol.GetDefaultCompatibleDbType(DupSaveAs.Default) ?? UnifiedDbType.String;

        if (attributeData is not null && dbType != UnifiedDbType.Default)
        {
            VerifyTypeCompatibility(attributeData, dbType.Value, DupSaveAs.Default, xTypeInfo.DbStorageTypeSymbol, paramSymbol);
        }

        return new ParameterGenerationSpec(paramSymbol, typeGenerationSpec, bindParameterData,
            xTypeInfo.IsNullable, xTypeInfo.Kind, xTypeInfo.DbStorageTypeSymbol,
            dbType.Value, bindParameterData.Size ?? (dbType.Value.HasMaxSize() ? -1 : 0), bindParameterData.Precision ?? 0, bindParameterData.Scale ?? 0, bindParameterData.Direction ?? ParameterDirection.Input);
    }

    public ParameterGenerationSpec GetReturnValueGenerationSpec(IMethodSymbol methodSymbol)
    {
        var returnTypeSymbol = methodSymbol.ReturnType;
        var bindParameterData = new BindParameterData();

        var attributeData = methodSymbol.GetAttributes().FirstOrDefault(x => x.IsOfType(0, Constants.StormFunctionAttributeName));
        if (attributeData is not null)
        {
            foreach (var arg in attributeData.NamedArguments)
            {
                if (arg.Is(Constants.StormDbObjectObjectNamePropertyName, "string"))
                {
                    bindParameterData.ObjectName = (string?)ArgValue();
                }
                else
                if (arg.Is(Constants.StormDbObjectSchemaNamePropertyName, "string"))
                {
                    bindParameterData.SchemaName = (string?)ArgValue();
                }
                else
                if (arg.Is(Constants.DbTypePropertyName, Constants.UnifiedDbTypeFullName))
                {
                    bindParameterData.DbType = (UnifiedDbType)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.DbSizePropertyName, SpecialType.System_Int32))
                {
                    bindParameterData.Size = (int)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.DbPrecisionPropertyName, SpecialType.System_Int32))
                {
                    bindParameterData.Precision = (int)NotNullArgValue();
                }
                else
                if (arg.Is(Constants.DbScalePropertyName, SpecialType.System_Int32))
                {
                    bindParameterData.Scale = (int)NotNullArgValue();
                }

                continue;

                object? ArgValue() => arg.Value.Value;
                object NotNullArgValue() => ArgValue() ?? new Exception("Argument Value is null");
            }
        }

        var xTypeInfo = returnTypeSymbol.GetXTypeInfo(DupSaveAs.Default);

        if (_context.HasValue)
        {
            if (xTypeInfo.Kind is ClassKind.Object or ClassKind.List or ClassKind.Dictionary)
            {
                _context.ReportDiagnosticError("AL0009", "Unsupported return value type",
                    $"{xTypeInfo.Kind} is not supported in return values",
                    returnTypeSymbol.Locations.FirstOrDefault());
            }
        }

        var dbType = bindParameterData.DbType;
        TypeGenerationSpec? typeGenerationSpec = null;

        switch (xTypeInfo.Kind)
        {
            case ClassKind.Object:
            case ClassKind.List:
            case ClassKind.Dictionary:
                break;

            case ClassKind.SqlRowVersion:
                dbType ??= UnifiedDbType.Binary;
                bindParameterData.Size ??= 8;
                break;

            case ClassKind.SqlLogSequenceNumber:
                dbType ??= UnifiedDbType.Binary;
                bindParameterData.Size ??= 10;
                break;

            default:
                typeGenerationSpec = GetSimpleStormTypeGenerationSpec(xTypeInfo.DbStorageTypeSymbol);
                break;
        }

        dbType ??= xTypeInfo.DbStorageTypeSymbol.GetDefaultCompatibleDbType(DupSaveAs.Default) ?? UnifiedDbType.String;

        if (attributeData is not null && dbType != UnifiedDbType.Default)
        {
            VerifyTypeCompatibility(attributeData, dbType.Value, DupSaveAs.Default, xTypeInfo.DbStorageTypeSymbol, returnTypeSymbol);
        }

        return new ParameterGenerationSpec(returnTypeSymbol, typeGenerationSpec, bindParameterData,
            xTypeInfo.IsNullable, xTypeInfo.Kind, xTypeInfo.DbStorageTypeSymbol,
            dbType.Value, bindParameterData.Size ?? (dbType.Value.HasMaxSize() ? -1 : 0), bindParameterData.Precision ?? 0, bindParameterData.Scale ?? 0, bindParameterData.Direction ?? ParameterDirection.Input);
    }

    public (string? schemaName, string? objectName) GetProcedureGenerationSpec(IMethodSymbol methodSymbol)
    {
        string? schemaName = null;
        string? objectName = null;

        var attributeData = methodSymbol.GetAttributes().FirstOrDefault(x => x.IsOfType(0, Constants.StormProcedureAttributeName));
        if (attributeData is not null)
        {
            foreach (var arg in attributeData.NamedArguments)
            {
                if (arg.Is(Constants.StormDbObjectObjectNamePropertyName, "string"))
                {
                    objectName = (string?)ArgValue();
                }
                else
                if (arg.Is(Constants.StormDbObjectSchemaNamePropertyName, "string"))
                {
                    schemaName = (string?)ArgValue();
                }

                continue;

                object? ArgValue() => arg.Value.Value;
            }
        }

        return (schemaName, objectName);
    }

    /// <summary>
    /// Verifies the compatibility of a type with a UnifiedDbType and SaveAs option.
    /// </summary>
    /// <param name="attributeData">The attribute data.</param>
    /// <param name="dbType">The UnifiedDbType.</param>
    /// <param name="saveAs">The SaveAs option.</param>
    /// <param name="typeSymbol">The type symbol.</param>
    /// <param name="symbol">The property symbol.</param>
    private void VerifyTypeCompatibility(AttributeData attributeData, UnifiedDbType dbType, DupSaveAs saveAs, ITypeSymbol typeSymbol, ISymbol symbol)
    {
        var result = typeSymbol.CheckDbTypeCompatibility(dbType, saveAs);
        if (!_context.HasValue)
            return;

        switch (result)
        {
            case TypeCompatibility.ExactlyCompatible:
                break;

            case TypeCompatibility.NotCompatible:
                Report(DiagnosticSeverity.Error);
                break;

            case TypeCompatibility.PartiallyCompatible:
                Report(DiagnosticSeverity.Warning);
                break;
        }

        return;

        void Report(DiagnosticSeverity severity)
        {
            var text = ".Net type '{0}' compatibility issue with UnifiedDbType '{1}'. ";

            text += severity == DiagnosticSeverity.Error
                ? "Type '{0}' and UnifiedDbType.{1} are not compatible"
                : "Possible loss of data when casting to/from type '{0}' and UnifiedDbType.{1}";

            _context.Value.ReportDiagnostic("AL0004", $"Invalid use of {Constants.DbTypePropertyName} property", text, severity,
                GetAttributeArgumentLocation(attributeData, symbol, Constants.DbTypePropertyName), typeSymbol.Name, dbType.ToString());
        }
    }

    /// <summary>
    /// Retrieves the location of the named argument in the attribute data, or the location of the property symbol if the named argument is not found.
    /// </summary>
    /// <param name="attributeData">The attribute data.</param>
    /// <param name="symbol">The symbol.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The location of the named argument in the attribute data, or the location of the property symbol if the named argument is not found.</returns>
    private static Location? GetAttributeArgumentLocation(AttributeData? attributeData, ISymbol symbol, string propertyName)
    {
        return attributeData?.GetNamedArgumentLocation(propertyName) ?? symbol.Locations.FirstOrDefault();
    }
}

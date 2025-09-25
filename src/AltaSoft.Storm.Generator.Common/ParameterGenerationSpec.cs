using System.Data;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Represents a specification for generating a property, encapsulating various characteristics and configurations used in the generation process.
/// </summary>
[DebuggerDisplay("Name={ParameterName}")]
public sealed class ParameterGenerationSpec
{
    public ISymbol Parameter { get; }

    /// <summary>
    /// Gets the database type of the property.
    /// </summary>
    public UnifiedDbType DbType { get; }

    /// <summary>
    /// Gets the size of the property, relevant only for string types.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets the precision of the property, relevant only for numeric types.
    /// </summary>
    public int Precision { get; }

    /// <summary>
    /// Gets or sets the direction of the parameter within a query.
    /// </summary>
    public ParameterDirection Direction { get; set; }

    /// <summary>
    /// Gets the scale of the property, relevant only for numeric types.
    /// </summary>
    public int Scale { get; }

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

    ///// <summary>
    ///// Gets the friendly name of the property's type. Does not include nullability '?'
    ///// </summary>
    public string GetTypeAlias()
    {
        var typeName = Parameter is IParameterSymbol parameterSymbol ? parameterSymbol.Type.GetFullName() : Parameter.ToDisplayString();
        if (IsNullable)
            typeName += "?";
        return typeName;
    }

    ///// <summary>
    ///// Gets the friendly name of the property's type. If it's domain primitive, that undelying type is returned. Does not include nullability '?'
    ///// </summary>
    public string GetTypeAliasNoDomain()
    {
        var typeName = Parameter is IParameterSymbol parameterSymbol ? GetUnderlyingType(parameterSymbol.Type).GetFullName() : Parameter.ToDisplayString();
        if (IsNullable)
            typeName += "?";
        return typeName;

        static ITypeSymbol GetUnderlyingType(ITypeSymbol typeSymbol)
        {
            var xType = typeSymbol.GetXTypeInfo(null);
            if (xType.Kind == ClassKind.DomainPrimitive)
                return xType.DbStorageTypeSymbol;
            return typeSymbol;
        }
    }

    /// <summary>
    /// Generation specification for the property's type
    /// In case of ordinary column, this is the simple type of the property
    /// In case of FlatObject, this is the type of the flat object
    /// In case of DetailTable, this is the type of the detail object
    /// </summary>
    public TypeGenerationSpec? TypeGenerationSpec { get; }

    /// <summary>
    /// Gets or sets the BindParameterData object for binding parameter data.
    /// </summary>
    public BindParameterData BindParameterData { get; }

    /// <summary>
    /// Gets the name of the .net parameter.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Gets the corresponding parameter name in the database for this parameter.
    /// </summary>
    public string SqlParameterName { get; }

    public ParameterGenerationSpec(ISymbol parameter, TypeGenerationSpec? typeGenerationSpec,
        BindParameterData bindParameterData, bool isNullable, ClassKind kind,
        ITypeSymbol dbStorageTypeSymbol, UnifiedDbType dbType, int size, int precision, int scale, ParameterDirection direction)
    {
        Parameter = parameter;
        TypeGenerationSpec = typeGenerationSpec;

        ParameterName = parameter.Name;

        BindParameterData = bindParameterData;
        IsNullable = isNullable;
        Kind = kind;
        DbStorageTypeSymbol = dbStorageTypeSymbol;

        DbType = dbType;
        Size = size;
        Precision = precision;
        Scale = scale;
        Direction = direction;

        SqlParameterName = bindParameterData.ParameterName ??= "@" + parameter.Name;
    }
}

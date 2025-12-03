using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Generator.Common;
using AltaSoft.Storm.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Constants = AltaSoft.Storm.Generator.Common.Constants;

namespace AltaSoft.Storm.Generator;

internal static class Executor
{
    internal static void Execute(
        SourceProductionContext context,
        ImmutableArray<TypeDeclarationSyntax> classDeclarations,
        ImmutableArray<TypeDeclarationSyntax> classDeclarations2,
        ImmutableArray<MethodDeclarationSyntax> funcMethodDeclarations,
        ImmutableArray<MethodDeclarationSyntax> procMethodDeclarations,
        Compilation compilation)
    {
        var ormContext = new OrmGeneratorContext(context);
        try
        {
            var classes = classDeclarations.Union(classDeclarations2).Distinct();
            ProcessClasses(classes, ormContext, context, compilation);

            ormContext.ResetBuilder();

            var methods = funcMethodDeclarations.Distinct().Select(x => (methodSyntax: x, isProcedure: false))
                .Concat(procMethodDeclarations.Distinct().Select(x => (methodSyntax: x, isProcedure: true)));
            ProcessTemplateMethods(methods, ormContext, context, compilation);
        }
        catch (InvalidStormAttributeParams ex)
        {
            ormContext.Context.ReportDiagnostic(ex.Id, ex.Title, ex.Message, DiagnosticSeverity.Error, ex.Location);
        }
        catch (Exception ex)
        {
            ormContext.Context.ReportDiagnostic("AL0000", "Unexpected error", $"Unexpected error: {ex.Message}", DiagnosticSeverity.Error, null);
            throw;
        }
    }

    private static void ProcessClasses(IEnumerable<TypeDeclarationSyntax> classes, OrmGeneratorContext ormContext, SourceProductionContext context,
        Compilation compilation)
    {
        // 1st pass
        var dataBindableList = new List<(TypeDeclarationSyntax classSyntax, INamedTypeSymbol classSymbol, TypeGenerationSpec typeSpec)>(16);

        foreach (var classSyntax in classes)
        {
            var classModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);

            if (classModel.GetDeclaredSymbol(classSyntax, context.CancellationToken) is not INamedTypeSymbol classSymbol)
                continue; //this will never happen

            var typeSpec = ormContext.Parser.GetStormTypeGenerationSpec(classSymbol);

            if (classSymbol.BaseType is { } baseType && baseType.SpecialType != SpecialType.System_Object)
            {
                var baseTypeSpec = ormContext.Parser.GetStormTypeGenerationSpec(baseType);

                var baseUpdateMode = baseTypeSpec.UpdateMode();
                var thisUpdateMode = typeSpec.UpdateMode();

                if (thisUpdateMode < baseUpdateMode)
                {
                    ormContext.Context.ReportDiagnostic("AL0006", "Invalid use of UpdateMode property",
                        $"UpdateMode of this class is '{thisUpdateMode}' and base class '{baseTypeSpec.TypeFullName}' has '{baseUpdateMode}'",
                        DiagnosticSeverity.Error, classSymbol.Locations.FirstOrDefault());
                }
            }

            dataBindableList.Add((classSyntax, classSymbol, typeSpec));
        }

        // 2nd pass
        foreach (var (classSyntax, classSymbol, typeSpec) in dataBindableList)
        {
            ormContext.ResetBuilder();

            var sourceText = Generate(ormContext, classSyntax, classSymbol, typeSpec);

            if (sourceText is not null)
                context.AddSource($"{classSymbol.Name.Replace('<', '_').Replace('>', '_').Replace(',', '_')}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        }

        // Add assembly attributes
        context.AddSource("AssemblyAttributes.g.cs", SourceText.From("[assembly: AltaSoft.Storm.Attributes.StormAssembly]", Encoding.UTF8));
    }

    private static void ProcessTemplateMethods(IEnumerable<(MethodDeclarationSyntax methodSyntax, bool isProcedure)> methods, OrmGeneratorContext ormContext,
        SourceProductionContext context, Compilation compilation)
    {
        var parser = new Parser(context);

        var groupedByClass = methods.Select(x => (classSyntax: GetParentClassSyntax(x.methodSyntax), x.methodSyntax, x.isProcedure))
            .GroupBy(key => key.classSyntax, e => (e.methodSyntax, e.isProcedure));

        foreach (var group in groupedByClass)
        {
            ormContext.ResetBuilder();

            var classSyntax = group.Key;
            var classModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);
            if (classModel.GetDeclaredSymbol(classSyntax, context.CancellationToken) is not ITypeSymbol classSymbol)
                continue; //this will never happen

            var builder = ormContext.Builder;
            builder.AppendSourceHeader("AltaSoft Storm ORM Generator");
            GenerateUsingList(builder, [classSyntax]);

            builder.AppendLine("#pragma warning disable IDE1006, CS0612, CS8618", false);
            builder.AppendComment("ReSharper disable InconsistentNaming");
            builder.NewLine();
            builder.AppendNamespace(classSymbol.ContainingNamespace.ToDisplayString());

            var modifiers = classSyntax.Modifiers.ToFullString().Trim();

            builder.AppendClass(classSyntax is RecordDeclarationSyntax, modifiers, classSymbol.Name);

            var resultClassBuilder = new SourceCodeBuilder(1);

            foreach (var (methodSyntax, isProcedure) in group)
            {
                var methodModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);

                if (methodModel.GetDeclaredSymbol(methodSyntax, context.CancellationToken) is not IMethodSymbol methodSymbol)
                    continue; //this will never happen

                var parameters = methodSymbol.Parameters.Select(parser.GetParameterGenerationSpec).ToList();

                if (isProcedure)
                {
                    var (schemaName, objectName) = parser.GetProcedureGenerationSpec(methodSymbol);
                    GenerateStoredProcedure(ormContext, classSyntax, methodSymbol, parameters, schemaName, objectName, resultClassBuilder);
                }
                else
                {
                    var returnValueSpec = parser.GetReturnValueGenerationSpec(methodSymbol);

                    GenerateScalarFunction(ormContext, methodSymbol, parameters, returnValueSpec);
                }
            }

            if (resultClassBuilder.Length > 0)
            {
                builder.NewLine();
                builder.Append(resultClassBuilder.ToString());
            }

            builder.CloseBracket();

            var sourceText = builder.ToString();
            context.AddSource($"{classSymbol.Name.Replace('<', '_').Replace('>', '_').Replace(',', '_')}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        }

        return;

        static TypeDeclarationSyntax GetParentClassSyntax(MethodDeclarationSyntax methodSyntax)
        {
            // Traverse up the syntax tree to find the containing class or struct
            var parent = methodSyntax.Parent;
            while (parent is not null && parent is not TypeDeclarationSyntax)
            {
                parent = parent.Parent;
            }

            if (parent is null)
                throw new InvalidOperationException("Parent class not found");
            return (TypeDeclarationSyntax)parent;
        }
    }

    private static void GenerateScalarFunction(OrmGeneratorContext ormContext, ISymbol methodSymbol, List<ParameterGenerationSpec> parameters, ParameterGenerationSpec returnValueSpec)
    {
        var builder = ormContext.Builder;

        var methodName = methodSymbol.Name;
        var schemaName = returnValueSpec.BindParameterData.SchemaName;
        var objectName = returnValueSpec.BindParameterData.ObjectName;

        builder.AppendSummary($"Scalac function executor for <c>{schemaName}.{objectName ?? methodName}</c>");
        builder.Append("public IExecuteScalarFunc<").Continue(returnValueSpec.GetTypeAlias()).Continue("> Execute").Continue(methodName).Continue("(");
        for (var i = 0; i < parameters.Count; i++)
        {
            var p = parameters[i];
            builder.Continue(p.GetTypeAlias()).Continue(" ").Continue(p.ParameterName);
            if (i != parameters.Count - 1)
                builder.Continue(", ");
        }
        builder.ContinueLine(")");
        builder.OpenBracket();

        var callParamsStr = GenerateCallParamsString(builder, parameters);

        builder.Append("return StormCrudFactory.ExecuteScalarFunc<").Continue(returnValueSpec.GetTypeAlias()).Continue(">(this, ").Continue(callParamsStr).Continue(", ");

        if (schemaName is not null)
            builder.Continue(schemaName.QuoteName('"')).Continue(", ");
        else
            builder.Continue("null, ");
        builder.Continue((objectName ?? methodName).QuoteName('"')).ContinueLine(", ResultReader);").NewLine();

        var readSpec = new ReadGenerationSpec(returnValueSpec.Parameter, (ITypeSymbol)returnValueSpec.Parameter,
            returnValueSpec.DbType, DupSaveAs.Default, returnValueSpec.IsNullable, returnValueSpec.Kind, returnValueSpec.DbStorageTypeSymbol, null, null);

        builder.Append("static ").Continue(returnValueSpec.GetTypeAlias()).Continue(" ResultReader(StormDbDataReader dr) => ");
        GenerateReadAsXxxMethodName(ormContext, readSpec, "(0)");
        builder.ContinueLine(";");
        builder.CloseBracket();
    }

    private static void GenerateStoredProcedure(OrmGeneratorContext ormContext, TypeDeclarationSyntax classSyntax, ISymbol methodSymbol,
        List<ParameterGenerationSpec> parameters, string? schemaName, string? objectName, SourceCodeBuilder resultClassBuilder)
    {
        var builder = ormContext.Builder;

        var methodName = methodSymbol.Name;
        var resultClassName = methodName + "Result";

        builder.AppendSummary($"Stored procedure executor for <c>{schemaName}.{objectName ?? methodName}</c>");
        builder.Append("public IExecuteProc<").Continue(resultClassName).Continue("> Execute").Continue(methodName).Continue("(");

        var inputParams = parameters.Where(x => x.Direction is ParameterDirection.Input or ParameterDirection.InputOutput).ToList();
        var outputParams = parameters.Where(x => x.Direction is ParameterDirection.Output or ParameterDirection.InputOutput).ToList();

        for (var i = 0; i < inputParams.Count; i++)
        {
            var p = inputParams[i];
            builder.Continue(p.GetTypeAlias()).Continue(" ").Continue(p.ParameterName);
            if (i != inputParams.Count - 1)
                builder.Continue(", ");
        }
        builder.ContinueLine(")");
        builder.OpenBracket();

        var callParamsStr = GenerateCallParamsString(builder, parameters);

        builder.Continue("return StormCrudFactory.ExecuteProc<").Continue(resultClassName).Continue(">(this, ").Continue(callParamsStr).Continue(", ");

        if (schemaName is not null)
            builder.Continue(schemaName.QuoteName('"')).Continue(", ");
        else
            builder.Continue("null, ");
        builder.Continue((objectName ?? methodName).QuoteName('"')).ContinueLine(", ResultReader);").NewLine();

        builder.Append("static ").Continue(resultClassName).Continue(" ResultReader(int rowsAffected, StormDbParameterCollection parameters, Exception? ex)");
        builder.OpenBracket();

        builder.Append("return new ").Continue(resultClassName).ContinueLine("()");
        builder.OpenBracket();
        builder.AppendLine("RowsAffected = rowsAffected,");
        builder.AppendLine("Exception = ex,");
        builder.AppendLine("ReturnValue = (int)parameters[0].Value,");

        for (var i = 0; i < outputParams.Count; i++)
        {
            var p = outputParams[i];
            builder.Append(p.ParameterName.ToPascalCase()).Continue(" = parameters[")
                .Continue(p.SqlParameterName.QuoteName('"')).Continue("].Value.GetDbValue<")
                .Continue(p.GetTypeAliasNoDomain()).Continue(">()");
            if (i != outputParams.Count - 1)
                builder.ContinueLine(",");
        }

        builder.NewLine();
        builder.CloseBracketWithSemiColon();
        builder.CloseBracket();
        builder.CloseBracket();

        // Result class
        var modifiers = classSyntax.GetAccessibility();
        resultClassBuilder.AppendSummary($"Result class for <see cref=\"Execute{methodName}\"/>");
        resultClassBuilder.AppendClass(true, modifiers, resultClassName, "StormProcedureResult");
        foreach (var p in parameters)
        {
            if (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
            {
                resultClassBuilder.Append("public ").Continue(p.GetTypeAlias()).Continue(" ").Continue(p.ParameterName.ToPascalCase()).ContinueLine(" { get; set; }");
            }
        }

        resultClassBuilder.CloseBracket();
    }

    private static string GenerateCallParamsString(SourceCodeBuilder builder, List<ParameterGenerationSpec> parameters)
    {
        string callParamsStr;
        if (parameters.Count == 0)
        {
            callParamsStr = "[]";
        }
        else
        {
            callParamsStr = "callParams";

            builder.Append("var callParams = new List<StormCallParameter>(").Continue(parameters.Count.ToString(CultureInfo.InvariantCulture)).ContinueLine(")");
            builder.OpenBracket();
            for (var i = 0; i < parameters.Count; i++)
            {
                var p = parameters[i];
                builder.Append("new(\"")
                    .Continue(p.SqlParameterName)
                    .Continue("\", UnifiedDbType.").Continue(p.DbType.ToString()).Continue(", ") //db type
                    .Continue(p.Direction == ParameterDirection.Output ? "DBNull.Value" : p.ParameterName)
                    .Continue(", ") // value
                    .Continue(p.Size.ToString(CultureInfo.InvariantCulture)).Continue(", ") // size
                    .Continue(p.Precision.ToString(CultureInfo.InvariantCulture)).Continue(", ") // precision
                    .Continue(p.Scale.ToString(CultureInfo.InvariantCulture)).Continue(", ") // scale
                    .Continue(nameof(ParameterDirection)).Continue(".").Continue(p.Direction.ToString()) // direction
                    .Continue(")");

                if (i != parameters.Count - 1)
                    builder.ContinueLine(",");
            }
            builder.NewLine();
            builder.CloseBracketWithSemiColon().NewLine();
        }

        return callParamsStr;
    }

    private static string? Generate(OrmGeneratorContext ormContext, TypeDeclarationSyntax classSyntax, ITypeSymbol classSymbol, TypeGenerationSpec typeSpec)
    {
        var propertyGenSpecList = typeSpec.PropertyGenSpecList;
        if (propertyGenSpecList is null)
            return null;

        var cancellationToken = ormContext.Context.CancellationToken;
        var models = typeSpec.TypeSymbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax(cancellationToken)).OfType<TypeDeclarationSyntax>().ToList();

        var classes = new List<TypeDeclarationSyntax>(32) { classSyntax };
        classes.AddRange(models);

        var builder = ormContext.Builder;
        builder.AppendSourceHeader("AltaSoft Storm ORM Generator");

        var hasBulkInsert = typeSpec.ObjectVariants.Any(x => x.BindObjectData.BulkInsert);
        GenerateUsingList(builder, classes, hasBulkInsert ? ["Microsoft.Data.SqlClient", "System.Threading.Channels"] : null);

        builder.AppendLine("#pragma warning disable IDE1006, CS0612, CS8618", false);
        builder.AppendComment("ReSharper disable InconsistentNaming");

        builder.NewLine();

        builder.AppendNamespace(classSymbol.ContainingNamespace.ToDisplayString());

        var baseHasStormDbObjectAttribute = classSymbol.BaseType.HasStormDbObjectAttribute();

        GenerateMainClass(ormContext, classSyntax, typeSpec, baseHasStormDbObjectAttribute, propertyGenSpecList);

        if (!classSymbol.IsAbstract && typeSpec.ObjectVariants.Count > 0)
        {
            builder.NewLine();
            GenerateControllerClass(ormContext, classSyntax, typeSpec, propertyGenSpecList);

            builder.NewLine();
            GenerateStormContextClasses(ormContext, classSyntax, typeSpec, propertyGenSpecList);
        }

        return builder.ToString();
    }

    private static void GenerateMainClass(OrmGeneratorContext ormContext, MemberDeclarationSyntax classSyntax,
        TypeGenerationSpec typeSpec, bool baseHasStormDbObjectAttribute, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;

        var isStormDbObject = typeSpec.ObjectVariants.Count > 0;

        var modifiers = classSyntax.Modifiers.ToFullString().Trim();

        var hasKeys = isStormDbObject && HasKeys(propertyGenSpecList);
        var hasConcurrencyCheck = isStormDbObject && HasConcurrencyCheck(propertyGenSpecList);
        var hasBulkInsert = typeSpec.ObjectVariants.Any(x => x.BindObjectData.BulkInsert);
        var inheritance = isStormDbObject ? hasKeys ? "IDataBindableWithKey" : "IDataBindable" : "";

        if (hasConcurrencyCheck)
        {
            if (inheritance.Length > 0)
                inheritance += ", ";
            inheritance += "IConcurrencyCheck";
        }

        if (typeSpec.UpdateMode() == DupUpdateMode.ChangeTracking)
        {
            if (inheritance.Length > 0)
                inheritance += ", ";
            inheritance += "ITrackingObject";
        }

        if (hasKeys && typeSpec.TypeSymbol.AllInterfaces.Any(x =>
                string.Equals(x.Name, "IEquatable", StringComparison.Ordinal) &&
                string.Equals(x.ContainingNamespace.ToString(), "System", StringComparison.Ordinal)))
        {
            inheritance += $", IEntityComparer<{typeSpec.TypeSymbol.Name}>";
        }

        builder.AppendComment($"UpdateMode: {typeSpec.UpdateMode()}");
        for (var i = 0; i < typeSpec.ObjectVariants.Count; i++)
        {
            var objectVariant = typeSpec.ObjectVariants[i];
            builder.AppendLines("// ",
                $"{i.ToString(CultureInfo.InvariantCulture)}. ({objectVariant.BindObjectData.SchemaName}.{objectVariant.DisplayName} - {objectVariant.BindObjectData.ObjectType}, {objectVariant.BindObjectData.UpdateMode}");
        }

        builder.AppendClass(classSyntax is RecordDeclarationSyntax, modifiers, typeSpec.TypeSymbol.Name, inheritance);

        if (isStormDbObject)
        {
            var haveConcurrencyCheck = propertyGenSpecList.Exists(x =>
                (x.ColumnType & DupColumnType.ConcurrencyCheck) != DupColumnType.Default);

            GenerateCtor(ormContext, typeSpec, propertyGenSpecList, baseHasStormDbObjectAttribute, haveConcurrencyCheck);

            if (haveConcurrencyCheck)
            {
                GenerateConcurrencyProps(ormContext, propertyGenSpecList);
            }

            var baseHasKeys = false;
            if (hasKeys)
            {
                baseHasKeys = HasKeys(propertyGenSpecList
                    .FindAll(x => !SymbolEqualityComparer.Default.Equals(x.Property.ContainingType, typeSpec.TypeSymbol)));

                GenerateKeyComparer(ormContext, typeSpec, propertyGenSpecList);
                GenerateGetKeyValueMethod(ormContext, propertyGenSpecList, baseHasKeys);
            }

            GenerateSetAutoIncValueMethod(ormContext, propertyGenSpecList, baseHasStormDbObjectAttribute);

            GenerateAddDetailRowMethod(ormContext, propertyGenSpecList, baseHasStormDbObjectAttribute);

            var partialLoadFlags = propertyGenSpecList
                .Where(p => p.PartialLoadFlags != 0)
                .Select(p => (p.PropertyName, p.PartialLoadFlags, p.SaveAs == DupSaveAs.DetailTable)).ToList();

            GeneratePartialLoadFlagsEnum(ormContext, partialLoadFlags, baseHasStormDbObjectAttribute);

            GenerateOrderByEnum(ormContext, propertyGenSpecList, baseHasStormDbObjectAttribute, baseHasKeys);

            GenerateColumnValues(ormContext, typeSpec, propertyGenSpecList, baseHasStormDbObjectAttribute);

            if (hasConcurrencyCheck)
                GenerateConcurrencySupport(ormContext, typeSpec, propertyGenSpecList, baseHasStormDbObjectAttribute);

            if (hasBulkInsert && propertyGenSpecList.Any(x => x.SaveAs == DupSaveAs.DetailTable)) //TODO
            {
                ormContext.Context.ReportDiagnostic("AL5010", "Invalid column type for bulk insert",
                    "Cannot generate bulk insert when a property is saved as DetailTable", DiagnosticSeverity.Error, typeSpec.TypeSymbol.Locations.FirstOrDefault());
                return;
            }
        }

        if (typeSpec.UpdateMode() == DupUpdateMode.ChangeTracking)
        {
            GenerateChangeTrackingSupport(ormContext, typeSpec, propertyGenSpecList);
        }

        builder.CloseBracket();
    }

    private static void GenerateColumnValues(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec, List<PropertyGenerationSpec> propertyGenSpecList,
        bool baseHasStormDbObjectAttribute)
    {
        var builder = ormContext.Builder;
        var isAbstract = typeSpec.TypeSymbol.IsAbstract;

        // __GetLoadingFlags
        if (!isAbstract)
        {
            builder.AppendLine("private uint? __loadingFlags;");
        }

        builder.AppendLine("/// <inheritdoc />");
        builder.AppendLine("[EditorBrowsable(EditorBrowsableState.Never)]");
        builder.Append("public ").ContinueIf(baseHasStormDbObjectAttribute, "new ").ContinueLine(!isAbstract
            ? "uint? __GetLoadingFlags() => __loadingFlags;"
            : "uint? __GetLoadingFlags() => null;");
        builder.NewLine();

        // __GetColumnValues
        builder.AppendLine("/// <inheritdoc />");
        builder.AppendLine("[EditorBrowsable(EditorBrowsableState.Never)]");
        builder.Append("public ").ContinueIf(baseHasStormDbObjectAttribute, "new ").ContinueLine("(StormColumnDef column, object? value)[] __GetColumnValues()");
        builder.OpenBracket();
        if (!typeSpec.TypeSymbol.IsAbstract)
        {
            builder.Append("var columnDefs = ").Continue(typeSpec.TypeSymbol.Name).ContinueLine("StormController.__columnDefs;").NewLine();
            builder.AppendLine("return [");

            var idx = 0;
            foreach (var p in propertyGenSpecList)
            {
                if (p.SaveAs != DupSaveAs.FlatObject)
                {
                    builder.AppendIndentation().Continue("(columnDefs[")
                        .Continue(idx++.ToString(CultureInfo.InvariantCulture)).Continue("], ");
                    AppendValueString(builder, p, null, false);
                    builder.ContinueLine("),");
                }
                else
                {
                    Debug.Assert(p.TypeGenerationSpec?.PropertyGenSpecList is not null);

                    foreach (var subP in p.TypeGenerationSpec!.PropertyGenSpecList!)
                    {
                        builder.AppendIndentation().Continue("(columnDefs[")
                            .Continue(idx++.ToString(CultureInfo.InvariantCulture)).Continue("], ");
                        AppendValueString(builder, p, subP, false);
                        builder.ContinueLine("),");
                    }
                }
            }

            if (propertyGenSpecList.Count > 0)
                builder.Rollback(builder.GetNewLineLength() + 1).NewLine();
            builder.AppendLine("];");
        }
        else
        {
            builder.AppendLine("throw new NotImplementedException();");
        }

        builder.CloseBracket().NewLine();
    }

    private static void GenerateKeyComparer(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public bool KeyEquals(").Continue(typeSpec.TypeSymbol.Name).Continue(" other) => ");

        var expr = string.Join(" && ", propertyGenSpecList
            .Where(x => x.IsKey)
            .Select(x => $"{x.PropertyName} == other.{x.PropertyName}"));
        builder.Continue(expr).ContinueLine(";").NewLine();
    }

    private static void GenerateGetKeyValueMethod(OrmGeneratorContext ormContext, List<PropertyGenerationSpec> propertyGenSpecList, bool baseHasKeys)
    {
        var builder = ormContext.Builder;

        var tuple = string.Join(", ", propertyGenSpecList
            .Where(p => p.IsKey)
            .Select(p => p.PropertyName));

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public ").ContinueIf(baseHasKeys, "new ").Continue("object __GetKeyValue() => (").Continue(tuple).ContinueLine(");").NewLine();
    }

    private static void GenerateSetAutoIncValueMethod(OrmGeneratorContext ormContext, List<PropertyGenerationSpec> propertyGenSpecList,
        bool baseHasStormDbObjectAttribute)
    {
        var builder = ormContext.Builder;

        builder.NewLine();
        builder.AppendLine("/// <inheritdoc />");
        builder.AppendIf(baseHasStormDbObjectAttribute, "new ").Append("public void __SetAutoIncValue(StormDbDataReader dr, int idx)");
        builder.OpenBracket();

        var autoIncColumn = propertyGenSpecList.Find(x => (x.ColumnType & DupColumnType.AutoIncrement) != DupColumnType.Default);
        if (autoIncColumn is not null)
        {
            builder.Append(autoIncColumn.PropertyName).Continue(" = ");
            GenerateReadFromDbReader(ormContext, autoIncColumn);
            builder.ContinueLine(";");
        }

        builder.CloseBracket();
        builder.NewLine();
    }

    private static void GenerateAddDetailRowMethod(OrmGeneratorContext ormContext, List<PropertyGenerationSpec> propertyGenSpecList, bool baseHasStormDbObjectAttribute)
    {
        var builder = ormContext.Builder;

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public ").ContinueIf(baseHasStormDbObjectAttribute, "new ").ContinueLine("void __AddDetailRow(StormColumnDef column, object row)");
        builder.OpenBracket();

        builder.SavePosition();

        builder.Append("switch (column.PropertyName)");
        builder.OpenBracket();

        var savedPosition = builder.Length;
        foreach (var p in propertyGenSpecList.Where(x => x.SaveAs == DupSaveAs.DetailTable))
        {
            Debug.Assert(p.GetListItemTypeFullName() is not null);

            builder.Append("case nameof(").Continue(p.PropertyName).ContinueLine("):");
            builder.AppendIndentation().Continue(p.PropertyName).ContinueLine(" ??= new(); ");
            builder.AppendIndentation().Continue(p.PropertyName).Continue(".Add((").Continue(p.GetListItemTypeFullName()!).ContinueLine(")row);");
            builder.AppendIndentation().ContinueLine("break;");
        }

        if (builder.Length == savedPosition) // no changes, restore to checkpoint
        {
            builder.RestorePosition();
            builder.AppendComment("No master/detail properties");
        }
        else
        {
            builder.CloseBracket();
        }

        builder.CloseBracket();
        builder.NewLine();
    }

    private static void GenerateCreateDetailRowMethod(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override object CreateDetailRow(StormColumnDef column, StormDbDataReader dr, ref int idx)");
        builder.OpenBracket();
        builder.Append("return column.PropertyName switch");
        builder.OpenBracket();

        foreach (var p in propertyGenSpecList.Where(x => x.SaveAs == DupSaveAs.DetailTable))
        {
            builder.Append("nameof(").Continue(typeSpec.TypeSymbol.Name).Continue(".").Continue(p.PropertyName).Continue(") => ");

            Debug.Assert(p.ListItemTypeSymbol is not null);
            Debug.Assert(p.ListItemKind is not null);

            if (p.ListItemKind is ClassKind.KnownType or ClassKind.Enum or ClassKind.DomainPrimitive or ClassKind.SqlRowVersion or ClassKind.SqlLogSequenceNumber)
            {
                var readSpec = new ReadGenerationSpec(p.Property, p.Property.Type, p.DbType, DupSaveAs.Default, false, p.ListItemKind.Value, p.DbStorageTypeSymbol, null,
                    null);
                GenerateReadAsXxxMethodName(ormContext, readSpec);
                builder.ContinueLine(",");
            }
            else
            {
                builder.Continue("new ").Continue(p.ListItemTypeSymbol!.ToDisplayString()).ContinueLine("(dr, uint.MaxValue, ref idx),");
            }
        }

        builder.AppendLine("_ => throw new StormException($\"'{column.PropertyName}' is not a details list\")");

        builder.CloseBracketWithSemiColon();
        builder.CloseBracket();
        builder.NewLine();
    }

    private static void GenerateControllerGetKeyValueMethod(OrmGeneratorContext ormContext, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;

        builder.NewLine();
        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override object __ReadKeyValue(StormDbDataReader dr, ref int idx)");
        builder.OpenBracket();

        foreach (var propSpec in propertyGenSpecList.Where(x => x.IsKey))
        {
            builder.Append("var ").Continue(propSpec.PropertyName.ToCamelCase()).Continue(" = ");
            GenerateReadFromDbReader(ormContext, propSpec);
            builder.ContinueLine(";");
        }

        var tuple = string.Join(", ", propertyGenSpecList
            .Where(x => x.IsKey)
            .Select(x => x.PropertyName.ToCamelCase()));
        builder.Append("return (").Continue(tuple).ContinueLine("); ");
        builder.CloseBracket();
    }

    private static void GenerateControllerAutoIncMethods(OrmGeneratorContext ormContext, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;

        var autoIncColumnIndex = propertyGenSpecList.FindIndex(x => (x.ColumnType & DupColumnType.AutoIncrement) != DupColumnType.Default);

        builder.AppendLine("/// <inheritdoc />");
        builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        builder.Append("public override StormColumnDef? __GetAutoIncColumn() => ");
        if (autoIncColumnIndex >= 0)
            builder.Continue("__columnDefs[").Continue(autoIncColumnIndex.ToString(CultureInfo.InvariantCulture)).ContinueLine("];");
        else
            builder.ContinueLine("null;");
        builder.NewLine();
    }

    private static void GenerateControllerClass(OrmGeneratorContext ormContext, TypeDeclarationSyntax classSyntax,
        TypeGenerationSpec typeSpec, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;

        var className = typeSpec.TypeSymbol.Name + "StormController";
        var modifiers = classSyntax.GetAccessibility();

        builder.AppendLine("/// <summary>");
        for (var i = 0; i < typeSpec.ObjectVariants.Count; i++)
        {
            var objectVariant = typeSpec.ObjectVariants[i];
            builder.AppendLines("/// ",
                $"{i.ToString(CultureInfo.InvariantCulture)}. StormController for the {typeSpec.TypeSymbol.Name} ({objectVariant.BindObjectData.SchemaName}.{objectVariant.DisplayName})");
        }

        builder.AppendLine("/// </summary>");

        for (var variant = 0; variant < typeSpec.ObjectVariants.Count; variant++)
        {
            var objectVariant = typeSpec.ObjectVariants[variant];
            var bind = objectVariant.BindObjectData;
            var objectType = bind.ObjectType.ToString();

            builder.Append("[StormController(").Continue(bind.SchemaName is null ? "default" : bind.SchemaName.QuoteName('"')).Continue(", ");

            if (bind.ObjectType == DupDbObjectType.VirtualView)
            {
                builder.Append("VirtualViewSql").Continue(variant.ToString(CultureInfo.InvariantCulture));
            }
            else
            if (bind.ObjectType == DupDbObjectType.CustomSqlStatement)
            {
                builder.Continue("\"\"");
            }
            else
            {
                builder.Continue(objectVariant.ObjectName.QuoteName('"'));
            }

            builder
                .Continue(", typeof(")
                .Continue(typeSpec.TypeSymbol.Name).Continue("), ")
                .Continue(variant.ToString(CultureInfo.InvariantCulture)).Continue(", DbObjectType.")
                .Continue(objectType).ContinueLine(")]");
        }

        builder.AppendClass(false, modifiers + " sealed", className, "StormControllerBase");

        builder.Append("public ").Continue(className)
            .ContinueLine("(string? schemaName, string objectName, DbObjectType objectType) : base(schemaName, objectName, objectType)")
            .OpenBracket()
            .CloseBracket()
            .NewLine();

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override Type StormContext => typeof(")
            .Continue(typeSpec.ObjectVariants[0].BindObjectData.ContextTypeName).ContinueLine(");").NewLine();

        for (var variant = 0; variant < typeSpec.ObjectVariants.Count; variant++)
        {
            var objectVariant = typeSpec.ObjectVariants[variant];
            if (objectVariant.BindObjectData.ObjectType == DupDbObjectType.VirtualView)
            {
                builder.Append("internal const string VirtualViewSql").Continue(variant.ToString(CultureInfo.InvariantCulture)).AppendLine(" =");
                builder.IncreaseIndentations().AppendLine("\"\"\"");
                builder.AppendLines(objectVariant.VirtualViewSql);
                builder.AppendLine("\"\"\";").DecreaseIndentations();

                builder.NewLine();
            }
        }

        GenerateModuleInitializer(ormContext, typeSpec);

        GenerateCreateMethod(ormContext, typeSpec);

        GenerateReadSingleScalarValueMethod(ormContext, typeSpec, propertyGenSpecList);

        GenerateCreateDetailRowMethod(ormContext, typeSpec, propertyGenSpecList);

        GenerateColumnDefs(ormContext, typeSpec, propertyGenSpecList);

        var hasKeys = HasKeys(propertyGenSpecList);
        if (hasKeys)
        {
            GenerateControllerGetKeyValueMethod(ormContext, propertyGenSpecList);
        }

        GenerateControllerAutoIncMethods(ormContext, propertyGenSpecList);

        builder.CloseBracket();
    }

    private static void GenerateModuleInitializer(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec)
    {
        var builder = ormContext.Builder;
        var className = typeSpec.TypeSymbol.Name;
        builder.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
        builder.AppendLine("internal static void Initialize()");
        builder.OpenBracket();

        if (typeSpec.ObjectVariants.Any(x => x.BindObjectData.ObjectType != DupDbObjectType.CustomSqlStatement))
            builder.AppendLine("StormControllerBase ctrl;");

        for (var variant = 0; variant < typeSpec.ObjectVariants.Count; variant++)
        {
            var objectVariant = typeSpec.ObjectVariants[variant];
            var objectType = objectVariant.BindObjectData.ObjectType;

            // new StormController
            var tmpBuilder = new SourceCodeBuilder();
            tmpBuilder.Append("new ").Continue(className).Continue("StormController(")
                .Continue(objectVariant.BindObjectData.SchemaName is { } schemaName ? schemaName.QuoteName('"') : "null").Continue(", ");

            if (objectType == DupDbObjectType.VirtualView)
            {
                tmpBuilder.Continue("VirtualViewSql").Continue(variant.ToString(CultureInfo.InvariantCulture));
            }
            else
            if (objectType == DupDbObjectType.CustomSqlStatement)
            {
                tmpBuilder.Continue("string.Empty");
            }
            else
            {
                tmpBuilder.Continue(objectVariant.ObjectName.QuoteName('"'));
            }

            tmpBuilder.Continue(", DbObjectType.").Continue(objectType.ToString()).Continue(")");
            //

            if (objectType != DupDbObjectType.CustomSqlStatement)
            {
                builder.Append("ctrl = ").Continue(tmpBuilder.ToString()).ContinueLine(";");
                builder.Append("StormControllerCache.Add(typeof(").Continue(className).Continue("), ").Continue(variant.ToString(CultureInfo.InvariantCulture))
                    .ContinueLine(", ctrl);");
            }
            else
            {
                builder.Append("StormControllerCache.Add(typeof(").Continue(className).Continue("), ").Continue(variant.ToString(CultureInfo.InvariantCulture))
                    .Continue(", () => ").Continue(tmpBuilder.ToString()).ContinueLine(");");
            }
        }

        builder.CloseBracket();
        builder.NewLine();
    }

    private static void GenerateStormContextClasses(OrmGeneratorContext ormContext, TypeDeclarationSyntax classSyntax,
        TypeGenerationSpec typeSpec, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var className = typeSpec.TypeSymbol.Name;

        if (typeSpec.ObjectVariants.Count == 1)
        {
            var objectVariant = typeSpec.ObjectVariants[0];

            GenerateStormContextClass(ormContext, classSyntax, className, 0, objectVariant, propertyGenSpecList, typeSpec.IndexObjects);
        }
        else
        {
            for (var variant = 0; variant < typeSpec.ObjectVariants.Count; variant++)
            {
                var objectVariant = typeSpec.ObjectVariants[variant];

                GenerateStormContextClass(ormContext, classSyntax, className, variant + 1, objectVariant, propertyGenSpecList, typeSpec.IndexObjects);
            }
        }
    }

    private static void GenerateStormContextClass(OrmGeneratorContext ormContext, TypeDeclarationSyntax classSyntax,
        string className, int variant,
        ObjectVariant objectVariant, List<PropertyGenerationSpec> propertyGenSpecList, List<IndexObjectData> indexObjects)
    {
        var builder = ormContext.Builder;

        var objectType = objectVariant.BindObjectData.ObjectType;
        var contextName = objectVariant.BindObjectData.ContextTypeName;

        var classNameV = objectVariant.BindObjectData.DisplayName ?? className + (variant > 0 ? variant.ToString(CultureInfo.InvariantCulture) : "");

        var modifiers = classSyntax.GetAccessibility();

        string baseParams;
        if (variant > 0)
        {
            variant--;
            baseParams = "context, " + variant.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            baseParams = "context, 0";
        }

        var keys = propertyGenSpecList.Where(x => x.IsKey).ToList();
        var keyParams = $"this {contextName} context, " + string.Join(", ", keys.Select(x =>
        {
            if (x.TypeGenerationSpec is null)
                throw new InvalidOperationException("x.TypeGenerationSpec is null");
            return x.TypeSymbol.GetFullName() + " " + x.PropertyName.ToCamelCase();
            //if (x.Kind == ClassKind.DomainPrimitive)
            //{
            //    return x.TypeSymbol.GetFullName() + " " + x.PropertyName.ToCamelCase();
            //}
            //return x.TypeGenerationSpec.TypeFriendlyName + " " + x.PropertyName.ToCamelCase();
        }));
        var keyValues = string.Join(", ", keys.Select(x => x.PropertyName.ToCamelCase()));

        var contextParamWithoutParenthesis = $"(this {contextName} context";

        builder.AppendSummary("StormContext methods");
        builder.AppendClass(false, modifiers + " static partial", contextName + className + "Ext");

        var selGenerics = $"<{className}, {className}.OrderBy, {className}.PartialLoadFlags>";
        var updGenerics = $"<{className}>";
        var isTvf = objectType == DupDbObjectType.TableValuedFunction;
        var isCustomSql = objectType == DupDbObjectType.CustomSqlStatement;

        var outputClassBuilder = new SourceCodeBuilder(1);

        if (objectType is DupDbObjectType.StoredProcedure or DupDbObjectType.TableValuedFunction)
        {
            var outputClassName = "Execute" + objectVariant.DisplayName + "Output";
            var methodName = (isTvf ? " SelectFrom" : " Execute") + objectVariant.DisplayName;
            var classWithGenerics = isTvf ? "SelectFrom" + selGenerics : $"ExecuteFrom<{className}, {outputClassName}>";

            builder.AppendSummary(isTvf ? "Select row(s) from <c>Table Valued Function</c>" : "Select row(s) from <c>Stored Procedure</c>");
            builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            builder.Append("public static I").Continue(classWithGenerics).Continue(methodName).Continue("(this ");
            builder.Continue(contextName).Continue(" context");

            var parameters = objectVariant.BindObjectData.Parameters;

            if (parameters is not null)
            {
                foreach (var p in parameters)
                {
                    builder.Continue(", ").Continue(p.GetTypeAlias()).Continue(" ").Continue(p.ParameterName);
                }
            }

            builder.ContinueLine(")");
            builder.OpenBracket();

            string callParamsStr;
            if (parameters is null || parameters.Count == 0)
            {
                callParamsStr = !isTvf ? "[new(\"ReturnValue\", UnifiedDbType.Int32, 0, 0, 0, 0, ParameterDirection.ReturnValue)]" : "[]";
            }
            else
            {
                callParamsStr = "callParams";

                builder.Append("var callParams = new List<StormCallParameter>(").Continue(parameters.Count.ToString(CultureInfo.InvariantCulture)).ContinueLine(")");
                builder.OpenBracket();
                if (!isTvf)
                    builder.ContinueLine("new(\"ReturnValue\", UnifiedDbType.Int32, 0, 0, 0, 0, ParameterDirection.ReturnValue),");

                for (var i = 0; i < parameters.Count; i++)
                {
                    var p = parameters[i];
                    builder.Append("new(\"")
                        .Continue(p.SqlParameterName).Continue("\", UnifiedDbType.").Continue(p.DbType.ToString())
                        .Continue(", ") //db type
                        .Continue(p.ParameterName).Continue(", ") // value
                        .Continue(p.Size.ToString(CultureInfo.InvariantCulture)).Continue(", ") // size
                        .Continue(p.Precision.ToString(CultureInfo.InvariantCulture)).Continue(", ") // precision
                        .Continue(p.Scale.ToString(CultureInfo.InvariantCulture)).Continue(", ") // scale
                        .Continue(nameof(ParameterDirection)).Continue(".")
                        .Continue(p.Direction.ToString()) // direction
                        .Continue(")");

                    if (i != parameters.Count - 1)
                        builder.ContinueLine(",");
                }

                builder.NewLine();
                builder.CloseBracketWithSemiColon().NewLine();
            }

            builder.Append("return StormCrudFactory.").Continue(classWithGenerics).Continue("(").Continue(baseParams).Continue(", ").Continue(callParamsStr)
                .ContinueIf(!isTvf, ", OutputWriter").ContinueLine(");");

            if (!isTvf)
            {
                builder.NewLine();
                builder.Append("static void OutputWriter(int rowsAffected, StormDbParameterCollection parameters, ").Continue(outputClassName).ContinueLine(" output)");
                builder.OpenBracket();

                builder.AppendLine("output.RowsAffected = rowsAffected;");
                builder.AppendLine("output.ReturnValue = (int)parameters[0].Value;");
                if (parameters is not null)
                {
                    foreach (var p in parameters)
                    {
                        if (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
                        {
                            builder.Append("output.").Continue(p.ParameterName.ToPascalCase()).Continue(" = parameters[").Continue(p.SqlParameterName.QuoteName('"'))
                                .Continue("].Value.GetDbValue<").Continue(p.GetTypeAliasNoDomain()).ContinueLine(">();");
                        }
                    }
                }

                builder.CloseBracket();
            }

            builder.CloseBracket();

            if (!isTvf)
            {
                // Output class
                outputClassBuilder.AppendSummary($"Output class for <see cref={methodName.QuoteName('"')}/>");
                outputClassBuilder.AppendClass(true, modifiers, outputClassName, "StormProcedureResult");
                if (parameters is not null)
                {
                    foreach (var p in parameters)
                    {
                        if (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
                        {
                            outputClassBuilder.Append("public ").Continue(p.GetTypeAlias()).Continue(" ").Continue(p.ParameterName.ToPascalCase())
                                .ContinueLine(" { get; set; }");
                        }
                    }
                }

                outputClassBuilder.CloseBracket();
            }
        }

        if (objectType is DupDbObjectType.Table or DupDbObjectType.View or DupDbObjectType.VirtualView or DupDbObjectType.CustomSqlStatement)
        {
            // ISelectFrom
            builder.AppendSummary("Select row(s)");
            builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            builder.Append("public static ISelectFrom").Continue(selGenerics).Continue(" SelectFrom").Continue(classNameV);

            if (isCustomSql)
                builder.Continue("(this ").Continue(contextName).Continue(" context, string customSqlStatement, List<StormCallParameter>? callParameters = null)");
            else
                builder.Append($"{contextParamWithoutParenthesis})");


            builder.Continue(" => StormCrudFactory.SelectFrom").Continue(selGenerics).Continue("(").Continue(baseParams);
            builder.ContinueLine(isCustomSql ? ", customSqlStatement, callParameters);" : ");");

            if (keys.Count > 0)
            {
                builder.AppendSummary("Select single row by PK");
                builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                builder.Append("public static ISelectFromSingle").Continue(selGenerics).Continue(" SelectFrom").Continue(classNameV).Continue("(").Continue(keyParams);

                if (isCustomSql)
                    builder.Continue(", string customSqlStatement, List<StormCallParameter>? callParameters = null");
                builder.Continue(") => StormCrudFactory.SelectFromSingle").Continue(selGenerics).Continue("(").Continue(baseParams).Continue(", [").Continue(keyValues)
                    .Continue("], 0");
                builder.ContinueLine(isCustomSql ? ", customSqlStatement, callParameters);" : ");");
            }

            for (var i = 0; i < indexObjects.Count; i++)
            {
                var indexObjectData = indexObjects[i];

                builder.AppendSummary(indexObjectData.IsUnique
                    ? "Select single row using unique index"
                    : "Select rows using non-unique index");
                builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");

                var indexColumns = new List<PropertyGenerationSpec>();

                foreach (var indexColumn in indexObjectData.IndexColumns)
                {
                    var prop = propertyGenSpecList.Find(x => x.PropertyName == indexColumn)
                               ?? throw new InvalidOperationException($"Property '{indexColumn}' not found in {className}.");
                    indexColumns.Add(prop);
                }

                var indexParams = $"this {contextName} context, " + string.Join(", ", indexColumns.Select(x =>
                {
                    if (x.TypeGenerationSpec is null)
                        throw new InvalidOperationException("x.TypeGenerationSpec is null");

                    return x.TypeSymbol.GetFullName() + " " + x.PropertyName.ToCamelCase();
                }));

                // Re-order index columns to match property declaration order (as used in __keyColumnDefs).
                // This ensures the array passed to the factory matches the expected column order.
                // Do NOT use indexColumns directly, as its order comes from StormIndex attribute, which may differ.
                var orderedIndexes = propertyGenSpecList.FindAll(x => indexColumns.Contains(x));
                var indexValues = string.Join(", ", orderedIndexes.Select(x => x.PropertyName.ToCamelCase()));

                if (indexObjectData.IsUnique)
                {
                    builder.Append("public static ISelectFromSingle").Continue(selGenerics).Continue(" SelectFrom").Continue(classNameV).Continue("(")
                        .Continue(indexParams);

                    if (isCustomSql)
                        builder.Continue(", string customSqlStatement, List<StormCallParameter>? callParameters = null");
                    builder.Continue(") => StormCrudFactory.SelectFromSingle");
                }
                else
                {
                    builder.Append("public static ISelectFrom").Continue(selGenerics).Continue(" SelectFrom").Continue(classNameV).Continue("(").Continue(indexParams);

                    if (isCustomSql)
                        builder.Continue(", string customSqlStatement, List<StormCallParameter>? callParameters = null");
                    builder.Continue(") => StormCrudFactory.SelectFrom");
                }

                builder.Continue(selGenerics).Continue("(").Continue(baseParams).Continue(", [").Continue(indexValues);
                builder.Continue("], ").Continue((i + 1).ToString(CultureInfo.InvariantCulture));
                builder.ContinueLine(isCustomSql ? ", customSqlStatement, callParameters);" : ");");
            }
        }

        if (objectType is DupDbObjectType.Table or DupDbObjectType.CustomSqlStatement)
        {
            var contextParam = $"{contextParamWithoutParenthesis}{(isCustomSql ? ", string customQuotedObjectFullName" : null)})";

            // IDeleteFrom
            builder.AppendSummary("Delete row(s)");
            builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            builder.Append("public static IDeleteFrom").Continue(updGenerics)
                .Continue(" DeleteFrom").Continue(classNameV).Continue(contextParam)
                .Continue(" => StormCrudFactory.DeleteFrom").Continue(updGenerics).Continue("(")
                .Continue(baseParams)
                .ContinueIf(isCustomSql, ", customQuotedObjectFullName")
                .ContinueLine(");");

            if (keys.Count > 0)
            {
                // Keys
                builder.AppendSummary("Delete row using PK");
                builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                builder.Append("public static IDeleteFromSingle").Continue(updGenerics)
                    .Continue(" DeleteFrom").Continue(classNameV).Continue("(").Continue(keyParams)
                    .ContinueIf(isCustomSql, ", string customQuotedObjectFullName")
                    .Continue(") => StormCrudFactory.DeleteFromSingle").Continue(updGenerics).Continue("(").Continue(baseParams).Continue(", [").Continue(keyValues);

                if (isCustomSql)
                    builder.ContinueLine("], 0, customQuotedObjectFullName);");
                else
                    builder.ContinueLine("], 0);");


                // Value
                builder.AppendSummary("Delete row using 1 value");
                builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                builder.Append("public static IDeleteFromSingle").Continue(updGenerics)
                    .Continue(" DeleteFrom").Continue(classNameV).Continue("(this ").Continue(contextName).Continue(" context, ").Continue(className)
                    .ContinueIf(isCustomSql, " value, string customQuotedObjectFullName) => StormCrudFactory.DeleteFromSingle")
                    .ContinueIf(!isCustomSql, " value) => StormCrudFactory.DeleteFromSingle")
                    .Continue(updGenerics).Continue("(").Continue(baseParams);

                if (isCustomSql)
                    builder.ContinueLine(", value, customQuotedObjectFullName);");
                else
                    builder.ContinueLine(", value);");

                // Values
                builder.AppendSummary("Delete rows using values");
                builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                builder.Append("public static IDeleteFromSingle").Continue(updGenerics)
                    .Continue(" DeleteFrom").Continue(classNameV).Continue("(this ").Continue(contextName).Continue(" context, IEnumerable<").Continue(className)
                    .ContinueIf(isCustomSql, "> values, string customQuotedObjectFullName) => StormCrudFactory.DeleteFromSingle")
                    .ContinueIf(!isCustomSql, "> values) => StormCrudFactory.DeleteFromSingle")
                    .Continue(updGenerics).Continue("(").Continue(baseParams);

                if (isCustomSql)
                    builder.ContinueLine(", values, customQuotedObjectFullName);");
                else
                    builder.ContinueLine(", values);");
            }

            // IInsertInto
            builder.AppendSummary("Insert row(s)");
            builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            builder.Append("public static IInsertInto").Continue(updGenerics)
                .Continue(" InsertInto").Continue(classNameV)
                .Continue(contextParam)
                .Continue(" => StormCrudFactory.InsertInto").Continue(updGenerics).Continue("(")
                .Continue(baseParams)
                .ContinueIf(isCustomSql, ", customQuotedObjectFullName")
                .ContinueLine(");");

            // Checking if we can have UpdateFrom
            if (objectVariant.UpdateMode != DupUpdateMode.NoUpdates && propertyGenSpecList.Exists(static x => !x.IsReadOnly && !x.IsKey))
            {
                // IUpdateFrom
                builder.AppendSummary("Update row(s)");
                builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                builder.Append("public static IUpdateFrom").Continue(updGenerics).Continue(" Update").Continue(classNameV)
                    .Continue(contextParam)
                    .Continue(" => StormCrudFactory.UpdateFrom").Continue(updGenerics).Continue("(").Continue(baseParams)
                    .ContinueIf(isCustomSql, ", customQuotedObjectFullName")
                    .ContinueLine(");");

                if (keys.Count > 0)
                {
                    builder.AppendSummary("Update row using PK");
                    builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    builder.Append("public static IUpdateFromSingle").Continue(updGenerics).Continue(" Update").Continue(classNameV)
                        .Continue("(").Continue(keyParams)
                        .ContinueIf(isCustomSql, ", string customQuotedObjectFullName")
                        .Continue(") => StormCrudFactory.UpdateFromSingle").Continue(updGenerics).Continue("(").Continue(baseParams)
                        .Continue(", [").Continue(keyValues);

                    if (isCustomSql)
                        builder.ContinueLine("], 0, customQuotedObjectFullName);");
                    else
                        builder.ContinueLine("], 0);");
                }

                // IMergeInto
                builder.AppendSummary("Merge row(s)");
                builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                builder.Append("public static IMergeInto").Continue(updGenerics)
                    .Continue(" MergeInto").Continue(classNameV)
                    .Continue(contextParam)
                    .Continue(" => StormCrudFactory.MergeInto").Continue(updGenerics).Continue("(")
                    .Continue(baseParams)
                    .ContinueIf(isCustomSql, ", customQuotedObjectFullName")
                    .ContinueLine(");");
            }

            if (objectVariant.BindObjectData.BulkInsert)
            {
                builder.AppendSummary("Bulk insert rows");
                builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                builder.Append("public static IBulkInsert").Continue(updGenerics)
                    .Continue(" BulkInsertInto").Continue(classNameV)
                    .Continue(contextParamWithoutParenthesis)
                    .Continue(") => StormCrudFactory.BulkInsert").Continue(updGenerics).Continue("(")
                    .Continue(baseParams)
                    .ContinueIf(isCustomSql, ", customQuotedObjectFullName")
                    .ContinueLine(");");
            }
        }

        if (outputClassBuilder.Length > 0)
        {
            builder.NewLine();
            builder.Append(outputClassBuilder.ToString());
        }

        builder.CloseBracket();
    }

    private static void GenerateUsingList(SourceCodeBuilder builder, List<TypeDeclarationSyntax> classes, string[]? additionalUsings = null)
    {
        var usingList = new List<string>(64);
        foreach (var classSyntax in classes)
        {
            var compilationUnitSyntax = classSyntax.GetCompilationUnit();
            usingList.AddRange(compilationUnitSyntax.GetUsings());
        }

        usingList.Add("System");
        usingList.Add("System.Collections.Generic");
        usingList.Add("System.ComponentModel");
        usingList.Add("System.Data");
        usingList.Add("System.Data.Common");
        usingList.Add("System.Linq");
        usingList.Add("System.Linq.Expressions");
        usingList.Add("System.Runtime.CompilerServices");
        usingList.Add("System.Threading");
        usingList.Add("System.Threading.Tasks");
        usingList.Add("AltaSoft.Storm");
        usingList.Add("AltaSoft.Storm.Attributes");
        usingList.Add("AltaSoft.Storm.Crud");
        usingList.Add("AltaSoft.Storm.Interfaces");
        usingList.Add("AltaSoft.Storm.Exceptions");
        usingList.Add("AltaSoft.Storm.Extensions");

        builder.AppendUsings(usingList.Concat(additionalUsings ?? []));
    }

    private static void GenerateCtor(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec, List<PropertyGenerationSpec> propertyGenSpecList,
        bool baseHasStormDbObjectAttribute, bool haveConcurrencyCheck)
    {
        var className = typeSpec.TypeSymbol.Name;

        var builder = ormContext.Builder;

        if (!typeSpec.TypeSymbol.HasParameterlessConstructor())
        {
            // Add parameterless constructor
            builder.AppendSummary("Default constructor.")
                .Append("public ").Continue(className).ContinueLine("()")
                .OpenBracket()
                .CloseBracket()
                .NewLine();
        }

        builder.AppendSummary("Initializes a new instance with the values from the specified DbDataReader and updates the index.");
        builder.AppendLine("/// <param name=\"dr\">The DbDataReader object containing the data.</param>");
        builder.AppendLine("/// <param name=\"partialLoadFlags\">The PartialLoadFlags that used when loading this instance from database.</param>");
        builder.AppendLine("/// <param name=\"idx\">The reference to the index.</param>");
        //builder.Append("[MethodImpl(MethodImplOptions.AggressiveOptimization)]");
        builder.AppendLine("#if NET7_0_OR_GREATER");
        builder.AppendLine("[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");
        builder.AppendLine("#endif");

        var typeSymbol = typeSpec.TypeSymbol;
        var propList = propertyGenSpecList.Where(x =>
            !x.IsReadOnly && x.SaveAs != DupSaveAs.DetailTable &&
            x.Property.ContainingType.Equals(typeSymbol, SymbolEqualityComparer.Default)).ToList();

        builder.Append(typeSymbol.IsSealed ? "internal " : "protected internal ")
            .Continue(className).Continue("(StormDbDataReader dr, uint partialLoadFlags, ref int idx)")
            .ContinueIf(baseHasStormDbObjectAttribute, " : base(dr, partialLoadFlags, ref idx)").NewLine();

        builder.OpenBracket();

        if (!typeSymbol.IsAbstract)
            builder.AppendLine("__loadingFlags = partialLoadFlags;");

        for (var index = 0; index < propList.Count; index++)
        {
            var propSpec = propList[index];
            builder.Append(propSpec.PropertyName).Continue(" = ");
            GenerateReadFromDbReader(ormContext, propSpec);
            builder.ContinueLine(";");

            if ((propSpec.ColumnType & DupColumnType.ConditionalTerminator) != 0)
            {
                builder.Append("if (").Continue(propSpec.PropertyName).ContinueLine(")");
                builder.OpenBracket();
                builder.Append("idx += ").Continue((propList.Count - index - 1).ToString(CultureInfo.InvariantCulture)).ContinueLine(";");
                builder.AppendLine("return;");
                builder.CloseBracket();
            }
        }

        if (haveConcurrencyCheck)
        {
            builder.NewLine();
            foreach (var p in propertyGenSpecList)
            {
                if (p.IsConcurrencyCheck)
                {
                    builder.Append("__saved_").Continue(p.PropertyName).Continue(" = ").Continue(p.PropertyName).ContinueLine(";");
                    continue;
                }

                if (p.SaveAs != DupSaveAs.FlatObject)
                    continue;

                Debug.Assert(p.TypeGenerationSpec?.PropertyGenSpecList is not null);

                foreach (var subP in p.TypeGenerationSpec!.PropertyGenSpecList!)
                {
                    if (subP.IsConcurrencyCheck)
                    {
                        builder.Append("__saved_").Continue(p.PropertyName).Continue("_").Continue(subP.PropertyName).Continue(" = ").Continue(p.PropertyName)
                            .Continue(".").Continue(subP.PropertyName).ContinueLine(";");
                    }
                }
            }
        }
        builder.CloseBracket().NewLine();
    }

    private static void GenerateConcurrencyProps(OrmGeneratorContext ormContext, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;
        foreach (var p in propertyGenSpecList)
        {
            if (p.IsConcurrencyCheck)
            {
                builder.Append("private ").Continue(p.GetTypeAlias()).Continue(" __saved_").Continue(p.PropertyName).ContinueLine(";");
                continue;
            }

            if (p.SaveAs != DupSaveAs.FlatObject)
                continue;

            Debug.Assert(p.TypeGenerationSpec?.PropertyGenSpecList is not null);

            foreach (var subP in p.TypeGenerationSpec!.PropertyGenSpecList!)
            {
                if (subP.IsConcurrencyCheck)
                {
                    builder.Append("private ").Continue(subP.GetTypeAlias()).Continue(" __saved_")
                        .Continue(p.PropertyName).Continue("_").Continue(subP.PropertyName).ContinueLine(";");
                }
            }
        }

        builder.NewLine();
    }

    private static void GenerateReadFromDbReader(OrmGeneratorContext ormContext, ReadGenerationSpec propSpec)
    {
        switch (propSpec.SaveAs)
        {
            case DupSaveAs.Default:
                GenerateReadAsXxxMethodName(ormContext, propSpec);
                break;

            case DupSaveAs.String:
            case DupSaveAs.CompressedString:
                GenerateReadAsStringMethodName(ormContext, propSpec, propSpec.SaveAs == DupSaveAs.CompressedString);
                break;

            case DupSaveAs.Json:
            case DupSaveAs.CompressedJson:
                GenerateReadAsJsonMethodName(ormContext, propSpec, propSpec.SaveAs == DupSaveAs.CompressedJson);
                break;

            case DupSaveAs.Xml:
            case DupSaveAs.CompressedXml:
                GenerateReadAsXmlMethodName(ormContext, propSpec, propSpec.SaveAs == DupSaveAs.CompressedXml);
                break;

            case DupSaveAs.FlatObject:
                GenerateReadAsFlatObjectMethodName(ormContext, propSpec);
                break;

            default:
                throw new InvalidOperationException("Invalid SaveAs value");
        }
    }

    private static void GenerateCreateMethod(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec)
    {
        var builder = ormContext.Builder;

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override IDataBindable Create(StormDbDataReader dr, uint partialLoadFlags, ref int idx)");

        builder.OpenBracket();

        var hasAfterLoadMethod = false;

        var methods = typeSpec.TypeSymbol.GetMembersOfType<IMethodSymbol>()
            .FirstOrDefault(x => string.Equals(x.Name, Constants.AfterLoadMethodName, StringComparison.Ordinal));
        if (methods?.Parameters.Length == 1)
        {
            var p = methods.Parameters[0];

            // ReSharper disable once MergeIntoPattern
            if (p is { IsStatic: false, Type.SpecialType: SpecialType.System_UInt32 })
                hasAfterLoadMethod = true;
        }

        if (hasAfterLoadMethod)
        {
            builder.Append("var result = new ").Continue(typeSpec.TypeSymbol.Name).ContinueLine("(dr, partialLoadFlags, ref idx);");
            builder.AppendLine("result.AfterLoad(partialLoadFlags);");
            builder.AppendLine("return result;");
        }
        else
        {
            builder.Append("return new ").Continue(typeSpec.TypeSymbol.Name).ContinueLine("(dr, partialLoadFlags, ref idx);");
        }

        builder.CloseBracket().NewLine();
    }

    private static void GenerateReadSingleScalarValueMethod(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override object? ReadSingleScalarValue(StormDbDataReader dr, string propertyName, ref int idx)");
        builder.OpenBracket();

        builder.Append("return propertyName switch");
        builder.OpenBracket();

        foreach (var p in propertyGenSpecList.Where(x => x.SaveAs != DupSaveAs.DetailTable))
        {
            builder.Append("nameof(").Continue(typeSpec.TypeSymbol.Name).Continue(".").Continue(p.PropertyName).Continue(") => ");
            GenerateReadFromDbReader(ormContext, p);
            builder.AppendLine(",");
        }

        builder.AppendLine("_ => throw new StormException($\"'{propertyName}' is not a column of the table\")");

        builder.CloseBracketWithSemiColon();
        builder.CloseBracket();
        builder.NewLine();
    }

    private static void GenerateColumnDefs(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;

        var className = typeSpec.TypeSymbol.Name;

        //var keyColumnIds = new List<int>(4);
        var keyColumns = new List<(List<int> columnsIds, string? name)>(4) { (new List<int>(4), "PK") };
        var hasConcurrencyCheck = false;

        builder.AppendSummary("Array of StormColumnDef objects representing the column definitions.");
        builder.AppendLine("internal static readonly StormColumnDef[] __columnDefs =").OpenBracket();

        var id = 0;
        foreach (var p in propertyGenSpecList)
        {
            if (p.SaveAs != DupSaveAs.FlatObject)
            {
                string detailType;
                string detailTableName;
                if (p.SaveAs == DupSaveAs.DetailTable)
                {
                    Debug.Assert(p.GetListItemTypeFullName() is not null);

                    detailType = "typeof(" + p.GetListItemTypeFullName() + ")";
                    detailTableName = p.GetDetailTableName(typeSpec.TypeSymbol.GetFullName());

                    detailTableName = detailTableName.QuoteName('"');
                }
                else
                {
                    detailType = "null";
                    detailTableName = "null";
                }

                builder.Append("new(nameof(")
                    .Continue(className).Continue(".").Continue(p.PropertyName).Continue("), null, \"")
                    .Continue(p.ColumnName).Continue("\", ")
                    .Continue(GetFlags(p))
                    .Continue(", UnifiedDbType.").Continue(p.DbType.ToString()) // db Type
                    .Continue(", ").Continue(p.Size.ToString(CultureInfo.InvariantCulture))
                    .Continue(", ").Continue(p.Precision.ToString(CultureInfo.InvariantCulture))
                    .Continue(", ").Continue(p.Scale.ToString(CultureInfo.InvariantCulture))
                    .Continue(", SaveAs.").Continue(p.SaveAs.ToString())
                    .Continue(", ").Continue(p.PartialLoadFlags.ToString())
                    .Continue(", ").Continue(p.IsNullable || p.SaveAs == DupSaveAs.DetailTable ? "true" : "false")
                    .Continue(", ").Continue(detailType)
                    .Continue(", ").Continue(detailTableName)
                    .Continue(", typeof(").Continue(p.Property.Type.GetFullName()).Continue(")");

                if (p.SaveAs is DupSaveAs.Json or DupSaveAs.Xml or DupSaveAs.CompressedJson or DupSaveAs.CompressedXml)
                    builder.Continue(", typeof(").Continue(p.Property.Type.GetFullName()).Continue(")");
                else
                    builder.Continue(", null");

                builder.ContinueLine("),");

                if (p.IsKey)
                    keyColumns[0].columnsIds.Add(id);
                var indexId = 0;
                foreach (var indexObject in typeSpec.IndexObjects)
                {
                    if (!indexObject.IsUnique || !indexObject.IndexColumns.Contains(p.PropertyName))
                        continue;
                    indexId++;
                    if (keyColumns.Count <= indexId)
                        keyColumns.Add((new List<int>(4), indexObject.IndexName));
                    keyColumns[indexId].columnsIds.Add(id);
                }

                if (p.IsConcurrencyCheck)
                    hasConcurrencyCheck = true;
                id++;
            }
            else
            {
                Debug.Assert(p.TypeGenerationSpec?.PropertyGenSpecList is not null);

                foreach (var subP in p.TypeGenerationSpec!.PropertyGenSpecList!)
                {
                    builder.Append("new(nameof(")
                        .Continue(className).Continue(".").Continue(p.PropertyName)
                        .Continue("), \"").Continue(subP.PropertyName).Continue("\", \"")
                        .Continue(p.ColumnName).Continue(".").Continue(subP.ColumnName).Continue("\", ")
                        .Continue(GetFlags(subP))
                        .Continue(", UnifiedDbType.").Continue(subP.DbType.ToString())
                        .Continue(", ").Continue(subP.Size.ToString(CultureInfo.InvariantCulture))
                        .Continue(", ").Continue(subP.Precision.ToString(CultureInfo.InvariantCulture))
                        .Continue(", ").Continue(subP.Scale.ToString(CultureInfo.InvariantCulture))
                        .Continue(", SaveAs.").Continue(subP.SaveAs.ToString())
                        .Continue(", ").Continue(subP.PartialLoadFlags.ToString())
                        .Continue(", ").Continue(subP.IsNullable ? "true" : "false")
                        .Continue(", null")
                        .Continue(", null")
                        .Continue(", typeof(").Continue(p.Property.Type.GetFullName()).Continue(")");

                    if (p.SaveAs is DupSaveAs.Json or DupSaveAs.Xml or DupSaveAs.CompressedJson or DupSaveAs.CompressedXml)
                        builder.Continue(", typeof(").Continue(p.Property.Type.GetFullName()).Continue(")");
                    else
                        builder.Continue(", null");

                    builder.ContinueLine("),");

                    if (subP.IsKey)
                        keyColumns[0].columnsIds.Add(id);
                    var indexId = 0;
                    for (var i = 0; i < typeSpec.IndexObjects.Count; i++)
                    {
                        var indexObject = typeSpec.IndexObjects[i];
                        if (!indexObject.IsUnique || !indexObject.IndexColumns.Contains(subP.PropertyName))
                            continue;
                        indexId++;
                        if (keyColumns.Count <= indexId)
                            keyColumns.Add((new List<int>(4), indexObject.IndexName));
                        keyColumns[indexId].columnsIds.Add(id);
                    }

                    if (subP.IsConcurrencyCheck)
                        hasConcurrencyCheck = true;
                    id++;
                }
            }
        }

        if (propertyGenSpecList.Count > 0)
            builder.Rollback(builder.GetNewLineLength() + 1).NewLine();

        builder.CloseBracketWithSemiColon().NewLine();

        builder.AppendSummary("Array of StormColumnDef objects representing the key column definitions.");
        builder.AppendLine("internal static readonly StormColumnDef[][] __keyColumnDefs = [");
        builder.IncreaseIndentations();

        for (var i = 0; i < keyColumns.Count; i++)
        {
            var (columnsIds, name) = keyColumns[i];
            builder.Append("[ ").Continue(string.Join(", ", columnsIds.Select(x => $"__columnDefs[{x.ToString(CultureInfo.InvariantCulture)}]"))).Continue(" ]");
            if (i < keyColumns.Count - 1)
                builder.Continue(",");
            if (name is not null)
                builder.Continue(" // ").Continue(name);
            builder.NewLine();
        }

        //foreach (var indexObjectData in typeSpec.IndexObjects.Where(x => x.IsUnique))
        //{
        //    builder.ContinueLine(", ");
        //    builder.Append("[ ").Continue(string.Join(", ", indexObjectData.IndexColumns.Select(x => $"__columnDefs[{x.ToString(CultureInfo.InvariantCulture)}]"))).Continue(" ]"); ;
        //}

        builder.DecreaseIndentations();
        builder.AppendLine("];").NewLine();

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override ").ContinueLine("StormColumnDef[] ColumnDefs => __columnDefs;")
            .NewLine();

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override ").ContinueLine("StormColumnDef[][] KeyColumnDefs => __keyColumnDefs;")
            .NewLine();

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override ").Continue("bool HasConcurrencyCheck => ").Continue(hasConcurrencyCheck ? "true" : "false").Continue(";")
            .NewLine();

        var hasPartial = propertyGenSpecList.Exists(p => p.PartialLoadFlags != 0);

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override ").Continue("uint PartialLoadFlagsAll => ");
        if (hasPartial)
            builder.Continue("(uint)").Continue(className).ContinueLine(".PartialLoadFlags.All;");
        else
            builder.ContinueLine("0;"); // basic

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public override ").Continue("uint PartialLoadFlagsAllWithoutDetails => ");
        if (hasPartial)
            builder.Continue("(uint)").Continue(className).ContinueLine(".PartialLoadFlags.AllExceptDetailTables;");
        else
            builder.ContinueLine("0;"); // basic
    }

    private static void GeneratePartialLoadFlagsEnum(OrmGeneratorContext ormContext,
        List<(string name, uint value, bool isDetailTable)> partialLoadFlags, bool baseHasStormDbObjectAttribute)
    {
        var builder = ormContext.Builder;
        builder.AppendSummary("Represents a set of flags that can be used to specify partial loading options.");

        builder.AppendLine("[Flags]")
            .Append("public ").ContinueIf(baseHasStormDbObjectAttribute, "new ")
            .ContinueLine("enum PartialLoadFlags : uint")
            .OpenBracket()
            .AppendLine("Basic = 0,");

        if (partialLoadFlags.Count == 0)
        {
            builder.Rollback(builder.GetNewLineLength() + 1).NewLine();
        }
        else
        {
            foreach (var (name, value, isDetailTable) in partialLoadFlags)
            {
                builder.Append(name).Continue(" = ").Continue(value.ToString()).Continue(",").ContinueIf(isDetailTable, " // Detail table").NewLine();
            }

            builder.Append("All = Basic | ").Continue(string.Join(" | ", partialLoadFlags.Select(x => x.name))).ContinueLine(",");
            builder.Append("AllExceptDetailTables = Basic");
            var flags = partialLoadFlags.Where(x => !x.isDetailTable).Select(x => x.name).ToArray();
            if (flags.Length > 0)
            {
                builder.Continue(" | ").Continue(string.Join(" | ", flags));
            }
        }

        builder.CloseBracket().NewLine();
    }

    private static void GenerateConcurrencySupport(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec,
        List<PropertyGenerationSpec> propertyGenSpecList, bool baseHasStormDbObjectAttribute)
    {
        var builder = ormContext.Builder;

        builder.NewLine().AppendLine("#region Concurrency Support").NewLine();

        builder.AppendLine("/// <inheritdoc />");
        builder.Append("public ").ContinueIf(baseHasStormDbObjectAttribute, "new ").ContinueLine("(StormColumnDef column, object? value)[] __ConcurrencyColumnValues()");
        builder.OpenBracket();
        builder.Append("var columnDefs = ").Continue(typeSpec.TypeSymbol.Name).ContinueLine("StormController.__columnDefs;").NewLine();

        builder.AppendLine("return [");

        var idx = 0;
        foreach (var p in propertyGenSpecList)
        {
            if (p.SaveAs != DupSaveAs.FlatObject)
            {
                if (p.IsConcurrencyCheck)
                    AddConcurrencyColumn(p, null);
                idx++;
            }
            else
            {
                Debug.Assert(p.TypeGenerationSpec?.PropertyGenSpecList is not null);

                if (p.IsConcurrencyCheck)
                {
                    AddConcurrencyColumn(p, null);
                    idx += p.TypeGenerationSpec!.PropertyGenSpecList!.Count; // Add all sub-properties
                }
                else
                {
                    foreach (var subP in p.TypeGenerationSpec!.PropertyGenSpecList!)
                    {
                        if (subP.IsConcurrencyCheck)
                            AddConcurrencyColumn(p, subP);
                        idx++;
                    }
                }
            }

            continue;

            void AddConcurrencyColumn(PropertyGenerationSpec propSpec, PropertyGenerationSpec? subPropSpec)
            {
                builder.AppendIndentation().Continue("(columnDefs[")
                    .Continue(idx.ToString(CultureInfo.InvariantCulture)).Continue("], ");
                AppendValueString(builder, propSpec, subPropSpec, true);
                builder.ContinueLine("),");
            }
        }

        builder.AppendLine("];");
        builder.CloseBracket().NewLine();

        builder.AppendLine("#endregion Concurrency Support");
    }

    private static void GenerateChangeTrackingSupport(OrmGeneratorContext ormContext, TypeGenerationSpec typeSpec, List<PropertyGenerationSpec> propertyGenSpecList)
    {
        var builder = ormContext.Builder;

        var baseIsChangeTrackable = false;
        if (typeSpec.TypeSymbol.BaseType is not null)
        {
            var baseTypeSpec = ormContext.Parser.GetStormTypeGenerationSpec(typeSpec.TypeSymbol.BaseType);
            baseIsChangeTrackable = baseTypeSpec.UpdateMode() == DupUpdateMode.ChangeTracking;
        }

        builder.NewLine().AppendLine("#region Change Tracking Support").NewLine();

        builder.AppendLine("/// <inheritdoc />");

        if (typeSpec.TypeSymbol.IsAbstract)
        {
            if (!baseIsChangeTrackable)
                builder.AppendLine("public abstract (string propertyName, IChangeTrackable? value)[] __TrackableMembers();");
        }
        else
        {
            string modifier;

            if (baseIsChangeTrackable)
                modifier = "override";
            else if (typeSpec.TypeSymbol.IsSealed)
                modifier = "";
            else
                modifier = "virtual";

            builder.Append("public ").Continue(modifier).Continue(" (string propertyName, IChangeTrackable? value)[] __TrackableMembers()");

            builder.OpenBracket();
            builder.Continue("return [");

            var haveSomething = false;

            foreach (var propertyName in propertyGenSpecList
                         .Where(p => p.IsTrackingList || (p.Kind != ClassKind.List && p.TypeGenerationSpec?.UpdateMode() == DupUpdateMode.ChangeTracking))
                         .Select(p => p.PropertyName))
            {
                haveSomething = true;
                builder.NewLine().AppendIndentation().Continue("(nameof(").Continue(propertyName).Continue("), ").Continue(propertyName).Continue("),");
            }

            if (haveSomething)
                builder.Rollback(1).NewLine();
            builder.AppendLine("];");
            builder.CloseBracket();
        }

        if (!baseIsChangeTrackable)
        {
            var modifier = typeSpec.TypeSymbol.IsSealed ? "private" : "protected";

            builder.Append(modifier).ContinueLine(" bool _isChangeTrackingActive;");
            builder.Append(modifier).ContinueLine(" ChangeTrackingStateMachine? _changeTrackingStateMachine;");
            //builder.Append(modifier).ContinueLine(" void __PropertyChanged(string propertyName, object? value) => _changeTrackingStateMachine?.PropertyChanged(propertyName, value);");

            builder.AppendLine("/// <inheritdoc />");
            builder.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            builder.AppendLine("public bool IsChangeTrackingActive() => _isChangeTrackingActive;");
            builder.AppendLine("/// <inheritdoc />");
            builder.AppendLine(
                "public void StartChangeTracking() { (_changeTrackingStateMachine ??= new(this)).StartChangeTracking(); _isChangeTrackingActive = true; }");
            builder.AppendLine("/// <inheritdoc />");
            builder.AppendLine(
                "public void AcceptChanges(bool stopTracking = true) { _changeTrackingStateMachine?.AcceptChanges(stopTracking); _isChangeTrackingActive = !stopTracking; }");
            builder.AppendLine("/// <inheritdoc />");
            builder.AppendLine("public bool IsDirty() => _changeTrackingStateMachine?.IsDirty() ?? false;");
            builder.AppendLine("/// <inheritdoc />");

            builder.AppendLine(
                "public IReadOnlySet<string> __GetChangedPropertyNames() => _changeTrackingStateMachine is null ? ChangeTrackingStateMachine.EmptyStringSet : _changeTrackingStateMachine.__GetChangedPropertyNames();");
        }

        var typeRef = typeSpec.TypeSymbol;
        foreach (var p in propertyGenSpecList.Where(x =>
                     !x.IsReadOnly && x.SaveAs != DupSaveAs.Ignore && x.Property.ContainingType.Equals(typeRef, SymbolEqualityComparer.Default)))
        {
            var typeFriendlyName = p.GetTypeAlias() + (p.IsNullable ? "?" : "");

            builder.AppendLine(
                $"private void __PropertySet_{p.PropertyName}(ref {typeFriendlyName} newValue, ref {typeFriendlyName} oldValue) {{ if (_isChangeTrackingActive && oldValue != newValue) _changeTrackingStateMachine!.PropertyChanged(\"{p.PropertyName}\", newValue); }}");

        }
        builder.NewLine().AppendLine("#endregion Change Tracking Support");
    }

    private static void GenerateOrderByEnum(OrmGeneratorContext ormContext, List<PropertyGenerationSpec> propertyGenSpecList, bool baseHasStormDbObjectAttribute, bool baseHasKeys)
    {
        var builder = ormContext.Builder;

        builder.AppendSummary("Enum representing different options for ordering data.");
        builder.Append("public ").ContinueIf(baseHasStormDbObjectAttribute, "new ").ContinueLine("enum OrderBy")
            .OpenBracket();

        var isEmpty = true;
        var orderId = 1;
        var orderByKey = new List<string>(2);
        foreach (var p in propertyGenSpecList.Where(x => !x.IsReadOnly && x.SaveAs != DupSaveAs.DetailTable))
        {
            if (p.SaveAs is DupSaveAs.Json or DupSaveAs.Xml or DupSaveAs.CompressedJson or DupSaveAs.CompressedXml or DupSaveAs.CompressedString)
            {
                orderId++;
                continue;
            }

            isEmpty = false;
            if (p.SaveAs != DupSaveAs.FlatObject)
            {
                builder.Append(p.PropertyName).Continue(" = ").Continue(orderId.ToString(CultureInfo.InvariantCulture)).ContinueLine(",");
                builder.Append(p.PropertyName).Continue("_Desc").Continue(" = ").Continue((-orderId).ToString(CultureInfo.InvariantCulture)).ContinueLine(",");
                orderId++;
            }
            else
            {
                Debug.Assert(p.TypeGenerationSpec?.PropertyGenSpecList is not null);

                foreach (var subP in p.TypeGenerationSpec!.PropertyGenSpecList!)
                {
                    builder.Append(p.PropertyName).Continue("_").Continue(subP.PropertyName).Continue(" = ").Continue(orderId.ToString(CultureInfo.InvariantCulture))
                        .ContinueLine(",");
                    builder.Append(p.PropertyName).Continue("_").Continue(subP.PropertyName).Continue("_Desc").Continue(" = ")
                        .Continue((-orderId).ToString(CultureInfo.InvariantCulture)).ContinueLine(",");
                    orderId++;
                }
            }

            if (p.IsKey)
            {
                orderByKey.Add("OrderBy." + p.PropertyName);
            }
        }

        if (!isEmpty)
        {
            builder.Rollback(builder.GetNewLineLength() + 1).NewLine();
        }

        builder.CloseBracket().NewLine();

        builder.AppendSummary("Do not order queried data.");
        builder.Append("public ").ContinueIf(baseHasStormDbObjectAttribute, "new ").ContinueLine("const OrderBy[]? Unordered = default;");
        builder.NewLine();

        if (orderByKey.Count > 0)
        {
            builder.AppendSummary($"Order queried data by the {string.Join(" and ", orderByKey)} columns");
            builder.Append("public ").ContinueIf(baseHasKeys, "new ").Continue("static readonly OrderBy[] OrderByKey = new[] { ")
                .Continue(string.Join(", ", orderByKey)).ContinueLine(" };");
            builder.NewLine();
        }
    }

    private static bool HasKeys(List<PropertyGenerationSpec> props) => props.Exists(p => p.IsKey);

    private static string GetFlags(PropertyGenerationSpec p)
    {
        var flags = new List<string>(4);

        if (p.IsKey)
            flags.Add("Key");

        if (!p.IsReadOnly)
        {
            flags.Add("CanSelect");

            if ((p.ColumnType & (DupColumnType.AutoIncrement | DupColumnType.RowVersion)) == DupColumnType.Default)
            {
                flags.Add("CanInsert");
                if (!p.IsKey)
                    flags.Add("CanUpdate");
            }

            if ((p.ColumnType & DupColumnType.ConcurrencyCheck) != DupColumnType.Default)
                flags.Add("ConcurrencyCheck");
        }

        if ((p.ColumnType & DupColumnType.AutoIncrement) != DupColumnType.Default)
            flags.Add("AutoIncrement");
        if ((p.ColumnType & DupColumnType.RowVersion) != DupColumnType.Default)
            flags.Add("RowVersion");

        return "StormColumnFlags." + (flags.Count > 0 ? string.Join(" | StormColumnFlags.", flags) : "None");
    }

    private static void GenerateReadAsXxxMethodName(OrmGeneratorContext ormContext, ReadGenerationSpec propSpec, string indexStr = "(idx++)")
    {
        InternalGenerate(ormContext, propSpec.DbType, propSpec.IsNullable, propSpec.Kind, propSpec, indexStr);

        return;

        static void InternalGenerate(OrmGeneratorContext ormContext, UnifiedDbType dbType, bool isNullable, ClassKind classKind, ReadGenerationSpec propSpec,
            string indexStr)
        {
            var builder = ormContext.Builder;

            switch (classKind)
            {
                case ClassKind.KnownType:
                case ClassKind.DomainPrimitive:
                case ClassKind.SqlRowVersion:
                case ClassKind.SqlLogSequenceNumber:
                    if (classKind == ClassKind.DomainPrimitive)
                        builder.Continue("(").Continue(propSpec.GetTypeFullName()).ContinueIf(isNullable, "?").Continue(")"); // Typecasting
                    builder.Continue("(").Continue(propSpec.DbStorageTypeSymbol.GetFullName()).ContinueIf(isNullable, "?").Continue(")"); // Typecasting
                    if (dbType == UnifiedDbType.DateTime)
                        builder.Continue("dr.GetLocalDateTime");
                    else
                        builder.Continue("dr.Get").Continue(dbType.ToString());
                    if (isNullable)
                        builder.Continue("OrNull");
                    builder.Continue(indexStr);
                    break;

                case ClassKind.Enum:
                    builder.Continue("(").Continue(propSpec.GetTypeFullName()).ContinueIf(isNullable, "?").Continue(")");
                    InternalGenerate(ormContext, dbType, isNullable, ClassKind.KnownType, propSpec, indexStr);
                    break;

                case ClassKind.Object:
                case ClassKind.List:
                case ClassKind.Dictionary:
                default:
                    throw new InvalidOperationException("Invalid ClassKind. KnownType, Enum, SqlRowVersion, SqlLogSequenceNumber or DomainPrimitive expected");
            }
        }
    }

    private static void GenerateReadAsStringMethodName(OrmGeneratorContext ormContext, ReadGenerationSpec propSpec, bool useCompression)
    {
        var builder = ormContext.Builder;

        var typeAlias = propSpec.GetTypeAlias();

        if (typeAlias == "string")
        {
            if (useCompression)
                builder.Continue("dr.AsStringCompressed");
            else
                builder.Continue("dr.GetString");
            if (propSpec.IsNullable)
                builder.Continue("OrNull");
            builder.Continue("(idx++)");
            return;
        }

        string? enumConverterFullName = null;
        if (propSpec is PropertyGenerationSpec typeSpec)
            enumConverterFullName = typeSpec.TypeGenerationSpec?.EnumConverterFullName;

        if (enumConverterFullName is not null)
        {
            builder.Continue(enumConverterFullName).Continue(".").Continue(Constants.StormStringEnumAttributeFromStringName).Continue("(");
        }
        else
        {
            if (propSpec.Kind == ClassKind.Enum)
                builder.Continue("Enum.Parse<").Continue(typeAlias).Continue(">(");
            else
                builder.Continue(typeAlias).Continue(".Parse(");
        }

        if (useCompression)
            builder.Continue("dr.AsStringCompressed");
        else
            builder.Continue("dr.GetString");
        if (propSpec.IsNullable)
            builder.Continue("OrNull");
        builder.Continue("(idx++)");

        if (enumConverterFullName is not null)
        {
            builder.Continue(")");
        }
        else
        {
            if (propSpec.Kind == ClassKind.Enum)
                builder.Continue(")");
            else
                builder.Continue(", System.Globalization.CultureInfo.InvariantCulture)");
        }
    }

    private static void GenerateReadAsJsonMethodName(OrmGeneratorContext ormContext, ReadGenerationSpec propSpec, bool useCompression)
    {
        var builder = ormContext.Builder;

        builder.Continue("dr.AsJson");
        if (useCompression)
            builder.Continue("Compressed");

        if (propSpec.IsNullable)
        {
            builder.Continue("OrNull");
        }

        builder.Continue("<").Continue(propSpec.GetTypeAlias()).Continue(">").Continue("(idx++)");
    }

    private static void GenerateReadAsXmlMethodName(OrmGeneratorContext ormContext, ReadGenerationSpec propSpec, bool useCompression)
    {
        var builder = ormContext.Builder;

        builder.Continue("dr.AsXml");
        if (useCompression)
            builder.Continue("Compressed");

        if (propSpec.IsNullable)
        {
            builder.Continue("OrNull");
        }

        builder.Continue("<").Continue(propSpec.GetTypeAlias()).Continue(">").Continue("(idx++)");
    }

    private static void GenerateReadAsFlatObjectMethodName(OrmGeneratorContext ormContext, ReadGenerationSpec propSpec)
    {
        var builder = ormContext.Builder;

        if (propSpec.IsNullable)
        {
            ormContext.Context.ReportDiagnostic("AL0002", "Invalid use of SaveAs property",
                $"SaveAs.{nameof(DupSaveAs.FlatObject)} is specified for nullable object property. Please specify SaveAs.{nameof(DupSaveAs.Json)}, SaveAs.{nameof(DupSaveAs.Xml)}, SaveAs.{nameof(DupSaveAs.CompressedJson)}, SaveAs.{nameof(DupSaveAs.CompressedXml)} or make property not nullable",
                DiagnosticSeverity.Error, propSpec.Symbol.Locations.FirstOrDefault());
        }

        builder.Continue("dr.AsFlatObject");

        builder.Continue("<").Continue(propSpec.GetTypeAlias()).Continue(">").Continue("(ref idx)");
    }

    private static bool HasConcurrencyCheck(List<PropertyGenerationSpec> propertyGenSpecList)
    {
        foreach (var p in propertyGenSpecList)
        {
            if (p.SaveAs != DupSaveAs.FlatObject)
            {
                if (p.IsConcurrencyCheck)
                    return true;
            }
            else if (p.TypeGenerationSpec?.PropertyGenSpecList is not null && p.TypeGenerationSpec.PropertyGenSpecList.Exists(subP => subP.IsConcurrencyCheck))
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendValueString(SourceCodeBuilder builder, PropertyGenerationSpec propSpec, PropertyGenerationSpec? subPropSpec, bool isSavedField)
    {
        var p = subPropSpec ?? propSpec;

        var dbValueTypeName = p.DbType.ToDotNetTypeName();

        if (dbValueTypeName is not null && !string.Equals(dbValueTypeName, p.GetTypeAlias(), StringComparison.Ordinal)
                                        && p.SaveAs != DupSaveAs.DetailTable && p.SaveAs != DupSaveAs.String && p.SaveAs != DupSaveAs.CompressedString
                                        && p.SaveAs != DupSaveAs.CompressedJson && p.SaveAs != DupSaveAs.CompressedXml)
        {
            builder.Continue("(").Continue(dbValueTypeName).ContinueIf(p.IsNullable, "?").Continue(")");
        }

        var propertyNamePrefix = isSavedField ? "__saved_" : "";

        var toString = (p.SaveAs is (DupSaveAs.String or DupSaveAs.CompressedString) && p.TypeGenerationSpec?.TypeFullName != "string");

        var enumConverterFullName = p.TypeGenerationSpec?.EnumConverterFullName;
        if (toString && enumConverterFullName is not null)
        {
            builder.Continue(enumConverterFullName).Continue(".").Continue(Constants.StormStringEnumAttributeToStringName).Continue("(");
        }

        if (subPropSpec is null)
            builder.Continue(propertyNamePrefix).Continue(propSpec.PropertyName);
        else
            builder.Continue(propertyNamePrefix).Continue(propSpec.PropertyName).Continue(isSavedField ? "_" : ".").Continue(subPropSpec.PropertyName);

        if (toString)
        {
            builder.Continue(enumConverterFullName is not null ? ")" : ".ToString()");
        }
    }
}

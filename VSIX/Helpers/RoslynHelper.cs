#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Generator.Common;
using AltaSoft.Storm.Models;
using Community.VisualStudio.Toolkit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;

namespace AltaSoft.Storm.Helpers;

internal static class RoslynHelper
{
    public static async Task<StormTypeDefList> GetStormTypesAsync(Action<string, int, int> onProgress, CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var result = new StormTypeDefList();

        onProgress("Getting active project", -1, 100);

        var dteProject = ProjectHelpers.GetActiveProject();
        if (dteProject is null)
        {
            onProgress("Active project not found", 0, 100);
            return result;
        }
        var dteProjectPath = dteProject.FullName;

        onProgress($"Current project is {dteProjectPath}", -1, 100);

        onProgress("Getting project from solution", -1, 100);
        var workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();

        // Find the corresponding project in the Roslyn workspace
        var currentProject = workspace.CurrentSolution.Projects.FirstOrDefault(p => string.Equals(p.FilePath, dteProjectPath, StringComparison.OrdinalIgnoreCase));
        if (currentProject is null)
        {
            onProgress("Current project not found in solution", 0, 100);
            return result;
        }

        //.Properties.Item("TargetFrameworkMoniker")
        //var workspace = await StormPackage.PackageInstance.GetServiceAsync<Microsoft.CodeAnalysis.Workspace, Microsoft.CodeAnalysis.Workspace>();

        onProgress("Getting compilation", -1, 100);

        //var compilation = await currentProject.GetCompilationAsync(cancellationToken);

        var parser = new Parser(null);

        var step = 0;
        var numberOfSteps = currentProject.DocumentIds.Count;

        onProgress("Processing documents", -1, 100);

        foreach (var document in currentProject.Documents)
        {
            onProgress(document.Name, step++, numberOfSteps);

            if (!document.SupportsSyntaxTree || !document.SupportsSemanticModel)
            {
                onProgress($"{document.Name} skipped. Supports syntax tree: {document.SupportsSyntaxTree}, Supports semantic model: {document.SupportsSemanticModel}", step, numberOfSteps);
                continue;
            }

            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot is null)
            {
                onProgress($"{document.Name} skipped. Syntax root is null", step, numberOfSteps);
                continue;
            }

            SemanticModel? semanticModel = null;

            foreach (var typeDecl in syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword)) // Our type  will be partial
                    continue;

                if (typeDecl.Modifiers.Any(SyntaxKind.AbstractKeyword))
                    continue;

                if (!typeDecl.AttributeLists.SelectMany(attrList => attrList.Attributes).Any()) // No attributes
                    continue;

                semanticModel ??= await document.GetSemanticModelAsync(cancellationToken);

                if (semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken) is not { } typeSymbol)
                    continue;

                if (!typeSymbol.HasStormDbObjectAttribute())
                    continue;

                var typeSpec = parser.GetStormTypeGenerationSpec(typeSymbol);

                if (typeSpec.PropertyGenSpecList is null)
                    continue;

                var properties = new ObservableCollection<StormPropertyDef>(typeSpec.PropertyGenSpecList
                    .Where(p => (p.BindColumnData.SaveAs ?? p.SaveAs) != DupSaveAs.Ignore)
                    .Select(p =>
                    {
                        var prop = new StormPropertyDef(false, p, typeSpec);

                        var saveAs = prop.BindColumnData.SaveAs ?? prop.PropertyGenSpec.SaveAs;

                        if (saveAs is not (DupSaveAs.FlatObject or DupSaveAs.DetailTable))
                            return prop;

                        if (prop.PropertyGenSpec.TypeGenerationSpec?.PropertyGenSpecList is { } spec)
                        {
                            var props = spec.Select(p2 =>
                                new StormPropertyDef(false, p2, typeSpec, saveAs == DupSaveAs.FlatObject ? p.ColumnName + '.' : ""));

                            if (saveAs == DupSaveAs.DetailTable)
                            {
                                // Add key properties
                                var keys = typeSpec.PropertyGenSpecList
                                    .Where(key => (key.BindColumnData.ColumnType ?? key.ColumnType).IsKey())
                                    .Select(key => new StormPropertyDef(true, key, typeSpec));

                                prop.Details = new ObservableCollection<StormPropertyDef>(keys.Concat(props));
                            }
                            else // FlatObject
                            {
                                prop.Details = new ObservableCollection<StormPropertyDef>(props);
                            }
                        }
                        else // Primitive
                        {
                            if (saveAs != DupSaveAs.DetailTable)
                                return prop;

                            // Add key properties
                            var keys = typeSpec.PropertyGenSpecList
                                .Where(key => (key.BindColumnData.ColumnType ?? key.ColumnType).IsKey())
                                .Select(key => new StormPropertyDef(true, key, typeSpec));

                            var props = new[] { new StormPropertyDef(false, prop.PropertyGenSpec, typeSpec, false) };

                            prop.Details = new ObservableCollection<StormPropertyDef>(keys.Concat(props));
                        }

                        return prop;
                    }));

                var type = new StormTypeDef(typeSymbol, typeSpec.ObjectVariants[0].BindObjectData, properties); //TODO [0] variant

                foreach (var prop in properties)
                {
                    prop.ParentStormType = type;
                    if (prop.Details is null)
                        continue;

                    foreach (var detProp in prop.Details)
                    {
                        detProp.ParentStormType = type;
                    }
                }
                result.Add(type);
            }
        }

        onProgress("Done.", 100, 100);
        return result;
    }
}

using AltaSoft.Storm.Generator.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AltaSoft.Storm.Generator;

/// <summary>
/// Generator class that implements the IIncrementalGenerator interface.
/// </summary>
/// <remarks>
/// This class is responsible for generating code based on the provided syntax and semantic information.
/// It initializes the generator and registers the source output for code generation.
/// It also handles exceptions that occur during code generation and reports them as diagnostics.
/// </remarks>
[Generator]
public sealed class StormGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator with the provided context.
    /// </summary>
    /// <param name="context">The incremental generator initialization context.</param>
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        //System.Diagnostics.Debugger.Launch();
#endif
        var classes = context.SyntaxProvider
            .ForAttributeWithMetadataName(Constants.StormDbObjectAttributeFullName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, _) => GetSemanticTargetForGenerationClass(ctx))
            .Where(x => x is not null);

        var classes2 = context.SyntaxProvider
            .ForAttributeWithMetadataName(Constants.StormTrackableObjectAttributeFullName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, _) => GetSemanticTargetForGenerationClass(ctx))
            .Where(x => x is not null);

        var scalarFuncMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(Constants.StormFunctionAttributeFullName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, _) => GetSemanticTargetForGenerationMethod(ctx))
            .Where(x => x is not null);

        var execProcMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(Constants.StormProcedureAttributeFullName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, _) => GetSemanticTargetForGenerationMethod(ctx))
            .Where(x => x is not null);

        var all = classes.Collect().Combine(classes2.Collect()).Combine(scalarFuncMethods.Collect().Combine(execProcMethods.Collect()));

        var compilationProvider = context.CompilationProvider;

        var allData = all.Combine(compilationProvider);

        context.RegisterSourceOutput(allData, (spc, pair) =>
            Executor.Execute(spc, pair.Left.Left.Left, pair.Left.Left.Right,
                pair.Left.Right.Left, pair.Left.Right.Right, pair.Right));
    }

    /// <summary>
    /// Retrieves the semantic target for code generation based on the provided syntax context.
    /// </summary>
    /// <param name="context">The generator syntax context.</param>
    /// <returns>The class declaration syntax if the class symbol is a data bindable named type symbol; otherwise, null.</returns>
    private static TypeDeclarationSyntax GetSemanticTargetForGenerationClass(GeneratorAttributeSyntaxContext context)
    {
        return (TypeDeclarationSyntax)context.TargetNode;
    }

    /// <summary>
    /// Retrieves the semantic target for code generation based on the provided syntax context.
    /// </summary>
    /// <param name="context">The generator syntax context.</param>
    /// <returns>The class declaration syntax if the class symbol is a data bindable named type symbol; otherwise, null.</returns>
    private static MethodDeclarationSyntax GetSemanticTargetForGenerationMethod(GeneratorAttributeSyntaxContext context)
    {
        return (MethodDeclarationSyntax)context.TargetNode;
    }
}

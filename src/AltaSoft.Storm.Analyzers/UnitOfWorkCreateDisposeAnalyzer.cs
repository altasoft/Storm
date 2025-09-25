//using System.Collections.Immutable;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Diagnostics;
//using System.Linq;

//namespace AltaSoft.Storm.Analyzers;

//[DiagnosticAnalyzer(LanguageNames.CSharp)]
//public class UnitOfWorkCreateDisposeAnalyzer : DiagnosticAnalyzer
//{
//    public const string DiagnosticId = "ASUOW001";
//    private static readonly LocalizableString Title = "UnitOfWork.Create() must be disposed";
//    private static readonly LocalizableString MessageFormat = "The result of 'UnitOfWork.Create()' must be disposed using a 'using' statement or by calling Dispose()";
//    private static readonly LocalizableString Description = "To ensure proper resource management, always dispose the result of UnitOfWork.Create() using a 'using' statement or by calling Dispose().";
//    private const string Category = "Usage";

//    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
//        DiagnosticId, Title, MessageFormat, Category,
//        DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

//    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

//    public override void Initialize(AnalysisContext context)
//    {
//        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
//        context.EnableConcurrentExecution();
//        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
//    }

//    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
//    {
//        var invocation = (InvocationExpressionSyntax)context.Node;

//        // Check if the method is UnitOfWork.Create
//        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
//        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
//        if (methodSymbol == null)
//            return;

//        if (methodSymbol.Name != "Create" || methodSymbol.Parameters.Length != 0)
//            return;

//        var containingType = methodSymbol.ContainingType;
//        if (containingType == null || containingType.Name != "UnitOfWork" || containingType.ContainingNamespace.ToDisplayString() != "AltaSoft.Storm")
//            return;

//        // Check if the invocation is used in a using declaration or statement
//        SyntaxNode? node = invocation;
//        while (node != null)
//        {
//            // Case 1: using var tx = UnitOfWork.Create();
//            if (node is LocalDeclarationStatementSyntax localDecl)
//            {
//                if (localDecl.UsingKeyword != default)
//                    return; // Correct usage
//            }
//            // Case 2: using (var tx = UnitOfWork.Create()) { ... }
//            if (node is UsingStatementSyntax usingStmt)
//            {
//                return; // Correct usage
//            }
//            // Case 3: discard assignment (using var _ = ...)
//            if (node is VariableDeclaratorSyntax varDecl && varDecl.Identifier.Text == "_")
//            {
//                return; // Discard, ignore
//            }
//            node = node.Parent;
//        }

//        // If not used in a using, check for explicit Dispose() call on the result variable
//        // Only check for simple assignments: var tx = UnitOfWork.Create();
//        if (invocation.Parent is EqualsValueClauseSyntax equalsValue &&
//            equalsValue.Parent is VariableDeclaratorSyntax varDeclarator &&
//            varDeclarator.Parent is VariableDeclarationSyntax varDecl2 &&
//            varDecl2.Parent is LocalDeclarationStatementSyntax localDecl2)
//        {
//            var variableName = varDeclarator.Identifier.Text;
//            var methodBody = localDecl2.Parent as BlockSyntax;
//            if (methodBody == null)
//            {
//                // Try to find the containing block
//                methodBody = localDecl2.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
//            }
//            if (methodBody != null)
//            {
//                // Look for tx.Dispose() in the block
//                var disposeCalled = methodBody.DescendantNodes()
//                    .OfType<InvocationExpressionSyntax>()
//                    .Any(inv =>
//                    {
//                        if (inv.Expression is MemberAccessExpressionSyntax memberAccess)
//                        {
//                            return memberAccess.Expression is IdentifierNameSyntax id &&
//                                   id.Identifier.Text == variableName &&
//                                   memberAccess.Name.Identifier.Text == "Dispose";
//                        }
//                        return false;
//                    });
//                if (disposeCalled)
//                    return; // Explicit Dispose() found
//            }
//        }

//        // If we get here, the result is not disposed
//        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
//    }
//}

//using System.Collections.Immutable;
//using System.Linq;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Diagnostics;

//namespace AltaSoft.Storm.Analyzers;

//[DiagnosticAnalyzer(LanguageNames.CSharp)]
//public class UnitOfWorkBeginAsyncAwaitUsingAnalyzer : DiagnosticAnalyzer
//{
//    public const string DiagnosticId = "ASUOW002";
//    private static readonly LocalizableString Title = "UnitOfWork.BeginAsync must be awaited and disposed asynchronously";
//    private static readonly LocalizableString MessageFormat = "The result of 'UnitOfWork.BeginAsync' must be awaited and disposed asynchronously (with 'await using' or 'await DisposeAsync()')";
//    private static readonly LocalizableString Description = "To ensure proper disposal, always await UnitOfWork.BeginAsync and dispose the result asynchronously.";
//    private const string Category = "Usage";

//    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
//        DiagnosticId, Title, MessageFormat, Category,
//        DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

//    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

//    public override void Initialize(AnalysisContext context)
//    {
//        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
//        context.EnableConcurrentExecution();
//        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.AwaitExpression);
//    }

//    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
//    {
//        var awaitExpr = (AwaitExpressionSyntax)context.Node;
//        if (awaitExpr.Expression is not InvocationExpressionSyntax invocation)
//            return;

//        // Check if the method is UnitOfWork.BeginAsync
//        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
//        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
//        if (methodSymbol == null)
//            return;

//        if (methodSymbol.Name != "BeginAsync")
//            return;

//        var containingType = methodSymbol.ContainingType;
//        if (containingType == null || (containingType.Name != "IUnitOfWork" && containingType.Name != "IUnitOfWorkStandalone") || containingType.ContainingNamespace.ToDisplayString() != "AltaSoft.Storm")
//            return;

//        // Check if the awaited result is assigned to a variable
//        var parent = awaitExpr.Parent;
//        ISymbol? assignedSymbol = null;
//        if (parent is EqualsValueClauseSyntax equalsValue && equalsValue.Parent is VariableDeclaratorSyntax varDecl)
//        {
//            assignedSymbol = context.SemanticModel.GetDeclaredSymbol(varDecl);
//        }
//        else if (parent is AssignmentExpressionSyntax assignExpr)
//        {
//            assignedSymbol = context.SemanticModel.GetSymbolInfo(assignExpr.Left).Symbol;
//        }
//        else if (parent is ArgumentSyntax)
//        {
//            // e.g. await using (var tx = await uow.BeginAsync(...))
//            // handled below
//        }
//        else
//        {
//            // Not assigned to a variable
//            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
//            return;
//        }

//        // Check if the variable is disposed asynchronously (await using or await tx.DisposeAsync())
//        // 1. Check for 'await using' (declaration or statement)
//        SyntaxNode? node = awaitExpr;
//        while (node != null)
//        {
//            if (node is LocalDeclarationStatementSyntax localDecl && localDecl.UsingKeyword != default && localDecl.AwaitKeyword != default)
//                return; // Correct usage
//            if (node is UsingStatementSyntax usingStmt && usingStmt.AwaitKeyword != default)
//                return; // Correct usage
//            node = node.Parent;
//        }

//        // 2. Check for 'await tx.DisposeAsync()' in the same method/block
//        if (assignedSymbol != null)
//        {
//            var method = awaitExpr.FirstAncestorOrSelf<MethodDeclarationSyntax>();
//            if (method != null)
//            {
//                var disposeCalls = method.DescendantNodes()
//                    .OfType<AwaitExpressionSyntax>()
//                    .Select(ae => ae.Expression)
//                    .OfType<InvocationExpressionSyntax>()
//                    .Where(inv =>
//                    {
//                        if (inv.Expression is MemberAccessExpressionSyntax memberAccess)
//                        {
//                            var assignedSymbolEquals = SymbolEqualityComparer.Default.Equals(
//                                context.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol, 
//                                assignedSymbol
//                            );
//                            return memberAccess.Name.Identifier.Text == "DisposeAsync" && assignedSymbolEquals;
//                        }
//                        return false;
//                    });
//                if (disposeCalls.Any())
//                    return; // Correct usage
//            }
//        }

//        // If we get here, the result is not used correctly
//        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
//    }
//}

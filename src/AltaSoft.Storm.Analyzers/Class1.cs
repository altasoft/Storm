//using System.Collections.Immutable;
//using System.Linq;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Diagnostics;

//namespace AltaSoft.Storm.Analyzers
//{
//    [DiagnosticAnalyzer(LanguageNames.CSharp)]
//    public class UnitOfWorkUsageAnalyzer : DiagnosticAnalyzer
//    {
//        public const string MissingTransactionId = "UW001";
//        public const string MissingCompleteId = "UW002";

//        private static readonly DiagnosticDescriptor MissingTransactionRule =
//            new DiagnosticDescriptor(
//                id: MissingTransactionId,
//                title: "Missing transaction scope",
//                messageFormat: "After creating UnitOfWork '{0}', you must write `await using var tx = await {0}.BeginAsync(...)`",
//                category: "Usage",
//                defaultSeverity: DiagnosticSeverity.Warning,
//                isEnabledByDefault: true);

//        private static readonly DiagnosticDescriptor MissingCompleteRule =
//            new DiagnosticDescriptor(
//                id: MissingCompleteId,
//                title: "Missing CompleteAsync call",
//                messageFormat: "Transaction '{0}' must call `await {0}.CompleteAsync(...)` before exiting the method",
//                category: "Usage",
//                defaultSeverity: DiagnosticSeverity.Warning,
//                isEnabledByDefault: true);

//        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
//            => ImmutableArray.Create(MissingTransactionRule, MissingCompleteRule);

//        public override void Initialize(AnalysisContext context)
//        {
//            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
//            context.EnableConcurrentExecution();

//            // Analyze every method declaration
//            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
//        }

//        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
//        {
//            var methodDecl = (MethodDeclarationSyntax)context.Node;
//            var body = methodDecl.Body;
//            if (body == null)
//                return; // skip expression-bodied or abstract methods

//            var semanticModel = context.SemanticModel;

//            // 1) Find all "using var uow = UnitOfWork.Create();"
//            var uowDecls = body.DescendantNodes()
//                .OfType<UsingDeclarationSyntax>()
//                .Where(u =>
//                    u.AwaitKeyword.IsKind(SyntaxKind.None)            // plain `using`, not `await using`
//                 && u.Declaration.Variables.Count == 1
//                 && u.Declaration.Variables[0].Initializer?.Value is InvocationExpressionSyntax inv
//                 && IsUnitOfWorkCreate(inv, semanticModel))
//                .Select(u => u.Declaration.Variables[0].Identifier.ValueText)
//                .ToList();

//            foreach (var uowName in uowDecls)
//            {
//                // 2) Look for "await using var tx = await uow.BeginAsync(...);"
//                var txDecl = body.DescendantNodes()
//                    .OfType<UsingDeclarationSyntax>()
//                    .FirstOrDefault(u =>
//                        u.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)
//                     && u.Declaration.Variables.Count == 1
//                     && u.Declaration.Variables[0].Initializer?.Value is AwaitExpressionSyntax awaitExpr
//                     && awaitExpr.Expression is InvocationExpressionSyntax innerInv
//                     && innerInv.Expression is MemberAccessExpressionSyntax member
//                     && member.Name.Identifier.ValueText == "BeginAsync"
//                     && member.Expression is IdentifierNameSyntax idName
//                     && idName.Identifier.ValueText == uowName);

//                if (txDecl == null)
//                {
//                    // missing the `await using ... BeginAsync`
//                    var diag = Diagnostic.Create(
//                        MissingTransactionRule,
//                        body.GetLocation(),
//                        uowName);
//                    context.ReportDiagnostic(diag);
//                    continue;
//                }

//                // 3) Having found the tx declaration, ensure there's a CompleteAsync call on it
//                var txName = txDecl.Declaration.Variables[0].Identifier.ValueText;
//                var completeUsed = body.DescendantNodes()
//                    .OfType<InvocationExpressionSyntax>()
//                    .Any(inv =>
//                        inv.Expression is MemberAccessExpressionSyntax m
//                     && m.Name.Identifier.ValueText == "CompleteAsync"
//                     && m.Expression is IdentifierNameSyntax inst
//                     && inst.Identifier.ValueText == txName);

//                if (!completeUsed)
//                {
//                    var diag = Diagnostic.Create(
//                        MissingCompleteRule,
//                        txDecl.GetLocation(),
//                        txName);
//                    context.ReportDiagnostic(diag);
//                }
//            }
//        }

//        private static bool IsUnitOfWorkCreate(
//            InvocationExpressionSyntax invocation,
//            SemanticModel model)
//        {
//            var sym = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
//            if (sym == null) return false;

//            // must be static Create() on AltaSoft.Storm.UnitOfWork
//            return sym.Name == "Create"
//                && sym.MethodKind == MethodKind.Ordinary
//                && sym.IsStatic
//                && sym.ContainingType.ToDisplayString() == "AltaSoft.Storm.UnitOfWork";
//        }
//    }
//}

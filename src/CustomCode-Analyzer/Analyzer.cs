using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Collections.Concurrent;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class Analyzer : DiagnosticAnalyzer
{
    public static class DiagnosticIds
    {
        public const string TodoComment = "TODOCommentDetected";
        public const string NonPublicInterface = "NonPublicInterface";
        public const string NoSingleInterface = "NoSingleInterface";
        public const string ManyInterfaces = "ManyInterfaces";
        public const string NameBeginsWithUnderscore = "NameBeginsWithUnderscores";
    }

    public static class Categories
    {
        public const string Documentation = "Documentation";
        public const string Design = "Design";
        public const string Naming = "Naming";
    }

    private static readonly DiagnosticDescriptor TodoRule = new(
            DiagnosticIds.TodoComment,
            title: "TODO comment detected",
            messageFormat: "TODO comment detected in method '{0}' - create a work item instead",
            category: Categories.Documentation,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "TODO comments should be tracked in work items.");

    private static readonly DiagnosticDescriptor NoSingleInterfaceRule = new(
        DiagnosticIds.NoSingleInterface,
        title: "Missing OSInterface declaration",
        messageFormat: "No OSInterface found - exactly one interface must be decorated with [OSInterface]",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly DiagnosticDescriptor ManyInterfacesRule = new(
        DiagnosticIds.ManyInterfaces,
        title: "Multiple OSInterface declarations",
        messageFormat: "Multiple OSInterface attributes found: {0} - only one interface should have this attribute",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly DiagnosticDescriptor InterfaceRule = new(
        DiagnosticIds.NonPublicInterface,
        title: "Non-public OSInterface",
        messageFormat: "The OSInterface '{0}' must be public",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NoUnderscoreRule = new(
        DiagnosticIds.NameBeginsWithUnderscore,
        title: "Name begins with underscore",
        messageFormat: "The {0} name '{1}' should not begin with underscores",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(TodoRule, InterfaceRule, NoSingleInterfaceRule, ManyInterfacesRule, NoUnderscoreRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var osInterfaces = new ConcurrentDictionary<string, InterfaceDeclarationSyntax>();

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeNode(context, osInterfaces),
                SyntaxKind.MethodDeclaration,
                SyntaxKind.InterfaceDeclaration);

            compilationContext.RegisterCompilationEndAction(
                context => AnalyzeOSInterfacesAtEnd(context, osInterfaces));
        });
    }

    private static void AnalyzeNode(
        SyntaxNodeAnalysisContext context,
        ConcurrentDictionary<string, InterfaceDeclarationSyntax> osInterfaces)
    {
        switch (context.Node)
        {
            case MethodDeclarationSyntax methodDeclaration:
                AnalyzeMethod(context, methodDeclaration);
                AnalyzeNameForUnderscore(context, methodDeclaration.Identifier.Text, "Method");
                break;

            case InterfaceDeclarationSyntax interfaceDeclaration:
                AnalyzeInterface(context, interfaceDeclaration, osInterfaces);
                AnalyzeNameForUnderscore(context, interfaceDeclaration.Identifier.Text, "Interface");
                break;
        }
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
    {
        var leadingTrivia = methodDeclaration.GetLeadingTrivia();
        foreach (var trivia in leadingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                trivia.ToString().Contains("TODO", StringComparison.OrdinalIgnoreCase))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        TodoRule,
                        Location.Create(context.Node.SyntaxTree, trivia.Span),
                        methodDeclaration.Identifier.Text));
                break;
            }
        }
    }

    private static void AnalyzeInterface(
        SyntaxNodeAnalysisContext context,
        InterfaceDeclarationSyntax interfaceDeclaration,
        ConcurrentDictionary<string, InterfaceDeclarationSyntax> osInterfaces)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclaration);
        if (symbol == null) return;

        var hasOSInterfaceAttribute = symbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name is "OSInterfaceAttribute" or "OSInterface");

        if (!hasOSInterfaceAttribute) return;

        osInterfaces.TryAdd(interfaceDeclaration.Identifier.Text, interfaceDeclaration);

        if (!interfaceDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(InterfaceRule, interfaceDeclaration.GetLocation(), interfaceDeclaration.Identifier.Text));
        }
    }

    private static void AnalyzeOSInterfacesAtEnd(
        CompilationAnalysisContext context,
        ConcurrentDictionary<string, InterfaceDeclarationSyntax> osInterfaces)
    {
        if (osInterfaces.Count == 0)
        {
            // Only report NoSingleInterface if we find at least one interface declaration
            if (context.Compilation.SyntaxTrees
                .Any(tree => tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Any()))
            {
                var firstInterface = context.Compilation.SyntaxTrees
                    .SelectMany(tree => tree.GetRoot()
                        .DescendantNodes()
                        .OfType<InterfaceDeclarationSyntax>())
                    .First();

                context.ReportDiagnostic(
                    Diagnostic.Create(NoSingleInterfaceRule, firstInterface.GetLocation()));
            }
        }
        else if (osInterfaces.Count > 1)
        {
            // Get the first interface by source location for consistent reporting
            var firstInterface = osInterfaces.Values
                .OrderBy(i => i.GetLocation().GetLineSpan().StartLinePosition)
                .First();

            // Sort interface names for consistent order in the message
            var interfaceNames = string.Join(", ",
                osInterfaces.Keys.OrderBy(name => name));

            context.ReportDiagnostic(
                Diagnostic.Create(ManyInterfacesRule, firstInterface.GetLocation(), interfaceNames));
        }
    }

    private static void AnalyzeNameForUnderscore(SyntaxNodeAnalysisContext context, string identifier, string nodeType)
    {
        if (string.IsNullOrEmpty(identifier) || !identifier.StartsWith('_')) return;

        context.ReportDiagnostic(
            Diagnostic.Create(NoUnderscoreRule,
            context.Node.GetLocation(),
            nodeType,
            identifier));
    }
}
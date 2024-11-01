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
        public const string NoImplementingClass = "NoImplementingClass";
        public const string NoParameterlessConstructor = "NoParameterlessConstructor";
        public const string EmptyInterface = "EmptyInterface";
        public const string MultipleImplementations = "MultipleImplementations";
        public const string NonPublicImplementation = "NonPublicImplementation";
        public const string NonPublicStruct = "NonPublicStruct";
        public const string NonPublicStructureField = "NonPublicStructureField";
        public const string NonPublicIgnoredField = "NonPublicIgnoredField";
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
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05002");

    private static readonly DiagnosticDescriptor ManyInterfacesRule = new(
        DiagnosticIds.ManyInterfaces,
        title: "Multiple OSInterface declarations",
        messageFormat: "Multiple OSInterface attributes found: {0} - only one interface should have this attribute",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05002");

    private static readonly DiagnosticDescriptor InterfaceRule = new(
        DiagnosticIds.NonPublicInterface,
        title: "Non-public OSInterface",
        messageFormat: "The OSInterface '{0}' must be public",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05002");

    private static readonly DiagnosticDescriptor NoUnderscoreRule = new(
        DiagnosticIds.NameBeginsWithUnderscore,
        title: "Name begins with underscore",
        messageFormat: "The {0} name '{1}' should not begin with underscores",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05002");

    private static readonly DiagnosticDescriptor NoImplementingClassRule = new(
        DiagnosticIds.NoImplementingClass,
        title: "No implementing class found",
        messageFormat: "No class implementing the interface decorated with OSInterface '{0}' found in your file",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each interface decorated with OSInterface must have an implementing class.",
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05006");

    private static readonly DiagnosticDescriptor NoParameterlessConstructorRule = new(
        DiagnosticIds.NoParameterlessConstructor,
        title: "Missing public parameterless constructor",
        messageFormat: "The interface decorated with OSInterface is implemented by class '{0}' which doesn't have a public parameterless constructor",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each class implementing an OSInterface-decorated interface must have a public parameterless constructor.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05005");

    private static readonly DiagnosticDescriptor EmptyInterfaceRule = new(
        DiagnosticIds.EmptyInterface,
        title: "Empty OSInterface",
        messageFormat: "No methods found in the interface decorated with OSInterface '{0}'",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The interface decorated with OSInterface must define at least one method.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05007");

    private static readonly DiagnosticDescriptor MultipleImplementationsRule = new(
        DiagnosticIds.MultipleImplementations,
        title: "Multiple implementations of OSInterface",
        messageFormat: "The interface decorated with OSInterface '{0}' is implemented by multiple classes: {1}",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Only one class should implement an interface decorated with OSInterface.",
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05008");

        private static readonly DiagnosticDescriptor NonPublicImplementationRule = new(
        DiagnosticIds.NonPublicImplementation,
        title: "Non-public implementation of OSInterface",
        messageFormat: "The class that implements the interface decorated with OSInterface '{0}' must be public",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Classes implementing interfaces decorated with OSInterface must be public.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05018");

    private static readonly DiagnosticDescriptor NonPublicStructRule = new(
        DiagnosticIds.NonPublicStruct,
        title: "Non-public OSStructure",
        messageFormat: "The struct decorated with OSStructure '{0}' is not public",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Structs decorated with OSStructure must be public.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05010");

    private static readonly DiagnosticDescriptor NonPublicStructureFieldRule = new(
        DiagnosticIds.NonPublicStructureField,
        title: "Non-public OSStructureField",
        messageFormat: "The property/field decorated by OSStructureField '{0}' in struct {1} is not public",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Properties and fields decorated with OSStructureField must be public.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05011");

    private static readonly DiagnosticDescriptor NonPublicIgnoredFieldRule = new(
        DiagnosticIds.NonPublicIgnoredField,
        title: "Non-public OSIgnore field",
        messageFormat: "The property/field decorated by OSIgnore '{0}' in struct '{1}' is not public",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Properties and fields decorated with OSIgnore must be public.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05012");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            TodoRule,
            InterfaceRule,
            NoSingleInterfaceRule,
            ManyInterfacesRule,
            NoUnderscoreRule,
            NoImplementingClassRule,
            NoParameterlessConstructorRule,
            EmptyInterfaceRule,
            MultipleImplementationsRule,
            NonPublicImplementationRule,
            NonPublicStructRule,
            NonPublicStructureFieldRule,
            NonPublicIgnoredFieldRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var osInterfaces = new ConcurrentDictionary<string, (InterfaceDeclarationSyntax Syntax, INamedTypeSymbol Symbol)>();

            // Register for struct analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is INamedTypeSymbol typeSymbol &&
                    typeSymbol.TypeKind == TypeKind.Struct)
                {
                    var hasOSStructureAttribute = typeSymbol.GetAttributes()
                        .Any(attr => attr.AttributeClass?.Name is "OSStructureAttribute" or "OSStructure");

                    if (hasOSStructureAttribute)
                    {
                        // Check struct accessibility
                        var structDeclaration = typeSymbol.DeclaringSyntaxReferences
                            .FirstOrDefault()?.GetSyntax() as StructDeclarationSyntax;

                        if (structDeclaration != null && !typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    NonPublicStructRule,
                                    structDeclaration.Identifier.GetLocation(),
                                    typeSymbol.Name));
                        }

                        // Check fields and properties
                        foreach (var member in typeSymbol.GetMembers())
                        {
                            Location GetMemberLocation()
                            {
                                var syntax = member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                                if (syntax == null) return Location.None;

                                if (member is IFieldSymbol)
                                {
                                    var variableDeclarator = syntax as VariableDeclaratorSyntax;
                                    if (variableDeclarator != null)
                                    {
                                        return variableDeclarator.Identifier.GetLocation();
                                    }
                                }
                                else if (member is IPropertySymbol && syntax is PropertyDeclarationSyntax propertyDeclaration)
                                {
                                    return propertyDeclaration.Identifier.GetLocation();
                                }
                                return Location.None;
                            }

                            // Check for OSStructureField
                            var hasOSStructureFieldAttribute = member.GetAttributes()
                                .Any(attr => attr.AttributeClass?.Name is "OSStructureFieldAttribute" or "OSStructureField");

                            if (hasOSStructureFieldAttribute && !member.DeclaredAccessibility.HasFlag(Accessibility.Public))
                            {
                                var location = GetMemberLocation();
                                if (!location.Equals(Location.None))
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            NonPublicStructureFieldRule,
                                            location,
                                            member.Name,
                                            typeSymbol.Name));
                                }
                            }

                            // Check for OSIgnore
                            var hasOSIgnoreAttribute = member.GetAttributes()
                                .Any(attr => attr.AttributeClass?.Name is "OSIgnoreAttribute" or "OSIgnore");

                            if (hasOSIgnoreAttribute && !member.DeclaredAccessibility.HasFlag(Accessibility.Public))
                            {
                                var location = GetMemberLocation();
                                if (!location.Equals(Location.None))
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            NonPublicIgnoredFieldRule,
                                            location,
                                            member.Name,
                                            typeSymbol.Name));
                                }
                            }
                        }
                    }
                }
            }, SymbolKind.NamedType);


            // Register for interface analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is INamedTypeSymbol typeSymbol &&
                    typeSymbol.TypeKind == TypeKind.Interface)
                {
                    var hasOSInterfaceAttribute = typeSymbol.GetAttributes()
                        .Any(attr => attr.AttributeClass?.Name is "OSInterfaceAttribute" or "OSInterface");

                    if (hasOSInterfaceAttribute)
                    {
                        var syntaxRef = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                        if (syntaxRef != null &&
                            syntaxRef.GetSyntax() is InterfaceDeclarationSyntax syntax)
                        {
                            osInterfaces.TryAdd(typeSymbol.Name, (syntax, typeSymbol));

                            // Check if interface is public
                            if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(InterfaceRule, syntax.GetLocation(), typeSymbol.Name));
                            }

                            // Check for underscore prefix
                            if (typeSymbol.Name.StartsWith("_"))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(NoUnderscoreRule,
                                    syntax.GetLocation(),
                                    "Interface",
                                    typeSymbol.Name));
                            }
                            // Check for empty interface
                            if (!typeSymbol.GetMembers().OfType<IMethodSymbol>().Any())
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(EmptyInterfaceRule,
                                    syntax.Identifier.GetLocation(),
                                    typeSymbol.Name));
                            }
                        }
                    }
                }
            }, SymbolKind.NamedType);

            // Register for method analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is IMethodSymbol methodSymbol)
                {
                    var syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntaxRef != null &&
                        syntaxRef.GetSyntax() is MethodDeclarationSyntax methodSyntax)
                    {
                        // Check for TODO comments
                        var leadingTrivia = methodSyntax.GetLeadingTrivia();
                        foreach (var trivia in leadingTrivia)
                        {
                            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                                trivia.ToString().Contains("TODO", StringComparison.OrdinalIgnoreCase))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        TodoRule,
                                        Location.Create(methodSyntax.SyntaxTree, trivia.Span),
                                        methodSymbol.Name));
                                break;
                            }
                        }

                        // Check for underscore prefix
                        if (methodSymbol.Name.StartsWith("_"))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(NoUnderscoreRule,
                                methodSyntax.GetLocation(),
                                "Method",
                                methodSymbol.Name));
                        }
                    }
                }
            }, SymbolKind.Method);

            // Register for class analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is INamedTypeSymbol typeSymbol &&
                    typeSymbol.TypeKind == TypeKind.Class)
                {
                    // Check if this class implements any OSInterface-decorated interfaces
                    foreach (var implementedInterface in typeSymbol.Interfaces)
                    {
                        var hasOSInterfaceAttribute = implementedInterface.GetAttributes()
                            .Any(attr => attr.AttributeClass?.Name is "OSInterfaceAttribute" or "OSInterface");

                        if (hasOSInterfaceAttribute)
                        {
                            // Check if the class is public
                            if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
                            {
                                var classDeclaration = typeSymbol.DeclaringSyntaxReferences
                                    .FirstOrDefault()?.GetSyntax() as ClassDeclarationSyntax;

                                if (classDeclaration != null)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            NonPublicImplementationRule,
                                            classDeclaration.Identifier.GetLocation(),
                                            implementedInterface.Name));
                                }
                            }

                            // Check if the class has a public parameterless constructor
                            var hasPublicParameterlessConstructor = typeSymbol.Constructors.Any(c =>
                                c.DeclaredAccessibility == Accessibility.Public &&
                                c.Parameters.Length == 0);

                            if (!hasPublicParameterlessConstructor)
                            {
                                var classDeclaration = typeSymbol.DeclaringSyntaxReferences
                                    .FirstOrDefault()?.GetSyntax() as ClassDeclarationSyntax;

                                if (classDeclaration != null)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            NoParameterlessConstructorRule,
                                            classDeclaration.Identifier.GetLocation(),
                                            typeSymbol.Name));
                                }
                            }
                        }
                    }
                }
            }, SymbolKind.NamedType);


            // Register compilation end analysis
            compilationContext.RegisterCompilationEndAction(context =>
            {
                if (osInterfaces.Count == 0)
                {
                    // Only report NoSingleInterface if we find at least one interface
                    var interfaces = context.Compilation.GlobalNamespace.GetTypeMembers()
                        .Where(t => t.TypeKind == TypeKind.Interface);

                    if (interfaces.Any())
                    {
                        var firstInterface = interfaces.First().DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                        if (firstInterface != null)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(NoSingleInterfaceRule, firstInterface.GetLocation()));
                        }
                    }
                }
                else if (osInterfaces.Count > 1)
                {
                    // Multiple OSInterfaces found
                    var firstInterface = osInterfaces.Values
                        .OrderBy(i => i.Syntax.GetLocation().GetLineSpan().StartLinePosition)
                        .First();

                    var interfaceNames = string.Join(", ",
                        osInterfaces.Keys.OrderBy(name => name));

                    context.ReportDiagnostic(
                        Diagnostic.Create(ManyInterfacesRule, firstInterface.Syntax.GetLocation(), interfaceNames));
                }
                else
                {
                    // Check implementation for single OSInterface
                    var osInterface = osInterfaces.Values.First();

                    var implementations = context.Compilation.GlobalNamespace
                        .GetTypeMembers()
                        .Where(t => t.TypeKind == TypeKind.Class &&
                               t.Interfaces.Contains(osInterface.Symbol))
                        .ToList();

                    if (!implementations.Any())
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                NoImplementingClassRule,
                                osInterface.Syntax.GetLocation(),
                                osInterface.Symbol.Name));
                    }
                    else if (implementations.Count > 1)
                    {
                        // Report multiple implementations diagnostic
                        var implementationNames = string.Join(", ",
                            implementations.Select(i => i.Name).OrderBy(name => name));

                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                MultipleImplementationsRule,
                                osInterface.Syntax.GetLocation(),
                                osInterface.Symbol.Name,
                                implementationNames));
                    }
                }
            });
        });
    }
}
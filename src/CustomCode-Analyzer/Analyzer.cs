using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Collections.Concurrent;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class Analyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Contains all diagnostic IDs used by this analyzer.
    /// These IDs are used to uniquely identify each type of diagnostic that can be reported.
    /// </summary>
    public static class DiagnosticIds
    {
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
        public const string NoPublicMembers = "NoPublicMembers";
        public const string DuplicateStructureName = "DuplicateStructureName";
        public const string ReferenceParameter = "ReferenceParameter";
        public const string NameTooLong = "NameTooLong";
        public const string NameStartsWithNumber = "NameStartsWithNumber";
        public const string InvalidCharactersInName = "InvalidCharactersInName";
    }
    /// <summary>
    /// Defines the categories used to group diagnostics.
    /// These categories help organize diagnostics in IDE warning lists.
    /// </summary>
    public static class Categories
    {
        public const string Design = "Design";   // Issues related to code structure and design
        public const string Naming = "Naming";   // Issues related to naming conventions
    }

    // Define diagnostic descriptors for each rule
    // Each descriptor includes:
    // - A unique ID
    // - A title and message format
    // - The category and severity
    // - Whether it's enabled by default
    // - Optional help link and custom tags

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

    private static readonly DiagnosticDescriptor NoPublicMembersRule = new(
        DiagnosticIds.NoPublicMembers,
        title: "No public members in OSStructure",
        messageFormat: "No public properties/fields found in the struct decorated with OSStructure '{0}'",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Structs decorated with OSStructure must have at least one public property or field.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05013");

    private static readonly DiagnosticDescriptor DuplicateStructureNameRule = new(
        DiagnosticIds.DuplicateStructureName,
        title: "Duplicate OSStructure name",
        messageFormat: "More than one structure, '{0}', was found with the name '{1}'",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each structure with the OSStructure attribute must have a unique name.",
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05014");

    private static readonly DiagnosticDescriptor ReferenceParameterRule = new(
        DiagnosticIds.ReferenceParameter,
        title: "Reference parameter not supported",
        messageFormat: "The parameter '{0}' in action '{1}' is passed by reference. Passing parameters by reference is not supported.",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parameters in actions must be passed by value. Return modified values instead of using reference parameters.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05016");

    private static readonly DiagnosticDescriptor NameTooLongRule = new( 
        DiagnosticIds.NameTooLong,
        title: "Name exceeds maximum length",
        messageFormat: "The name '{0}' is not supported as it has more than 50 characters",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Names must not exceed 50 characters in length.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05019");

    private static readonly DiagnosticDescriptor NameStartsWithNumberRule = new(
        DiagnosticIds.NameStartsWithNumber,
        title: "Name starts with number",
        messageFormat: "The name '{0}' is not supported as it begins with a number",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Names must not begin with a number. Use a letter as the first character.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05020");

    private static readonly DiagnosticDescriptor InvalidCharactersInNameRule = new(
        DiagnosticIds.InvalidCharactersInName,
        title: "Invalid characters in name",
        messageFormat: "The name '{0}' is not supported as it has the following invalid characters '{1}'",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Names must only contain letters, numbers, and underscores.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05021");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
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
            NonPublicIgnoredFieldRule,
            NoPublicMembersRule,
            DuplicateStructureNameRule,
            ReferenceParameterRule,
            NameTooLongRule,
            NameStartsWithNumberRule,
            InvalidCharactersInNameRule);

    /// <summary>
    /// Initializes the analyzer by registering all necessary analysis actions.
    /// This method is called once at the start of the analysis.
    /// </summary>
    /// <param name="context">The context for initialization</param>
    public override void Initialize(AnalysisContext context)
    {
        // Disable analysis for generated code to improve performance
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // Enable concurrent analysis for better performance
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Create thread-safe collections to track interfaces and structures across the compilation
            var osInterfaces = new ConcurrentDictionary<string, (InterfaceDeclarationSyntax Syntax, INamedTypeSymbol Symbol)>();
            var structureNames = new ConcurrentBag<(string Name, StructDeclarationSyntax Syntax, string StructName)>();

            // Register handler for struct analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is INamedTypeSymbol typeSymbol &&
                    typeSymbol.TypeKind == TypeKind.Struct)
                {
                    // Check if struct has OSStructure attribute
                    var hasOSStructureAttribute = typeSymbol.GetAttributes()
                        .Any(attr => attr.AttributeClass?.Name is "OSStructureAttribute" or "OSStructure");

                    if (hasOSStructureAttribute)
                    {
                        // Get the syntax node for the struct declaration
                        var structDeclaration = typeSymbol.DeclaringSyntaxReferences
                            .FirstOrDefault()?.GetSyntax() as StructDeclarationSyntax;

                        if (structDeclaration == null) return;

                        // Extract the Name parameter from the OSStructure attribute if present
                        var osStructureAttribute = typeSymbol.GetAttributes()
                            .First(attr => attr.AttributeClass?.Name is "OSStructureAttribute" or "OSStructure");

                        var nameArg = osStructureAttribute.NamedArguments
                            .FirstOrDefault(na => na.Key == "Name");

                        // Add structure name to tracking collection if specified
                        if (nameArg.Key != null && nameArg.Value.Value is string structureName)
                        {
                            structureNames.Add((structureName, structDeclaration, typeSymbol.Name));
                        }

                        // Verify struct is declared as public
                        if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    NonPublicStructRule,
                                    structDeclaration.Identifier.GetLocation(),
                                    typeSymbol.Name));
                        }

                        // Check that struct has at least one public member
                        var hasPublicMembers = typeSymbol.GetMembers()
                            .Any(member =>
                                (member is IFieldSymbol || member is IPropertySymbol) &&
                                member.DeclaredAccessibility == Accessibility.Public);

                        if (!hasPublicMembers)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    NoPublicMembersRule,
                                    structDeclaration.Identifier.GetLocation(),
                                    typeSymbol.Name));
                        }

                        // Analyze each member of the struct
                        foreach (var member in typeSymbol.GetMembers())
                        {
                            // Helper function to get the source location for error reporting
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

                            // Check OSStructureField attribute requirements
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

                            // Check OSIgnore attribute requirements
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

            // Register handler for interface analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is INamedTypeSymbol typeSymbol &&
                    typeSymbol.TypeKind == TypeKind.Interface)
                {
                    // Check if interface has OSInterface attribute
                    var osInterfaceAttribute = typeSymbol.GetAttributes()
                        .FirstOrDefault(attr => attr.AttributeClass?.Name is "OSInterfaceAttribute" or "OSInterface");

                    if (osInterfaceAttribute != null)
                    {
                        // Get the syntax node for the interface declaration
                        var syntaxRef = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                        if (syntaxRef != null &&
                            syntaxRef.GetSyntax() is InterfaceDeclarationSyntax syntax)
                        {
                            // Add interface to tracking dictionary for later analysis
                            osInterfaces.TryAdd(typeSymbol.Name, (syntax, typeSymbol));

                            // Check if interface has any methods - must not be empty
                            if (!typeSymbol.GetMembers().OfType<IMethodSymbol>().Any())
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        EmptyInterfaceRule,
                                        syntax.Identifier.GetLocation(),
                                        typeSymbol.Name));
                            }

                            // Extract library name from attribute or interface name
                            string? libraryName = null;

                            // First check Name property in attribute
                            var nameArg = osInterfaceAttribute.NamedArguments
                                .FirstOrDefault(na => na.Key == "Name");
                            if (nameArg.Key != null && nameArg.Value.Value is string specifiedName)
                            {
                                libraryName = specifiedName;
                            }
                            else
                            {
                                // If Name not found, check OriginalName property
                                var originalNameArg = osInterfaceAttribute.NamedArguments
                                    .FirstOrDefault(na => na.Key == "OriginalName");
                                if (originalNameArg.Key != null && originalNameArg.Value.Value is string originalName)
                                {
                                    libraryName = originalName;
                                }
                            }

                            // If no name specified in attributes, use interface name without 'I' prefix
                            if (libraryName == null)
                            {
                                libraryName = typeSymbol.Name.StartsWith("I", StringComparison.Ordinal) ?
                                    typeSymbol.Name[1..] : typeSymbol.Name;
                            }

                            // Validate library name constraints
                            // Check maximum length (50 characters)
                            if (libraryName.Length > 50)
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        NameTooLongRule,
                                        syntax.GetLocation(),
                                        libraryName));
                            }

                            // Check if name starts with a number
                            if (char.IsDigit(libraryName[0]))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        NameStartsWithNumberRule,
                                        syntax.GetLocation(),
                                        libraryName));
                            }

                            // Check for invalid characters (only letters, numbers, and underscore allowed)
                            var invalidChars = libraryName.Where(c => !char.IsLetterOrDigit(c) && c != '_')
                             .Distinct()
                             .ToArray();
                            if (invalidChars.Length > 0)
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        InvalidCharactersInNameRule,
                                        syntax.GetLocation(),
                                        libraryName,
                                        string.Join(", ", invalidChars)));
                            }

                            // Verify interface is declared as public
                            if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        InterfaceRule,
                                        syntax.GetLocation(),
                                        typeSymbol.Name));
                            }
                        }
                    }
                }
            }, SymbolKind.NamedType);

            // Register handler for method analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is IMethodSymbol methodSymbol)
                {
                    // Get method declaration syntax
                    var syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntaxRef != null &&
                        syntaxRef.GetSyntax() is MethodDeclarationSyntax methodSyntax)
                    {
                        // Get containing type to check if it's an OSInterface or implements one
                        var containingType = methodSymbol.ContainingType;

                        // Check if method is directly in an OSInterface
                        var hasOSInterfaceAttribute = containingType.GetAttributes()
                            .Any(attr => attr.AttributeClass?.Name is "OSInterfaceAttribute" or "OSInterface");

                        var implementsOSInterface = false;
                        if (!hasOSInterfaceAttribute)
                        {
                            // Check if method is in a class that implements an OSInterface
                            implementsOSInterface = containingType.Interfaces
                                .Any(i => i.GetAttributes()
                                    .Any(attr => attr.AttributeClass?.Name is "OSInterfaceAttribute" or "OSInterface"));
                        }

                        if (hasOSInterfaceAttribute || implementsOSInterface)
                        {
                            // Check for underscore prefix in method names
                            if (methodSymbol.Name.StartsWith("_"))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        NoUnderscoreRule,
                                        methodSyntax.GetLocation(),
                                        "Method",
                                        methodSymbol.Name));
                            }
                        }

                        // Reference parameter check only for OSInterface methods (not implementations)
                        if (hasOSInterfaceAttribute)
                        {
                            // Check each parameter for ref/out/in modifiers
                            foreach (var parameter in methodSymbol.Parameters)
                            {
                                if (parameter.RefKind == RefKind.Ref ||
                                    parameter.RefKind == RefKind.Out ||
                                    parameter.RefKind == RefKind.In)
                                {
                                    // Get parameter syntax for accurate error location
                                    var parameterSyntax = parameter.DeclaringSyntaxReferences
                                        .FirstOrDefault()?.GetSyntax() as ParameterSyntax;
                                    if (parameterSyntax != null)
                                    {
                                        context.ReportDiagnostic(
                                            Diagnostic.Create(
                                                ReferenceParameterRule,
                                                parameterSyntax.GetLocation(),
                                                parameter.Name,
                                                methodSymbol.Name));
                                    }
                                }
                            }
                        }
                    }
                }
            }, SymbolKind.Method);

            // Register handler for class analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is INamedTypeSymbol typeSymbol &&
                    typeSymbol.TypeKind == TypeKind.Class)
                {
                    // Check each interface implemented by this class
                    foreach (var implementedInterface in typeSymbol.Interfaces)
                    {
                        // Check if the interface has OSInterface attribute
                        var hasOSInterfaceAttribute = implementedInterface.GetAttributes()
                            .Any(attr => attr.AttributeClass?.Name is "OSInterfaceAttribute" or "OSInterface");

                        if (hasOSInterfaceAttribute)
                        {
                            // Verify implementing class is public
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

                            // Check for public parameterless constructor
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


            // Register end-of-compilation analysis to perform final validations
            compilationContext.RegisterCompilationEndAction(context =>
            {
                if (osInterfaces.Count == 0)
                {
                    // If no OSInterface found, only report if there are any interfaces at all
                    var interfaces = context.Compilation.GlobalNamespace.GetTypeMembers()
                        .Where(t => t.TypeKind == TypeKind.Interface);

                    if (interfaces.Any())
                    {
                        // Report missing OSInterface only if at least one interface exists
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
                    // Get the first interface by source location for error reporting
                    var firstInterface = osInterfaces.Values
                        .OrderBy(i => i.Syntax.GetLocation().GetLineSpan().StartLinePosition)
                        .First();

                    // Create comma-separated list of interface names
                    var interfaceNames = string.Join(", ",
                        osInterfaces.Keys.OrderBy(name => name));

                    context.ReportDiagnostic(
                        Diagnostic.Create(ManyInterfacesRule, firstInterface.Syntax.GetLocation(), interfaceNames));
                }
                else
                {
                    // Exactly one OSInterface found - validate its implementation
                    var osInterface = osInterfaces.Values.First();

                    // Find all classes that implement this interface
                    var implementations = context.Compilation.GlobalNamespace
                        .GetTypeMembers()
                        .Where(t => t.TypeKind == TypeKind.Class &&
                               t.Interfaces.Contains(osInterface.Symbol))
                        .ToList();

                    if (!implementations.Any())
                    {
                        // No implementing class found
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                NoImplementingClassRule,
                                osInterface.Syntax.GetLocation(),
                                osInterface.Symbol.Name));
                    }
                    else if (implementations.Count > 1)
                    {
                        // Multiple implementations found - create list of class names
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

                // Check for duplicate structure names
                var duplicateNames = structureNames
                    .GroupBy(x => x.Name)
                    .Where(g => g.Count() > 1)
                    .ToList();

                foreach (var duplicate in duplicateNames)
                {
                    // Get the first struct (ordered by name) for consistent error reporting
                    var firstStruct = duplicate
                        .OrderBy(d => d.StructName)
                        .First();

                    // Create comma-separated list of struct names that share the same name
                    var structNames = string.Join(", ",
                        duplicate.Select(d => d.StructName).OrderBy(n => n));

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DuplicateStructureNameRule,
                            firstStruct.Syntax.Identifier.GetLocation(),
                            structNames,
                            duplicate.Key));
                }
            });
        });
    }
}
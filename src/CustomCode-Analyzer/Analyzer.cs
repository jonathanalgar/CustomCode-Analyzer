using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;


[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class Analyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Contains all diagnostic IDs used by this analyzer.
    /// These IDs are used to uniquely identify each type of diagnostic that can be reported.
    /// </summary>
    public static class DiagnosticIds
    {
        public const string NoSingleInterface = "NoSingleInterface";
        public const string ManyInterfaces = "ManyInterfaces";
        public const string NonPublicInterface = "NonPublicInterface";
        public const string NonInstantiableInterface = "NonInstantiableInterface";
        public const string EmptyInterface = "EmptyInterface";
        public const string NonPublicStruct = "NonPublicStruct";
        public const string NonPublicStructureField = "NonPublicStructureField";
        public const string NonPublicIgnored = "NonPublicIgnored";
        public const string UnsupportedType = "UnsupportedType";
        public const string NameBeginsWithUnderscore = "NameBeginsWithUnderscores";
        public const string MissingImplementation = "MissingImplementation";
        public const string ManyImplementation = "ManyImplementation";
        public const string MissingPublicImplementation = "MissingPublicImplementation";
        public const string EmptyStructure = "EmptyStructure";
        public const string ParameterByReference = "ParameterByReference";
        public const string NameMaxLengthExceeded = "NameMaxLengthExceeded";
        public const string NameBeginsWithNumbers = "NameBeginsWithNumbers";
        public const string UnsupportedCharactersInName = "UnsupportedCharactersInName";
        public const string DuplicateName = "DuplicateName";
        public const string UnsupportedTypeMapping = "UnsupportedTypeMapping";
        public const string MissingStructureDecoration = "MissingStructureDecoration";
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

    // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05001 - not implementing

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
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05003");

    private static readonly DiagnosticDescriptor NonPublicInterfaceRule = new(
        DiagnosticIds.NonPublicInterface,
        title: "Non-public OSInterface",
        messageFormat: "The OSInterface '{0}' must be public",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05004");

    private static readonly DiagnosticDescriptor NonInstantiableInterfaceRule = new(
        DiagnosticIds.NonInstantiableInterface,
        title: "Non-instantiable interface",
        messageFormat: "The interface decorated with OSInterface is implemented by class '{0}' which doesn't have a public parameterless constructor",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each class implementing an OSInterface-decorated interface must have a public parameterless constructor.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05005");

    private static readonly DiagnosticDescriptor MissingImplementationRule = new(
        DiagnosticIds.MissingImplementation,
        title: "Missing implementation",
        messageFormat: "No class implementing the interface decorated with OSInterface '{0}' found in your file",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each interface decorated with OSInterface must have an implementing class.",
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05006");

    private static readonly DiagnosticDescriptor EmptyInterfaceRule = new(
        DiagnosticIds.EmptyInterface,
        title: "Empty interface",
        messageFormat: "No methods found in the interface decorated with OSInterface '{0}'",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The interface decorated with OSInterface must define at least one method.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05007");

    private static readonly DiagnosticDescriptor ManyImplementationRule = new(
        DiagnosticIds.ManyImplementation,
        title: "Many implementation",
        messageFormat: "The interface decorated with OSInterface '{0}' is implemented by multiple classes: {1}",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Only one class should implement an interface decorated with OSInterface.",
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05008");

    // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05009 - not implementing

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
        DiagnosticIds.NonPublicIgnored,
        title: "Non-public field ignored",
        messageFormat: "The property/field decorated by OSIgnore '{0}' in struct '{1}' is not public",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Properties and fields decorated with OSIgnore must be public.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05012");

    private static readonly DiagnosticDescriptor EmptyStructureRule = new(
        DiagnosticIds.EmptyStructure,
        title: "Empty structure",
        messageFormat: "No public properties/fields found in the struct decorated with OSStructure '{0}'",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Structs decorated with OSStructure must have at least one public property or field.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05013");

    // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05015 - TODO: implement

    private static readonly DiagnosticDescriptor ParameterByReferenceRule = new(
        DiagnosticIds.ParameterByReference,
        title: "Unsupported ref parameter",
        messageFormat: "The parameter '{0}' in action '{1}' is passed by reference. Passing parameters by reference is not supported.",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parameters in actions must be passed by value. Return modified values instead of using reference parameters.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05016");

    private static readonly DiagnosticDescriptor UnsupportedTypeMappingRule = new(
        DiagnosticIds.UnsupportedTypeMapping,
        title: "Unsupported type mapping",
        messageFormat: "{0} has an incompatible DataType assigned and cannot be converted",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The DataType assigned to a property or field is incompatible with its corresponding .NET type. It can't be automatically converted to the specified OutSystems type..",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05017");

    private static readonly DiagnosticDescriptor MissingPublicImplementationRule = new(
        DiagnosticIds.MissingPublicImplementation,
        title: "Missing public implementation",
        messageFormat: "The class that implements the interface decorated with OSInterface '{0}' must be public",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Classes implementing interfaces decorated with OSInterface must be public.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05018");

    private static readonly DiagnosticDescriptor NameMaxLengthExceededRule = new( 
        DiagnosticIds.NameMaxLengthExceeded,
        title: "Name exceeds maximum length",
        messageFormat: "The name '{0}' is not supported as it has more than 50 characters",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Names must not exceed 50 characters in length.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05019");

    private static readonly DiagnosticDescriptor NameBeginsWithNumbersRule = new(
        DiagnosticIds.NameBeginsWithNumbers,
        title: "Name begins with numbers",
        messageFormat: "The name '{0}' is not supported as it begins with a number",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Names must not begin with a number. Use a letter as the first character.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05020");

    private static readonly DiagnosticDescriptor UnsupportedCharactersInNameRule = new(
        DiagnosticIds.UnsupportedCharactersInName,
        title: "Unsupported characters in a name",
        messageFormat: "The name '{0}' is not supported as it has the following invalid characters '{1}'",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Names must only contain letters, numbers, and underscores.",
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05021");

    private static readonly DiagnosticDescriptor NameBeginsWithUnderscoresRule = new(
        DiagnosticIds.NameBeginsWithUnderscore,
        title: "Name begins with underscores",
        messageFormat: "The {0} name '{1}' should not begin with underscores",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05022");

    // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05023 - not implementing

    private static readonly DiagnosticDescriptor MissingStructureDecorationRule = new(
        DiagnosticIds.MissingStructureDecoration,
        title: "Missing structure decoration",
        messageFormat: "The struct '{0}' used as '{1}' is missing OSStructure decoration",
        category: Categories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05022");
    // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05024 - TODO: implement

    private static readonly DiagnosticDescriptor DuplicateNameRule = new(
        DiagnosticIds.DuplicateName,
        title: "Duplicated name",
        messageFormat: "More than one object with name '{0}' was found",
        category: Categories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05025");

    // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05026 - TODO: implement

    // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05027 - not implementing

    // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05028 - not implementing

    // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05029 - not implementing

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            NonPublicInterfaceRule,
            NoSingleInterfaceRule,
            ManyInterfacesRule,
            NameBeginsWithUnderscoresRule,
            MissingImplementationRule,
            NonInstantiableInterfaceRule,
            EmptyInterfaceRule,
            ManyImplementationRule,
            MissingPublicImplementationRule,
            NonPublicStructRule,
            NonPublicStructureFieldRule,
            NonPublicIgnoredFieldRule,
            EmptyStructureRule,
            ParameterByReferenceRule,
            NameMaxLengthExceededRule,
            NameBeginsWithNumbersRule,
            UnsupportedCharactersInNameRule,
            DuplicateNameRule,
            UnsupportedTypeMappingRule,
            MissingStructureDecorationRule);

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

            // Helper method to search all namespaces
            IEnumerable<INamedTypeSymbol> GetAllTypesInCompilation(Compilation compilation, Func<INamedTypeSymbol, bool> predicate)
            {
                var stack = new Stack<INamespaceSymbol>();
                stack.Push(compilation.GlobalNamespace);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();

                    // Get types in current namespace
                    foreach (var type in current.GetTypeMembers().Where(predicate))
                    {
                        yield return type;
                    }

                    // Add nested namespaces to stack
                    foreach (var nested in current.GetNamespaceMembers())
                    {
                        stack.Push(nested);
                    }
                }
            }

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
                                    EmptyStructureRule,
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

                            if (hasOSStructureFieldAttribute)
                            {
                                var osStructureField = member.GetAttributes()
                                    .First(attr => attr.AttributeClass?.Name is "OSStructureField" or "OSStructureFieldAttribute");
                                if (osStructureField.NamedArguments.Any(na => na.Key == "DataType"))
                                {
                                    var dataType = osStructureField.NamedArguments.First(na => na.Key == "DataType").Value;
                                    var type = member is IFieldSymbol fieldSymbol ? fieldSymbol.Type :
                                        (member is IPropertySymbol propertySymbol ? propertySymbol.Type : null);
                                    if (AreIncompatibleTypes(type, dataType))
                                    {
                                        context.ReportDiagnostic(
                                            Diagnostic.Create(
                                                UnsupportedTypeMappingRule,
                                                GetMemberLocation()));
                                    }
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
                            string libraryName = null;

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
                                    typeSymbol.Name.Substring(1) : typeSymbol.Name;
                            }

                            // Validate library name constraints
                            // Check maximum length (50 characters)
                            if (libraryName.Length > 50)
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        NameMaxLengthExceededRule,
                                        syntax.GetLocation(),
                                        libraryName));
                            }

                            // Check if name starts with a number
                            if (char.IsDigit(libraryName[0]))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        NameBeginsWithNumbersRule,
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
                                        UnsupportedCharactersInNameRule,
                                        syntax.GetLocation(),
                                        libraryName,
                                        string.Join(", ", invalidChars)));
                            }

                            // Verify interface is declared as public
                            if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        NonPublicInterfaceRule,
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
                                        NameBeginsWithUnderscoresRule,
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
                                                ParameterByReferenceRule,
                                                parameterSyntax.GetLocation(),
                                                parameter.Name,
                                                methodSymbol.Name));
                                    }
                                }
                                var allStructuresNotExposed = GetAllTypesInCompilation(
                                    context.Compilation,
                                    t => !t.DeclaringSyntaxReferences.IsEmpty &&
                                        t.TypeKind == TypeKind.Struct &&
                                         !t.GetAttributes().Any(a => a.AttributeClass?.Name is "OSStructureAttribute" or "OSStructure")
                                );
                                if (allStructuresNotExposed.Any(s => s.Name == parameter.Type.Name || ((INamedTypeSymbol)parameter.Type).IsGenericType && ((INamedTypeSymbol)parameter.Type).TypeArguments.Any(t => t.Name == s.Name)))
                                {
                                    var structure = allStructuresNotExposed.First(s => s.Name == parameter.Type.Name || ((INamedTypeSymbol)parameter.Type).IsGenericType && ((INamedTypeSymbol)parameter.Type).TypeArguments.Any(t => t.Name == s.Name));
                                    // Get parameter syntax for accurate error location
                                    var parameterSyntax = parameter.DeclaringSyntaxReferences
                                        .FirstOrDefault()?.GetSyntax() as ParameterSyntax;
                                    if (parameterSyntax != null)
                                    {
                                        context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            MissingStructureDecorationRule,
                                            parameterSyntax.GetLocation(),
                                            structure.Name,
                                            parameter.Name));
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
                                            MissingPublicImplementationRule,
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
                                            NonInstantiableInterfaceRule,
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
                    // Check if any interfaces exist in any namespace
                    var anyInterface = GetAllTypesInCompilation(
                        context.Compilation,
                        t => t.TypeKind == TypeKind.Interface).Any();

                    if (anyInterface)
                    {
                        // Report missing OSInterface only if at least one interface exists
                        var firstInterface = GetAllTypesInCompilation(
                            context.Compilation,
                            t => t.TypeKind == TypeKind.Interface).First();

                        var syntax = firstInterface.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                        if (syntax != null)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(NoSingleInterfaceRule, syntax.GetLocation()));
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

                    // Find implementing classes in all namespaces
                    var implementations = GetAllTypesInCompilation(
                        context.Compilation,
                        t => t.TypeKind == TypeKind.Class &&
                             t.Interfaces.Contains(osInterface.Symbol, SymbolEqualityComparer.Default)
                    ).ToList();

                    if (!implementations.Any())
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                MissingImplementationRule,
                                osInterface.Syntax.GetLocation(),
                                osInterface.Symbol.Name));
                    }
                    else if (implementations.Count > 1)
                    {
                        var implementationNames = string.Join(", ",
                            implementations.Select(i => i.Name).OrderBy(name => name));

                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                ManyImplementationRule,
                                osInterface.Syntax.GetLocation(),
                                osInterface.Symbol.Name,
                                implementationNames));
                    }
                }

                // Check for duplicate structure names
                var allStructures = GetAllTypesInCompilation(
                    context.Compilation,
                    t => t.TypeKind == TypeKind.Struct &&
                         t.GetAttributes().Any(a => a.AttributeClass?.Name is "OSStructureAttribute" or "OSStructure")
                );

                var duplicates = allStructures
                    .GroupBy(x => x.Name)
                    .Where(g => g.Count() > 1);

                foreach (var duplicate in duplicates)
                {
                    // Get the first struct (ordered by name) for consistent error reporting
                    var firstStruct = duplicate
                        .OrderBy(d => d.Name)
                        .First();

                    // Create comma-separated list of struct names that share the same name
                    var structNames = string.Join(", ",
                        duplicate.Select(d => d.Name).OrderBy(n => n));

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DuplicateNameRule,
                            firstStruct.Locations.First(),
                            structNames,
                            duplicate.Key));
                }

            });
        });
    }

    private bool AreIncompatibleTypes(ITypeSymbol? type, TypedConstant dataType)
    {
        if (type == null) return false;
        // https://github.com/OutSystems/OutSystems.ExternalLibraries.SDK/blob/master/src/OutSystems.ExternalLibraries.SDK/OSDataType.cs
        // https://success.outsystems.com/documentation/outsystems_developer_cloud/errors/external_libraries_sdk_errors/os_elg_modl_05017/
        switch (dataType.Value)
        {
            case 1: // Text
                return type.Name.ToLowerInvariant() != "string";
            case 2: // Integer
                return type.Name.ToLowerInvariant() != "int32";
            case 3: // LongInteger
                return type.Name.ToLowerInvariant() != "int64";
            case 4: // Decimal
                return type.Name.ToLowerInvariant() != "decimal";
            case 5: // Boolean
                return type.Name.ToLowerInvariant() != "bool";
            case 6: // DateTime
                return type.Name.ToLowerInvariant() != "datetime";
            case 7: // Date
                return type.Name.ToLowerInvariant() != "datetime";
            case 8: // Time
                return type.Name.ToLowerInvariant() != "datetime";
            case 9: // PhoneNumber
                return type.Name.ToLowerInvariant() != "string";
            case 10: // Email
                return type.Name.ToLowerInvariant() != "string";
            case 11: // BinaryData
                return type.Name.ToLowerInvariant() != "byte[]";
            case 12: // Currency
                return type.Name.ToLowerInvariant() != "decimal";
            default:
                return true; // Unknown OSDataType
        }
    }
}
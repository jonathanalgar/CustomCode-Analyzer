using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace CustomCode_Analyzer
{
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
            public const string UnsupportedParameterType = "UnsupportedParameterType";
            public const string UnsupportedDefaultValue = "UnsupportedDefaultValue";
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

        // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05014 - not implementing

        private static readonly DiagnosticDescriptor UnsupportedParameterTypeRule = new(
            DiagnosticIds.UnsupportedParameterType,
            title: "Unsupported parameter type in OSStructure",
            messageFormat: "The struct decorated with OSStructure '{0}' contains a public property/field that uses an unsupported parameter type '{1}'",
            category: Categories.Design,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Public properties or fields in structs decorated with OSStructure must use supported types.",
            helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05015");

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
            helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05024");

        private static readonly DiagnosticDescriptor DuplicateNameRule = new(
            DiagnosticIds.DuplicateName,
            title: "Duplicated name",
            messageFormat: "More than one object with name '{0}' was found",
            category: Categories.Naming,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.CompilationEnd,
            helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05025");

        private static readonly DiagnosticDescriptor UnsupportedDefaultValueRule = new(
            DiagnosticIds.UnsupportedDefaultValue,
            title: "Unsupported default value",
            messageFormat: "The default value specified for {0} is unsupported",
            category: Categories.Design,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Default values for parameters must be compile-time constants of supported types.",
            helpLinkUri: "https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05026");

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
                MissingStructureDecorationRule,
                UnsupportedParameterTypeRule,
                UnsupportedDefaultValueRule);

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

            // Register a compilation start action to set up context-specific analysis
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Check if the OutSystems.ExternalLibraries.SDK package is referenced
                bool isPackageReferenced = compilationContext.Compilation.ReferencedAssemblyNames
                    .Any(reference => reference.Name.Equals("OutSystems.ExternalLibraries.SDK", StringComparison.OrdinalIgnoreCase));

                if (!isPackageReferenced) { return; }

                // Create thread-safe collections to track interfaces across the compilation
                var osInterfaces = new ConcurrentDictionary<string, (InterfaceDeclarationSyntax Syntax, INamedTypeSymbol Symbol)>();

                // Helper method to retrieve all types in the compilation that match a predicate
                IEnumerable<INamedTypeSymbol> GetAllTypesInCompilation(Compilation compilation, Func<INamedTypeSymbol, bool> predicate)
                {
                    var stack = new Stack<INamespaceSymbol>();
                    stack.Push(compilation.GlobalNamespace);

                    // Use a stack to perform depth-first traversal of namespaces
                    while (stack.Count > 0)
                    {
                        var current = stack.Pop();

                        // Get types in the current namespace that match the predicate
                        foreach (var type in current.GetTypeMembers().Where(predicate))
                        {
                            yield return type;
                        }

                        // Add nested namespaces to the stack for further traversal
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
                        // Check if the struct has the OSStructure attribute
                        var hasOSStructureAttribute = typeSymbol.GetAttributes()
                            .Any(attr => attr.AttributeClass?.Name is "OSStructureAttribute" or "OSStructure");

                        if (hasOSStructureAttribute)
                        {
                            // Retrieve the syntax node for the struct declaration
                            if (typeSymbol.DeclaringSyntaxReferences
                                .FirstOrDefault()?.GetSyntax() is not StructDeclarationSyntax structDeclaration)
                            {
                                return;
                            }

                            // Verify struct is declared as public
                            if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
                            {
                                // Report diagnostic if struct is not public
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        NonPublicStructRule,
                                        structDeclaration.Identifier.GetLocation(),
                                        typeSymbol.Name));
                            }

                            // Check that struct has at least one public member (field or property)
                            var hasPublicMembers = typeSymbol.GetMembers()
                                .Any(member =>
                                    (member is IFieldSymbol || member is IPropertySymbol) &&
                                    member.DeclaredAccessibility == Accessibility.Public);

                            if (!hasPublicMembers)
                            {
                                // Report diagnostic if no public members are found
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
                                    if (member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not SyntaxNode syntax)
                                        return Location.None;

                                    return (member, syntax) switch
                                    {
                                        (IFieldSymbol, VariableDeclaratorSyntax declarator) => declarator.Identifier.GetLocation(),
                                        (IPropertySymbol, PropertyDeclarationSyntax property) => property.Identifier.GetLocation(),
                                        _ => Location.None
                                    };
                                }

                                // Check if the member has the OSStructureField attribute
                                var hasOSStructureFieldAttribute = member.GetAttributes()
                                    .Any(attr => attr.AttributeClass?.Name is "OSStructureFieldAttribute" or "OSStructureField");

                                if (hasOSStructureFieldAttribute && !member.DeclaredAccessibility.HasFlag(Accessibility.Public))
                                {
                                    // If the member is decorated with OSStructureField but not public, report diagnostic
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
                                    // Retrieve the OSStructureField attribute instance
                                    var osStructureField = member.GetAttributes()
                                        .First(attr => attr.AttributeClass?.Name is "OSStructureField" or "OSStructureFieldAttribute");

                                    // Check if the DataType named argument is specified
                                    if (osStructureField.NamedArguments.Any(na => na.Key == "DataType"))
                                    {
                                        var dataType = osStructureField.NamedArguments.First(na => na.Key == "DataType").Value;

                                        // Determine the type based on member kind
                                        ITypeSymbol type;
                                        if (member is IFieldSymbol fieldSymbol)
                                        {
                                            type = fieldSymbol.Type;
                                        }
                                        else if (member is IPropertySymbol propertySymbol)
                                        {
                                            type = propertySymbol.Type;
                                        }
                                        else
                                        {
                                            type = null;
                                        }

                                        // Check if the DataType mapping is incompatible
                                        if (HasIncompatibleDataTypeMapping(type, dataType))
                                        {
                                            // Report diagnostic if the type mapping is unsupported
                                            context.ReportDiagnostic(
                                                Diagnostic.Create(
                                                    UnsupportedTypeMappingRule,
                                                    GetMemberLocation(),
                                                    member.Name));
                                        }
                                    }
                                }

                                // Check if the member has the OSIgnore attribute
                                var hasOSIgnoreAttribute = member.GetAttributes()
                                    .Any(attr => attr.AttributeClass?.Name is "OSIgnoreAttribute" or "OSIgnore");

                                if (hasOSIgnoreAttribute && !member.DeclaredAccessibility.HasFlag(Accessibility.Public))
                                // If the member is decorated with OSIgnore but not public, report diagnostic
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

                                // If the member is a public field or property, validate its type
                                if ((member is IFieldSymbol field && field.DeclaredAccessibility == Accessibility.Public) ||
                                    (member is IPropertySymbol property && property.DeclaredAccessibility == Accessibility.Public))
                                {
                                    ITypeSymbol memberType = member switch
                                    {
                                        IFieldSymbol f => f.Type,
                                        IPropertySymbol p => p.Type,
                                        _ => null
                                    };

                                    if (memberType != null && !IsValidParameterType(memberType, context.Compilation))
                                    {
                                        // Get the location of the member's type
                                        Location location = member.DeclaringSyntaxReferences.First().GetSyntax().GetLocation();

                                        // Report diagnostic if the parameter type is unsupported
                                        context.ReportDiagnostic(
                                            Diagnostic.Create(
                                                UnsupportedParameterTypeRule,
                                                location,
                                                typeSymbol.Name,
                                                memberType.ToDisplayString()));
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
                        // Check if the interface has the OSInterface attribute
                        var osInterfaceAttribute = typeSymbol.GetAttributes()
                            .FirstOrDefault(attr => attr.AttributeClass?.Name is "OSInterfaceAttribute" or "OSInterface");

                        if (osInterfaceAttribute != null)
                        {
                            // Get the syntax node for the interface declaration
                            var syntaxRef = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                            if (syntaxRef != null &&
                                syntaxRef.GetSyntax() is InterfaceDeclarationSyntax syntax)
                            {
                                // Add the interface to the tracking dictionary for later analysis
                                osInterfaces.TryAdd(typeSymbol.Name, (syntax, typeSymbol));

                                // Check if the interface has any methods - it must not be empty
                                if (!typeSymbol.GetMembers().OfType<IMethodSymbol>().Any())
                                {
                                    // Report diagnostic if the interface is empty
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            EmptyInterfaceRule,
                                            syntax.Identifier.GetLocation(),
                                            typeSymbol.Name));
                                }

                                // Extract library name from attribute or interface name
                                string libraryName = null;

                                // First, check the 'Name' property in the attribute
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
                                    // If 'Name' not found, check the 'OriginalName' property
                                    if (originalNameArg.Key != null && originalNameArg.Value.Value is string originalName)
                                    {
                                        libraryName = originalName;
                                    }
                                }

                                // If no name specified in attributes, use the interface name without 'I' prefix
                                libraryName ??= typeSymbol.Name.StartsWith("I", StringComparison.Ordinal) ?
                                        typeSymbol.Name.Substring(1) : typeSymbol.Name;

                                // Validate library name constraints

                                // Check maximum length
                                if (libraryName.Length > 50)
                                {
                                    // Report diagnostic if name exceeds maximum length
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            NameMaxLengthExceededRule,
                                            syntax.GetLocation(),
                                            libraryName));
                                }

                                // Check if name starts with a number
                                if (char.IsDigit(libraryName[0]))
                                {
                                    // Report diagnostic if name begins with a number
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            NameBeginsWithNumbersRule,
                                            syntax.GetLocation(),
                                            libraryName));
                                }

                                // Check for invalid characters (only letters, numbers, and underscores allowed)
                                var invalidChars = libraryName.Where(c => !char.IsLetterOrDigit(c) && c != '_')
                                 .Distinct()
                                 .ToArray();
                                if (invalidChars.Length > 0)
                                {
                                    // Report diagnostic if name contains unsupported characters
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
                                    // Report diagnostic if interface is not public
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
                    // Ensure the symbol is a method
                    if (context.Symbol is IMethodSymbol methodSymbol)
                    {
                        // Get method declaration syntax
                        var syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                        if (syntaxRef != null &&
                            syntaxRef.GetSyntax() is MethodDeclarationSyntax methodSyntax)
                        {
                            // Get containing type to check if it's an OSInterface or implements one
                            var containingType = methodSymbol.ContainingType;

                            // Check if the method is directly in an OSInterface
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

                            if ((hasOSInterfaceAttribute || implementsOSInterface) &&
                                                            methodSymbol.Name.StartsWith("_"))
                            {
                                // Report diagnostic if method name starts with an underscore
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        NameBeginsWithUnderscoresRule,
                                        methodSyntax.GetLocation(),
                                        "Method",
                                        methodSymbol.Name));
                            }

                            // Reference parameter check only for OSInterface methods (not implementations)
                            if (hasOSInterfaceAttribute)
                            {
                                foreach (var parameter in methodSymbol.Parameters)
                                {
                                    // Check for reference parameters
                                    if (parameter.RefKind is RefKind.Ref or RefKind.Out or RefKind.In &&
                                        parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ParameterSyntax refParameterSyntax)
                                    {
                                        // Report diagnostic if parameter is passed by reference
                                        context.ReportDiagnostic(
                                            Diagnostic.Create(
                                                ParameterByReferenceRule,
                                                refParameterSyntax.GetLocation(),
                                                parameter.Name,
                                                methodSymbol.Name));
                                    }

                                    // Check for default values
                                    if (parameter.HasExplicitDefaultValue &&
                                        !IsValidParameterDefaultValue(parameter) &&
                                        parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ParameterSyntax defaultParameterSyntax &&
                                        defaultParameterSyntax.Default?.Value != null)
                                    {
                                        // Report diagnostic if default value is unsupported
                                        context.ReportDiagnostic(
                                            Diagnostic.Create(
                                                UnsupportedDefaultValueRule,
                                                defaultParameterSyntax.Default.Value.GetLocation(),
                                                parameter.Name));
                                    }

                                    // Check if the parameter type requires OSStructure decoration
                                    var allStructuresNotExposed = GetAllTypesInCompilation(
                                        context.Compilation,
                                        t => !t.DeclaringSyntaxReferences.IsEmpty &&
                                            t.TypeKind == TypeKind.Struct &&
                                             !t.GetAttributes().Any(a => a.AttributeClass?.Name is "OSStructureAttribute" or "OSStructure")
                                    );

                                    // Determine if any structure type used in the parameter is not decorated with OSStructure
                                    if (allStructuresNotExposed.Any(s => s.Name == parameter.Type.Name || ((INamedTypeSymbol)parameter.Type).IsGenericType && ((INamedTypeSymbol)parameter.Type).TypeArguments.Any(t => t.Name == s.Name)))
                                    {
                                        var structure = allStructuresNotExposed.First(s => s.Name == parameter.Type.Name ||
                                            ((INamedTypeSymbol)parameter.Type).IsGenericType &&
                                            ((INamedTypeSymbol)parameter.Type).TypeArguments.Any(t => t.Name == s.Name));

                                        if (parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ParameterSyntax parameterSyntax)
                                        {
                                            // Report diagnostic if struct is missing OSStructure decoration
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
                        // Iterate through each interface implemented by the class
                        foreach (var implementedInterface in typeSymbol.Interfaces)
                        {
                            // Check if the interface has the OSInterface attribute
                            var hasOSInterfaceAttribute = implementedInterface.GetAttributes()
                                .Any(attr => attr.AttributeClass?.Name is "OSInterfaceAttribute" or "OSInterface");

                            if (hasOSInterfaceAttribute)
                            {
                                // Verify that the implementing class is public
                                if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public) &&
                                    typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax publicClassDeclaration)
                                {
                                    // Report diagnostic if the implementing class is not public
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            MissingPublicImplementationRule,
                                            publicClassDeclaration.Identifier.GetLocation(),
                                            implementedInterface.Name));
                                }

                                // Check for a public parameterless constructor
                                var hasPublicParameterlessConstructor = typeSymbol.Constructors.Any(c =>
                                    c.DeclaredAccessibility == Accessibility.Public &&
                                    c.Parameters.Length == 0);

                                if (!hasPublicParameterlessConstructor &&
                                    typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax constructorClassDeclaration)
                                {
                                    // Report diagnostic if no public parameterless constructor is found
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            NonInstantiableInterfaceRule,
                                            constructorClassDeclaration.Identifier.GetLocation(),
                                            typeSymbol.Name));
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
                        // If no OSInterface is found, check if any interfaces exist at all
                        var interfaces = GetAllTypesInCompilation(
                            context.Compilation,
                            t => !t.DeclaringSyntaxReferences.IsEmpty &&
                                 t.TypeKind == TypeKind.Interface).ToList();

                        if (interfaces.Any() &&
                                                   interfaces[0].DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is InterfaceDeclarationSyntax interfaceDeclaration)
                        {
                            // Report diagnostic if no interface is decorated with OSInterface
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    NoSingleInterfaceRule,
                                    interfaceDeclaration.GetLocation()));
                        }
                    }
                    else if (osInterfaces.Count > 1)
                    {
                        // If multiple OSInterfaces are found, report diagnostic
                        // Get the first interface by source location for error reporting
                        var firstSyntax = osInterfaces.Values
                            .OrderBy(i => i.Syntax.GetLocation().GetLineSpan().StartLinePosition)
                            .First()
                            .Syntax;

                        // Create a comma-separated list of interface names
                        var interfaceNames = string.Join(", ",
                            osInterfaces.Keys.OrderBy(name => name));

                        // Report diagnostic indicating multiple OSInterfaces
                        context.ReportDiagnostic(
                            Diagnostic.Create(ManyInterfacesRule, firstSyntax.GetLocation(), interfaceNames));
                    }
                    else
                    {
                        // Exactly one OSInterface found - validate its implementation
                        var (Syntax, Symbol) = osInterfaces.Values.First();

                        // Find implementing classes across all namespaces
                        var implementations = GetAllTypesInCompilation(
                            context.Compilation,
                            t => t.TypeKind == TypeKind.Class &&
                                 t.Interfaces.Contains(Symbol, SymbolEqualityComparer.Default)
                        ).ToList();

                        if (!implementations.Any())
                        {
                            // Report diagnostic if no implementing class is found
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    MissingImplementationRule,
                                    Syntax.GetLocation(),
                                    Symbol.Name));
                        }
                        else if (implementations.Count > 1)
                        {
                            // Create a comma-separated list of implementing class names
                            var implementationNames = string.Join(", ",
                                implementations.Select(i => i.Name).OrderBy(name => name));

                            // Report diagnostic indicating multiple implementations
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    ManyImplementationRule,
                                    Syntax.GetLocation(),
                                    Symbol.Name,
                                    implementationNames));
                        }
                    }

                    // Check for duplicate structure names across the compilation
                    var allStructures = GetAllTypesInCompilation(
                        context.Compilation,
                        t => t.TypeKind == TypeKind.Struct &&
                             t.GetAttributes().Any(a => a.AttributeClass?.Name is "OSStructureAttribute" or "OSStructure")
                    );

#pragma warning disable RS1024 // Intentionally comparing names as strings to find duplicates
                    var duplicates = allStructures
                        .GroupBy(x => x.Name)
                        .Where(g => g.Count() > 1);
#pragma warning restore RS1024

                    foreach (var duplicate in duplicates)
                    {
                        // Get the first struct (ordered by name) for consistent error reporting
                        var firstStruct = duplicate
                            .OrderBy(d => d.Name)
                            .First();

                        // Create a comma-separated list of struct names that share the same name
                        var structNames = string.Join(", ",
                            duplicate.Select(d => d.Name).OrderBy(n => n));

                        // Report diagnostic indicating duplicate structure names
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

        // A set of valid parameter types for UnsupportedDefaultValueRule
        private static readonly HashSet<string> ValidParameterTypes = new()
        {
            "String",
            "Int32",
            "Int64",
            "Single",
            "Double",
            "Decimal",
            "Boolean",
            "DateTime",
            "Byte[]"
        };

        /// <summary>
        /// Validates parameter default values for UnsupportedDefaultValueRule
        /// </summary>
        private static bool IsValidParameterDefaultValue(IParameterSymbol parameter)
        {
            // If no explicit default value is specified, it's considered valid
            if (!parameter.HasExplicitDefaultValue)
            {
                return true;
            }

            // Allow null for reference types
            if (parameter.ExplicitDefaultValue == null && !parameter.Type.IsValueType)
            {
                return true;
            }

            // The type must be among the supported parameter types
            if (!ValidParameterTypes.Contains(parameter.Type.Name))
            {
                return false;
            }

            var parameterSyntax = parameter.DeclaringSyntaxReferences
                .Select(sr => sr.GetSyntax())
                .OfType<ParameterSyntax>()
                .FirstOrDefault();

            // If the syntax node is not found, assume the default value is invalid
            if (parameterSyntax == null)
            {
                return false;
            }

            // Check if the default value is a literal expression (e.g., "hello", 42)
            // This ensures that the default value is a compile-time constant
            return parameterSyntax.Default?.Value is LiteralExpressionSyntax;
        }


        /// <summary>
        /// Validates type support for UnsupportedParameterTypeRule
        /// </summary>
        private static bool IsValidParameterType(ITypeSymbol typeSymbol, Compilation compilation)
        {
            if (typeSymbol == null)
            {
                // Null types are considered unsupported
                return false;
            }

            // Check for primitive types using special type enumeration
            if (typeSymbol.SpecialType is
                SpecialType.System_String or
                SpecialType.System_Int32 or
                SpecialType.System_Int64 or
                SpecialType.System_Boolean or
                SpecialType.System_Decimal or
                SpecialType.System_Single or
                SpecialType.System_Double or
                SpecialType.System_DateTime)
            {
                return true;
            }

            // Check if the type is a byte array
            if (typeSymbol is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            {
                return true;
            }

            // Check if the type is a struct decorated with OSStructure
            if (typeSymbol.TypeKind == TypeKind.Struct &&
                typeSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name is "OSStructureAttribute" or "OSStructure"))
            {
                return true;
            }

            // Check for generic enumerable types
            if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol &&
            namedTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == "System.Collections.IEnumerable"))
            {
                var typeArg = namedTypeSymbol.TypeArguments.FirstOrDefault();
                return typeArg != null && IsValidParameterType(typeArg, compilation);
            }

            // If none of the above conditions are met, the type is unsupported
            return false;
        }

        /// <summary>
        /// Validates OSStructureField DataType mappings for the UnsupportedTypeMappingRule
        /// </summary>
        private bool HasIncompatibleDataTypeMapping(ITypeSymbol type, TypedConstant dataType)
        {
            if (type == null) return false;
            return dataType.Value switch
            {
                // Text
                1 => type.Name.ToLowerInvariant() != "string",
                // Integer
                2 => type.Name.ToLowerInvariant() != "int32",
                // LongInteger
                3 => type.Name.ToLowerInvariant() != "int64",
                // Decimal
                4 => type.Name.ToLowerInvariant() != "decimal",
                // Boolean
                5 => type.Name.ToLowerInvariant() != "bool",
                // DateTime
                6 => type.Name.ToLowerInvariant() != "datetime",
                // Date
                7 => type.Name.ToLowerInvariant() != "datetime",
                // Time
                8 => type.Name.ToLowerInvariant() != "datetime",
                // PhoneNumber
                9 => type.Name.ToLowerInvariant() != "string",
                // Email
                10 => type.Name.ToLowerInvariant() != "string",
                // BinaryData
                11 => type.Name.ToLowerInvariant() != "byte[]",
                // Currency
                12 => type.Name.ToLowerInvariant() != "decimal",
                _ => true,// Unknown OSDataType
            };
        }
    }
}
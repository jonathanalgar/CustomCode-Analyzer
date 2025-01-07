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
        /// These IDs uniquely identify each type of diagnostic that can be reported.
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
            public const string PotentialStatefulImplementation = "PotentialStatefulImplementation";
            public const string InputSizeLimit = "InputSizeLimit";
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
        // Below are DiagnosticDescriptors that describe each rule:
        // - Unique ID
        // - Title and message
        // - Category and default severity
        // - Whether the diagnostic is enabled by default
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

        // ----------------------------------------------- BEST PRACTICES

        private static readonly DiagnosticDescriptor PotentialStatefulImplementationRule = new(
            DiagnosticIds.PotentialStatefulImplementation,
            title: "Possible stateful behavior",
            messageFormat: "The class '{0}' contains static members ({1}) which could persist state between calls. External libraries should be designed to be stateless.",
            category: Categories.Design,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "External libraries should be designed to be stateless. Consider passing state information as method parameters instead of storing it in fields.",
            helpLinkUri: "https://success.outsystems.com/documentation/outsystems_developer_cloud/building_apps/extend_your_apps_with_custom_code/external_libraries_sdk_readme/#architecture");

        private static readonly DiagnosticDescriptor InputSizeLimitRule = new(
            DiagnosticIds.InputSizeLimit,
            title: "Possible input size limit",
            messageFormat: "This method accepts binary data. Note that external libraries have a 5.5MB total input size limit. For large files, use a REST API endpoint or file URL instead.",
            category: Categories.Design,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "External libraries have a 5.5MB total input size limit. For large binary files, expose them through a REST API endpoint in your app or provide a URL to download them.",
            helpLinkUri: "https://success.outsystems.com/documentation/outsystems_developer_cloud/building_apps/extend_your_apps_with_custom_code/external_libraries_sdk_readme/#use-with-large-binary-files");

        /// <summary>
        /// Returns the full set of DiagnosticDescriptors that this analyzer is capable of producing.
        /// </summary>
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
                UnsupportedDefaultValueRule,
                PotentialStatefulImplementationRule,
                InputSizeLimitRule);

        /// <summary>
        /// Entry point for the analyzer. Initializes analysis by setting up compilation-level
        /// actions.
        /// </summary>
        public override void Initialize(AnalysisContext context)
        {
            // Disable analysis for generated code for better performance
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            // Enable concurrent analysis for better performance
            context.EnableConcurrentExecution();

            // Register a compilation start action to set up context-specific analysis
            context.RegisterCompilationStartAction(InitializeCompilationAnalysis);
        }

        /// <summary>
        /// Called once per compilation to initialize per-compilation data structures and 
        /// register further symbol and compilation end actions.
        /// </summary>
        private void InitializeCompilationAnalysis(CompilationStartAnalysisContext compilationContext)
        {
            // Check if the OutSystems.ExternalLibraries.SDK package is referenced
            // If not, skip all checks
            bool isPackageReferenced = compilationContext.Compilation.ReferencedAssemblyNames
                .Any(r => r.Name.Equals("OutSystems.ExternalLibraries.SDK", StringComparison.OrdinalIgnoreCase));

            if (!isPackageReferenced)
                return;

            // A thread-safe collection to track all OSInterface-decorated interfaces in the compilation
            var osInterfaces = new ConcurrentDictionary<string, (InterfaceDeclarationSyntax Syntax, INamedTypeSymbol Symbol)>();

            // Register symbol actions for struct, interface, and class analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is INamedTypeSymbol typeSymbol)
                {
                    switch (typeSymbol.TypeKind)
                    {
                        case TypeKind.Struct:
                            AnalyzeStruct(context, typeSymbol);
                            break;
                        case TypeKind.Interface:
                            AnalyzeInterface(context, typeSymbol, osInterfaces);
                            break;
                        case TypeKind.Class:
                            AnalyzeClass(context, typeSymbol);
                            break;
                    }
                }
            }, SymbolKind.NamedType);

            // Register a symbol action for method-level analysis
            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is IMethodSymbol methodSymbol)
                {
                    AnalyzeMethod(context, methodSymbol);
                }
            }, SymbolKind.Method);

            // Register a compilation end action to check for any final conditions
            // that can only be verified after all symbols have been processed (for example, # of OSInterfaces).
            compilationContext.RegisterCompilationEndAction(ctx =>
            {
                AnalyzeCompilationEnd(ctx, osInterfaces);
            });
        }

        /// <summary>
        /// Analyzes struct declarations.
        /// </summary>
        private void AnalyzeStruct(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol)
        {
            // Check if the struct has the OSStructure attribute
            bool hasOSStructureAttribute = HasAttribute(typeSymbol, OSStructureAttributeNames);
            if (!hasOSStructureAttribute) return;

            // Retrieve the actual syntax node for reporting precise locations
            if (typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not StructDeclarationSyntax structDecl)
                return;

            // Verify struct is declared as public
            if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        NonPublicStructRule,
                        structDecl.Identifier.GetLocation(),
                        typeSymbol.Name));
            }

            // Check that the struct has at least one public field or property
            bool hasPublicMembers = typeSymbol.GetMembers()
                .Any(m => (m is IFieldSymbol || m is IPropertySymbol) &&
                          m.DeclaredAccessibility == Accessibility.Public);

            if (!hasPublicMembers)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        EmptyStructureRule,
                        structDecl.Identifier.GetLocation(),
                        typeSymbol.Name));
            }

            // Analyze each member in the struct
            foreach (var member in typeSymbol.GetMembers())
            {
                // Helper function to get the source location for error reporting
                Location GetMemberLocation()
                {
                    if (member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not SyntaxNode syntax)
                        return Location.None;

                    return (member, syntax) switch
                    {
                        (IFieldSymbol, VariableDeclaratorSyntax vds) => vds.Identifier.GetLocation(),
                        (IPropertySymbol, PropertyDeclarationSyntax pds) => pds.Identifier.GetLocation(),
                        _ => Location.None
                    };
                }

                // Check if the member has the OSStructureField attribute
                bool hasOSStructureField = HasAttribute(member, OSStructureFieldAttributeNames);

                // If the member is decorated with OSStructureField but not public, report a diagnostic
                if (hasOSStructureField && !member.DeclaredAccessibility.HasFlag(Accessibility.Public))
                {
                    var loc = GetMemberLocation();
                    if (loc != Location.None)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                NonPublicStructureFieldRule,
                                loc,
                                member.Name,
                                typeSymbol.Name));
                    }
                }

                // If the member has [OSStructureField] and a "DataType" named argument, 
                // check for incompatible type mappings between .NET type and OutSystems type
                if (hasOSStructureField)
                {
                    var osStructureField = member.GetAttributes()
                        .First(a => a.AttributeClass?.Name is "OSStructureField" or "OSStructureFieldAttribute");

                    // Check if the DataType named argument is specified
                    if (osStructureField.NamedArguments.Any(na => na.Key == "DataType"))
                    {
                        var dataType = osStructureField.NamedArguments.First(na => na.Key == "DataType").Value;

                        ITypeSymbol realType = member switch
                        {
                            IFieldSymbol f => f.Type,
                            IPropertySymbol p => p.Type,
                            _ => null
                        };
                        if (realType != null && HasIncompatibleDataTypeMapping(realType, dataType))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    UnsupportedTypeMappingRule,
                                    GetMemberLocation(),
                                    member.Name));
                        }
                    }
                }

                // Check if the member has the OSIgnore attribute
                bool hasOSIgnore = HasAttribute(member, OSIgnoreAttributeNames);

                // If the member is decorated with OSIgnore but not public, report a diagnostic
                if (hasOSIgnore && !member.DeclaredAccessibility.HasFlag(Accessibility.Public))
                {
                    var loc = GetMemberLocation();
                    if (loc != Location.None)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                NonPublicIgnoredFieldRule,
                                loc,
                                member.Name,
                                typeSymbol.Name));
                    }
                }

                // For public fields or properties, confirm that the type is supported
                if ((member is IFieldSymbol field && field.DeclaredAccessibility == Accessibility.Public) ||
                    (member is IPropertySymbol prop && prop.DeclaredAccessibility == Accessibility.Public))
                {
                    ITypeSymbol memberType = member switch
                    {
                        IFieldSymbol f => f.Type,
                        IPropertySymbol p => p.Type,
                        _ => null
                    };

                    // If the type is not valid, report the unsupported parameter type
                    if (memberType != null && !IsValidParameterType(memberType, context.Compilation))
                    {
                        // Get the location of the member's type
                        var loc = member.DeclaringSyntaxReferences.First().GetSyntax().GetLocation();

                        // Report diagnostic if the parameter type is unsupported
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                UnsupportedParameterTypeRule,
                                loc,
                                typeSymbol.Name,
                                memberType.ToDisplayString()));
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes interface declarations.
        /// </summary>
        private static void AnalyzeInterface(
            SymbolAnalysisContext context,
            INamedTypeSymbol typeSymbol,
            ConcurrentDictionary<string, (InterfaceDeclarationSyntax Syntax, INamedTypeSymbol Symbol)> osInterfaces)
        {
            // Check if the interface has the OSInterface attribute
            var osInterfaceAttr = GetAttribute(typeSymbol, OSInterfaceAttributeNames);
            if (osInterfaceAttr == null) return;

            var syntaxRef = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null) return;
            if (syntaxRef.GetSyntax() is not InterfaceDeclarationSyntax syntax) return;

            // Track this OSInterface for final "single interface" checks in AnalyzeCompilationEnd
            osInterfaces.TryAdd(typeSymbol.Name, (syntax, typeSymbol));

            // Check if the interface has at least one method
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
            // First, check the 'Name' property
            var nameArg = osInterfaceAttr.NamedArguments.FirstOrDefault(na => na.Key == "Name");
            if (nameArg.Key != null && nameArg.Value.Value is string specifiedName)
            {
                libraryName = specifiedName;
            }
            else
            {
                // Otherwise, check 'OriginalName' property
                var originalNameArg = osInterfaceAttr.NamedArguments.FirstOrDefault(na => na.Key == "OriginalName");
                if (originalNameArg.Key != null && originalNameArg.Value.Value is string originalName)
                {
                    libraryName = originalName;
                }
            }
            // If no name specified in attributes, use the interface name without 'I' prefix
            libraryName ??= (typeSymbol.Name.StartsWith("I", StringComparison.Ordinal))
                ? typeSymbol.Name.Substring(1)
                : typeSymbol.Name;

            // Validate name constraints
            if (libraryName.Length > 50)
            {
                // Report a diagnostic if name exceeds maximum length
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        NameMaxLengthExceededRule,
                        syntax.GetLocation(),
                        libraryName));
            }

            // Name must not start with a digit
            if (char.IsDigit(libraryName[0]))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        NameBeginsWithNumbersRule,
                        syntax.GetLocation(),
                        libraryName));
            }

            // Name must only contain letters, digits, or underscores
            var invalidChars = libraryName.Where(c => !char.IsLetterOrDigit(c) && c != '_').Distinct().ToArray();
            if (invalidChars.Length > 0)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        UnsupportedCharactersInNameRule,
                        syntax.GetLocation(),
                        libraryName,
                        string.Join(", ", invalidChars)));
            }

            // The interface must be public
            if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        NonPublicInterfaceRule,
                        syntax.GetLocation(),
                        typeSymbol.Name));
            }
        }

        /// <summary>
        /// Analyzes method declarations.
        /// <summary>
        private static void AnalyzeMethod(SymbolAnalysisContext context, IMethodSymbol methodSymbol)
        {
            var syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null) return;
            if (syntaxRef.GetSyntax() is not MethodDeclarationSyntax methodSyntax) return;

            var containingType = methodSymbol.ContainingType;

            // Determine if the method is in an OSInterface or in a class that implements one
            var hasOSInterfaceAttribute = HasAttribute(containingType, OSInterfaceAttributeNames);

            bool implementsOSInterface = false;
            if (!hasOSInterfaceAttribute)
            {
                implementsOSInterface = containingType.Interfaces.Any(i => HasAttribute(i, OSInterfaceAttributeNames));
            }

            // If this method is part of the OSInterface or an implementation of it, check for underscores
            if ((hasOSInterfaceAttribute || implementsOSInterface) &&
                methodSymbol.Name.StartsWith("_", StringComparison.Ordinal))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        NameBeginsWithUnderscoresRule,
                        methodSyntax.GetLocation(),
                        "Method",
                        methodSymbol.Name));
            }

            // Only check for by-ref parameters in the OSInterface itself (not the implementation)
            if (hasOSInterfaceAttribute)
            {
                foreach (var parameter in methodSymbol.Parameters)
                {
                    // Disallow reference-like (ref/out/in) parameters
                    if (parameter.RefKind is RefKind.Ref or RefKind.Out or RefKind.In)
                    {
                        var paramSyntax = parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ParameterSyntax;
                        if (paramSyntax != null)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    ParameterByReferenceRule,
                                    paramSyntax.GetLocation(),
                                    parameter.Name,
                                    methodSymbol.Name));
                        }
                    }

                    // Check for potential input size limit issues
                    if (parameter.Type is IArrayTypeSymbol arrayType && arrayType.ElementType.SpecialType == SpecialType.System_Byte)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                InputSizeLimitRule,
                                methodSyntax.GetLocation()));
                        break;
                    }

                    // Check if the default value is valid (compile-time constant and supported type)
                    if (parameter.HasExplicitDefaultValue &&
                        !IsValidParameterDefaultValue(parameter) &&
                        parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ParameterSyntax defParamSyn &&
                        defParamSyn.Default?.Value != null)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                UnsupportedDefaultValueRule,
                                defParamSyn.Default.Value.GetLocation(),
                                parameter.Name));
                    }

                    // Check if the parameter references a struct that is not decorated with [OSStructure]
                    var allStructuresNotExposed = GetAllTypesInCompilation(
                        context.Compilation,
                        t => !t.DeclaringSyntaxReferences.IsEmpty &&
                             t.TypeKind == TypeKind.Struct &&
                             !HasAttribute(t, OSStructureAttributeNames));

                    // Determine if any structure type used in the parameter is not decorated with OSStructure
                    bool usesUndecoratedStruct = allStructuresNotExposed.Any(s =>
                        s.Name == parameter.Type.Name ||
                        (parameter.Type is INamedTypeSymbol nts && nts.IsGenericType &&
                         nts.TypeArguments.Any(t => t.Name == s.Name)));

                    if (usesUndecoratedStruct)
                    {
                        // Find the actual struct in question
                        var structure = allStructuresNotExposed.First(s =>
                            s.Name == parameter.Type.Name ||
                            (parameter.Type is INamedTypeSymbol nts2 && nts2.IsGenericType &&
                             nts2.TypeArguments.Any(t => t.Name == s.Name)));

                        if (parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ParameterSyntax paramSyn)
                        {
                            // Report diagnostic if struct is missing OSStructure decoration
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    MissingStructureDecorationRule,
                                    paramSyn.GetLocation(),
                                    structure.Name,
                                    parameter.Name));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes class declarations.
        /// </summary>
        private static void AnalyzeClass(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol)
        {
            // Check each interface implemented by this class to see if it has [OSInterface]
            foreach (var implementedInterface in typeSymbol.Interfaces)
            {
                bool hasOSInterfaceAttribute = HasAttribute(implementedInterface, OSInterfaceAttributeNames);

                if (hasOSInterfaceAttribute)
                {
                    // The implementing class must be public
                    if (!typeSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public) &&
                        typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax clsDecl)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                MissingPublicImplementationRule,
                                clsDecl.Identifier.GetLocation(),
                                implementedInterface.Name));
                    }

                    // Must have a public parameterless constructor
                    bool hasPublicParameterlessConstructor = typeSymbol.Constructors.Any(c =>
                        c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 0);
                    if (!hasPublicParameterlessConstructor &&
                        typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax ctorDecl)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                NonInstantiableInterfaceRule,
                                ctorDecl.Identifier.GetLocation(),
                                typeSymbol.Name));
                    }

                    // Check for static members that could indicate attempts to maintain state
                    var staticMembers = typeSymbol.GetMembers()
                        .Where(member =>
                            member.IsStatic &&
                            !member.IsImplicitlyDeclared && // Skip compiler-generated members
                            member is IFieldSymbol { IsConst: false } or IPropertySymbol)
                        .Select(m => m.Name)
                        .OrderBy(name => name)
                        .ToList();

                    if (staticMembers.Any() &&
                        typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax stateDecl)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                PotentialStatefulImplementationRule,
                                stateDecl.Identifier.GetLocation(),
                                typeSymbol.Name,
                                string.Join(", ", staticMembers)));
                    }
                }
            }
        }

        /// <summary>
        /// Called once after all symbols have been analyzed. Ensures:
        /// - Exactly one [OSInterface] is declared; otherwise, report 
        ///   NoSingleInterface or ManyInterfaces.
        /// - That each declared [OSStructure] name is unique across the compilation.
        /// - Checks if the single [OSInterface] had an implementing class or not, etc.
        /// </summary>
        private static void AnalyzeCompilationEnd(
            CompilationAnalysisContext context,
            ConcurrentDictionary<string, (InterfaceDeclarationSyntax Syntax, INamedTypeSymbol Symbol)> osInterfaces)
        {
            if (osInterfaces.Count == 0)
            {
                // If no OSInterface is found, check if we have any interface at all
                var interfaces = GetAllTypesInCompilation(
                    context.Compilation,
                    t => !t.DeclaringSyntaxReferences.IsEmpty &&
                         t.TypeKind == TypeKind.Interface).ToList();

                if (interfaces.Any() &&
                    interfaces[0].DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is InterfaceDeclarationSyntax ifDecl)
                {
                    // Report diagnostic if no interface is decorated with OSInterface
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            NoSingleInterfaceRule,
                            ifDecl.GetLocation()));
                }
            }
            else if (osInterfaces.Count > 1)
            {
                // Multiple OSInterface found
                var firstSyntax = osInterfaces.Values
                    .OrderBy(i => i.Syntax.GetLocation().GetLineSpan().StartLinePosition)
                    .First()
                    .Syntax;

                // Create a comma-separated list of interface names
                var interfaceNames = string.Join(", ", osInterfaces.Keys.OrderBy(n => n));
                // Report diagnostic indicating multiple OSInterfaces
                context.ReportDiagnostic(
                    Diagnostic.Create(ManyInterfacesRule, firstSyntax.GetLocation(), interfaceNames));
            }
            else
            {
                // Exactly one OSInterface found
                var (syntax, symbol) = osInterfaces.Values.First();

                // Find classes implementing this interface
                var implementations = GetAllTypesInCompilation(
                    context.Compilation,
                    t => t.TypeKind == TypeKind.Class &&
                         t.Interfaces.Contains(symbol, SymbolEqualityComparer.Default))
                    .ToList();

                if (!implementations.Any())
                {
                    // Report diagnostic if no implementing class is found
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            MissingImplementationRule,
                            syntax.GetLocation(),
                            symbol.Name));
                }
                else if (implementations.Count > 1)
                {
                    // Create a comma-separated list of implementing class names
                    var implNames = string.Join(", ",
                        implementations.Select(i => i.Name).OrderBy(n => n));

                    // Report diagnostic indicating multiple implementations
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            ManyImplementationRule,
                            syntax.GetLocation(),
                            symbol.Name,
                            implNames));
                }
            }

            // Check for duplicate struct names across the compilation
            var allStructures = GetAllTypesInCompilation(
                context.Compilation,
                t => t.TypeKind == TypeKind.Struct && HasAttribute(t, OSStructureAttributeNames));

#pragma warning disable RS1024
            var duplicates = allStructures.GroupBy(x => x.Name).Where(g => g.Count() > 1);
#pragma warning restore RS1024

            foreach (var duplicate in duplicates)
            {
                // Get the first struct (ordered by name) for consistent error reporting
                var firstStruct = duplicate.OrderBy(d => d.Name).First();

                // Create a comma-separated list of struct names that share the same name
                var structNames = string.Join(", ", duplicate.Select(d => d.Name).OrderBy(n => n));

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DuplicateNameRule,
                        firstStruct.Locations.First(),
                        structNames,
                        duplicate.Key));
            }
        }


        /// <summary>
        /// Valid names for the OSInterface attribute.
        /// </summary>
        private static readonly HashSet<string> OSInterfaceAttributeNames = new()
        {
            "OSInterfaceAttribute",
            "OSInterface"
        };

        /// <summary>
        /// Valid names for the OSStructure attribute.
        /// </summary>
        private static readonly HashSet<string> OSStructureAttributeNames = new()
        {
            "OSStructureAttribute",
            "OSStructure"
        };

        /// <summary>
        /// Valid names for the OSStructureField attribute.
        /// </summary>
        private static readonly HashSet<string> OSStructureFieldAttributeNames = new()
        {
            "OSStructureFieldAttribute",
            "OSStructureField"
        };

        /// <summary>
        /// Valid names for the OSIgnore attribute.
        /// </summary>
        private static readonly HashSet<string> OSIgnoreAttributeNames = new()
        {
            "OSIgnoreAttribute",
            "OSIgnore"
        };

        /// <summary>
        /// Contains valid names for OSIgnore attribute, used to mark fields that should be ignored during serialization.
        /// </summary>
        private static bool HasAttribute(ISymbol symbol, HashSet<string> attributeNames)
        {
            return symbol.GetAttributes()
                .Any(attr => attributeNames.Contains(attr.AttributeClass?.Name));
        }

        /// <summary>
        /// Checks if a symbol has any of the specified attributes from the provided set of names.
        /// </summary>
        private static AttributeData GetAttribute(ISymbol symbol, HashSet<string> attributeNames)
        {
            return symbol.GetAttributes()
                .FirstOrDefault(attr => attributeNames.Contains(attr.AttributeClass?.Name));
        }

        /// <summary>
        /// Retrieves all <see cref="INamedTypeSymbol"/>s in the current compilation 
        /// that match the given predicate. Traverses through all namespaces in a DFS manner.
        /// </summary>
        private static IEnumerable<INamedTypeSymbol> GetAllTypesInCompilation(
            Compilation compilation,
            Func<INamedTypeSymbol, bool> predicate)
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

        /// <summary>
        /// A set of valid parameter types for <see cref="UnsupportedDefaultValueRule"/>.
        /// Anything not in this set (and is not null for reference types) is considered invalid.
        /// </summary>
        private static readonly ImmutableHashSet<SpecialType> ValidParameterSpecialTypes = ImmutableHashSet.Create(
           SpecialType.System_String,
           SpecialType.System_Int32,
           SpecialType.System_Int64,
           SpecialType.System_Single,
           SpecialType.System_Double,
           SpecialType.System_Decimal,
           SpecialType.System_Boolean,
           SpecialType.System_DateTime
        );

        /// <summary>
        /// Checks whether a parameter's default value is a compile-time constant of a supported type.
        /// </summary>
        private static bool IsValidParameterDefaultValue(IParameterSymbol parameter)
        {
            if (!parameter.HasExplicitDefaultValue)
            {
                return true; // No default value means no rule violation
            }

            // Allow null for reference types
            if (parameter.ExplicitDefaultValue == null && !parameter.Type.IsValueType)
            {
                return true;
            }

            // Check if type is supported
            if (!ValidParameterSpecialTypes.Contains(parameter.Type.SpecialType) &&
                !(parameter.Type is IArrayTypeSymbol arrayType &&
                  arrayType.ElementType.SpecialType == SpecialType.System_Byte))
            {
                return false;
            }

            // Attempt to get the parameter syntax and ensure it's a literal expression
            var parameterSyntax = parameter.DeclaringSyntaxReferences
                .Select(sr => sr.GetSyntax())
                .OfType<ParameterSyntax>()
                .FirstOrDefault();

            // If no syntax found, we assume it's invalid
            if (parameterSyntax == null)
            {
                return false;
            }

            // Check if the default value is a literal expression
            // This ensures that the default value is a compile-time constant
            return parameterSyntax.Default?.Value is LiteralExpressionSyntax;
        }

        /// <summary>
        /// Checks if a given <see cref="ITypeSymbol"/> is a valid parameter type
        /// for fields/properties in an [OSStructure] context.
        /// </summary>
        private static bool IsValidParameterType(ITypeSymbol typeSymbol, Compilation compilation)
        {
            if (typeSymbol == null)
            {
                return false;
            }

            // Check for primitive or special types
            if (ValidParameterSpecialTypes.Contains(typeSymbol.SpecialType))
            {
                return true;
            }

            // Check if the type is a byte array
            if (typeSymbol is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            {
                return true;
            }

            // Check if the type is a struct with [OSStructure]
            if (typeSymbol is INamedTypeSymbol { TypeKind: TypeKind.Struct } structType &&
                HasAttribute(structType, OSStructureAttributeNames))
            {
                return true;
            }

            // Check if the type is a generic type that implements IEnumerable
            if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol &&
                namedTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == "System.Collections.IEnumerable"))
            {
                var typeArg = namedTypeSymbol.TypeArguments.FirstOrDefault();
                return typeArg != null && IsValidParameterType(typeArg, compilation);
            }

            // If none match, it's not a supported parameter type
            return false;
        }

        /// <summary>
        /// Checks for mismatches between the declared .NET type of a field/property and
        /// the OutSystems DataType specified in [OSStructureField(DataType = ...)].
        /// Returns <c>true</c> if the mapping is incompatible.
        /// </summary>
        private bool HasIncompatibleDataTypeMapping(ITypeSymbol type, TypedConstant dataType)
        {
            if (type == null)
            {
                return false;
            }
            return dataType.Value switch
            {
                1 => !type.Name.Equals("String", StringComparison.OrdinalIgnoreCase),   // Text
                2 => !type.Name.Equals("Int32", StringComparison.OrdinalIgnoreCase),    // Integer
                3 => !type.Name.Equals("Int64", StringComparison.OrdinalIgnoreCase),    // LongInteger
                4 => !type.Name.Equals("Decimal", StringComparison.OrdinalIgnoreCase),  // Decimal
                5 => !type.Name.Equals("Boolean", StringComparison.OrdinalIgnoreCase),  // Boolean
                6 => !type.Name.Equals("DateTime", StringComparison.OrdinalIgnoreCase), // DateTime
                7 => !type.Name.Equals("DateTime", StringComparison.OrdinalIgnoreCase), // Date
                8 => !type.Name.Equals("DateTime", StringComparison.OrdinalIgnoreCase), // Time
                9 => !type.Name.Equals("String", StringComparison.OrdinalIgnoreCase),   // PhoneNumber
                10 => !type.Name.Equals("String", StringComparison.OrdinalIgnoreCase),  // Email
                11 => !type.Name.Equals("Byte[]", StringComparison.OrdinalIgnoreCase),  // BinaryData
                12 => !type.Name.Equals("Decimal", StringComparison.OrdinalIgnoreCase), // Currency
                _ => true
            };
        }
    }
}
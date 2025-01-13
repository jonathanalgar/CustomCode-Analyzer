using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace CustomCode_Analyzer
{
    /// <summary>
    /// Provides code fixes for certain diagnostics reported by the <see cref="Analyzer"/> class.
    /// If a diagnostic can be automatically corrected without risking significant changes to
    /// user intent, this provider implements the fix.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Fixer))]
    [Shared]
    public class Fixer : CodeFixProvider
    {
        /// <summary>
        /// Lists the diagnostic IDs that this code fix provider can handle.
        /// Code fixes will only be offered for diagnostics with these IDs.
        /// </summary>
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Analyzer.DiagnosticIds.NameBeginsWithUnderscore,
                Analyzer.DiagnosticIds.NonPublicInterface,
                Analyzer.DiagnosticIds.UnsupportedTypeMapping,
                Analyzer.DiagnosticIds.NonPublicStruct,
                Analyzer.DiagnosticIds.NonPublicStructureField,
                Analyzer.DiagnosticIds.NonPublicIgnored,
                Analyzer.DiagnosticIds.MissingStructureDecoration
            );

        /// <summary>
        /// Returns a <see cref="FixAllProvider"/> that can handle applying fixes across an entire solution,
        /// project, or document. The default “BatchFixer” attempts to apply the same code fix to all
        /// matching diagnostics in a single pass.
        /// </summary>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <summary>
        /// Called by the IDE to register one or more code actions that can fix the specified diagnostics in
        /// a user’s code.
        /// </summary>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Check if the OutSystems.ExternalLibraries.SDK package is referenced
            var compilation = await context
                .Document.Project.GetCompilationAsync(context.CancellationToken)
                .ConfigureAwait(false);

            if (
                compilation is null
                || !compilation.ReferencedAssemblyNames.Any(r =>
                    r.Name.Equals(
                        "OutSystems.ExternalLibraries.SDK",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                return;
            }

            var root = await context
                .Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (root is null)
                return;

            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic is null)
                return;

            // Switch on the diagnostic ID and register a corresponding fix
            switch (diagnostic.Id)
            {
                case Analyzer.DiagnosticIds.NameBeginsWithUnderscore:
                {
                    var methodDecl =
                        root.FindToken(diagnostic.Location.SourceSpan.Start).Parent
                        as MethodDeclarationSyntax;
                    if (methodDecl is not null)
                    {
                        var newName = methodDecl.Identifier.Text.TrimStart('_');
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: $"Remove leading underscore (rename to '{newName}')",
                                createChangedDocument: c =>
                                    RemoveLeadingUnderscoreAsync(
                                        context.Document,
                                        methodDecl,
                                        newName,
                                        c
                                    ),
                                equivalenceKey: "RemoveLeadingUnderscore"
                            ),
                            diagnostic
                        );
                    }
                    break;
                }
                case Analyzer.DiagnosticIds.UnsupportedTypeMapping:
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Fix this type mapping",
                            createChangedDocument: c =>
                                FixSingleTypeMappingAsync(context.Document, diagnostic.Location, c),
                            equivalenceKey: "FixSingleTypeMapping"
                        ),
                        diagnostic
                    );
                    break;
                }
                case Analyzer.DiagnosticIds.NonPublicInterface:
                {
                    var node = root.FindNode(diagnostic.Location.SourceSpan)
                        .FirstAncestorOrSelf<InterfaceDeclarationSyntax>();
                    if (node is not null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Make interface public",
                                createChangedDocument: c =>
                                    MakeMemberPublicAsync(context.Document, node, c),
                                equivalenceKey: "MakeInterfacePublic"
                            ),
                            diagnostic
                        );
                    }
                    break;
                }
                case Analyzer.DiagnosticIds.NonPublicStruct:
                {
                    var node = root.FindNode(diagnostic.Location.SourceSpan)
                        .FirstAncestorOrSelf<StructDeclarationSyntax>();
                    if (node is not null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Make struct public",
                                createChangedDocument: c =>
                                    MakeMemberPublicAsync(context.Document, node, c),
                                equivalenceKey: "MakeStructPublic"
                            ),
                            diagnostic
                        );
                    }
                    break;
                }
                case Analyzer.DiagnosticIds.NonPublicStructureField:
                {
                    var memberNode = root.FindNode(diagnostic.Location.SourceSpan)
                        .FirstAncestorOrSelf<MemberDeclarationSyntax>();
                    if (memberNode is not null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Make OSStructureField public",
                                createChangedDocument: c =>
                                    MakeMemberPublicAsync(context.Document, memberNode, c),
                                equivalenceKey: "MakeOSStructureFieldPublic"
                            ),
                            diagnostic
                        );
                    }
                    break;
                }
                case Analyzer.DiagnosticIds.NonPublicIgnored:
                {
                    var memberNode = root.FindNode(diagnostic.Location.SourceSpan)
                        .FirstAncestorOrSelf<MemberDeclarationSyntax>();
                    if (memberNode is not null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Make OSIgnore field/property public",
                                createChangedDocument: c =>
                                    MakeMemberPublicAsync(context.Document, memberNode, c),
                                equivalenceKey: "MakeOSIgnorePublic"
                            ),
                            diagnostic
                        );
                    }
                    break;
                }
                case Analyzer.DiagnosticIds.MissingStructureDecoration:
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Add [OSStructure] attribute",
                            createChangedDocument: c =>
                                AddOSStructureAttributeAsync(
                                    context.Document,
                                    diagnostic.Location,
                                    c
                                ),
                            equivalenceKey: "AddOSStructureAttribute"
                        ),
                        diagnostic
                    );
                    break;
                }
            }
        }

        /// <summary>
        /// Adds the [OSStructure] attribute to a struct declaration that is missing it.
        /// This is called from the code fix for the MissingStructureDecoration diagnostic.
        /// </summary>
        private static async Task<Document> AddOSStructureAttributeAsync(
            Document document,
            Location diagNode,
            CancellationToken cancellationToken
        )
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
                return document;

            // Identify the parameter syntax node where the diagnostic was reported
            var node = root.FindNode(diagNode.SourceSpan);
            if (node is not ParameterSyntax parameterSyntax)
                return document;

            // From the parameter, retrieve the actual struct type symbol via the semantic model
            var semanticModel = await document
                .GetSemanticModelAsync(cancellationToken)
                .ConfigureAwait(false);
            if (semanticModel is null)
                return document;

            var typeInfo = semanticModel.GetTypeInfo(parameterSyntax.Type, cancellationToken);
            var structSymbol = typeInfo.Type as INamedTypeSymbol;
            if (structSymbol is null || structSymbol.DeclaringSyntaxReferences.Length == 0)
                return document;

            // Get the StructDeclarationSyntax for the referenced struct
            var structDeclRef = structSymbol.DeclaringSyntaxReferences[0];
            var structDeclNode = await structDeclRef
                .GetSyntaxAsync(cancellationToken)
                .ConfigureAwait(false);
            if (structDeclNode is not StructDeclarationSyntax structDecl)
                return document;

            // If it already has [OSStructure], skip
            var hasOSStructure = structDecl
                .AttributeLists.SelectMany(a => a.Attributes)
                .Any(a => a.Name.ToString().Contains("OSStructure"));
            if (hasOSStructure)
                return document;

            // Create the [OSStructure] attribute node
            var osStructureAttr = SyntaxFactory.Attribute(SyntaxFactory.ParseName("OSStructure"));
            var attributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(osStructureAttr)
            );

            // Insert into the struct's attribute lists.
            StructDeclarationSyntax newStructDecl;
            if (structDecl.AttributeLists.Count == 0)
            {
                // If no attributes exist, add the first one
                newStructDecl = structDecl.AddAttributeLists(attributeList);
            }
            else
            {
                // Otherwise, prepend this attribute before the first existing list
                var firstList = structDecl.AttributeLists.First();
                newStructDecl = structDecl.InsertNodesBefore(firstList, new[] { attributeList });
            }

            // Use a DocumentEditor to replace the old struct node with the new one containing[OSStructure]
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            editor.ReplaceNode(structDecl, newStructDecl);
            return editor.GetChangedDocument();
        }

        /// <summary>
        /// Changes a member (interface, struct, field, property) to be public if it's
        /// currently private, internal, or protected. If it is already public, no changes are made.
        /// This is used by multiple diagnostics (NonPublicInterface, NonPublicStruct, etc.).
        /// </summary>
        private static async Task<Document> MakeMemberPublicAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken
        )
        {
            if (node is not MemberDeclarationSyntax declaration)
                return document;

            var oldModifiers = declaration.Modifiers;

            var newModifiers = new List<SyntaxToken>(oldModifiers.Count);

            // Track whether we’ve already replaced or inserted 'public'
            bool alreadyHasPublic = oldModifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));

            foreach (var mod in oldModifiers)
            {
                // Replace private/internal/protected with 'public' if not done yet
                if (
                    !alreadyHasPublic
                    && (
                        mod.IsKind(SyntaxKind.PrivateKeyword)
                        || mod.IsKind(SyntaxKind.InternalKeyword)
                        || mod.IsKind(SyntaxKind.ProtectedKeyword)
                    )
                )
                {
                    var publicToken = SyntaxFactory.Token(
                        mod.LeadingTrivia,
                        SyntaxKind.PublicKeyword,
                        mod.TrailingTrivia
                    );

                    newModifiers.Add(publicToken);
                    alreadyHasPublic = true;
                }
                else
                {
                    // Otherwise, keep this token exactly as is.
                    newModifiers.Add(mod);
                }
            }

            // If we never encountered private/internal/protected, we still need 'public' if none is present
            if (!alreadyHasPublic)
            {
                if (newModifiers.Count > 0)
                {
                    // Insert 'public' at the front, reusing the first token’s leading trivia
                    var first = newModifiers[0];
                    var publicToken = SyntaxFactory.Token(
                        first.LeadingTrivia,
                        SyntaxKind.PublicKeyword,
                        first.TrailingTrivia
                    );

                    // Remove the leading trivia from the original first token
                    // to avoid duplication in the final output
                    var firstSansLeading = first.WithLeadingTrivia(SyntaxTriviaList.Empty);

                    // Replace the token list
                    newModifiers[0] = publicToken;
                    newModifiers.Insert(1, firstSansLeading);
                }
                else
                {
                    // No existing modifiers at all, so just add 'public'
                    newModifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                }
            }

            // Rebuild the declaration with the updated list of modifiers
            var updatedDeclaration = declaration.WithModifiers(
                SyntaxFactory.TokenList(newModifiers)
            );

            // Replace the old syntax node with the new one
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
                return document;

            var newRoot = root.ReplaceNode(declaration, updatedDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Repairs a single instance of the “UnsupportedTypeMapping” diagnostic
        /// by converting the .NET type to a correct equivalent based on the
        /// DataType in [OSStructureField(DataType = ...)].
        /// </summary>
        private static async Task<Document> FixSingleTypeMappingAsync(
            Document document,
            Location location,
            CancellationToken cancellationToken
        )
        {
            var editor = await DocumentEditor
                .CreateAsync(document, cancellationToken)
                .ConfigureAwait(false);
            var root = await editor
                .OriginalDocument.GetSyntaxRootAsync(cancellationToken)
                .ConfigureAwait(false);
            if (root is null)
                return document;

            // Identify the node that triggered the diagnostic (variable or property)
            var diagNode = root.FindNode(location.SourceSpan);
            if (diagNode is null)
                return document;

            SyntaxNode memberDecl = diagNode switch
            {
                VariableDeclaratorSyntax vds => vds.Parent,
                PropertyDeclarationSyntax pds => pds,
                _ => diagNode.Parent,
            };
            if (memberDecl is null)
                return document;

            // Collect attribute lists from either FieldDeclaration or PropertyDeclaration
            SyntaxList<AttributeListSyntax> attributeLists;
            if (memberDecl.Parent is FieldDeclarationSyntax fieldDecl)
            {
                attributeLists = fieldDecl.AttributeLists;
            }
            else if (memberDecl is PropertyDeclarationSyntax propDecl)
            {
                attributeLists = propDecl.AttributeLists;
            }
            else
            {
                return document;
            }

            // Look for OSStructureField(DataType=...)
            var osStructureFieldAttr = attributeLists
                .SelectMany(list => list.Attributes)
                .FirstOrDefault(attr =>
                    attr.Name.ToString() is "OSStructureField" or "OSStructureFieldAttribute"
                );

            if (osStructureFieldAttr?.ArgumentList is null)
                return document;

            var dataTypeArg = osStructureFieldAttr.ArgumentList.Arguments.FirstOrDefault(a =>
                a.NameEquals?.Name.Identifier.Text == "DataType"
            );
            if (dataTypeArg?.Expression is not MemberAccessExpressionSyntax memberAccessExpr)
                return document;

            // Get the .NET type that aligns with the specified 'OSDataType' (for example "Text" -> "string").
            var suggestedType = GetSuggestedType(memberAccessExpr.Name.Identifier.Text);
            if (string.IsNullOrEmpty(suggestedType))
                return document;

            if (memberDecl is VariableDeclarationSyntax varDeclSyntax)
            {
                var currentTypeNode = varDeclSyntax.Type;
                if (!TypesAreEquivalent(currentTypeNode.ToString(), suggestedType))
                {
                    var newType = SyntaxFactory
                        .ParseTypeName(suggestedType)
                        .WithLeadingTrivia(currentTypeNode.GetLeadingTrivia())
                        .WithTrailingTrivia(currentTypeNode.GetTrailingTrivia());

                    var newVarDecl = varDeclSyntax.WithType(newType);
                    editor.ReplaceNode(varDeclSyntax, newVarDecl);
                }
            }
            else if (memberDecl is PropertyDeclarationSyntax propertyDecl)
            {
                var currentTypeNode = propertyDecl.Type;
                if (!TypesAreEquivalent(currentTypeNode.ToString(), suggestedType))
                {
                    var newType = SyntaxFactory
                        .ParseTypeName(suggestedType)
                        .WithLeadingTrivia(currentTypeNode.GetLeadingTrivia())
                        .WithTrailingTrivia(currentTypeNode.GetTrailingTrivia());

                    var newPropDecl = propertyDecl.WithType(newType);
                    editor.ReplaceNode(propertyDecl, newPropDecl);
                }
            }

            return editor.GetChangedDocument();
        }

        /// <summary>
        /// Removes one or more leading underscores from a method name to comply with naming rules.
        /// </summary>
        private static async Task<Document> RemoveLeadingUnderscoreAsync(
            Document document,
            MethodDeclarationSyntax methodDecl,
            string newName,
            CancellationToken cancellationToken
        )
        {
            var newMethodDecl = methodDecl.WithIdentifier(SyntaxFactory.Identifier(newName));
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
                return document;

            var newRoot = root.ReplaceNode(methodDecl, newMethodDecl);
            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Checks if two type names are effectively the same,
        /// ignoring differences like "System.Int32" vs "int".
        /// </summary>
        private static bool TypesAreEquivalent(string currentType, string suggestedType)
        {
            static string Normalize(string t) => t.Replace("System.", "").Trim();
            return Normalize(currentType)
                .Equals(Normalize(suggestedType), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Provides a mapping from the 'OSDataType' enum’s name (like "Text", "Integer", "DateTime")
        /// to a corresponding .NET type string (like "string", "int", "System.DateTime").
        /// </summary>
        private static string GetSuggestedType(string dataTypeName)
        {
            return dataTypeName switch
            {
                "Text" or "PhoneNumber" or "Email" => "string",
                "Integer" => "int",
                "LongInteger" => "long",
                "Decimal" or "Currency" => "decimal",
                "Boolean" => "bool",
                "Date" or "DateTime" or "Time" => "DateTime",
                "BinaryData" => "byte[]",
                _ => null,
            };
        }
    }
}

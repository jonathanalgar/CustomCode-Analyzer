using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit.Abstractions;

namespace CustomCode_Analyzer.Tests
{
    public static class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        /// <summary>
        /// Creates a <see cref="DiagnosticResult"/> for the given diagnostic ID, preserving the rest of the
        /// diagnostic’s information to be filled in by the caller. This helps reduce boilerplate in test methods.
        /// </summary>
        public static DiagnosticResult Diagnostic(string diagnosticId) =>
            CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

        /// <summary>
        /// Analyzes the provided source code using <typeparamref name="TAnalyzer"/> and verifies whether the
        /// reported diagnostics match the expected results.
        /// </summary>
        public static async Task VerifyAnalyzerAsync(
            string source,
            TestContext testContext,
            bool skipSDKreference = false,
            params DiagnosticResult[] expected
        )
        {
            if (!skipSDKreference)
            {
                source = @"using System;using OutSystems.ExternalLibraries.SDK;" + source;
            }
            var logger = new DetailedTestLogger(testContext);
            var test = new Test(logger, skipSDKreference) { TestCode = source };
            logger.WriteLine($"Running test: {testContext.TestName}");
            logger.WriteLine("\nAnalyzing source code:");
            logger.WriteLine(source);

            test.ExpectedDiagnostics.AddRange(expected);
            logger.WriteLine("\nExpected diagnostics:");
            foreach (var diagnostic in expected)
            {
                logger.WriteLine(
                    $"- {diagnostic.Id}: {diagnostic.MessageFormat} at {diagnostic.Spans[0]}"
                );
            }

            await test.RunAsync();
        }

        /// <summary>
        /// Analyzes the provided source code using <typeparamref name="TAnalyzer"/>,
        /// applies any applicable <typeparamref name="TCodeFix"/> to fix the diagnostics, and then verifies the
        /// resulting code matches <paramref name="fixedSource"/>.
        public static async Task VerifyCodeFixAsync<TCodeFix>(
            string source,
            TestContext testContext,
            string fixedSource,
            bool skipSDKreference = false,
            params DiagnosticResult[] expected
        )
            where TCodeFix : CodeFixProvider, new()
        {
            if (!skipSDKreference)
            {
                source = @"using System;using OutSystems.ExternalLibraries.SDK;" + source;
                fixedSource = @"using System;using OutSystems.ExternalLibraries.SDK;" + fixedSource;
            }

            var logger = new DetailedTestLogger(testContext);
            var test = new CodeFixTest<TCodeFix>(logger, skipSDKreference)
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            logger.WriteLine($"Running code fix test: {testContext.TestName}");
            logger.WriteLine("\nOriginal source code:");
            logger.WriteLine(source);
            logger.WriteLine("\nExpected fixed code:");
            logger.WriteLine(fixedSource);

            test.ExpectedDiagnostics.AddRange(expected);

            await test.RunAsync();
        }

        /// <summary>
        /// Internal test class that builds on MSTest + Roslyn test infrastructure to
        /// verify analyzer diagnostics. Inherits <see cref="CSharpAnalyzerTest{TAnalyzer, TVerifier}"/>.
        /// </summary>
        private class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            private readonly ITestOutputHelper _logger;

            public Test(ITestOutputHelper logger, bool skipSDKreference = false)
            {
                _logger = logger;
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
                if (!skipSDKreference)
                {
                    ReferenceAssemblies = ReferenceAssemblies.WithPackages(
                        [new PackageIdentity("OutSystems.ExternalLibraries.SDK", "1.5.0")]
                    );
                }

                SolutionTransforms.Add(
                    (solution, projectId) =>
                    {
                        var compilationOptions = solution.GetProject(projectId)?.CompilationOptions;
                        if (compilationOptions != null)
                        {
                            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                                compilationOptions.SpecificDiagnosticOptions.SetItems(
                                    CSharpVerifierHelper.NullableWarnings
                                )
                            );
                            solution = solution.WithProjectCompilationOptions(
                                projectId,
                                compilationOptions
                            );
                        }
                        return solution;
                    }
                );
            }

            protected override CompilationOptions CreateCompilationOptions()
            {
                var compilationOptions = base.CreateCompilationOptions();
                return compilationOptions.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(
                        CSharpVerifierHelper.NullableWarnings
                    )
                );
            }
        }

        /// <summary>
        /// Internal test class that builds on MSTest + Roslyn test infrastructure to
        /// verify code fixes from a given <typeparamref name="TCodeFix"/>. Inherits
        /// <see cref="CSharpCodeFixTest{TAnalyzer, TCodeFix, TVerifier}"/>.
        /// </summary>
        private class CodeFixTest<TCodeFix>
            : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
            where TCodeFix : CodeFixProvider, new()
        {
            private readonly ITestOutputHelper _logger;

            public CodeFixTest(ITestOutputHelper logger, bool skipSDKreference = false)
            {
                _logger = logger;
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
                if (!skipSDKreference)
                {
                    ReferenceAssemblies = ReferenceAssemblies.WithPackages(
                        [new PackageIdentity("OutSystems.ExternalLibraries.SDK", "1.5.0")]
                    );
                }

                SolutionTransforms.Add(
                    (solution, projectId) =>
                    {
                        var compilationOptions = solution.GetProject(projectId)?.CompilationOptions;
                        if (compilationOptions != null)
                        {
                            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                                compilationOptions.SpecificDiagnosticOptions.SetItems(
                                    CSharpVerifierHelper.NullableWarnings
                                )
                            );
                            solution = solution.WithProjectCompilationOptions(
                                projectId,
                                compilationOptions
                            );
                        }
                        return solution;
                    }
                );
            }

            protected override CompilationOptions CreateCompilationOptions()
            {
                var compilationOptions = base.CreateCompilationOptions();
                return compilationOptions.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(
                        CSharpVerifierHelper.NullableWarnings
                    )
                );
            }
        }
    }
}

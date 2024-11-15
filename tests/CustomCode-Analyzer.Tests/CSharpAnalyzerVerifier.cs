using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit.Abstractions;

namespace CustomCode_Analyzer.Tests
{
    // Generic verifier class for analyzer tests
    public static class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        // Create a diagnostic result for the specified rule
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

        // Main method to verify analyzer behavior against source code
        public static async Task VerifyAnalyzerAsync(string source, TestContext testContext, bool skipSDKreference = false, params DiagnosticResult[] expected)
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

            // Add expected diagnostics to test
            test.ExpectedDiagnostics.AddRange(expected);

            // Log expected diagnostics for debugging
            logger.WriteLine("\nExpected diagnostics:");
            foreach (var diagnostic in expected)
            {
                logger.WriteLine($"- {diagnostic.Id}: {diagnostic.MessageFormat} at {diagnostic.Spans[0]}");
            }

            // Run the analyzer test
            await test.RunAsync();
        }

        // Internal test class that configures and runs individual analyzer tests
        private class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            private readonly ITestOutputHelper _logger;

            public Test(ITestOutputHelper logger, bool skipSDKreference = false)
            {
                // Configure test to use .NET 8.0 reference assemblies
                _logger = logger;
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

                if(!skipSDKreference)
                {
                    ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("OutSystems.ExternalLibraries.SDK", "1.5.0")]);
                }

                // Add solution-wide configuration for nullable warnings
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId)?.CompilationOptions;
                    if (compilationOptions != null)
                    {
                        // Configure specific diagnostic options for nullable warnings
                        compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                            compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                        solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
                    }
                    return solution;
                });
            }

            // Override to configure specific compilation options for each test
            protected override CompilationOptions CreateCompilationOptions()
            {
                var compilationOptions = base.CreateCompilationOptions();
                return compilationOptions.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
            }
        }
    }
}
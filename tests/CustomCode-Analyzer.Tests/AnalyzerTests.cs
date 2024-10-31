using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using static Analyzer;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;

namespace CustomCode_Analyzer.Tests
{
    public static class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpAnalyzerVerifier<TAnalyzer, MSTestVerifier>.Diagnostic(diagnosticId);

        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test { TestCode = source };

            test.TestState.Sources.Add(@"
            using System;
            [AttributeUsage(AttributeTargets.Interface)]
            public class OSInterfaceAttribute : Attribute { }
        ");

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private class Test : CSharpAnalyzerTest<TAnalyzer, MSTestVerifier>
        {
            public Test()
            {
                ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0",
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref",
                        "8.0.0"),
                    Path.Combine("ref", "net8.0"));

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId)?.CompilationOptions;
                    if (compilationOptions != null)
                    {
                        compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                            compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                        solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
                    }
                    return solution;
                });
            }

            protected override CompilationOptions CreateCompilationOptions()
            {
                var compilationOptions = base.CreateCompilationOptions();
                return compilationOptions.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
            }
        }
    }

    public static class CSharpVerifierHelper
    {
        public static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings
        {
            get
            {
                return ImmutableDictionary.CreateRange(new[]
                {
                        new KeyValuePair<string, ReportDiagnostic>("CS8632", ReportDiagnostic.Error),
                        new KeyValuePair<string, ReportDiagnostic>("CS8669", ReportDiagnostic.Error)
                    });
            }
        }
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public class OSInterfaceAttribute : Attribute { }

    [TestClass]
    public class AnalyzerTests
    {
        [TestMethod]
        public async Task TodoComment_ReportsWarning()
        {
            var test = @"
public class TestClass 
{
    // TODO: Implement this method
    public void TestMethod() 
    {
    }
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.TodoComment)
                .WithSpan(4, 5, 4, 35)
                .WithArguments("TestMethod");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task NonPublicOSInterface_ReportsWarning()
        {
            var test = @"
[OSInterface]
interface ITestInterface 
{
    void TestMethod();
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NonPublicInterface)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("ITestInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task UnderscorePrefix_ReportsWarning()
        {
            var test = @"
[OSInterface]
public interface ITestInterface 
{
    void _TestMethod();
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
                .WithSpan(5, 5, 5, 24)
                .WithArguments("Method", "_TestMethod");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task MultipleOSInterfaces_ReportsWarning()
        {
            var test = @"
[OSInterface]
public interface IFirstInterface 
{
    void TestMethod();
}

[OSInterface]
public interface ISecondInterface 
{
    void TestMethod();
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.ManyInterfaces)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("IFirstInterface, ISecondInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, expected);
        }


        [TestMethod]
        public async Task NoOSInterface_ReportsWarning()
        {
            var test = @"
public interface ITestInterface 
{
    void TestMethod();
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NoSingleInterface)
                .WithSpan(2, 1, 5, 2);

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task ValidCode_NoWarnings()
        {
            var test = @"
[OSInterface]
public interface ITestInterface 
{
    void ValidMethod();
}";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test);
        }
    }
}
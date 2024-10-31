using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using static Analyzer;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;
using System.Text;
using Xunit.Abstractions;

namespace CustomCode_Analyzer.Tests
{
    public class DetailedTestLogger : ITestOutputHelper
    {
        private readonly TestContext _testContext;
        private readonly StringBuilder _output = new();

        public DetailedTestLogger(TestContext testContext)
        {
            _testContext = testContext;
        }

        public void WriteLine(string message)
        {
            _output.AppendLine(message);
            _testContext.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public void Clear() => _output.Clear();

        public override string ToString() => _output.ToString();
    }



    public static class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpAnalyzerVerifier<TAnalyzer, MSTestVerifier>.Diagnostic(diagnosticId);

        public static async Task VerifyAnalyzerAsync(string source, TestContext testContext, params DiagnosticResult[] expected)
        {
            var logger = new DetailedTestLogger(testContext);
            var test = new Test(logger) { TestCode = source };

            logger.WriteLine($"Running test: {testContext.TestName}");
            logger.WriteLine("\nAnalyzing source code:");
            logger.WriteLine(source);

            test.TestState.Sources.Add(@"
                using System;
                [AttributeUsage(AttributeTargets.Interface)]
                public class OSInterfaceAttribute : Attribute { }
            ");

            test.ExpectedDiagnostics.AddRange(expected);

            logger.WriteLine("\nExpected diagnostics:");
            foreach (var diagnostic in expected)
            {
                logger.WriteLine($"- {diagnostic.Id}: {diagnostic.MessageFormat} at {diagnostic.Spans[0]}");
            }

            await test.RunAsync();
        }

        private class Test : CSharpAnalyzerTest<TAnalyzer, MSTestVerifier>
        {
            private readonly ITestOutputHelper _logger;

            public Test(ITestOutputHelper logger)
            {
                _logger = logger;
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
        public TestContext? TestContext { get; set; }

        [TestMethod]
        public async Task TodoComment_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

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

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task NonPublicOSInterface_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
interface ITestInterface 
{
    void TestMethod();
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NonPublicInterface)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("ITestInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task UnderscorePrefix_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
public interface ITestInterface 
{
    void _TestMethod();
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
                .WithSpan(5, 5, 5, 24)
                .WithArguments("Method", "_TestMethod");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task MultipleOSInterfaces_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

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

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task NoOSInterface_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
public interface ITestInterface 
{
    void TestMethod();
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NoSingleInterface)
                .WithSpan(2, 1, 5, 2);

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task ValidCode_NoWarnings()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
public interface ITestInterface 
{
    void ValidMethod();
}";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext);
        }
    }
}
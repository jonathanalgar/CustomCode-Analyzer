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

            // Add all required attribute definitions
            test.TestState.Sources.Add(@"
        using System;
        
        [AttributeUsage(AttributeTargets.Interface)]
        public class OSInterfaceAttribute : Attribute { }
        
        [AttributeUsage(AttributeTargets.Struct)]
        public class OSStructureAttribute : Attribute { }
        
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public class OSStructureFieldAttribute : Attribute { }
        
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public class OSIgnoreAttribute : Attribute { }
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
}

public class TestImplementation : ITestInterface 
{
    public void TestMethod() { }
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
}

public class TestImplementation : ITestInterface 
{
    public void _TestMethod() { }
}";

            var expected = new[]
            {
        // Warning for the interface method
        CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
            .WithSpan(5, 5, 5, 24)
            .WithArguments("Method", "_TestMethod"),
            
        // Warning for the implementing method
        CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
            .WithSpan(10, 5, 10, 34)
            .WithArguments("Method", "_TestMethod")
    };

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
}

public class FirstImplementation : IFirstInterface 
{
    public void TestMethod() { }
}

public class SecondImplementation : ISecondInterface 
{
    public void TestMethod() { }
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
        public async Task EmptyInterface_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
public interface ITestInterface 
{
}";

            var expected = new[] {
        // Empty interface warning
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.EmptyInterface)
            .WithSpan(3, 18, 3, 32)  // Changed end column from 31 to 32
            .WithArguments("ITestInterface"),
            
        // No implementing class warning
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.NoImplementingClass)
            .WithSpan(2, 1, 5, 2)
            .WithArguments("ITestInterface")
    };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }


        [TestMethod]
        public async Task NoParameterlessConstructor_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
public interface ITestInterface 
{
    void TestMethod();
}

public class TestImplementation : ITestInterface 
{
    public TestImplementation(int value)
    {
    }

    public void TestMethod() { }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NoParameterlessConstructor)
                .WithSpan(8, 14, 8, 32)  // Changed end column from 31 to 32
                .WithArguments("TestImplementation");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task MultipleImplementations_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
public interface ITestInterface 
{
    void TestMethod();
}

public class FirstImplementation : ITestInterface 
{
    public void TestMethod() { }
}

public class SecondImplementation : ITestInterface 
{
    public void TestMethod() { }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.MultipleImplementations)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("ITestInterface", "FirstImplementation, SecondImplementation");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task NonPublicImplementingClass_ReportsError()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
public interface ITestInterface 
{
    void TestMethod();
}

internal class TestImplementation : ITestInterface  // internal instead of public
{
    public void TestMethod() { }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NonPublicImplementation)
                .WithSpan(8, 16, 8, 34)
                .WithArguments("ITestInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task NonPublicStruct_ReportsError()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSStructure]
internal struct TestStruct  // internal instead of public
{
    public int Value;
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NonPublicStruct)
                .WithSpan(3, 17, 3, 27)
                .WithArguments("TestStruct");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task NonPublicIgnoredField_ReportsError()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSStructure]
public struct TestStruct
{
    [OSIgnore]
    private int IgnoredValue;  // private instead of public

    [OSIgnore]
    internal string IgnoredName { get; set; }  // internal instead of public
}";

            var expected = new[]
            {
        // Error for private field
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.NonPublicIgnoredField)
            .WithSpan(6, 17, 6, 29)
            .WithArguments("IgnoredValue", "TestStruct"),

        // Error for internal property
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.NonPublicIgnoredField)
            .WithSpan(9, 21, 9, 32)
            .WithArguments("IgnoredName", "TestStruct")
    };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task NonPublicStructureField_ReportsError()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSStructure]
public struct TestStruct
{
    [OSStructureField]
    private int Value;  // private instead of public

    [OSStructureField]
    internal string Name { get; set; }  // internal instead of public
}";

            var expected = new[]
            {
        // Error for private field
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.NonPublicStructureField)
            .WithSpan(6, 17, 6, 22)
            .WithArguments("Value", "TestStruct"),

        // Error for internal property
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.NonPublicStructureField)
            .WithSpan(9, 21, 9, 25)
            .WithArguments("Name", "TestStruct")
    };

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
}

public class TestImplementation : ITestInterface 
{
    public void ValidMethod() { }
}";

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext);
        }

        [TestMethod]
        public async Task NoImplementingClass_ReportsError()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
public interface ITestInterface 
{
    void TestMethod();
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NoImplementingClass)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("ITestInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }
    }
}
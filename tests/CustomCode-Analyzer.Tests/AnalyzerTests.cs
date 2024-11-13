using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Threading.Tasks;
using static Analyzer;
using System.Collections.Immutable;
using System.Text;
using Xunit.Abstractions;

namespace CustomCode_Analyzer.Tests
{
    /// <summary>
    /// Provides detailed logging capabilities for analyzer tests.
    /// Implements ITestOutputHelper to capture and display test execution details.
    /// </summary>
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

    // Generic verifier class for analyzer tests
    public static class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        // Create a diagnostic result for the specified rule
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

        // Main method to verify analyzer behavior against source code
        public static async Task VerifyAnalyzerAsync(string source, TestContext testContext, params DiagnosticResult[] expected)
        {
            var logger = new DetailedTestLogger(testContext);
            var test = new Test(logger) { TestCode = source };

            logger.WriteLine($"Running test: {testContext.TestName}");
            logger.WriteLine("\nAnalyzing source code:");
            logger.WriteLine(source);

            // Add required attribute definitions that would normally be in referenced assemblies
            test.TestState.Sources.Add(@"
        using System;
        
        [AttributeUsage(AttributeTargets.Interface)]
        public class OSInterfaceAttribute : Attribute 
        { 
            public string Name { get; set; }
            public string Description { get; set; }
            public string IconResourceName { get; set; }
            public string OriginalName { get; set; }
        }
        
        [AttributeUsage(AttributeTargets.Struct)]
        public class OSStructureAttribute : Attribute 
        { 
            public string Name { get; set; }
            public string Description { get; set; }
            public string OriginalName { get; set; }
        }
        
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public class OSStructureFieldAttribute : Attribute 
        {
            public string Description { get; set; }
            public int Length { get; set; } = 50;
            public string OriginalName { get; set; }
        }
        
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public class OSIgnoreAttribute : Attribute { }
    ");
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

            public Test(ITestOutputHelper logger)
            {
                // Configure test to use .NET 8.0 reference assemblies
                _logger = logger;
                ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0",
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref",
                        "8.0.0"),
                    Path.Combine("ref", "net8.0"));

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

    // Helper class to configure nullable reference type warnings
    public static class CSharpVerifierHelper
    {
        // Dictionary of nullable warning configurations
        public static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings
        {
            get
            {
                return ImmutableDictionary.CreateRange(new[]
                {
                        // Configure specific nullable warning codes as errors
                        new KeyValuePair<string, ReportDiagnostic>("CS8632", ReportDiagnostic.Error),
                        new KeyValuePair<string, ReportDiagnostic>("CS8669", ReportDiagnostic.Error)
                    });
            }
        }
    }

    // Define OSInterface attribute for test use
    [AttributeUsage(AttributeTargets.Interface)]
    public class OSInterfaceAttribute : Attribute { }

    [TestClass]
    public class AnalyzerTests
    {
        public TestContext? TestContext { get; set; }


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
            // Warning for non-public interface with OSInterface - spans the entire interface declaration
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
        public async Task DuplicateExplicitStructureName_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSStructure(Name = ""MyStructure"")]  // Using escaped quotes
public struct Structure1
{
    public int Value;
}

[OSStructure(Name = ""MyStructure"")]  // Using escaped quotes
public struct Structure2
{
    public float Value;
}";
            // Warning for duplicate structure name - spans first occurrence of the duplicate name
            // Reports both struct names (Structure1, Structure2) and the duplicate name they share (MyStructure)
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.DuplicateStructureName)
                .WithSpan(3, 15, 3, 25)
                .WithArguments("Structure1, Structure2", "MyStructure");

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
            // Warning for multiple OSInterface attributes - spans the first interface's entire declaration
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
            // Warning for missing OSInterface attribute - spans the interface declaration
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
        // Warning for empty interface - spans the interface name
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.EmptyInterface)
            .WithSpan(3, 18, 3, 32)
            .WithArguments("ITestInterface"),
            
        // Warning for missing implementing class - spans the entire interface declaration
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
            // Warning for missing parameterless constructor - spans the implementing class name
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
            // Warning for multiple implementations of OSInterface - spans the interface declaration
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.MultipleImplementations)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("ITestInterface", "FirstImplementation, SecondImplementation");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task NoPublicMembers_ReportsError()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSStructure]
public struct TestStruct
{
    private int PrivateValue;
    internal string InternalProperty { get; set; }
}";
            // Warning for struct with no public members - spans the struct name
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NoPublicMembers)
                .WithSpan(3, 15, 3, 25)
                .WithArguments("TestStruct");

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
            // Warning for non-public implementing class - spans the class name
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
            // Warning for non-public struct with OSStructure - spans struct name
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
    public int PublicValue;  // Added public member

    [OSIgnore]
    private int IgnoredValue;  // private instead of public

    [OSIgnore]
    internal string IgnoredName { get; set; }  // internal instead of public
}";

            var expected = new[]
            {
        // Warning for private field with OSIgnore - spans the field identifier
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.NonPublicIgnoredField)
            .WithSpan(8, 17, 8, 29)
            .WithArguments("IgnoredValue", "TestStruct"),

        // Warning for internal property with OSIgnore - spans the property identifier
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.NonPublicIgnoredField)
            .WithSpan(11, 21, 11, 32)
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
    public int PublicValue;  // Added public member

    [OSStructureField]
    private int Value;  // private instead of public

    [OSStructureField]
    internal string Name { get; set; }  // internal instead of public
}";

            var expected = new[]
            {
        // Warning for private field with OSStructureField - spans the field name
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.NonPublicStructureField)
            .WithSpan(8, 17, 8, 22)
            .WithArguments("Value", "TestStruct"),

        // Warning for internal property with OSStructureField - spans the property name
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.NonPublicStructureField)
            .WithSpan(11, 21, 11, 25)
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

            // No diagnostics expected - all code follows the rules:
            // - Public interface with OSInterface attribute
            // - Public implementing class
            // - Public method
            // - Valid method name
            // - Single implementation
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext);
        }

        [TestMethod]
        public async Task NameTooLong_ExternalLibraryName_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface(Name = ""ThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed"")]
public interface ITestInterface 
{
    void Method();
}

public class TestImplementation : ITestInterface 
{
    public void Method() { }
}";
            // Warning for library name exceeding maximum length - spans interface declaration
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NameTooLong)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("ThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task NameTooLong_DefaultInterfaceNameWithoutI_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
public interface IThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed 
{
    void Method();
}

public class TestImplementation : IThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed 
{
    public void Method() { }
}";
            // Warning for interface name too long (after removing 'I' prefix) - spans interface declaration
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NameTooLong)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("ThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task NameStartsWithNumber_Interface_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface(Name = ""123Service"")]
public interface ITestInterface 
{
    void Method();
}

public class TestImplementation : ITestInterface 
{
    public void Method() { }
}";
            // Warning for name starting with number - spans the interface declaration
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NameStartsWithNumber)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("123Service");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }

        [TestMethod]
        public async Task InvalidCharactersInName_Interface_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface(Name = ""Invalid*Name@123"")]
public interface ITestInterface 
{
    void Method();
}

public class TestImplementation : ITestInterface 
{
    public void Method() { }
}";
            // Warning for invalid characters in name - spans the interface declaration
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.InvalidCharactersInName)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("Invalid*Name@123", "*, @");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
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
            // Warning for interface without implementing class - spans entire interface declaration
            var expected = CSharpAnalyzerVerifier<Analyzer>.Diagnostic(DiagnosticIds.NoImplementingClass)
                .WithSpan(2, 1, 6, 2)
                .WithArguments("ITestInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }
        [TestMethod]
        public async Task ReferenceParameter_ReportsWarning()
        {
            Assert.IsNotNull(TestContext, "TestContext should not be null");

            var test = @"
[OSInterface]
public interface ITestInterface 
{
    void UpdateValue(ref int value);
    void GetValue(out string text);
    void ReadValue(in double number);
}

public class TestImplementation : ITestInterface 
{
    public void UpdateValue(ref int value) { value += 1; }
    public void GetValue(out string text) { text = ""test""; }
    public void ReadValue(in double number) { }
}";

            var expected = new[]
            {
        // Warning for ref parameter - spans the entire parameter declaration
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.ReferenceParameter)
            .WithSpan(5, 22, 5, 35)
            .WithArguments("value", "UpdateValue"),

        // Warning for out parameter - spans the entire parameter declaration
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.ReferenceParameter)
            .WithSpan(6, 19, 6, 34)
            .WithArguments("text", "GetValue"),

        // Warning for in parameter - spans the entire parameter declaration
        CSharpAnalyzerVerifier<Analyzer>
            .Diagnostic(DiagnosticIds.ReferenceParameter)
            .WithSpan(7, 20, 7, 36)
            .WithArguments("number", "ReadValue")
    };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext, expected);
        }
    }
}
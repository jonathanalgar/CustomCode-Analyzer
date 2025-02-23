using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CustomCode_Analyzer.Analyzer;

namespace CustomCode_Analyzer.Tests
{
    [TestClass]
    public class AnalyzerTests
    {
        public TestContext TestContext { get; set; } = null!;

        /// <summary>
        /// Helper method to verify both diagnostic and code fix for a test case.
        /// </summary>
        private async Task VerifyDiagnosticAndFix(
            string source,
            string expectedFixed,
            DiagnosticResult[] expectedDiagnostics
        )
        {
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                source,
                TestContext,
                skipSDKreference: false,
                expectedDiagnostics
            );

            await CSharpAnalyzerVerifier<Analyzer>.VerifyCodeFixAsync<Fixer>(
                source,
                TestContext,
                expectedFixed,
                skipSDKreference: false,
                expectedDiagnostics
            );
        }

        // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05001 - not implementing

        // --------------- NoSingleInterfaceRule (OS-ELG-MODL-05002) ---------------
        [TestMethod]
        public async Task NoSingleInterfaceRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
public interface ITestInterface 
{
    void TestMethod();
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NoSingleInterface)
                .WithSpan(2, 18, 2, 32);

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task NoSingleInterfaceRule_InNamespace_ReportsWarning()
        {
            var test =
                @"
namespace TestNamespace 
{
    public interface ITestInterface 
    {
        void TestMethod();
    }
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NoSingleInterface)
                .WithSpan(4, 22, 4, 36);

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- ManyInterfacesRule (OS-ELG-MODL-05003) ------------------
        [TestMethod]
        public async Task ManyInterfacesRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
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

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyInterfaces)
                    .WithSpan(3, 18, 3, 33)
                    .WithArguments("IFirstInterface, ISecondInterface"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyInterfaces)
                    .WithSpan(9, 18, 9, 34)
                    .WithArguments("IFirstInterface, ISecondInterface"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task ManyInterfacesRule_InSingleNamespace_ReportsWarning()
        {
            var test =
                @"
namespace MyNamespace
{
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
    }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyInterfaces)
                    .WithSpan(5, 22, 5, 37)
                    .WithArguments("IFirstInterface, ISecondInterface"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyInterfaces)
                    .WithSpan(11, 22, 11, 38)
                    .WithArguments("IFirstInterface, ISecondInterface"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task MultipleOSInterfaces_InDifferentNamespaces_ReportsWarning()
        {
            var test =
                @"
namespace First
{
    [OSInterface]
    public interface IFirstInterface 
    {
        void TestMethod();
    }

    public class FirstImplementation : IFirstInterface 
    {
        public void TestMethod() { }
    }
}

namespace Second
{
    [OSInterface]
    public interface ISecondInterface 
    {
        void TestMethod();
    }

    public class SecondImplementation : ISecondInterface 
    {
        public void TestMethod() { }
    }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyInterfaces)
                    .WithSpan(5, 22, 5, 37)
                    .WithArguments("IFirstInterface, ISecondInterface"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyInterfaces)
                    .WithSpan(19, 22, 19, 38)
                    .WithArguments("IFirstInterface, ISecondInterface"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- NonPublicInterfaceRule (OS-ELG-MODL-05004) --------------
        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task NonPublicInterfaceRule_InGlobalScope_ExplicitInternal_ReportsWarningAndFixes()
        {
            var test =
                @"
[OSInterface]
internal interface ITestInterface 
{
    void ProcessData();
}
public class TestImplementation : ITestInterface
{
    public void ProcessData() { }
}";

            var testfix =
                @"
[OSInterface]
public interface ITestInterface 
{
    void ProcessData();
}
public class TestImplementation : ITestInterface
{
    public void ProcessData() { }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicInterface)
                    .WithSpan(3, 10, 3, 34)
                    .WithArguments("ITestInterface"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expected);
        }

        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task NonPublicInterfaceRule_InGlobalScope_ImplicitInternal_ReportsWarningAndFixes()
        {
            var test =
                @"
[OSInterface]
interface ITestInterface 
{
    void ProcessData();
}
public class TestImplementation : ITestInterface
{
    public void ProcessData() { }
}";

            var testfix =
                @"
[OSInterface]
public interface ITestInterface 
{
    void ProcessData();
}
public class TestImplementation : ITestInterface
{
    public void ProcessData() { }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicInterface)
                    .WithSpan(3, 1, 3, 25)
                    .WithArguments("ITestInterface"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expected);
        }

        // --------------- NonInstantiableInterfaceRule (OS-ELG-MODL-05005) --------
        [TestMethod]
        public async Task NonInstantiableInterfaceRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
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
                .Diagnostic(DiagnosticIds.NonInstantiableInterface)
                .WithSpan(8, 14, 8, 32)
                .WithArguments("TestImplementation");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task NonInstantiableInterfaceRule_InNamespace_ReportsWarning()
        {
            var test =
                @"
namespace TestNamespace
{
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
   }
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NonInstantiableInterface)
                .WithSpan(10, 17, 10, 35)
                .WithArguments("TestImplementation");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- MissingImplementationRule (OS-ELG-MODL-05006) -----------
        [TestMethod]
        public async Task MissingImplementationRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
[OSInterface]
public interface ITestInterface 
{
    void TestMethod();
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.MissingImplementation)
                .WithSpan(3, 18, 3, 32)
                .WithArguments("ITestInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task MissingImplementationRule_InNamespace_ReportsWarning()
        {
            var test =
                @"
namespace TestNamespace
{
    [OSInterface]
    public interface ITestInterface 
    {
        void TestMethod();
    }
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.MissingImplementation)
                .WithSpan(5, 22, 5, 36)
                .WithArguments("ITestInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- EmptyInterfaceRule (OS-ELG-MODL-05007) ------------------
        [TestMethod]
        public async Task EmptyInterfaceRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
[OSInterface]
public interface ITestInterface 
{
}";
            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.MissingImplementation)
                    .WithSpan(3, 18, 3, 32)
                    .WithArguments("ITestInterface"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.EmptyInterface)
                    .WithSpan(3, 18, 3, 32)
                    .WithArguments("ITestInterface"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task EmptyInterfaceRule_InNamespace_ReportsWarning()
        {
            var test =
                @"
namespace TestNamespace
{
    [OSInterface]
    public interface ITestInterface 
    {
    }
}";
            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.MissingImplementation)
                    .WithSpan(5, 22, 5, 36)
                    .WithArguments("ITestInterface"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.EmptyInterface)
                    .WithSpan(5, 22, 5, 36)
                    .WithArguments("ITestInterface"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- ManyImplementationRule (OS-ELG-MODL-05008) --------------
        [TestMethod]
        public async Task ManyImplementationRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
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

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyImplementation)
                    .WithSpan(8, 14, 8, 33)
                    .WithArguments("ITestInterface", "FirstImplementation, SecondImplementation"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyImplementation)
                    .WithSpan(13, 14, 13, 34)
                    .WithArguments("ITestInterface", "FirstImplementation, SecondImplementation"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task ManyImplementationRule_InSingleNamespace_ReportsWarning()
        {
            var test =
                @"
namespace TestNamespace
{
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
   }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyImplementation)
                    .WithSpan(10, 17, 10, 36)
                    .WithArguments("ITestInterface", "FirstImplementation, SecondImplementation"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyImplementation)
                    .WithSpan(15, 17, 15, 37)
                    .WithArguments("ITestInterface", "FirstImplementation, SecondImplementation"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task ManyImplementationRule_InDifferentNamespaces_ReportsWarning()
        {
            var test =
                @"
namespace First
{
   [OSInterface]
   public interface ITestInterface 
   {
       void TestMethod();
   }

   public class FirstImplementation : ITestInterface 
   {
       public void TestMethod() { }
   }
}

namespace Second
{
   public class SecondImplementation : First.ITestInterface 
   {
       public void TestMethod() { }
   }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyImplementation)
                    .WithSpan(10, 17, 10, 36)
                    .WithArguments("ITestInterface", "FirstImplementation, SecondImplementation"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ManyImplementation)
                    .WithSpan(18, 17, 18, 37)
                    .WithArguments("ITestInterface", "FirstImplementation, SecondImplementation"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05009 - not implementing

        // --------------- NonPublicStructRule (OS-ELG-MODL-05010) -----------------
        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task NonPublicStructRule_InGlobalScope_ExplicitInternal_ReportsWarningAndFixes()
        {
            var test =
                @"
[OSStructure]
internal struct TestStruct
{
   public int Value;
}";

            var testfix =
                @"
[OSStructure]
public struct TestStruct
{
   public int Value;
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicStruct)
                    .WithSpan(3, 10, 3, 27)
                    .WithArguments("TestStruct"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expected);
        }

        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task NonPublicStructRule_InNamespace_ImplicitInternal_ReportsWarningAndFixes()
        {
            var test =
                @"
namespace TestNamespace
{
    [OSStructure]
    struct TestStruct
    {
        public int Value;
    }

    namespace TestNamespace2 {
        [OSStructure]
        internal struct AnotherStruct
        {
            public string Name;
        }
    }
}";

            var testfix =
                @"
namespace TestNamespace
{
    [OSStructure]
    public struct TestStruct
    {
        public int Value;
    }

    namespace TestNamespace2 {
        [OSStructure]
        public struct AnotherStruct
        {
            public string Name;
        }
    }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicStruct)
                    .WithSpan(5, 5, 5, 22)
                    .WithArguments("TestStruct"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicStruct)
                    .WithSpan(12, 18, 12, 38)
                    .WithArguments("AnotherStruct"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expected);
        }

        // --------------- NonPublicStructureFieldRule (OS-ELG-MODL-05011) ------------
        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task NonPublicStructureFieldRule_InGlobalScope_ReportsWarningAndFixes()
        {
            var test =
                @"
[OSStructure]
public struct TestStruct
{
   public int PublicValue;

   [OSStructureField]
   private int Value;

   [OSStructureField]
   internal string Name { get; set; }
}";
            var testfix =
                @"
[OSStructure]
public struct TestStruct
{
   public int PublicValue;

   [OSStructureField]
   public int Value;

   [OSStructureField]
   public string Name { get; set; }
}";

            var expectedDiagnostics = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicStructureField)
                    .WithSpan(8, 16, 8, 21)
                    .WithArguments("Value", "TestStruct"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicStructureField)
                    .WithSpan(11, 20, 11, 24)
                    .WithArguments("Name", "TestStruct"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expectedDiagnostics);
        }

        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task NonPublicStructureFieldRule_InNamespace_ReportsWarningAndFixes()
        {
            var test =
                @"
namespace TestNamespace
{
   [OSStructure]
   public struct TestStruct
   {
       public int PublicValue;

       [OSStructureField]
       private int Value;

       [OSStructureField]
       internal string Name { get; set; }
   }
}";
            var testfix =
                @"
namespace TestNamespace
{
   [OSStructure]
   public struct TestStruct
   {
       public int PublicValue;

       [OSStructureField]
       public int Value;

       [OSStructureField]
       public string Name { get; set; }
   }
}";

            var expectedDiagnostics = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicStructureField)
                    .WithSpan(10, 20, 10, 25)
                    .WithArguments("Value", "TestStruct"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicStructureField)
                    .WithSpan(13, 24, 13, 28)
                    .WithArguments("Name", "TestStruct"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expectedDiagnostics);
        }

        // --------------- NonPublicIgnoredFieldRule (OS-ELG-MODL-05012) -----------
        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task NonPublicIgnoredFieldRule_InNamespace_ReportsWarningAndFixes()
        {
            var test =
                @"
namespace TestNamespace
{
    [OSStructure]
    public struct TestStruct
    {
        public int PublicValue;

        [OSIgnore]
        private int IgnoredValue;

        [OSIgnore]
        internal string IgnoredName { get; set; }
    }
}";

            var testfix =
                @"
namespace TestNamespace
{
    [OSStructure]
    public struct TestStruct
    {
        public int PublicValue;

        [OSIgnore]
        public int IgnoredValue;

        [OSIgnore]
        public string IgnoredName { get; set; }
    }
}";

            var expectedDiagnostics = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicIgnored)
                    .WithSpan(10, 21, 10, 33)
                    .WithArguments("IgnoredValue", "TestStruct"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicIgnored)
                    .WithSpan(13, 25, 13, 36)
                    .WithArguments("IgnoredName", "TestStruct"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expectedDiagnostics);
        }

        // --------------- EmptyStructureRule (OS-ELG-MODL-05013) -----------
        [TestMethod]
        public async Task EmptyStructureRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
[OSStructure]
public struct TestStruct
{
    private int PrivateValue;
    internal string InternalProperty { get; set; }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.EmptyStructure)
                .WithSpan(3, 15, 3, 25)
                .WithArguments("TestStruct");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task EmptyStructureRule_InNamespace_ReportsWarning()
        {
            var test =
                @"
namespace TestNamespace
{
    [OSStructure]
    public struct TestStruct
    {
        private int PrivateValue;
        internal string InternalProperty { get; set; }
    }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.EmptyStructure)
                .WithSpan(5, 19, 5, 29)
                .WithArguments("TestStruct");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05014 - not implementing

        // --------------- UnsupportedParameterTypeRule (OS-ELG-MODL-05015) --------

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_PublicField_UnsupportedType_ReportsWarning()
        {
            var test =
                @"
[OSStructure]
public struct MyStructure
{
    public int SupportedValue;
    public UnsupportedType UnsupportedField; // Unsupported type
}

public struct UnsupportedType { }
";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.UnsupportedParameterType)
                .WithSpan(6, 28, 6, 44)
                .WithArguments("MyStructure", "UnsupportedType");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_PublicProperty_UnsupportedType_ReportsWarning()
        {
            var test =
                @"
[OSStructure]
public struct MyStructure
{
    public string SupportedProperty { get; set; }
    public UnsupportedType UnsupportedProperty { get; set; } // Unsupported type
}

public struct UnsupportedType { }
";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.UnsupportedParameterType)
                .WithSpan(6, 28, 6, 47)
                .WithArguments("MyStructure", "UnsupportedType");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_PublicField_SupportedType_NoWarning()
        {
            var test =
                @"
using System.Collections.Generic;

[OSStructure]
public struct MyStructure
{
    public int SupportedValue;
    public string SupportedField;
    public List<int> SupportedList;
}
";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false
            );
        }

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_PublicProperty_StructWithOSStructure_NoWarning()
        {
            var test =
                @"
[OSStructure]
public struct NestedStructure
{
    public int Value;
}

[OSStructure]
public struct MyStructure
{
    public NestedStructure SupportedNested;
}
";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false
            );
        }

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_PublicField_ListOfSupportedType_NoWarning()
        {
            var test =
                @"
using System.Collections.Generic;

[OSStructure]
public struct MyStructure
{
    public List<string> SupportedList;
    public IEnumerable<int> SupportedEnumerable;
}
";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false
            );
        }

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_PublicField_ListOfUnsupportedType_ReportsWarning()
        {
            var test =
                @"
using System.Collections.Generic;

[OSStructure]
public struct MyStructure
{
    public List<UnsupportedType> UnsupportedList;
}

public struct UnsupportedType { }
    ";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.UnsupportedParameterType)
                .WithSpan(7, 34, 7, 49)
                .WithArguments("MyStructure", "System.Collections.Generic.List<UnsupportedType>");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_PrivateField_UnsupportedType_NoWarning()
        {
            var test =
                @"
[OSStructure]
public struct MyStructure
{
    public int ValidField;
    private UnsupportedType PrivateField; // Private field should not be analyzed
}

public struct UnsupportedType { }
";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false
            );
        }

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_InternalProperty_UnsupportedType_NoWarning()
        {
            var test =
                @"
[OSStructure]
public struct MyStructure
{
    public string ValidProperty { get; set; }
    internal UnsupportedType InternalProperty { get; set; } // Internal property should not be analyzed
}

public struct UnsupportedType { }
";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false
            );
        }

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_StructWithoutOSStructure_NoAnalysis()
        {
            var test =
                @"
public struct MyStructure
{
    public int Value;
    public UnsupportedType UnsupportedField; // Should not be analyzed as OSStructure is not present
}

public struct UnsupportedType { }
";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false
            );
        }

        [TestMethod]
        public async Task UnsupportedParameterTypeRule_InGlobalScope_PublicField_NullableStruct_NoWarning()
        {
            var test =
                @"
[OSStructure]
public struct NestedStructure
{
    public int Value;
}

[OSStructure]
public struct MyStructure
{
    public NestedStructure? NullableNested;    // Nullable struct OK
}
";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false
            );
        }

        // --------------- ParameterByReferenceRule (OS-ELG-MODL-05016) ------------
        [TestMethod]
        public async Task ParameterByReferenceRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
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
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ParameterByReference)
                    .WithSpan(5, 22, 5, 35)
                    .WithArguments("value", "UpdateValue"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ParameterByReference)
                    .WithSpan(6, 19, 6, 34)
                    .WithArguments("text", "GetValue"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ParameterByReference)
                    .WithSpan(7, 20, 7, 36)
                    .WithArguments("number", "ReadValue"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task ParameterByReferenceRule_InDifferentNamespaces_ReportsWarning()
        {
            var test =
                @"
namespace Contracts
{
    [OSInterface]
    public interface ITestInterface 
    {
        void UpdateValue(ref int value);
        void GetValue(out string text);
        void ReadValue(in double number);
    }
}

namespace Implementation
{
    public class TestImplementation : Contracts.ITestInterface 
    {
        public void UpdateValue(ref int value) { value += 1; }
        public void GetValue(out string text) { text = ""test""; }
        public void ReadValue(in double number) { }
    }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ParameterByReference)
                    .WithSpan(7, 26, 7, 39)
                    .WithArguments("value", "UpdateValue"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ParameterByReference)
                    .WithSpan(8, 23, 8, 38)
                    .WithArguments("text", "GetValue"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.ParameterByReference)
                    .WithSpan(9, 24, 9, 40)
                    .WithArguments("number", "ReadValue"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- UnsupportedTypeMappingRule (OS-ELG-MODL-05017) ----------
        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task UnsupportedTypeMappingRule_InGlobalScope_ReportsWarningAndFixes()
        {
            var source =
                @"
[OSStructure]
public struct TestStruct
{
    [OSStructureField(DataType = OSDataType.Text)]
    public int Value;

    [OSStructureField(DataType = OSDataType.Currency)]
    public string Name { get; set; }

    [OSStructureField(DataType = OSDataType.Currency)]
    public decimal Currency { get; set; }

    [OSStructureField(DataType = OSDataType.Decimal)]
    public decimal MyDecimal { get; set; }

    [OSStructureField(DataType = OSDataType.Email)]
    public string Email { get; set; }

    [OSStructureField(DataType = OSDataType.PhoneNumber)]
    public string Phone { get; set; }
}";

            var expectedFixed =
                @"
[OSStructure]
public struct TestStruct
{
    [OSStructureField(DataType = OSDataType.Text)]
    public string Value;

    [OSStructureField(DataType = OSDataType.Currency)]
    public decimal Name { get; set; }

    [OSStructureField(DataType = OSDataType.Currency)]
    public decimal Currency { get; set; }

    [OSStructureField(DataType = OSDataType.Decimal)]
    public decimal MyDecimal { get; set; }

    [OSStructureField(DataType = OSDataType.Email)]
    public string Email { get; set; }

    [OSStructureField(DataType = OSDataType.PhoneNumber)]
    public string Phone { get; set; }
}";

            var expectedDiagnostics = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.UnsupportedTypeMapping)
                    .WithSpan(6, 16, 6, 21)
                    .WithArguments("Value"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.UnsupportedTypeMapping)
                    .WithSpan(9, 19, 9, 23)
                    .WithArguments("Name"),
            };

            await VerifyDiagnosticAndFix(source, expectedFixed, expectedDiagnostics);
        }

        [TestMethod]
        public async Task UnsupportedTypeMappingRule_InGlobalScope_InferredFromDotNetType_NoDiagnostic()
        {
            var source =
                @"
[OSStructure]
public struct TestStruct
{
    [OSStructureField(DataType = OSDataType.InferredFromDotNetType)]
    public int SomeValue; // Should not raise any diagnostic with InferredFromDotNetType
}";

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                source,
                TestContext,
                skipSDKreference: false
            );
        }

        [TestMethod]
        public async Task UnsupportedTypeMappingRule_ArrayOfBytes_NoDiagnostic()
        {
            var source =
                @"
[OSStructure]
public struct TestStruct
{
    [OSStructureField(DataType = OSDataType.BinaryData)]
    public byte[] RawBytes;
}";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                source,
                TestContext,
                skipSDKreference: false
            );
        }

        // --------------- MissingPublicImplementationRule (OS-ELG-MODL-05018) --------
        [TestMethod]
        public async Task MissingPublicImplementationRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
[OSInterface]
public interface ITestInterface 
{
    void TestMethod();
}

internal class TestImplementation : ITestInterface
{
    public void TestMethod() { }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.MissingPublicImplementation)
                .WithSpan(8, 1, 8, 34)
                .WithArguments("ITestInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task MissingPublicImplementationRule_InDifferentNamespaces_ReportsWarning()
        {
            var test =
                @"
namespace Contracts
{
    [OSInterface]
    public interface ITestInterface 
    {
        void TestMethod();
    }
}

namespace Implementation
{
    internal class TestImplementation : Contracts.ITestInterface
    {
        public void TestMethod() { }
    }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.MissingPublicImplementation)
                .WithSpan(13, 5, 13, 38)
                .WithArguments("ITestInterface");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- NameMaxLengthExceededRule (OS-ELG-MODL-05019) -----------
        [TestMethod]
        public async Task NameMaxLengthExceededRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
[OSInterface(Name = ""ThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed"")]
public interface ITestInterface 
{
    void Method();
}

public class TestImplementation : ITestInterface 
{
    public void Method() { }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NameMaxLengthExceeded)
                .WithSpan(2, 21, 2, 101)
                .WithArguments(
                    "ThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed"
                );

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task NameMaxLengthExceededRule_InDifferentNamespaces_ReportsWarning()
        {
            var test =
                @"
namespace Contracts
{
    [OSInterface(Name = ""ThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed"")]
    public interface ITestInterface 
    {
        void Method();
    }
}

namespace Implementation
{
    public class TestImplementation : Contracts.ITestInterface 
    {
        public void Method() { }
    }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NameMaxLengthExceeded)
                .WithSpan(4, 25, 4, 105)
                .WithArguments(
                    "ThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed"
                );

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task NameTooLong_DefaultInterfaceNameWithoutI_ReportsWarning()
        {
            var test =
                @"
[OSInterface]
public interface IThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed 
{
    void Method();
}

public class TestImplementation : IThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed 
{
    public void Method() { }
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NameMaxLengthExceeded)
                .WithSpan(3, 18, 3, 97)
                .WithArguments(
                    "ThisExternalLibraryNameIsMuchTooLongAndExceedsFiftyCharactersWhichIsNotAllowed"
                );

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- NameBeginsWithNumbersRule (OS-ELG-MODL-05020) -----------
        [TestMethod]
        public async Task NameStartsWithNumber_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
[OSInterface(Name = ""123Service"")]
public interface ITestInterface 
{
    void Method();
}

public class TestImplementation : ITestInterface 
{
    public void Method() { }
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.NameBeginsWithNumbers)
                .WithSpan(2, 21, 2, 33)
                .WithArguments("123Service");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- UnsupportedCharactersInNameRule (OS-ELG-MODL-05021) -----
        [TestMethod]
        public async Task UnsupportedCharactersInNameRule_InGlobalScope_ReportsWarning()
        {
            var test =
                @"
[OSInterface(Name = ""Invalid*Name@123"")]
public interface ITestInterface 
{
    void Method();
}

public class TestImplementation : ITestInterface 
{
    public void Method() { }
}";
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.UnsupportedCharactersInName)
                .WithSpan(2, 21, 2, 39)
                .WithArguments("Invalid*Name@123", "*, @");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- NameBeginsWithUnderscoreRule (OS-ELG-MODL-05022) -------
        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task NameBeginsWithUnderscoreRule_InGlobalScope_ReportsWarningAndFixes()
        {
            var test =
                @"
[OSInterface]
public interface ITestInterface 
{
    void _TestMethod();
}

public class TestImplementation : ITestInterface 
{
    public void _TestMethod() { }
}";

            var testfix =
                @"
[OSInterface]
public interface ITestInterface 
{
    void TestMethod();
}

public class TestImplementation : ITestInterface 
{
    public void TestMethod() { }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
                    .WithSpan(5, 10, 5, 21)
                    .WithArguments("Method", "_TestMethod"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
                    .WithSpan(10, 17, 10, 28)
                    .WithArguments("Method", "_TestMethod"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expected);
        }

        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task NameBeginsWithUnderscoreRule_InDifferentNamespaces_ReportsWarningAndFixes()
        {
            var test =
                @"
namespace Contracts
{
    [OSInterface]
    public interface ITestInterface 
    {
        void __TestMethod();
    }
}

namespace Implementation
{
    public class TestImplementation : Contracts.ITestInterface 
    {
        public void __TestMethod() { }
    }
}";

            var testfix =
                @"
namespace Contracts
{
    [OSInterface]
    public interface ITestInterface 
    {
        void TestMethod();
    }
}

namespace Implementation
{
    public class TestImplementation : Contracts.ITestInterface 
    {
        public void TestMethod() { }
    }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
                    .WithSpan(7, 14, 7, 26)
                    .WithArguments("Method", "__TestMethod"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
                    .WithSpan(15, 21, 15, 33)
                    .WithArguments("Method", "__TestMethod"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expected);
        }

        // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05023 - not implementing

        // --------------- MissingStructureDecorationRule (OS-ELG-MODL-05024) -------
        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task MissingStructureDecorationRule_InNamespace_ReportsWarningAndFixes()
        {
            var test =
                @"
namespace TestCalculator
{
    [OSInterface(Name = ""TestCalculator"")]
    public interface ICalculator
    {
        int Add(FooStruct x, BarStruct y);
    }

    public class Calculator : ICalculator
    {
        public int Add(FooStruct x, BarStruct y)
        {
            return 0;
        }
    }

    public struct FooStruct 
    {
        public int DummyFoo;
    }

    public struct BarStruct 
    {
        public int DummyBar;
    }
}";

            var testfix =
                @"
namespace TestCalculator
{
    [OSInterface(Name = ""TestCalculator"")]
    public interface ICalculator
    {
        int Add(FooStruct x, BarStruct y);
    }

    public class Calculator : ICalculator
    {
        public int Add(FooStruct x, BarStruct y)
        {
            return 0;
        }
    }

    [OSStructure]
    public struct FooStruct 
    {
        public int DummyFoo;
    }

    [OSStructure]
    public struct BarStruct 
    {
        public int DummyBar;
    }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.MissingStructureDecoration)
                    .WithSpan(7, 17, 7, 28)
                    .WithArguments("FooStruct", "x"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.MissingStructureDecoration)
                    .WithSpan(7, 30, 7, 41)
                    .WithArguments("BarStruct", "y"),
            };

            await VerifyDiagnosticAndFix(test, testfix, expected);
        }

        // --------------- DuplicateNameRule (OS-ELG-MODL-05025) -------------------
        [TestMethod]
        public async Task DuplicateNameRule_InDifferentNamespaces_ReportsWarning()
        {
            var test =
                @"
namespace First
{
    [OSStructure]
    public struct Structure
    {
        public int Value;
    }
}

namespace Second
{
    [OSStructure]
    public struct Structure  // Duplicate name in different namespace
    {
        public float Value;
    }
}";
            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.DuplicateName)
                    .WithSpan(5, 19, 5, 28)
                    .WithArguments("Structure"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.DuplicateName)
                    .WithSpan(14, 19, 14, 28)
                    .WithArguments("Structure"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------- UnsupportedDefaultValueRule (OS-ELG-MODL-05026) ---------
        [TestMethod]
        public async Task UnsupportedDefaultValueRule_ValidLiterals_NoWarning()
        {
            var test =
                @"
[OSInterface]
public interface ITestInterface 
{
    void TestMethod(string text = ""hello"",
                    int? number = 42,
                    long bigNumber = 123L,
                    double precise = 3.14,
                    decimal money = 10.5m,
                    bool flag = true,
                    DateTime? date = default,
                    string nullString = null);
}

public class Implementation : ITestInterface 
{
    public void TestMethod(string text = ""hello"",
                    int? number = 42,
                    long bigNumber = 123L,
                    double precise = 3.14,
                    decimal money = 10.5m,
                    bool flag = true,
                    DateTime? date = default,
                    string nullString = null) { }
}";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false
            );
        }

        [TestMethod]
        public async Task UnsupportedDefaultValueRule_UnsupportedExpressions_ReportsWarning()
        {
            var test =
                @"
public class Constants 
{
    public const string DefaultText = ""invalid"";
}

[OSInterface]
public interface ITestInterface 
{
    void TestMethod(string text1 = Constants.DefaultText,
                    int number = 40 + 2);
}

public class Implementation : ITestInterface 
{
    public void TestMethod(string text1 = Constants.DefaultText,
                        int number = 40 + 2) { }
}";

            var expected = new[]
            {
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.UnsupportedDefaultValue)
                    .WithSpan(10, 36, 10, 57)
                    .WithArguments("text1"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.UnsupportedDefaultValue)
                    .WithSpan(11, 34, 11, 40)
                    .WithArguments("number"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05027 - not implementing

        // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05028 - not implementing

        // https://www.outsystems.com/tk/redirect?g=OS-ELG-MODL-05029 - not implementing

        // ----------------------------------------------- BEST PRACTICES

        // --------------------- PotentialStatefulImplementationRule ---------------
        [TestMethod]
        public async Task PotentialStatefulImplementationRule_DetectsStaticState()
        {
            var test =
                @"
using System.Collections.Generic;

[OSInterface]
public interface ICalculator 
{
    decimal Add(decimal a, decimal b);
    int GetTotalCalculations();
}

public class Calculator : ICalculator 
{
    // Attempting to track state between calls
    private static int _totalCalculations;
    private static readonly List<decimal> _history = new();
    public static decimal LastResult { get; private set; }
    
    public decimal Add(decimal a, decimal b) 
    {
        _totalCalculations++;
        var result = a + b;
        _history.Add(result);
        LastResult = result;
        return result;
    }
    
    public int GetTotalCalculations() 
    {
        return _totalCalculations;
    }
}";

            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.PotentialStatefulImplementation)
                .WithSpan(11, 14, 11, 24)
                .WithArguments("Calculator", "_history, _totalCalculations, LastResult");

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // --------------------- InputSizeLimitRule --------------------------------
        [TestMethod]
        public async Task InputSizeLimitRule_ReportsOnlyOneWarningForMultipleMethods()
        {
            var test =
                @"
namespace TestNamespace
{
    [OSInterface]
    public interface IDocumentProcessor
    {
        void ProcessDocument(byte[] documentData);
        void ProcessAnotherDocument(byte[] otherData);
        void ProcessMetadata(string metadata);
    }

    public class DocumentProcessor : IDocumentProcessor
    {
        public void ProcessDocument(byte[] documentData) { }
        public void ProcessAnotherDocument(byte[] otherData) { }
        public void ProcessMetadata(string metadata) { }
    }
}";
            // Expect only one diagnostic (highlighting the "byte[]" in the first method)
            var expected = CSharpAnalyzerVerifier<Analyzer>
                .Diagnostic(DiagnosticIds.InputSizeLimit)
                .WithSpan(7, 30, 7, 36);

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // ----------------------------------------------- MIXED TESTS!
        [TestMethod]
        public async Task ComplexScenario_MultipleNamingAndStructureIssues_ReportsWarnings()
        {
            var test =
                @"
using System.Collections.Generic;

namespace Company.ExternalLibs.Data
{
    [OSStructure]
    public struct CustomStruct
    {
        internal int Value;  // Non-public field
        [OSStructureField]
        private string Name;  // Non-public field with OSStructureField

        [OSStructureField(DataType = OSDataType.Text)]
        public int InvalidMapping;  // Type mapping mismatch
    }
}

namespace Company.ExternalLibs.Core
{
    using Company.ExternalLibs.Data;

    [OSInterface(Name = ""123_Invalid*Name"")]  // Starts with number and has invalid char
    public interface ITestInterface 
    {
        void _ProcessData(CustomStruct data);  // Method starts with _
    }

    public class Implementation : ITestInterface  // Missing public constructor
    {
        private readonly string _config;
        
        public Implementation(string config)  // Has constructor but with parameters
        {
            _config = config;
        }

        public void _ProcessData(CustomStruct data) { }
    }
}";

            var expected = new[]
            {
                // NonPublicStructureField
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonPublicStructureField)
                    .WithSpan(11, 24, 11, 28)
                    .WithArguments("Name", "CustomStruct"),
                // UnsupportedTypeMapping
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.UnsupportedTypeMapping)
                    .WithSpan(14, 20, 14, 34)
                    .WithArguments("InvalidMapping"),
                // NameBeginsWithNumbers
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NameBeginsWithNumbers)
                    .WithSpan(22, 25, 22, 43)
                    .WithArguments("123_Invalid*Name"),
                // UnsupportedCharactersInName
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.UnsupportedCharactersInName)
                    .WithSpan(22, 25, 22, 43)
                    .WithArguments("123_Invalid*Name", "*"),
                // NameBeginsWithUnderscores (interface)
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
                    .WithSpan(25, 14, 25, 26)
                    .WithArguments("Method", "_ProcessData"),
                // NonInstantiableInterface
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NonInstantiableInterface)
                    .WithSpan(28, 18, 28, 32)
                    .WithArguments("Implementation"),
                // NameBeginsWithUnderscores (implementation)
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.NameBeginsWithUnderscore)
                    .WithSpan(37, 21, 37, 33)
                    .WithArguments("Method", "_ProcessData"),
            };

            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        [TestMethod]
        public async Task ComplexScenario_MultipleStructureAndTypeIssues_ReportsWarnings()
        {
            var test =
                @"
using System.Collections.Generic;

namespace Company.ExternalLibs.Common
{
    [OSStructure]
    public struct UnsupportedType { }  // Empty structure

    [OSStructure]
    public struct DuplicateStruct 
    { 
        public int Value; 
    }
}

namespace Company.ExternalLibs.Models
{
    using Company.ExternalLibs.Common;

    public struct InputStruct   // Missing OSStructure attribute
    {
        public int Value;
    }

    [OSStructure]
    public struct ResultStruct
    {
        public UnsupportedType Result;  // Unsupported type
        
        [OSStructureField(DataType = OSDataType.Text)]
        public int StringValue;  // Type mismatch with DataType
        
        public List<UnsupportedType> Items;  // List of unsupported type
        public InputStruct NestedInput;  // Nested struct without OSStructure
    }
}

namespace Company.ExternalLibs.Utils
{
    [OSStructure]
    public struct DuplicateStruct   // Duplicate structure name
    { 
        public string Name; 
    }
}

namespace Company.ExternalLibs.Interfaces
{
    using Company.ExternalLibs.Models;

    [OSInterface]
    public interface IDataProcessor 
    {
        List<ResultStruct> ProcessData(InputStruct data);
    }

    public class DataProcessor : IDataProcessor
    {
        public DataProcessor() { }
        
        public List<ResultStruct> ProcessData(InputStruct data) 
        {
            return new List<ResultStruct>();
        }
    }
}";
            var expected = new[]
            {
                // EmptyStructure
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.EmptyStructure)
                    .WithSpan(7, 19, 7, 34)
                    .WithArguments("UnsupportedType"),
                // UnsupportedTypeMapping
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.UnsupportedTypeMapping)
                    .WithSpan(31, 20, 31, 31)
                    .WithArguments("StringValue"),
                // UnsupportedParameterType
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.UnsupportedParameterType)
                    .WithSpan(34, 28, 34, 39)
                    .WithArguments("ResultStruct", "Company.ExternalLibs.Models.InputStruct"),
                // DuplicateName
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.DuplicateName)
                    .WithSpan(10, 19, 10, 34)
                    .WithArguments("DuplicateStruct"),
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.DuplicateName)
                    .WithSpan(41, 19, 41, 34)
                    .WithArguments("DuplicateStruct"),
                // MissingStructureDecoration
                CSharpAnalyzerVerifier<Analyzer>
                    .Diagnostic(DiagnosticIds.MissingStructureDecoration)
                    .WithSpan(54, 40, 54, 56)
                    .WithArguments("InputStruct", "data"),
            };
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: false,
                expected
            );
        }

        // ----------------------------------------------- OTHER TESTS!
        [TestMethod]
        public async Task AnalyzerRules_Disabled_IfSDKNotAvailable()
        {
            var test =
                @"
    public interface ICalculator 
    {
        int Add(int a, int b);
    }

    public class Calculator : ICalculator 
    {
        public int Add(int a, int b) 
        {
            return a + b;
        }
    }
";
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(
                test,
                TestContext,
                skipSDKreference: true
            );
        }

        [TestMethod]
        [TestCategory("CodeFix")]
        public async Task CodeFix_Disabled_IfSDKNotAvailable()
        {
            var test =
                @"
public interface ITestInterface 
{
    void _processData();
}";

            var testfix = test;

            await CSharpAnalyzerVerifier<Analyzer>.VerifyCodeFixAsync<Fixer>(
                test,
                TestContext,
                testfix,
                skipSDKreference: true
            );
        }

        [TestMethod]
        public async Task ValidImplementation_DeeperNamespace_NoWarning()
        {
            var test =
                @"
namespace Root 
{
    [OSInterface(Name = ""TestCalculator"")]
    public interface ICalculator 
    {
        int Add(int a, int b);
    }

    namespace Services.Math
    {
        public class Calculator : Root.ICalculator 
        {
            public int Add(int a, int b) 
            {
                return a + b;
            }
        }
    }
}";
            // Verifies that implementing class can be in a deeper namespace than the interface.
            // This ensures the analyzer correctly traverses the namespace hierarchy.
            await CSharpAnalyzerVerifier<Analyzer>.VerifyAnalyzerAsync(test, TestContext);
        }
    }
}

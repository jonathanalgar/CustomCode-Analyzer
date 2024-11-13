using Microsoft.VisualStudio.TestTools.UnitTesting;
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
}
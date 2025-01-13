using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CustomCode_Analyzer.Tests
{
    // Helper class to configure nullable reference type warnings
    public static class CSharpVerifierHelper
    {
        // Dictionary of nullable warning configurations
        public static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings
        {
            get
            {
                return ImmutableDictionary.CreateRange(
                    new[]
                    {
                        // Configure specific nullable warning codes as errors
                        new KeyValuePair<string, ReportDiagnostic>(
                            "CS8632",
                            ReportDiagnostic.Error
                        ),
                        new KeyValuePair<string, ReportDiagnostic>(
                            "CS8669",
                            ReportDiagnostic.Error
                        ),
                    }
                );
            }
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a path to a C# file.");
            return;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File not found: {filePath}");
            return;
        }

        // Read the file
        var sourceText = await File.ReadAllTextAsync(filePath);
        
        // Create syntax tree from the file
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: filePath);

        // Create a compilation
        var compilation = CSharpCompilation.Create(
            "FileAnalysis",
            new[] { syntaxTree },
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create analyzer instance
        var analyzer = new Analyzer();

        // Create the analysis context
        var compilationWithAnalyzer = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        // Get the diagnostics
        var diagnostics = await compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync();

        // Print results
        if (!diagnostics.Any())
        {
            Console.WriteLine("No issues found.");
            return;
        }

        foreach (var diagnostic in diagnostics.OrderBy(d => d.Location.GetLineSpan().StartLinePosition.Line))
        {
            var lineSpan = diagnostic.Location.GetLineSpan();
            var line = lineSpan.StartLinePosition.Line + 1; // Convert to 1-based line number
            var column = lineSpan.StartLinePosition.Character + 1; // Convert to 1-based column number
            
            Console.WriteLine();
            Console.WriteLine($"{Path.GetFileName(filePath)}({line},{column}): " +
                            $"{diagnostic.Severity.ToString().ToLower()}: {diagnostic.Id}: {diagnostic.GetMessage()}");

            // Print the offending line with an underline
            if (diagnostic.Location.SourceTree != null)
            {
                var lineText = diagnostic.Location.SourceTree.GetText()
                    .Lines[lineSpan.StartLinePosition.Line].ToString();
                Console.WriteLine(lineText);

                // Create the underline
                var underline = new StringBuilder();
                underline.Append(' ', lineSpan.StartLinePosition.Character);
                var length = lineSpan.EndLinePosition.Character - lineSpan.StartLinePosition.Character;
                underline.Append('~', Math.Max(1, length));
                Console.WriteLine(underline.ToString());
            }
        }
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        // Add basic .NET references
        var references = new List<MetadataReference>();
        
        var assemblies = new[]
        {
            typeof(object).Assembly, // mscorlib or System.Runtime
            typeof(Console).Assembly, // System.Console
            typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly, // System.Runtime
            typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly, // Microsoft.CSharp
        };

        foreach (var assembly in assemblies)
        {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        // Add netstandard
        var netstandardPath = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        // Add System.Runtime
        var runtimePath = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll");
        if (File.Exists(runtimePath))
        {
            references.Add(MetadataReference.CreateFromFile(runtimePath));
        }

        return references;
    }
}
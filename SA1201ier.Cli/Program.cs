using SA1201ier.Core;

namespace SA1201ier;

/// <summary>
/// Main entry point for the SA1201ier CLI.
/// </summary>
internal class Program
{
    /// <summary>
    /// Executes the formatting operation.
    /// </summary>
    /// <param name="path">The file or directory path.</param>
    /// <param name="check">Whether to only check without formatting.</param>
    /// <param name="verbose">Whether to show verbose output.</param>
    private static async Task ExecuteAsync(string path, bool check, bool verbose)
    {
        try
        {
            var processor = new FileProcessor();
            var results = await processor.ProcessAsync(path, check, writeChanges: !check);

            var filesWithViolations = results.Where(r => r.Violations.Count > 0).ToList();
            var filesWithChanges = results.Where(r => r.HasChanges).ToList();

            if (check)
            {
                // Check mode
                if (filesWithViolations.Count == 0)
                {
                    Console.WriteLine("✓ All files are properly formatted according to SA1201.");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine(
                        $"✗ Found {filesWithViolations.Count} file(s) with SA1201 violations:"
                    );
                    Console.WriteLine();

                    foreach (var result in filesWithViolations)
                    {
                        Console.WriteLine($"  {result.FilePath}:");
                        foreach (var violation in result.Violations)
                        {
                            Console.WriteLine(
                                $"    Line {violation.LineNumber}, Column {violation.ColumnNumber}: {violation.Description}"
                            );
                        }
                        Console.WriteLine();
                    }

                    Environment.Exit(1);
                }
            }
            else
            {
                // Format mode
                if (filesWithChanges.Count == 0)
                {
                    Console.WriteLine(
                        "✓ All files are already properly formatted according to SA1201."
                    );
                }
                else
                {
                    Console.WriteLine($"✓ Formatted {filesWithChanges.Count} file(s):");
                    foreach (var result in filesWithChanges)
                    {
                        Console.WriteLine($"  {result.FilePath}");
                        if (verbose && result.Violations.Count > 0)
                        {
                            foreach (var violation in result.Violations)
                            {
                                Console.WriteLine($"    - {violation.Description}");
                            }
                        }
                    }
                }

                if (verbose)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Processed {results.Count} file(s) total.");
                }
            }
        }
        catch (FileNotFoundException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Unexpected error: {ex.Message}");
            if (verbose)
            {
                await Console.Error.WriteLineAsync(ex.StackTrace);
            }
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Main method for the CLI application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            ShowHelp();
            return 0;
        }

        var check = args.Contains("--check") || args.Contains("-c");
        var verbose = args.Contains("--verbose") || args.Contains("-v");
        var path = args.FirstOrDefault(a => !a.StartsWith('-'));

        if (string.IsNullOrWhiteSpace(path))
        {
            await Console.Error.WriteLineAsync("Error: Path argument is required.");
            await Console.Error.WriteLineAsync();
            ShowHelp();
            return 1;
        }

        await ExecuteAsync(path, check, verbose);
        return 0;
    }

    /// <summary>
    /// Shows help information.
    /// </summary>
    private static void ShowHelp()
    {
        Console.WriteLine("SA1201ier - Format C# files according to StyleCop rule SA1201");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  SA1201ier <path> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>    The file or directory path to format or check");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine(
            "  -c, --check      Check for formatting violations without making changes"
        );
        Console.WriteLine("  -v, --verbose    Show detailed output");
        Console.WriteLine("  -h, --help       Show this help message");
    }
}

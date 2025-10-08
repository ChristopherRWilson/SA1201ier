namespace SA1201ier.Core;

/// <summary>
/// Processes C# and Razor files and directories for SA1201 formatting.
/// </summary>
public class FileProcessor
{
    private readonly ConfigurationLoader _configLoader;
    private readonly FormatterOptions? _cliOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessor" /> class.
    /// </summary>
    /// <param name="cliOptions">Optional formatter options from CLI that override config files.</param>
    public FileProcessor(FormatterOptions? cliOptions = null)
    {
        _configLoader = new ConfigurationLoader();
        _cliOptions = cliOptions;
    }

    /// <summary>
    /// Processes a file or directory.
    /// </summary>
    /// <param name="path">The file or directory path.</param>
    /// <param name="check">If true, only checks for violations without formatting.</param>
    /// <param name="writeChanges">If true, writes changes to disk.</param>
    /// <returns>A list of formatting results.</returns>
    public async Task<IReadOnlyList<FormattingResult>> ProcessAsync(
        string path,
        bool check,
        bool writeChanges
    )
    {
        if (File.Exists(path))
        {
            var result = await ProcessFileAsync(path, check, writeChanges);
            return new[] { result };
        }
        else if (Directory.Exists(path))
        {
            return await ProcessDirectoryAsync(path, check, writeChanges);
        }
        else
        {
            throw new FileNotFoundException($"Path not found: {path}");
        }
    }

    /// <summary>
    /// Processes all C# and Razor files in a directory recursively.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    /// <param name="check">If true, only checks for violations without formatting.</param>
    /// <param name="writeChanges">If true, writes changes to disk.</param>
    /// <returns>A list of formatting results.</returns>
    private async Task<IReadOnlyList<FormattingResult>> ProcessDirectoryAsync(
        string directoryPath,
        bool check,
        bool writeChanges
    )
    {
        var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
        var razorFiles = Directory.GetFiles(directoryPath, "*.razor", SearchOption.AllDirectories);
        var allFiles = csFiles.Concat(razorFiles);
        var results = new List<FormattingResult>();

        foreach (var file in allFiles)
        {
            try
            {
                var result = await ProcessFileAsync(file, check, writeChanges);
                results.Add(result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing {file}: {ex.Message}");
            }
        }

        return results;
    }

    /// <summary>
    /// Processes a single file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="check">If true, only checks for violations without formatting.</param>
    /// <param name="writeChanges">If true, writes changes to disk.</param>
    /// <returns>The formatting result.</returns>
    private async Task<FormattingResult> ProcessFileAsync(
        string filePath,
        bool check,
        bool writeChanges
    )
    {
        var isRazorFile = RazorFileProcessor.IsRazorFile(filePath);
        var isCsFile = filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

        if (!isCsFile && !isRazorFile)
        {
            throw new ArgumentException(
                "Only C# files (.cs) and Razor files (.razor) are supported.",
                nameof(filePath)
            );
        }

        if (isRazorFile)
        {
            return await ProcessRazorFileAsync(filePath, check, writeChanges);
        }

        // Load configuration for this file (hierarchical merge from config files)
        var fileOptions = _configLoader.LoadConfiguration(filePath);

        // CLI options override file-based config
        var effectiveOptions =
            _cliOptions != null ? fileOptions.MergeWith(_cliOptions) : fileOptions;

        var formatter = new Sa1201IerFormatter(effectiveOptions);

        FormattingResult result;

        if (check)
        {
            result = await formatter.CheckFileAsync(filePath);
        }
        else
        {
            result = await formatter.FormatFileAsync(filePath);

            if (writeChanges && result.HasChanges && result.FormattedContent != null)
            {
                await File.WriteAllTextAsync(filePath, result.FormattedContent);
            }
        }

        return result;
    }

    /// <summary>
    /// Processes a Razor file by extracting and formatting C# code blocks.
    /// </summary>
    /// <param name="filePath">The Razor file path.</param>
    /// <param name="check">If true, only checks for violations without formatting.</param>
    /// <param name="writeChanges">If true, writes changes to disk.</param>
    /// <returns>The formatting result.</returns>
    private async Task<FormattingResult> ProcessRazorFileAsync(
        string filePath,
        bool check,
        bool writeChanges
    )
    {
        var razorContent = await File.ReadAllTextAsync(filePath);

        // Extract C# code blocks
        var extraction = RazorFileProcessor.ExtractCodeBlocks(razorContent);

        if (extraction.CodeBlocks.Count == 0)
        {
            // No @code blocks found, return empty result
            return new FormattingResult(filePath, razorContent, null, new List<Sa1201Violation>());
        }

        // Load configuration for this file
        var fileOptions = _configLoader.LoadConfiguration(filePath);
        var effectiveOptions =
            _cliOptions != null ? fileOptions.MergeWith(_cliOptions) : fileOptions;

        var formatter = new Sa1201IerFormatter(effectiveOptions);
        var allViolations = new List<Sa1201Violation>();
        var hasChanges = false;

        // Process each code block
        foreach (var block in extraction.CodeBlocks)
        {
            // Wrap the code in a class for parsing
            var wrappedCode = RazorFileProcessor.WrapCodeForParsing(block.OriginalCode);

            FormattingResult blockResult;

            if (check)
            {
                blockResult = Sa1201IerFormatter.CheckContent(
                    filePath,
                    wrappedCode,
                    effectiveOptions
                );
            }
            else
            {
                blockResult = formatter.FormatContent(filePath, wrappedCode);
            }

            // Add violations from this block
            allViolations.AddRange(blockResult.Violations);

            if (!check && blockResult.HasChanges && blockResult.FormattedContent != null)
            {
                // Unwrap the formatted code
                var formattedCode = RazorFileProcessor.UnwrapFormattedCode(
                    blockResult.FormattedContent
                );
                block.FormattedCode = formattedCode;
                hasChanges = true;
            }
            else
            {
                block.FormattedCode = block.OriginalCode;
            }
        }

        string? finalContent = null;

        if (!check && hasChanges)
        {
            // Reinsert formatted code blocks
            finalContent = RazorFileProcessor.ReinsertCodeBlocks(
                extraction.Template,
                extraction.CodeBlocks
            );

            if (writeChanges)
            {
                await File.WriteAllTextAsync(filePath, finalContent);
            }
        }

        return new FormattingResult(filePath, razorContent, finalContent, allViolations);
    }
}

namespace SA1201ier.Core;

/// <summary>
/// Processes C# files and directories for SA1201 formatting.
/// </summary>
public class FileProcessor
{
    private readonly Sa1201IerFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessor" /> class.
    /// </summary>
    public FileProcessor()
    {
        _formatter = new Sa1201IerFormatter();
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
    /// Processes all C# files in a directory recursively.
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
        var results = new List<FormattingResult>();

        foreach (var file in csFiles)
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
        if (!filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only C# files (.cs) are supported.", nameof(filePath));
        }

        FormattingResult result;

        if (check)
        {
            result = await _formatter.CheckFileAsync(filePath);
        }
        else
        {
            result = await _formatter.FormatFileAsync(filePath);

            if (writeChanges && result.HasChanges && result.FormattedContent != null)
            {
                await File.WriteAllTextAsync(filePath, result.FormattedContent);
            }
        }

        return result;
    }
}

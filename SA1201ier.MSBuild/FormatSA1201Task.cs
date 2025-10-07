using Microsoft.Build.Framework;
using SA1201ier.Core;

namespace SA1201ier.MSBuild;

/// <summary>
/// MSBuild task that formats C# files according to SA1201 rule.
/// </summary>
public class FormatSa1201Task : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Gets or sets a value indicating whether to only check without formatting.
    /// </summary>
    public bool CheckOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to fail the build on violations.
    /// </summary>
    public bool FailOnViolations { get; set; }

    /// <summary>
    /// Gets or sets the project directory to format.
    /// </summary>
    [Required]
    public string? ProjectDirectory { get; set; }

    /// <summary>
    /// Executes the task.
    /// </summary>
    /// <returns>True if successful; otherwise, false.</returns>
    public override bool Execute()
    {
        if (string.IsNullOrWhiteSpace(ProjectDirectory))
        {
            Log.LogError("ProjectDirectory is required.");
            return false;
        }

        if (!Directory.Exists(ProjectDirectory))
        {
            Log.LogError($"ProjectDirectory does not exist: {ProjectDirectory}");
            return false;
        }

        try
        {
            var processor = new FileProcessor();
            var results = processor
                .ProcessAsync(ProjectDirectory, CheckOnly, writeChanges: !CheckOnly)
                .Result;

            var filesWithViolations = results.Where(r => r.Violations.Count > 0).ToList();
            var filesWithChanges = results.Where(r => r.HasChanges).ToList();

            if (CheckOnly)
            {
                if (filesWithViolations.Count > 0)
                {
                    Log.LogWarning(
                        $"SA1201: Found {filesWithViolations.Count} file(s) with SA1201 violations."
                    );

                    foreach (var result in filesWithViolations)
                    {
                        foreach (var violation in result.Violations)
                        {
                            Log.LogWarning(
                                subcategory: "SA1201",
                                warningCode: "SA1201",
                                helpKeyword: null,
                                file: result.FilePath,
                                lineNumber: violation.LineNumber,
                                columnNumber: violation.ColumnNumber,
                                endLineNumber: violation.LineNumber,
                                endColumnNumber: violation.ColumnNumber,
                                message: violation.Description
                            );
                        }
                    }

                    if (FailOnViolations)
                    {
                        Log.LogError("SA1201: Build failed due to SA1201 violations.");
                        return false;
                    }
                }
                else
                {
                    Log.LogMessage(
                        MessageImportance.Normal,
                        "SA1201: All files are properly formatted."
                    );
                }
            }
            else
            {
                if (filesWithChanges.Count > 0)
                {
                    Log.LogMessage(
                        MessageImportance.Normal,
                        $"SA1201: Formatted {filesWithChanges.Count} file(s)."
                    );
                }
                else
                {
                    Log.LogMessage(
                        MessageImportance.Normal,
                        "SA1201: All files are already properly formatted."
                    );
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}

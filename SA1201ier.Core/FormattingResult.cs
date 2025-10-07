namespace SA1201ier.Core;

/// <summary>
/// Represents the result of a formatting operation.
/// </summary>
public class FormattingResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingResult" /> class.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="originalContent">The original content.</param>
    /// <param name="formattedContent">The formatted content.</param>
    /// <param name="violations">The list of violations.</param>
    public FormattingResult(
        string filePath,
        string originalContent,
        string? formattedContent,
        IReadOnlyList<Sa1201Violation> violations
    )
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        OriginalContent =
            originalContent ?? throw new ArgumentNullException(nameof(originalContent));
        FormattedContent = formattedContent;
        Violations = violations ?? throw new ArgumentNullException(nameof(violations));
        HasChanges =
            formattedContent != null
            && !string.Equals(originalContent, formattedContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the file path that was formatted.
    /// </summary>
    public string FilePath { get; init; }

    /// <summary>
    /// Gets the formatted content if changes were made.
    /// </summary>
    public string? FormattedContent { get; init; }

    /// <summary>
    /// Gets a value indicating whether any changes were made.
    /// </summary>
    public bool HasChanges { get; init; }

    /// <summary>
    /// Gets the original content.
    /// </summary>
    public string OriginalContent { get; init; }

    /// <summary>
    /// Gets the list of violations found.
    /// </summary>
    public IReadOnlyList<Sa1201Violation> Violations { get; init; }
}

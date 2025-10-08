namespace SA1201ier.Core;

/// <summary>
/// Configuration options for SA1201 formatting.
/// </summary>
public class FormatterOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to alphabetically sort members
    /// within the same access level and member type.
    /// </summary>
    public bool AlphabeticalSort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to sort top-level types
    /// (classes, records, interfaces, etc.) within a file by access level.
    /// </summary>
    public bool SortTopLevelTypes { get; set; }

    /// <summary>
    /// Creates a new instance with default options.
    /// </summary>
    public static FormatterOptions Default =>
        new FormatterOptions { AlphabeticalSort = false, SortTopLevelTypes = false };

    /// <summary>
    /// Merges this options instance with another, with the other taking precedence.
    /// </summary>
    /// <param name="other">The options to merge with.</param>
    /// <returns>A new options instance with merged values.</returns>
    public FormatterOptions MergeWith(FormatterOptions? other)
    {
        if (other == null)
        {
            return this;
        }

        return new FormatterOptions
        {
            AlphabeticalSort = other.AlphabeticalSort,
            SortTopLevelTypes = other.SortTopLevelTypes,
        };
    }
}

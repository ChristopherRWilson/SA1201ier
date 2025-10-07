namespace SA1201ier.Core;

/// <summary>
/// Represents a violation of the SA1201 rule.
/// </summary>
public class Sa1201Violation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Sa1201Violation" /> class.
    /// </summary>
    /// <param name="lineNumber">The line number.</param>
    /// <param name="columnNumber">The column number.</param>
    /// <param name="description">The description.</param>
    /// <param name="memberName">The member name.</param>
    public Sa1201Violation(int lineNumber, int columnNumber, string description, string memberName)
    {
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
    }

    /// <summary>
    /// Gets the column number where the violation occurred.
    /// </summary>
    public int ColumnNumber { get; init; }

    /// <summary>
    /// Gets the description of the violation.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Gets the line number where the violation occurred.
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// Gets the member that is out of order.
    /// </summary>
    public string MemberName { get; init; }
}

using SA1201ier.Core;
using Xunit;
using Xunit.Abstractions;

namespace SA1201ier.Tests;

public class XmlCommentBugTest
{
    private readonly ITestOutputHelper _output;

    public XmlCommentBugTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void FormatContent_WithXmlComments_PreservesIndentationAndBlankLines()
    {
        // Test case from the bug report
        var input =
            @"namespace com.PitHim.Contracts.Dto.Endpoints.Auth;

/// <summary>
/// Represents a request to log in to the system, containing the user's email and password.
/// </summary>
/// <remarks>
/// This request is typically used to authenticate a user by providing their email address and
/// password. Both properties must be set to valid, non-empty values for the request to be processed successfully.
/// </remarks>
public class PostLoginRequest : PitHimApiRequest
{
    /// <summary>
    /// Gets or sets the email address associated with the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password associated with the user or system.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}";

        var formatter = new Sa1201IerFormatter(
            new FormatterOptions { InsertBlankLineBetweenMembers = true }
        );
        var result = formatter.FormatContent("test.cs", input);

        var formatted = result.FormattedContent ?? result.OriginalContent;

        _output.WriteLine("=== FORMATTED OUTPUT ===");
        _output.WriteLine(formatted);
        _output.WriteLine("=== END ===");

        var normalizedFormatted = formatted.Replace("\r\n", "\n");

        // Check that there is still a blank line between the two properties
        Assert.Contains(
            "Email { get; set; } = string.Empty;\n\n    /// <summary>",
            normalizedFormatted
        );

        // Check that the XML comment indentation is correct for property-level comments (4 spaces)
        var lines = normalizedFormatted.Split('\n');
        var insideClass = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();

            // Track when we're inside the class
            if (trimmed.StartsWith("public class"))
            {
                insideClass = true;
                continue;
            }
            if (insideClass && trimmed.StartsWith("}"))
            {
                insideClass = false;
                continue;
            }

            // Only check XML comments and properties inside the class
            if (insideClass)
            {
                // XML comments for properties should start with exactly 4 spaces
                if (
                    trimmed.StartsWith("/// <summary>")
                    || trimmed.StartsWith("/// Gets or sets")
                    || trimmed.StartsWith("/// </summary>")
                )
                {
                    var leadingSpaces = line.Length - trimmed.Length;
                    Assert.Equal(4, leadingSpaces);
                }

                // Property declarations should start with exactly 4 spaces
                if (
                    trimmed.StartsWith("public string Email")
                    || trimmed.StartsWith("public string Password")
                )
                {
                    var leadingSpaces = line.Length - trimmed.Length;
                    Assert.Equal(4, leadingSpaces);
                }
            }
        }
    }
}

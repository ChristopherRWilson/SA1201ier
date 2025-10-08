using System.Text;
using System.Text.RegularExpressions;

namespace SA1201ier.Core;

/// <summary>
/// Handles extraction and reinsertion of C# code blocks from Razor files.
/// </summary>
public class RazorFileProcessor
{
    private const string CodeBlockPattern = @"@code\s*\{";

    /// <summary>
    /// Determines if a file is a Razor file.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is a Razor file; otherwise, false.</returns>
    public static bool IsRazorFile(string filePath)
    {
        return filePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts C# code blocks from a Razor file.
    /// </summary>
    /// <param name="razorContent">The Razor file content.</param>
    /// <returns>A result containing the extracted code blocks and template.</returns>
    public static RazorExtractionResult ExtractCodeBlocks(string razorContent)
    {
        var codeBlocks = new List<CodeBlock>();
        var result = new StringBuilder();
        var lastIndex = 0;

        // Find all @code blocks
        var matches = Regex.Matches(razorContent, CodeBlockPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var startIndex = match.Index;
            var codeStartIndex = match.Index + match.Length;

            // Append content before this code block
            result.Append(razorContent.Substring(lastIndex, startIndex - lastIndex));

            // Find the matching closing brace
            var braceCount = 1;
            var currentIndex = codeStartIndex;
            var inString = false;
            var inChar = false;
            var inComment = false;
            var inMultiLineComment = false;
            var escaped = false;

            while (currentIndex < razorContent.Length && braceCount > 0)
            {
                var c = razorContent[currentIndex];
                var nextChar =
                    currentIndex + 1 < razorContent.Length ? razorContent[currentIndex + 1] : '\0';

                // Handle escape sequences
                if (escaped)
                {
                    escaped = false;
                    currentIndex++;
                    continue;
                }

                if (c == '\\' && (inString || inChar))
                {
                    escaped = true;
                    currentIndex++;
                    continue;
                }

                // Handle multi-line comments
                if (!inString && !inChar && !inComment && c == '/' && nextChar == '*')
                {
                    inMultiLineComment = true;
                    currentIndex += 2;
                    continue;
                }

                if (inMultiLineComment && c == '*' && nextChar == '/')
                {
                    inMultiLineComment = false;
                    currentIndex += 2;
                    continue;
                }

                // Handle single-line comments
                if (!inString && !inChar && !inMultiLineComment && c == '/' && nextChar == '/')
                {
                    inComment = true;
                    currentIndex++;
                    continue;
                }

                if (inComment && (c == '\n' || c == '\r'))
                {
                    inComment = false;
                    currentIndex++;
                    continue;
                }

                // Skip if in comment
                if (inComment || inMultiLineComment)
                {
                    currentIndex++;
                    continue;
                }

                // Handle strings
                if (c == '"' && !inChar)
                {
                    inString = !inString;
                }

                // Handle chars
                if (c == '\'' && !inString)
                {
                    inChar = !inChar;
                }

                // Count braces only when not in string/char/comment
                if (!inString && !inChar)
                {
                    if (c == '{')
                    {
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                    }
                }

                currentIndex++;
            }

            if (braceCount == 0)
            {
                // Extract the code block content (without the braces)
                var codeContent = razorContent
                    .Substring(codeStartIndex, currentIndex - codeStartIndex - 1)
                    .Trim();

                var codeBlock = new CodeBlock
                {
                    StartIndex = startIndex,
                    EndIndex = currentIndex,
                    OriginalCode = codeContent,
                    Placeholder = $"__RAZOR_CODE_BLOCK_{codeBlocks.Count}__",
                };

                codeBlocks.Add(codeBlock);

                // Add placeholder in the template
                result.Append(codeBlock.Placeholder);

                lastIndex = currentIndex;
            }
        }

        // Append remaining content
        if (lastIndex < razorContent.Length)
        {
            result.Append(razorContent.Substring(lastIndex));
        }

        return new RazorExtractionResult { Template = result.ToString(), CodeBlocks = codeBlocks };
    }

    /// <summary>
    /// Reinserts formatted code blocks back into the Razor template.
    /// </summary>
    /// <param name="template">The template with placeholders.</param>
    /// <param name="codeBlocks">The code blocks with formatted code.</param>
    /// <returns>The complete Razor file with formatted code.</returns>
    public static string ReinsertCodeBlocks(string template, List<CodeBlock> codeBlocks)
    {
        var result = template;

        foreach (var block in codeBlocks)
        {
            var formattedBlock = $"@code {{\r\n{block.FormattedCode}\r\n}}";
            result = result.Replace(block.Placeholder, formattedBlock);
        }

        return result;
    }

    /// <summary>
    /// Wraps C# code in a class structure for parsing.
    /// </summary>
    /// <param name="code">The C# code to wrap.</param>
    /// <returns>The wrapped code.</returns>
    public static string WrapCodeForParsing(string code)
    {
        return $"public class __RazorCodeBlock\r\n{{\r\n{code}\r\n}}";
    }

    /// <summary>
    /// Extracts the code from the wrapped class structure.
    /// </summary>
    /// <param name="wrappedCode">The wrapped code.</param>
    /// <returns>The original code without the wrapper.</returns>
    public static string UnwrapFormattedCode(string wrappedCode)
    {
        // Remove the class wrapper
        var lines = wrappedCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var resultLines = new List<string>();
        var insideClass = false;
        var braceCount = 0;

        foreach (var line in lines)
        {
            if (!insideClass && line.TrimStart().StartsWith("public class __RazorCodeBlock"))
            {
                insideClass = true;
                continue;
            }

            if (insideClass)
            {
                // Count braces to find the end of the class
                foreach (var c in line)
                {
                    if (c == '{')
                        braceCount++;
                    else if (c == '}')
                        braceCount--;
                }

                // Skip the first opening brace line
                if (braceCount == 1 && line.Trim() == "{")
                {
                    continue;
                }

                // Stop at the last closing brace
                if (braceCount == 0 && line.Trim() == "}")
                {
                    break;
                }

                resultLines.Add(line);
            }
        }

        return string.Join("\r\n", resultLines).Trim();
    }
}

/// <summary>
/// Represents the result of extracting code blocks from a Razor file.
/// </summary>
public class RazorExtractionResult
{
    /// <summary>
    /// Gets or sets the Razor template with placeholders for code blocks.
    /// </summary>
    public required string Template { get; set; }

    /// <summary>
    /// Gets or sets the extracted code blocks.
    /// </summary>
    public required List<CodeBlock> CodeBlocks { get; set; }
}

/// <summary>
/// Represents a C# code block extracted from a Razor file.
/// </summary>
public class CodeBlock
{
    /// <summary>
    /// Gets or sets the start index in the original Razor file.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the end index in the original Razor file.
    /// </summary>
    public int EndIndex { get; set; }

    /// <summary>
    /// Gets or sets the original C# code.
    /// </summary>
    public required string OriginalCode { get; set; }

    /// <summary>
    /// Gets or sets the formatted C# code.
    /// </summary>
    public string? FormattedCode { get; set; }

    /// <summary>
    /// Gets or sets the placeholder used in the template.
    /// </summary>
    public required string Placeholder { get; set; }
}

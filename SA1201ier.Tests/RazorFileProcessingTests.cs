using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Tests for Razor file processing.
/// </summary>
public class RazorFileProcessingTests
{
    /// <summary>
    /// Tests that code blocks can be extracted from Razor files.
    /// </summary>
    [Fact]
    public void ExtractCodeBlocks_WithSingleCodeBlock_ExtractsCorrectly()
    {
        // Arrange
        var razorContent =
            @"@page ""/counter""

<h1>Counter</h1>

<p>Current count: @currentCount</p>

<button @onclick=""IncrementCount"">Click me</button>

@code {
    private int currentCount = 0;
    public void IncrementCount()
    {
        currentCount++;
    }
}";

        // Act
        var result = RazorFileProcessor.ExtractCodeBlocks(razorContent);

        // Assert
        Assert.Single(result.CodeBlocks);
        Assert.Contains("private int currentCount = 0;", result.CodeBlocks[0].OriginalCode);
        Assert.Contains("public void IncrementCount()", result.CodeBlocks[0].OriginalCode);
        Assert.Contains("__RAZOR_CODE_BLOCK_0__", result.Template);
    }

    /// <summary>
    /// Tests that multiple code blocks can be extracted.
    /// </summary>
    [Fact]
    public void ExtractCodeBlocks_WithMultipleCodeBlocks_ExtractsAll()
    {
        // Arrange
        var razorContent =
            @"@page ""/test""

@code {
    private int field1;
}

<div>Content</div>

@code {
    public int field2;
}";

        // Act
        var result = RazorFileProcessor.ExtractCodeBlocks(razorContent);

        // Assert
        Assert.Equal(2, result.CodeBlocks.Count);
        Assert.Contains("__RAZOR_CODE_BLOCK_0__", result.Template);
        Assert.Contains("__RAZOR_CODE_BLOCK_1__", result.Template);
    }

    /// <summary>
    /// Tests that code blocks with nested braces are handled correctly.
    /// </summary>
    [Fact]
    public void ExtractCodeBlocks_WithNestedBraces_HandlesCorrectly()
    {
        // Arrange
        var razorContent =
            @"@code {
    private void Method()
    {
        if (true)
        {
            var x = new { Name = ""Test"" };
        }
    }
}";

        // Act
        var result = RazorFileProcessor.ExtractCodeBlocks(razorContent);

        // Assert
        Assert.Single(result.CodeBlocks);
        Assert.Contains("if (true)", result.CodeBlocks[0].OriginalCode);
    }

    /// <summary>
    /// Tests that code blocks with strings containing braces are handled correctly.
    /// </summary>
    [Fact]
    public void ExtractCodeBlocks_WithBracesInStrings_HandlesCorrectly()
    {
        // Arrange
        var razorContent =
            @"@code {
    private string template = ""Hello {name}"";
    private string json = ""{ \""key\"": \""value\"" }"";
}";

        // Act
        var result = RazorFileProcessor.ExtractCodeBlocks(razorContent);

        // Assert
        Assert.Single(result.CodeBlocks);
        Assert.Contains("Hello {name}", result.CodeBlocks[0].OriginalCode);
        Assert.Contains(@"""{ \""key\""", result.CodeBlocks[0].OriginalCode);
    }

    /// <summary>
    /// Tests that wrapping and unwrapping code works correctly.
    /// </summary>
    [Fact]
    public void WrapAndUnwrap_PreservesCode()
    {
        // Arrange
        var originalCode =
            @"    private int field1;
    public void Method()
    {
        field1++;
    }";

        // Act
        var wrapped = RazorFileProcessor.WrapCodeForParsing(originalCode);
        var unwrapped = RazorFileProcessor.UnwrapFormattedCode(wrapped);

        // Assert
        Assert.Contains("public class __RazorCodeBlock", wrapped);
        Assert.DoesNotContain("public class __RazorCodeBlock", unwrapped);
        Assert.Contains("private int field1;", unwrapped);
        Assert.Contains("public void Method()", unwrapped);
    }

    /// <summary>
    /// Tests that Razor files are formatted correctly.
    /// </summary>
    [Fact]
    public async Task ProcessRazorFile_WithUnorderedMembers_FormatsCorrectly()
    {
        // Arrange
        var razorContent =
            @"@page ""/test""

<h1>Test</h1>

@code {
    private void PrivateMethod() { }
    public int PublicField;
    private int PrivateField;
    public void PublicMethod() { }
}";

        var tempFile = Path.GetTempFileName();
        var razorFile = Path.ChangeExtension(tempFile, ".razor");
        File.Move(tempFile, razorFile);

        try
        {
            await File.WriteAllTextAsync(razorFile, razorContent);

            var processor = new FileProcessor();

            // Act
            var results = await processor.ProcessAsync(razorFile, check: false, writeChanges: true);

            // Assert
            Assert.Single(results);
            Assert.True(results[0].HasChanges);

            var formattedContent = await File.ReadAllTextAsync(razorFile);

            // Fields should come before methods
            var publicFieldIndex = formattedContent.IndexOf("public int PublicField");
            var publicMethodIndex = formattedContent.IndexOf("public void PublicMethod()");
            Assert.True(publicFieldIndex < publicMethodIndex, "Fields should come before methods");

            // Public should come before private
            var privateFieldIndex = formattedContent.IndexOf("private int PrivateField");
            Assert.True(
                publicFieldIndex < privateFieldIndex,
                "Public fields should come before private fields"
            );
        }
        finally
        {
            if (File.Exists(razorFile))
            {
                File.Delete(razorFile);
            }
        }
    }

    /// <summary>
    /// Tests that Razor files without code blocks are handled gracefully.
    /// </summary>
    [Fact]
    public async Task ProcessRazorFile_WithoutCodeBlocks_ReturnsNoViolations()
    {
        // Arrange
        var razorContent =
            @"@page ""/test""

<h1>Test</h1>
<p>No code blocks here</p>";

        var tempFile = Path.GetTempFileName();
        var razorFile = Path.ChangeExtension(tempFile, ".razor");
        File.Move(tempFile, razorFile);

        try
        {
            await File.WriteAllTextAsync(razorFile, razorContent);

            var processor = new FileProcessor();

            // Act
            var results = await processor.ProcessAsync(razorFile, check: true, writeChanges: false);

            // Assert
            Assert.Single(results);
            Assert.Empty(results[0].Violations);
            Assert.False(results[0].HasChanges);
        }
        finally
        {
            if (File.Exists(razorFile))
            {
                File.Delete(razorFile);
            }
        }
    }

    /// <summary>
    /// Tests that check mode detects violations without modifying Razor files.
    /// </summary>
    [Fact]
    public async Task ProcessRazorFile_CheckMode_DetectsViolationsWithoutModifying()
    {
        // Arrange
        var razorContent =
            @"@page ""/test""

@code {
    private void PrivateMethod() { }
    public void PublicMethod() { }
}";

        var tempFile = Path.GetTempFileName();
        var razorFile = Path.ChangeExtension(tempFile, ".razor");
        File.Move(tempFile, razorFile);

        try
        {
            await File.WriteAllTextAsync(razorFile, razorContent);

            var originalContent = await File.ReadAllTextAsync(razorFile);
            var processor = new FileProcessor();

            // Act
            var results = await processor.ProcessAsync(razorFile, check: true, writeChanges: false);

            // Assert
            Assert.Single(results);
            Assert.NotEmpty(results[0].Violations);

            // File should not be modified
            var currentContent = await File.ReadAllTextAsync(razorFile);
            Assert.Equal(originalContent, currentContent);
        }
        finally
        {
            if (File.Exists(razorFile))
            {
                File.Delete(razorFile);
            }
        }
    }
}

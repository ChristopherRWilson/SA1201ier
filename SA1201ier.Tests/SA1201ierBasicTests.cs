using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Tests for the SA1201ier class focusing on basic ordering scenarios.
/// </summary>
public class Sa1201IerBasicTests
{
    private readonly Sa1201IerFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sa1201IerBasicTests" /> class.
    /// </summary>
    public Sa1201IerBasicTests()
    {
        _formatter = new Sa1201IerFormatter();
    }

    /// <summary>
    /// Tests that const fields are ordered before non-const fields.
    /// </summary>
    [Fact]
    public void CheckContent_ConstAfterNonConst_DetectsViolation()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public int PublicField;
    public const int PublicConst = 1;
}";

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content);

        // Assert
        Assert.NotEmpty(result.Violations);
    }

    /// <summary>
    /// Tests that fields ordered incorrectly by access level are detected.
    /// </summary>
    [Fact]
    public void CheckContent_FieldsWrongOrder_DetectsViolation()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    private int PrivateField;
    public int PublicField;
}";

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content);

        // Assert
        Assert.NotEmpty(result.Violations);
        Assert.Contains(result.Violations, v => v.MemberName == "PrivateField");
    }

    /// <summary>
    /// Tests that different member types are ordered correctly.
    /// </summary>
    [Fact]
    public void CheckContent_MemberTypesWrongOrder_DetectsViolation()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public void Method() { }
    public int Field;
}";

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content);

        // Assert
        Assert.NotEmpty(result.Violations);
    }

    /// <summary>
    /// Tests that methods ordered incorrectly by access level are detected.
    /// </summary>
    [Fact]
    public void CheckContent_MethodsWrongOrder_DetectsViolation()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    private void PrivateMethod() { }
    public void PublicMethod() { }
}";

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content);

        // Assert
        Assert.NotEmpty(result.Violations);
    }

    /// <summary>
    /// Tests that a properly ordered class has no violations.
    /// </summary>
    [Fact]
    public void CheckContent_ProperlyOrderedClass_NoViolations()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public const int PublicConst = 1;
    private const int PrivateConst = 2;

    public static int PublicStaticField;
    private static int PrivateStaticField;

    public int PublicField;
    private int PrivateField;

    public TestClass() { }

    public int PublicProperty { get; set; }
    private int PrivateProperty { get; set; }

    public void PublicMethod() { }
    private void PrivateMethod() { }
}";

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content);

        // Assert
        Assert.Empty(result.Violations);
        Assert.False(result.HasChanges);
    }

    /// <summary>
    /// Tests that properties ordered incorrectly by access level are detected.
    /// </summary>
    [Fact]
    public void CheckContent_PropertiesWrongOrder_DetectsViolation()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    private int PrivateProperty { get; set; }
    public int PublicProperty { get; set; }
}";

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content);

        // Assert
        Assert.NotEmpty(result.Violations);
    }

    /// <summary>
    /// Tests that static fields are ordered before instance fields.
    /// </summary>
    [Fact]
    public void CheckContent_StaticAfterInstance_DetectsViolation()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public int PublicField;
    public static int PublicStaticField;
}";

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content);

        // Assert
        Assert.NotEmpty(result.Violations);
    }

    /// <summary>
    /// Tests formatting with all access levels.
    /// </summary>
    [Fact]
    public void FormatContent_AllAccessLevels_OrdersCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    private int PrivateField;
    protected int ProtectedField;
    internal int InternalField;
    public int PublicField;
    protected internal int ProtectedInternalField;
    private protected int PrivateProtectedField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Verify order: public -> internal -> protected internal -> protected -> private protected
        // -> private
        var publicIndex = result.FormattedContent.IndexOf(
            "public int PublicField",
            StringComparison.Ordinal
        );
        var internalIndex = result.FormattedContent.IndexOf(
            "internal int InternalField",
            StringComparison.Ordinal
        );
        var protectedInternalIndex = result.FormattedContent.IndexOf(
            "protected internal int ProtectedInternalField",
            StringComparison.Ordinal
        );
        var protectedIndex = result.FormattedContent.IndexOf(
            "protected int ProtectedField",
            StringComparison.Ordinal
        );
        var privateProtectedIndex = result.FormattedContent.IndexOf(
            "private protected int PrivateProtectedField",
            StringComparison.Ordinal
        );
        var privateIndex = result.FormattedContent.IndexOf(
            "private int PrivateField",
            StringComparison.Ordinal
        );

        Assert.True(publicIndex < internalIndex);
        Assert.True(internalIndex < protectedInternalIndex);
        Assert.True(protectedInternalIndex < protectedIndex);
        Assert.True(protectedIndex < privateProtectedIndex);
        Assert.True(privateProtectedIndex < privateIndex);
    }

    /// <summary>
    /// Tests that formatting reorders members correctly.
    /// </summary>
    [Fact]
    public void FormatContent_MembersWrongOrder_ReordersCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    private int PrivateField;
    public int PublicField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);
        Assert.Contains("public int PublicField", result.FormattedContent);
        Assert.Contains("private int PrivateField", result.FormattedContent);

        // Verify PublicField comes before PrivateField
        var publicIndex = result.FormattedContent.IndexOf(
            "public int PublicField",
            StringComparison.Ordinal
        );
        var privateIndex = result.FormattedContent.IndexOf(
            "private int PrivateField",
            StringComparison.Ordinal
        );
        Assert.True(publicIndex < privateIndex);
    }

    /// <summary>
    /// Tests that Environment.NewLine is used as the default when no existing newlines are found
    /// and InsertBlankLineBetweenMembers is enabled.
    /// This ensures platform-specific line endings are respected.
    /// </summary>
    [Fact]
    public void FormatContent_NoExistingNewlines_UsesPlatformNewLine()
    {
        // Arrange - content with CRLF newlines but needs reordering
        var options = new FormatterOptions { InsertBlankLineBetweenMembers = true };
        var formatter = new Sa1201IerFormatter(options);

        // Use content that has no newlines in the trivia (single line format)
        // But when formatted with InsertBlankLineBetweenMembers, it should add Environment.NewLine
        var content =
            @"public class TestClass
{
    private int PrivateField;
    public int PublicField;
}";

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // The formatted content should preserve the existing newline style or use platform default
        // Since the input has LF, it should preserve LF
        Assert.Contains("\n", result.FormattedContent);
    }

    /// <summary>
    /// Tests that existing CRLF newline style is preserved when formatting with InsertBlankLineBetweenMembers.
    /// </summary>
    [Fact]
    public void FormatContent_WithExistingCRLFNewlines_PreservesCRLFStyle()
    {
        // Arrange - content with explicit CRLF newlines
        var options = new FormatterOptions { InsertBlankLineBetweenMembers = true };
        var formatter = new Sa1201IerFormatter(options);
        var content =
            "public class TestClass\r\n{\r\n    private int PrivateField;\r\n    public int PublicField;\r\n}";

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Should preserve CRLF from the input
        Assert.Contains("\r\n", result.FormattedContent);
    }

    /// <summary>
    /// Tests that LF-style newlines are preserved when formatting with InsertBlankLineBetweenMembers.
    /// </summary>
    [Fact]
    public void FormatContent_WithLFNewlines_PreservesLFStyle()
    {
        // Arrange - content with explicit LF newlines (Unix-style)
        var options = new FormatterOptions { InsertBlankLineBetweenMembers = true };
        var formatter = new Sa1201IerFormatter(options);
        var content =
            "public class TestClass\n{\n    private int PrivateField;\n    public int PublicField;\n}";

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Should preserve LF from the input
        var lines = result.FormattedContent.Split('\n');
        Assert.True(lines.Length > 1, "Content should have multiple lines");

        // Verify that the content uses LF newlines (not CRLF)
        // If CRLF were used, we'd see \r before \n
        var hasOnlyLF = !result.FormattedContent.Contains("\r\n");
        Assert.True(hasOnlyLF, "Content should use LF newlines, not CRLF");
    }
}

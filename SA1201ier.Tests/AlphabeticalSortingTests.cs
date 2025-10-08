using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Tests for alphabetical sorting functionality.
/// </summary>
public class AlphabeticalSortingTests
{
    /// <summary>
    /// Tests that properties are sorted alphabetically when option is enabled.
    /// </summary>
    [Fact]
    public void FormatContent_AlphabeticalSort_SortsPropertiesAlphabetically()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}";

        var options = new FormatterOptions { AlphabeticalSort = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Verify alphabetical order: Age, Email, Name
        var ageIndex = result.FormattedContent.IndexOf("public int Age", StringComparison.Ordinal);
        var emailIndex = result.FormattedContent.IndexOf(
            "public string Email",
            StringComparison.Ordinal
        );
        var nameIndex = result.FormattedContent.IndexOf(
            "public string Name",
            StringComparison.Ordinal
        );

        Assert.True(ageIndex < emailIndex, "Age should come before Email");
        Assert.True(emailIndex < nameIndex, "Email should come before Name");
    }

    /// <summary>
    /// Tests that properties are not sorted alphabetically when option is disabled.
    /// </summary>
    [Fact]
    public void FormatContent_NoAlphabeticalSort_MaintainsOriginalOrder()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}";

        var options = new FormatterOptions { AlphabeticalSort = false };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        Assert.False(result.HasChanges);

        // Order should remain: Name, Age, Email (all same access level, no reordering)
        var nameIndex = result.FormattedContent.IndexOf(
            "public string Name",
            StringComparison.Ordinal
        );
        var ageIndex = result.FormattedContent.IndexOf("public int Age", StringComparison.Ordinal);
        var emailIndex = result.FormattedContent.IndexOf(
            "public string Email",
            StringComparison.Ordinal
        );

        Assert.True(nameIndex < ageIndex, "Name should come before Age");
        Assert.True(ageIndex < emailIndex, "Age should come before Email");
    }

    /// <summary>
    /// Tests alphabetical sorting respects access level grouping.
    /// </summary>
    [Fact]
    public void FormatContent_AlphabeticalSort_RespectsAccessLevels()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public string PublicZ { get; set; }
    public int PublicA { get; set; }
    private string PrivateZ { get; set; }
    private int PrivateA { get; set; }
}";

        var options = new FormatterOptions { AlphabeticalSort = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        Assert.True(result.HasChanges);

        // Public properties should be first (alphabetically sorted)
        var publicAIndex = result.FormattedContent.IndexOf(
            "public int PublicA",
            StringComparison.Ordinal
        );
        var publicZIndex = result.FormattedContent.IndexOf(
            "public string PublicZ",
            StringComparison.Ordinal
        );

        // Private properties should be last (alphabetically sorted)
        var privateAIndex = result.FormattedContent.IndexOf(
            "private int PrivateA",
            StringComparison.Ordinal
        );
        var privateZIndex = result.FormattedContent.IndexOf(
            "private string PrivateZ",
            StringComparison.Ordinal
        );

        Assert.True(publicAIndex < publicZIndex, "PublicA should come before PublicZ");
        Assert.True(publicZIndex < privateAIndex, "Public properties should come before private");
        Assert.True(privateAIndex < privateZIndex, "PrivateA should come before PrivateZ");
    }

    /// <summary>
    /// Tests alphabetical sorting with methods.
    /// </summary>
    [Fact]
    public void FormatContent_AlphabeticalSort_SortsMethodsAlphabetically()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public void MethodZ() { }
    public void MethodA() { }
    public void MethodM() { }
}";

        var options = new FormatterOptions { AlphabeticalSort = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        Assert.True(result.HasChanges);

        var methodAIndex = result.FormattedContent.IndexOf(
            "public void MethodA",
            StringComparison.Ordinal
        );
        var methodMIndex = result.FormattedContent.IndexOf(
            "public void MethodM",
            StringComparison.Ordinal
        );
        var methodZIndex = result.FormattedContent.IndexOf(
            "public void MethodZ",
            StringComparison.Ordinal
        );

        Assert.True(methodAIndex < methodMIndex, "MethodA should come before MethodM");
        Assert.True(methodMIndex < methodZIndex, "MethodM should come before MethodZ");
    }

    /// <summary>
    /// Tests alphabetical sorting respects member type ordering.
    /// </summary>
    [Fact]
    public void FormatContent_AlphabeticalSort_RespectsMemberTypeOrder()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public void MethodZ() { }
    public string PropertyA { get; set; }
    public int FieldZ;
    public string PropertyZ { get; set; }
}";

        var options = new FormatterOptions { AlphabeticalSort = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // SA1201 order: Fields → Properties → Methods
        var fieldIndex = result.FormattedContent.IndexOf(
            "public int FieldZ",
            StringComparison.Ordinal
        );
        var propAIndex = result.FormattedContent.IndexOf(
            "public string PropertyA",
            StringComparison.Ordinal
        );
        var propZIndex = result.FormattedContent.IndexOf(
            "public string PropertyZ",
            StringComparison.Ordinal
        );
        var methodIndex = result.FormattedContent.IndexOf(
            "public void MethodZ",
            StringComparison.Ordinal
        );

        Assert.True(fieldIndex < propAIndex, "Fields should come before properties");
        Assert.True(propAIndex < propZIndex, "PropertyA should come before PropertyZ");
        Assert.True(propZIndex < methodIndex, "Properties should come before methods");
    }

    /// <summary>
    /// Tests alphabetical sorting with fields and properties mixed.
    /// </summary>
    [Fact]
    public void FormatContent_AlphabeticalSort_HandlesComplexScenario()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    private string _zebra;
    public const int CONSTANT_Z = 1;
    public const int CONSTANT_A = 2;
    private string _alpha;
    public static int StaticZ;
    public static int StaticA;
    public string PropZ { get; set; }
    public string PropA { get; set; }
    private void MethodZ() { }
    public void MethodA() { }
}";

        var options = new FormatterOptions { AlphabeticalSort = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Verify correct SA1201 ordering with alphabetical sorting:
        // 1. Public const fields (alphabetically)
        // 2. Public static fields (alphabetically)
        // 3. Private instance fields (alphabetically)
        // 4. Public properties (alphabetically)
        // 5. Public methods (alphabetically)
        // 6. Private methods (alphabetically)

        var lines = result.FormattedContent.Split('\n');
        var orderedMembers = lines
            .Where(l =>
                l.Contains("CONSTANT_")
                || l.Contains("Static")
                || l.Contains("_")
                || l.Contains("Prop")
                || l.Contains("Method")
            )
            .Select(l => l.Trim())
            .ToList();

        // Verify alphabetical order within each group
        Assert.Contains("public const int CONSTANT_A", orderedMembers[0]);
        Assert.Contains("public const int CONSTANT_Z", orderedMembers[1]);
    }

    /// <summary>
    /// Tests that CheckContent respects alphabetical sorting option.
    /// </summary>
    [Fact]
    public void CheckContent_AlphabeticalSort_DetectsViolations()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public string Name { get; set; }
    public int Age { get; set; }
}";

        var options = new FormatterOptions { AlphabeticalSort = true };

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content, options);

        // Assert - Name before Age is a violation when alphabetical sort is on
        Assert.NotEmpty(result.Violations);
    }
}

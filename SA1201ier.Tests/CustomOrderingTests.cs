using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Tests for custom ordering configuration options.
/// </summary>
public class CustomOrderingTests
{
    /// <summary>
    /// Tests that custom access level order works (Private before Public).
    /// </summary>
    [Fact]
    public void FormatContent_CustomAccessLevelOrder_OrdersCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public int PublicField;
    private int PrivateField;
}";

        var options = new FormatterOptions
        {
            AccessLevelOrder = new List<string> { "Private", "Public" },
        };

        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        var privateIndex = result.FormattedContent.IndexOf("private int PrivateField");
        var publicIndex = result.FormattedContent.IndexOf("public int PublicField");
        Assert.True(
            privateIndex < publicIndex,
            "Private field should come before public field with custom order"
        );
    }

    /// <summary>
    /// Tests that custom member type order works (Method before Field).
    /// </summary>
    [Fact]
    public void FormatContent_CustomMemberTypeOrder_OrdersCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public int Field;
    public void Method() { }
}";

        var options = new FormatterOptions
        {
            MemberTypeOrder = new List<string> { "Method", "Field" },
        };

        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        var methodIndex = result.FormattedContent.IndexOf("public void Method()");
        var fieldIndex = result.FormattedContent.IndexOf("public int Field");
        Assert.True(methodIndex < fieldIndex, "Method should come before field with custom order");
    }

    /// <summary>
    /// Tests that staticMembersFirst=false puts instance members before static.
    /// </summary>
    [Fact]
    public void FormatContent_StaticMembersLast_OrdersCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public static int StaticField;
    public int InstanceField;
}";

        var options = new FormatterOptions { StaticMembersFirst = false };

        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        var instanceIndex = result.FormattedContent.IndexOf("public int InstanceField");
        var staticIndex = result.FormattedContent.IndexOf("public static int StaticField");
        Assert.True(
            instanceIndex < staticIndex,
            "Instance field should come before static field when staticMembersFirst is false"
        );
    }

    /// <summary>
    /// Tests that constMembersFirst=false puts non-const members before const.
    /// </summary>
    [Fact]
    public void FormatContent_ConstMembersLast_OrdersCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public const int ConstField = 1;
    public int RegularField;
}";

        var options = new FormatterOptions { ConstMembersFirst = false };

        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        var regularIndex = result.FormattedContent.IndexOf("public int RegularField");
        var constIndex = result.FormattedContent.IndexOf("public const int ConstField");
        Assert.True(
            regularIndex < constIndex,
            "Regular field should come before const field when constMembersFirst is false"
        );
    }

    /// <summary>
    /// Tests that custom top-level type order works.
    /// </summary>
    [Fact]
    public void FormatContent_CustomTopLevelTypeOrder_OrdersCorrectly()
    {
        // Arrange
        var content =
            @"
namespace TestNamespace
{
    public enum MyEnum { }
    public class MyClass { }
}";

        var options = new FormatterOptions
        {
            SortTopLevelTypes = true,
            TopLevelTypeOrder = new List<string> { "Class", "Enum" },
        };

        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        var classIndex = result.FormattedContent.IndexOf("public class MyClass");
        var enumIndex = result.FormattedContent.IndexOf("public enum MyEnum");
        Assert.True(
            classIndex < enumIndex,
            "Class should come before enum with custom top-level type order"
        );
    }

    /// <summary>
    /// Tests complex custom ordering with multiple rules.
    /// </summary>
    [Fact]
    public void FormatContent_ComplexCustomOrder_OrdersCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public void PublicMethod() { }
    private int PrivateField;
    public int PublicField;
    private void PrivateMethod() { }
}";

        var options = new FormatterOptions
        {
            // Methods before fields, Private before Public
            MemberTypeOrder = new List<string> { "Method", "Field" },
            AccessLevelOrder = new List<string> { "Private", "Public" },
        };

        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Extract positions
        var privateMethodIndex = result.FormattedContent.IndexOf("private void PrivateMethod()");
        var publicMethodIndex = result.FormattedContent.IndexOf("public void PublicMethod()");
        var privateFieldIndex = result.FormattedContent.IndexOf("private int PrivateField");
        var publicFieldIndex = result.FormattedContent.IndexOf("public int PublicField");

        // Methods should come before fields
        Assert.True(privateMethodIndex < privateFieldIndex, "Methods should come before fields");
        Assert.True(publicMethodIndex < publicFieldIndex, "Methods should come before fields");

        // Private should come before public
        Assert.True(
            privateMethodIndex < publicMethodIndex,
            "Private method should come before public method"
        );
        Assert.True(
            privateFieldIndex < publicFieldIndex,
            "Private field should come before public field"
        );
    }

    /// <summary>
    /// Tests that alphabetical sorting works with custom orders.
    /// </summary>
    [Fact]
    public void FormatContent_CustomOrderWithAlphabetical_OrdersCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public int Zebra;
    public int Apple;
    public int Banana;
}";

        var options = new FormatterOptions { AlphabeticalSort = true };

        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        var appleIndex = result.FormattedContent.IndexOf("public int Apple");
        var bananaIndex = result.FormattedContent.IndexOf("public int Banana");
        var zebraIndex = result.FormattedContent.IndexOf("public int Zebra");

        Assert.True(appleIndex < bananaIndex, "Apple should come before Banana");
        Assert.True(bananaIndex < zebraIndex, "Banana should come before Zebra");
    }
}

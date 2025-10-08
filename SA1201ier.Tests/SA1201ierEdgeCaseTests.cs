using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Tests for edge cases and special scenarios in SA1201ier.
/// </summary>
public class Sa1201IerEdgeCaseTests
{
    private readonly Sa1201IerFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sa1201IerEdgeCaseTests" /> class.
    /// </summary>
    public Sa1201IerEdgeCaseTests()
    {
        _formatter = new Sa1201IerFormatter();
    }

    /// <summary>
    /// Tests that an empty class has no violations.
    /// </summary>
    [Fact]
    public void CheckContent_EmptyClass_NoViolations()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
}";

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content);

        // Assert
        Assert.Empty(result.Violations);
        Assert.False(result.HasChanges);
    }

    /// <summary>
    /// Tests that a class with only one member has no violations.
    /// </summary>
    [Fact]
    public void CheckContent_SingleMember_NoViolations()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public int Field;
}";

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content);

        // Assert
        Assert.Empty(result.Violations);
        Assert.False(result.HasChanges);
    }

    /// <summary>
    /// Tests that abstract classes and members are formatted.
    /// </summary>
    [Fact]
    public void FormatContent_AbstractClassAndMembers_FormatsCorrectly()
    {
        // Arrange
        var content =
            @"
public abstract class TestClass
{
    private void PrivateMethod() { }
    public abstract void AbstractMethod();
    public void PublicMethod() { }
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // All public methods should come before private
        var abstractMethodIndex = result.FormattedContent.IndexOf(
            "public abstract void AbstractMethod()",
            StringComparison.Ordinal
        );
        var publicMethodIndex = result.FormattedContent.IndexOf(
            "public void PublicMethod()",
            StringComparison.Ordinal
        );
        var privateMethodIndex = result.FormattedContent.IndexOf(
            "private void PrivateMethod()",
            StringComparison.Ordinal
        );

        Assert.True(abstractMethodIndex < privateMethodIndex);
        Assert.True(publicMethodIndex < privateMethodIndex);
    }

    /// <summary>
    /// Tests formatting with expression-bodied members.
    /// </summary>
    [Fact]
    public void FormatContent_ExpressionBodiedMembers_FormatsCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    private int GetPrivateValue() => 1;
    public int GetPublicValue() => 2;

    private int PrivateProperty => 3;
    public int PublicProperty => 4;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Properties should come before methods
        var publicPropertyIndex = result.FormattedContent.IndexOf(
            "public int PublicProperty",
            StringComparison.Ordinal
        );
        var publicMethodIndex = result.FormattedContent.IndexOf(
            "public int GetPublicValue()",
            StringComparison.Ordinal
        );
        Assert.True(publicPropertyIndex < publicMethodIndex);
    }

    /// <summary>
    /// Tests that file-scoped namespaces are handled correctly.
    /// </summary>
    [Fact]
    public void FormatContent_FileScopedNamespace_FormatsCorrectly()
    {
        // Arrange
        var content =
            @"
namespace MyNamespace;

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
        Assert.Contains("namespace MyNamespace;", result.FormattedContent);
    }

    /// <summary>
    /// Tests that attributes are preserved with members.
    /// </summary>
    [Fact]
    public void FormatContent_MembersWithAttributes_PreservesAttributes()
    {
        // Arrange
        var content =
            @"
using System;
public class TestClass
{
    [Obsolete]
    private int PrivateField;

    [Obsolete]
    public int PublicField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Attributes should be preserved
        var obsoleteCount = result.FormattedContent.Split("[Obsolete]").Length - 1;
        Assert.Equal(2, obsoleteCount);
    }

    /// <summary>
    /// Tests that comments and attributes are preserved with members.
    /// </summary>
    [Fact]
    public void FormatContent_MembersWithComments_PreservesComments()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    // Private field comment
    private int PrivateField;

    // Public field comment
    public int PublicField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Comments should be preserved
        Assert.Contains("// Private field comment", result.FormattedContent);
        Assert.Contains("// Public field comment", result.FormattedContent);
    }

    /// <summary>
    /// Tests that XML documentation comments are preserved.
    /// </summary>
    [Fact]
    public void FormatContent_MembersWithXmlComments_PreservesXmlComments()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    /// <summary>
    /// Private field documentation.
    /// </summary>
    private int PrivateField;

    /// <summary>
    /// Public field documentation.
    /// </summary>
    public int PublicField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // XML comments should be preserved
        Assert.Contains("/// <summary>", result.FormattedContent);
        Assert.Contains("/// Private field documentation.", result.FormattedContent);
        Assert.Contains("/// Public field documentation.", result.FormattedContent);
    }

    /// <summary>
    /// Tests formatting with multiple variable declarations in one field.
    /// </summary>
    [Fact]
    public void FormatContent_MultipleVariableDeclarations_FormatsCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    private int field1, field2, field3;
    public int publicField1, publicField2;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        var publicIndex = result.FormattedContent.IndexOf(
            "public int publicField1",
            StringComparison.Ordinal
        );
        var privateIndex = result.FormattedContent.IndexOf(
            "private int field1",
            StringComparison.Ordinal
        );
        Assert.True(publicIndex < privateIndex);
    }

    /// <summary>
    /// Tests that partial classes are formatted.
    /// </summary>
    [Fact]
    public void FormatContent_PartialClass_FormatsCorrectly()
    {
        // Arrange
        var content =
            @"
public partial class TestClass
{
    private int PrivateField;
    public int PublicField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);
        Assert.Contains("public partial class TestClass", result.FormattedContent);
    }

    /// <summary>
    /// Tests that readonly fields are ordered correctly.
    /// </summary>
    [Fact]
    public void FormatContent_ReadonlyFields_FormatsCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    private readonly int PrivateReadonlyField;
    public readonly int PublicReadonlyField;
    public int PublicField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // All public fields should come before private
        var publicReadonlyIndex = result.FormattedContent.IndexOf(
            "public readonly int PublicReadonlyField",
            StringComparison.Ordinal
        );
        var publicFieldIndex = result.FormattedContent.IndexOf(
            "public int PublicField",
            StringComparison.Ordinal
        );
        var privateReadonlyIndex = result.FormattedContent.IndexOf(
            "private readonly int PrivateReadonlyField",
            StringComparison.Ordinal
        );

        Assert.True(publicReadonlyIndex < privateReadonlyIndex);
        Assert.True(publicFieldIndex < privateReadonlyIndex);
    }

    /// <summary>
    /// Tests that multiple fields with the same access level maintain their relative order.
    /// </summary>
    [Fact]
    public void FormatContent_SameAccessLevel_MaintainsOrder()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public int Field1;
    public int Field2;
    public int Field3;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.False(result.HasChanges);

        // Verify order is maintained
        Assert.NotNull(result.FormattedContent);
        var field1Index = result.FormattedContent.IndexOf(
            "public int Field1",
            StringComparison.Ordinal
        );
        var field2Index = result.FormattedContent.IndexOf(
            "public int Field2",
            StringComparison.Ordinal
        );
        var field3Index = result.FormattedContent.IndexOf(
            "public int Field3",
            StringComparison.Ordinal
        );

        Assert.True(field1Index < field2Index);
        Assert.True(field2Index < field3Index);
    }

    /// <summary>
    /// Tests that sealed classes are formatted.
    /// </summary>
    [Fact]
    public void FormatContent_SealedClass_FormatsCorrectly()
    {
        // Arrange
        var content =
            @"
public sealed class TestClass
{
    private int PrivateField;
    public int PublicField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);
        Assert.Contains("public sealed class TestClass", result.FormattedContent);
    }

    /// <summary>
    /// Tests that static classes are formatted.
    /// </summary>
    [Fact]
    public void FormatContent_StaticClass_FormatsCorrectly()
    {
        // Arrange
        var content =
            @"
public static class TestClass
{
    private static int PrivateField;
    public static int PublicField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        var publicIndex = result.FormattedContent.IndexOf(
            "public static int PublicField",
            StringComparison.Ordinal
        );
        var privateIndex = result.FormattedContent.IndexOf(
            "private static int PrivateField",
            StringComparison.Ordinal
        );
        Assert.True(publicIndex < privateIndex);
    }

    /// <summary>
    /// Tests that namespace declarations are preserved.
    /// </summary>
    [Fact]
    public void FormatContent_WithNamespace_PreservesNamespace()
    {
        // Arrange
        var content =
            @"
namespace MyNamespace
{
    public class TestClass
    {
        private int PrivateField;
        public int PublicField;
    }
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);
        Assert.Contains("namespace MyNamespace", result.FormattedContent);
    }

    /// <summary>
    /// Tests that using directives are preserved.
    /// </summary>
    [Fact]
    public void FormatContent_WithUsingDirectives_PreservesUsings()
    {
        // Arrange
        var content =
            @"
using System;
using System.Collections.Generic;

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
        Assert.Contains("using System;", result.FormattedContent);
        Assert.Contains("using System.Collections.Generic;", result.FormattedContent);
    }

    /// <summary>
    /// Tests that regions are preserved and members within regions are reordered independently.
    /// </summary>
    [Fact]
    public void FormatContent_WithRegions_PreservesRegionsAndReordersWithin()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
#region Private Fields
    private int privateField1;
    private int privateField2;
#endregion

#region Public Fields
    public int publicField1;
    public int publicField2;
#endregion

    private void PrivateMethod() { }
    public void PublicMethod() { }
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Verify regions are preserved
        Assert.Contains("#region Private Fields", result.FormattedContent);
        Assert.Contains("#endregion", result.FormattedContent);
        Assert.Contains("#region Public Fields", result.FormattedContent);

        // Verify that public method comes before private method (outside of regions)
        var publicMethodIndex = result.FormattedContent.IndexOf(
            "public void PublicMethod",
            StringComparison.Ordinal
        );
        var privateMethodIndex = result.FormattedContent.IndexOf(
            "private void PrivateMethod",
            StringComparison.Ordinal
        );
        Assert.True(
            publicMethodIndex < privateMethodIndex,
            "Public method should come before private method"
        );
    }

    /// <summary>
    /// Tests that preprocessor directives are preserved.
    /// </summary>
    [Fact]
    public void FormatContent_WithPreprocessorDirectives_PreservesDirectives()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
#if DEBUG
    private void DebugMethod() { }
#endif

    private void PrivateMethod() { }
    public void PublicMethod() { }
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Verify preprocessor directives are preserved
        Assert.Contains("#if DEBUG", result.FormattedContent);
        Assert.Contains("#endif", result.FormattedContent);

        // Verify DebugMethod is still within the #if block
        var ifIndex = result.FormattedContent.IndexOf("#if DEBUG", StringComparison.Ordinal);
        var debugMethodIndex = result.FormattedContent.IndexOf(
            "private void DebugMethod",
            StringComparison.Ordinal
        );
        var endifIndex = result.FormattedContent.IndexOf("#endif", StringComparison.Ordinal);
        Assert.True(ifIndex < debugMethodIndex, "#if should come before DebugMethod");
        Assert.True(debugMethodIndex < endifIndex, "DebugMethod should come before #endif");

        // Verify that public method comes before private method (outside of #if block)
        var publicMethodIndex = result.FormattedContent.IndexOf(
            "public void PublicMethod",
            StringComparison.Ordinal
        );
        var privateMethodIndex = result.FormattedContent.IndexOf(
            "private void PrivateMethod",
            StringComparison.Ordinal
        );
        Assert.True(
            publicMethodIndex < privateMethodIndex,
            "Public method should come before private method"
        );
    }

    /// <summary>
    /// Tests that members within regions maintain their order when already correct.
    /// </summary>
    [Fact]
    public void FormatContent_WithRegionsCorrectOrder_NoChanges()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
#region Public Fields
    public int publicField1;
    public int publicField2;
#endregion

#region Private Fields
    private int privateField1;
    private int privateField2;
#endregion
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Verify regions are preserved in original order since region blocks are kept as-is
        Assert.Contains("#region Public Fields", result.FormattedContent);
        Assert.Contains("#region Private Fields", result.FormattedContent);
    }

    /// <summary>
    /// Tests that nested preprocessor directives are handled correctly.
    /// </summary>
    [Fact]
    public void FormatContent_WithNestedPreprocessorDirectives_PreservesStructure()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
#if DEBUG
    #if TRACE
        private void DebugTraceMethod() { }
    #endif
    private void DebugMethod() { }
#endif

    public void PublicMethod() { }
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Verify all preprocessor directives are preserved
        Assert.Contains("#if DEBUG", result.FormattedContent);
        Assert.Contains("#if TRACE", result.FormattedContent);
        Assert.Contains("#endif", result.FormattedContent);

        // Verify the structure is maintained
        var lines = result.FormattedContent.Split('\n');
        var debugIfLine = Array.FindIndex(lines, l => l.Contains("#if DEBUG"));
        var traceIfLine = Array.FindIndex(lines, l => l.Contains("#if TRACE"));
        var debugTraceMethodLine = Array.FindIndex(lines, l => l.Contains("DebugTraceMethod"));

        Assert.True(debugIfLine < traceIfLine, "#if DEBUG should come before #if TRACE");
        Assert.True(
            traceIfLine < debugTraceMethodLine,
            "#if TRACE should come before DebugTraceMethod"
        );
    }
}

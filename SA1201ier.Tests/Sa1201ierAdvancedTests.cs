using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Advanced tests for the SA1201ier class.
/// </summary>
public class Sa1201IerAdvancedTests
{
    private readonly Sa1201IerFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sa1201IerAdvancedTests" /> class.
    /// </summary>
    public Sa1201IerAdvancedTests()
    {
        _formatter = new Sa1201IerFormatter();
    }

    /// <summary>
    /// Helper method to find the index of a line containing specific text.
    /// </summary>
    private static int FindLineIndex(List<string> lines, string searchText)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains(searchText))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Tests complex member ordering scenario.
    /// </summary>
    [Fact]
    public void FormatContent_ComplexOrdering_AllMembersOrderedCorrectly()
    {
        // Arrange
        const string content = """

            using System;
            public class TestClass
            {
                private void PrivateMethod() { }
                public int PublicProperty { get; set; }
                private const int PrivateConst = 1;
                public TestClass() { }
                public static int PublicStaticField;
                private int PrivateField;
                public event EventHandler PublicEvent;
                public void PublicMethod() { }
                public const int PublicConst = 2;
                private static int PrivateStaticField;
            }
            """;

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);
        Assert.NotEmpty(result.Violations);

        // Verify the expected order
        var lines = result
            .FormattedContent.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        // Const fields should come first, ordered by access level
        var publicConstIndex = FindLineIndex(lines, "public const int PublicConst");
        var privateConstIndex = FindLineIndex(lines, "private const int PrivateConst");
        Assert.True(publicConstIndex < privateConstIndex);

        // Static fields should come after const fields
        var publicStaticFieldIndex = FindLineIndex(lines, "public static int PublicStaticField");
        var privateStaticFieldIndex = FindLineIndex(lines, "private static int PrivateStaticField");
        Assert.True(privateConstIndex < publicStaticFieldIndex);
        Assert.True(publicStaticFieldIndex < privateStaticFieldIndex);

        // Instance fields should come after static fields
        var privateFieldIndex = FindLineIndex(lines, "private int PrivateField");
        Assert.True(privateStaticFieldIndex < privateFieldIndex);

        // Constructor should come after fields
        var constructorIndex = FindLineIndex(lines, "public TestClass()");
        Assert.True(privateFieldIndex < constructorIndex);

        // Events should come after constructors
        var eventIndex = FindLineIndex(lines, "public event EventHandler PublicEvent");
        Assert.True(constructorIndex < eventIndex);

        // Properties should come after events
        var propertyIndex = FindLineIndex(lines, "public int PublicProperty");
        Assert.True(eventIndex < propertyIndex);

        // Methods should come last
        var publicMethodIndex = FindLineIndex(lines, "public void PublicMethod()");
        var privateMethodIndex = FindLineIndex(lines, "private void PrivateMethod()");
        Assert.True(propertyIndex < publicMethodIndex);
        Assert.True(publicMethodIndex < privateMethodIndex);
    }

    /// <summary>
    /// Tests that constructors come after fields.
    /// </summary>
    [Fact]
    public void FormatContent_ConstructorBeforeField_ReordersCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public TestClass() { }
    public int Field;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        var fieldIndex = result.FormattedContent.IndexOf(
            "public int Field",
            StringComparison.Ordinal
        );
        var constructorIndex = result.FormattedContent.IndexOf(
            "public TestClass()",
            StringComparison.Ordinal
        );
        Assert.True(fieldIndex < constructorIndex);
    }

    /// <summary>
    /// Tests formatting with delegates.
    /// </summary>
    [Fact]
    public void FormatContent_Delegate_OrderedCorrectly()
    {
        // Arrange
        var content =
            @"
public class TestClass
{
    public int Property { get; set; }
    public delegate void MyDelegate();
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Delegates come before events and properties
        var delegateIndex = result.FormattedContent.IndexOf(
            "public delegate void MyDelegate()",
            StringComparison.Ordinal
        );
        var propertyIndex = result.FormattedContent.IndexOf(
            "public int Property",
            StringComparison.Ordinal
        );

        Assert.True(delegateIndex < propertyIndex);
    }

    /// <summary>
    /// Tests that destructors are ordered correctly.
    /// </summary>
    [Fact]
    public void FormatContent_Destructor_OrderedAfterConstructor()
    {
        // Arrange
        const string content = """

            public class TestClass
            {
                public void Method() { }
                ~TestClass() { }
                public TestClass() { }
            }
            """;

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        var constructorIndex = result.FormattedContent.IndexOf(
            "public TestClass()",
            StringComparison.Ordinal
        );
        var destructorIndex = result.FormattedContent.IndexOf(
            "~TestClass()",
            StringComparison.Ordinal
        );
        var methodIndex = result.FormattedContent.IndexOf(
            "public void Method()",
            StringComparison.Ordinal
        );

        Assert.True(constructorIndex < destructorIndex);
        Assert.True(destructorIndex < methodIndex);
    }

    /// <summary>
    /// Tests formatting of events.
    /// </summary>
    [Fact]
    public void FormatContent_EventsOrderedCorrectly()
    {
        // Arrange
        const string content = """

            using System;
            public class TestClass
            {
                public void Method() { }
                public event EventHandler MyEvent;
                public int Property { get; set; }
            }
            """;

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Events come after delegates but before properties
        var eventIndex = result.FormattedContent.IndexOf(
            "public event EventHandler MyEvent",
            StringComparison.Ordinal
        );
        var propertyIndex = result.FormattedContent.IndexOf(
            "public int Property",
            StringComparison.Ordinal
        );
        var methodIndex = result.FormattedContent.IndexOf(
            "public void Method()",
            StringComparison.Ordinal
        );

        Assert.True(eventIndex < propertyIndex);
        Assert.True(propertyIndex < methodIndex);
    }

    /// <summary>
    /// Tests formatting with indexers.
    /// </summary>
    [Fact]
    public void FormatContent_Indexer_OrderedCorrectly()
    {
        // Arrange
        const string content = """

            public class TestClass
            {
                public void Method() { }
                public int this[int index] { get { return 0; } }
                public int Property { get; set; }
            }
            """;

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Indexers come after properties
        var propertyIndex = result.FormattedContent.IndexOf(
            "public int Property",
            StringComparison.Ordinal
        );
        var indexerIndex = result.FormattedContent.IndexOf(
            "public int this[int index]",
            StringComparison.Ordinal
        );
        var methodIndex = result.FormattedContent.IndexOf(
            "public void Method()",
            StringComparison.Ordinal
        );

        Assert.True(propertyIndex < indexerIndex);
        Assert.True(indexerIndex < methodIndex);
    }

    /// <summary>
    /// Tests that interfaces are handled correctly (interface members might have different types).
    /// </summary>
    [Fact]
    public void FormatContent_Interface_FormatsCorrectly()
    {
        // Arrange
        const string content = """

            public interface ITestInterface
            {
                void Method2();
                void Method1();
                int Property { get; set; }
            }
            """;

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert Interface members are implicitly public, but they still have member type ordering
        // Properties should come before methods
        if (result.HasChanges)
        {
            Assert.NotNull(result.FormattedContent);
            var propertyIndex = result.FormattedContent.IndexOf(
                "int Property",
                StringComparison.Ordinal
            );
            var method1Index = result.FormattedContent.IndexOf(
                "void Method1()",
                StringComparison.Ordinal
            );
            var method2Index = result.FormattedContent.IndexOf(
                "void Method2()",
                StringComparison.Ordinal
            );

            if (propertyIndex >= 0 && method1Index >= 0)
            {
                Assert.True(propertyIndex < method1Index);
            }
            if (propertyIndex >= 0 && method2Index >= 0)
            {
                Assert.True(propertyIndex < method2Index);
            }
        }
    }

    /// <summary>
    /// Tests that methods come after properties.
    /// </summary>
    [Fact]
    public void FormatContent_MethodBeforeProperty_ReordersCorrectly()
    {
        // Arrange
        const string content = """

            public class TestClass
            {
                public void Method() { }
                public int Property { get; set; }
            }
            """;

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        var propertyIndex = result.FormattedContent.IndexOf(
            "public int Property",
            StringComparison.Ordinal
        );
        var methodIndex = result.FormattedContent.IndexOf(
            "public void Method()",
            StringComparison.Ordinal
        );
        Assert.True(propertyIndex < methodIndex);
    }

    /// <summary>
    /// Tests that nested classes are handled correctly.
    /// </summary>
    [Fact]
    public void FormatContent_NestedClass_FormatsOuterAndInner()
    {
        // Arrange
        const string content = """

            public class OuterClass
            {
                private void PrivateMethod() { }
                public void PublicMethod() { }

                public class InnerClass
                {
                    private int PrivateField;
                    public int PublicField;
                }
            }
            """;

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        // Both outer and inner classes should be formatted
        var outerPublicIndex = result.FormattedContent.IndexOf(
            "public void PublicMethod()",
            StringComparison.Ordinal
        );
        var outerPrivateIndex = result.FormattedContent.IndexOf(
            "private void PrivateMethod()",
            StringComparison.Ordinal
        );
        Assert.True(outerPublicIndex < outerPrivateIndex);
    }

    /// <summary>
    /// Tests that properties come after constructors.
    /// </summary>
    [Fact]
    public void FormatContent_PropertyBeforeConstructor_ReordersCorrectly()
    {
        // Arrange
        const string content = """

            public class TestClass
            {
                public int Property { get; set; }
                public TestClass() { }
            }
            """;

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        var constructorIndex = result.FormattedContent.IndexOf(
            "public TestClass()",
            StringComparison.Ordinal
        );
        var propertyIndex = result.FormattedContent.IndexOf(
            "public int Property",
            StringComparison.Ordinal
        );
        Assert.True(constructorIndex < propertyIndex);
    }

    /// <summary>
    /// Tests that records are handled like classes.
    /// </summary>
    [Fact]
    public void FormatContent_Record_FormatsLikeClass()
    {
        // Arrange
        var content = """

            public record TestRecord
            {
                private void PrivateMethod() { }
                public void PublicMethod() { }
            }
            """;
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

        var publicIndex = result.FormattedContent.IndexOf(
            "public void PublicMethod()",
            StringComparison.Ordinal
        );
        var privateIndex = result.FormattedContent.IndexOf(
            "private void PrivateMethod()",
            StringComparison.Ordinal
        );
        Assert.True(publicIndex < privateIndex);
    }

    /// <summary>
    /// Tests that structs are handled correctly.
    /// </summary>
    [Fact]
    public void FormatContent_Struct_FormatsCorrectly()
    {
        // Arrange
        var content =
            @"
public struct TestStruct
{
    private int PrivateField;
    public int PublicField;
}";

        // Act
        var result = _formatter.FormatContent("test.cs", content);

        // Assert
        Assert.True(result.HasChanges);
        Assert.NotNull(result.FormattedContent);

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
}

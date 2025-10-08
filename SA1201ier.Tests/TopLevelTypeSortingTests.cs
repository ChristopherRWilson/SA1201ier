using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Tests for top-level type sorting functionality.
/// </summary>
public class TopLevelTypeSortingTests
{
    /// <summary>
    /// Tests that top-level types are sorted by access level.
    /// </summary>
    [Fact]
    public void FormatContent_SortTopLevelTypes_SortsByAccessLevel()
    {
        // Arrange
        var content =
            @"
namespace MyApp;

internal class InternalClass { }
public class PublicClass { }
internal record InternalRecord { }
public record PublicRecord { }
";

        var options = new FormatterOptions { SortTopLevelTypes = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        Assert.True(result.HasChanges);

        // Public types should come before internal types
        var publicClassIndex = result.FormattedContent.IndexOf(
            "public class PublicClass",
            StringComparison.Ordinal
        );
        var publicRecordIndex = result.FormattedContent.IndexOf(
            "public record PublicRecord",
            StringComparison.Ordinal
        );
        var internalClassIndex = result.FormattedContent.IndexOf(
            "internal class InternalClass",
            StringComparison.Ordinal
        );
        var internalRecordIndex = result.FormattedContent.IndexOf(
            "internal record InternalRecord",
            StringComparison.Ordinal
        );

        Assert.True(
            publicClassIndex < internalClassIndex,
            "Public class should come before internal class"
        );
        Assert.True(
            publicRecordIndex < internalRecordIndex,
            "Public record should come before internal record"
        );
    }

    /// <summary>
    /// Tests that top-level types maintain order when option is disabled.
    /// </summary>
    [Fact]
    public void FormatContent_NoSortTopLevelTypes_MaintainsOrder()
    {
        // Arrange
        var content =
            @"
namespace MyApp;

internal class InternalClass { }
public class PublicClass { }
";

        var options = new FormatterOptions { SortTopLevelTypes = false };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        Assert.False(result.HasChanges); // No changes since sorting is disabled
    }

    /// <summary>
    /// Tests sorting with different type kinds.
    /// </summary>
    [Fact]
    public void FormatContent_SortTopLevelTypes_SortsAllTypeKinds()
    {
        // Arrange
        var content =
            @"
namespace MyApp;

public class PublicClass { }
public enum PublicEnum { }
public interface IPublicInterface { }
public struct PublicStruct { }
";

        var options = new FormatterOptions { SortTopLevelTypes = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Verify type ordering: Enum → Interface → Struct → Class
        var enumIndex = result.FormattedContent.IndexOf(
            "public enum PublicEnum",
            StringComparison.Ordinal
        );
        var interfaceIndex = result.FormattedContent.IndexOf(
            "public interface IPublicInterface",
            StringComparison.Ordinal
        );
        var structIndex = result.FormattedContent.IndexOf(
            "public struct PublicStruct",
            StringComparison.Ordinal
        );
        var classIndex = result.FormattedContent.IndexOf(
            "public class PublicClass",
            StringComparison.Ordinal
        );

        Assert.True(enumIndex < interfaceIndex, "Enum should come before interface");
        Assert.True(interfaceIndex < structIndex, "Interface should come before struct");
        Assert.True(structIndex < classIndex, "Struct should come before class");
    }

    /// <summary>
    /// Tests that members within sorted types are also sorted.
    /// </summary>
    [Fact]
    public void FormatContent_SortTopLevelTypes_AlsoSortsMembersWithinTypes()
    {
        // Arrange
        var content =
            @"
namespace MyApp;

internal class InternalClass
{
    private int PrivateField;
    public int PublicField;
}

public class PublicClass
{
    private int PrivateField;
    public int PublicField;
}
";

        var options = new FormatterOptions { SortTopLevelTypes = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // PublicClass should come first
        var publicClassIndex = result.FormattedContent.IndexOf(
            "public class PublicClass",
            StringComparison.Ordinal
        );
        var internalClassIndex = result.FormattedContent.IndexOf(
            "internal class InternalClass",
            StringComparison.Ordinal
        );
        Assert.True(publicClassIndex < internalClassIndex);

        // Within each class, public fields should come before private
        var content2 = result.FormattedContent;
        var publicClassStart = publicClassIndex;
        var internalClassStart = internalClassIndex;

        // In PublicClass
        var publicFieldInPublicClass = content2.IndexOf(
            "public int PublicField",
            publicClassStart,
            StringComparison.Ordinal
        );
        var privateFieldInPublicClass = content2.IndexOf(
            "private int PrivateField",
            publicClassStart,
            StringComparison.Ordinal
        );

        if (
            publicFieldInPublicClass < internalClassStart
            && privateFieldInPublicClass < internalClassStart
        )
        {
            Assert.True(publicFieldInPublicClass < privateFieldInPublicClass);
        }
    }

    /// <summary>
    /// Tests alphabetical sorting combined with top-level type sorting.
    /// </summary>
    [Fact]
    public void FormatContent_BothOptions_SortsTypesAndMembersAlphabetically()
    {
        // Arrange
        var content =
            @"
namespace MyApp;

public class ZebraClass
{
    public string PropZ { get; set; }
    public string PropA { get; set; }
}

public class AlphaClass
{
    public string PropZ { get; set; }
    public string PropA { get; set; }
}
";

        var options = new FormatterOptions { SortTopLevelTypes = true, AlphabeticalSort = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Classes should be alphabetically sorted
        var alphaClassIndex = result.FormattedContent.IndexOf(
            "public class AlphaClass",
            StringComparison.Ordinal
        );
        var zebraClassIndex = result.FormattedContent.IndexOf(
            "public class ZebraClass",
            StringComparison.Ordinal
        );
        Assert.True(alphaClassIndex < zebraClassIndex, "AlphaClass should come before ZebraClass");

        // Properties within each class should be alphabetically sorted
        var propAInAlphaIndex = result.FormattedContent.IndexOf(
            "public string PropA",
            alphaClassIndex,
            StringComparison.Ordinal
        );
        var propZInAlphaIndex = result.FormattedContent.IndexOf(
            "public string PropZ",
            alphaClassIndex,
            StringComparison.Ordinal
        );

        // PropA should come before PropZ in AlphaClass
        if (
            propAInAlphaIndex > alphaClassIndex
            && propZInAlphaIndex > alphaClassIndex
            && propAInAlphaIndex < zebraClassIndex
            && propZInAlphaIndex < zebraClassIndex
        )
        {
            Assert.True(propAInAlphaIndex < propZInAlphaIndex);
        }
    }

    /// <summary>
    /// Tests that using directives are preserved when sorting top-level types.
    /// </summary>
    [Fact]
    public void FormatContent_SortTopLevelTypes_PreservesUsingDirectives()
    {
        // Arrange
        var content =
            @"
using System;
using System.Collections.Generic;

namespace MyApp;

internal class InternalClass { }
public class PublicClass { }
";

        var options = new FormatterOptions { SortTopLevelTypes = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);
        Assert.Contains("using System;", result.FormattedContent);
        Assert.Contains("using System.Collections.Generic;", result.FormattedContent);

        // Verify using directives come before namespace
        var usingIndex = result.FormattedContent.IndexOf("using System;", StringComparison.Ordinal);
        var namespaceIndex = result.FormattedContent.IndexOf(
            "namespace MyApp",
            StringComparison.Ordinal
        );
        Assert.True(usingIndex < namespaceIndex);
    }

    /// <summary>
    /// Tests sorting with only one top-level type.
    /// </summary>
    [Fact]
    public void FormatContent_SortTopLevelTypes_SingleType_NoChanges()
    {
        // Arrange
        var content =
            @"
namespace MyApp;

public class OnlyClass
{
    private int PrivateField;
    public int PublicField;
}
";

        var options = new FormatterOptions { SortTopLevelTypes = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Members should still be sorted even though there's only one type
        var publicFieldIndex = result.FormattedContent.IndexOf(
            "public int PublicField",
            StringComparison.Ordinal
        );
        var privateFieldIndex = result.FormattedContent.IndexOf(
            "private int PrivateField",
            StringComparison.Ordinal
        );
        Assert.True(publicFieldIndex < privateFieldIndex);
    }

    /// <summary>
    /// Tests that documentation comments move with types.
    /// </summary>
    [Fact]
    public void FormatContent_SortTopLevelTypes_PreservesDocComments()
    {
        // Arrange
        var content =
            @"
namespace MyApp;

/// <summary>
/// Internal class documentation.
/// </summary>
internal class InternalClass { }

/// <summary>
/// Public class documentation.
/// </summary>
public class PublicClass { }
";

        var options = new FormatterOptions { SortTopLevelTypes = true };
        var formatter = new Sa1201IerFormatter(options);

        // Act
        var result = formatter.FormatContent("test.cs", content);

        // Assert
        Assert.NotNull(result.FormattedContent);

        // Verify doc comments are preserved and in correct order
        Assert.Contains("/// <summary>", result.FormattedContent);
        Assert.Contains("/// Internal class documentation.", result.FormattedContent);
        Assert.Contains("/// Public class documentation.", result.FormattedContent);

        // Public class doc should come before internal class doc
        var publicDocIndex = result.FormattedContent.IndexOf(
            "/// Public class documentation.",
            StringComparison.Ordinal
        );
        var internalDocIndex = result.FormattedContent.IndexOf(
            "/// Internal class documentation.",
            StringComparison.Ordinal
        );
        Assert.True(publicDocIndex < internalDocIndex);
    }

    /// <summary>
    /// Tests CheckContent with top-level type sorting.
    /// </summary>
    [Fact]
    public void CheckContent_SortTopLevelTypes_DetectsViolations()
    {
        // Arrange
        var content =
            @"
namespace MyApp;

internal class InternalClass { }
public class PublicClass { }
";

        var options = new FormatterOptions { SortTopLevelTypes = true };

        // Act
        var result = Sa1201IerFormatter.CheckContent("test.cs", content, options);

        // Assert - should detect that types are in wrong order
        Assert.NotEmpty(result.Violations);
    }
}

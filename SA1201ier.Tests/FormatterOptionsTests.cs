using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Tests for FormatterOptions class.
/// </summary>
public class FormatterOptionsTests
{
    /// <summary>
    /// Tests that default options have correct values.
    /// </summary>
    [Fact]
    public void Default_ReturnsOptionsWithDefaultValues()
    {
        // Act
        var options = FormatterOptions.Default;

        // Assert
        Assert.False(options.AlphabeticalSort);
        Assert.False(options.SortTopLevelTypes);
        Assert.True(options.StaticMembersFirst);
        Assert.True(options.ConstMembersFirst);
        Assert.Null(options.AccessLevelOrder);
        Assert.Null(options.MemberTypeOrder);
        Assert.Null(options.TopLevelTypeOrder);
    }

    /// <summary>
    /// Tests that options can be created with custom values.
    /// </summary>
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new FormatterOptions { AlphabeticalSort = true, SortTopLevelTypes = true };

        // Assert
        Assert.True(options.AlphabeticalSort);
        Assert.True(options.SortTopLevelTypes);
    }

    /// <summary>
    /// Tests that MergeWith with null returns original options.
    /// </summary>
    [Fact]
    public void MergeWith_NullOther_ReturnsOriginal()
    {
        // Arrange
        var options = new FormatterOptions { AlphabeticalSort = true, SortTopLevelTypes = false };

        // Act
        var merged = options.MergeWith(null);

        // Assert
        Assert.True(merged.AlphabeticalSort);
        Assert.False(merged.SortTopLevelTypes);
    }

    /// <summary>
    /// Tests that MergeWith overrides with other's values.
    /// </summary>
    [Fact]
    public void MergeWith_OtherOptions_OverridesValues()
    {
        // Arrange
        var baseOptions = new FormatterOptions
        {
            AlphabeticalSort = false,
            SortTopLevelTypes = false,
        };

        var overrideOptions = new FormatterOptions
        {
            AlphabeticalSort = true,
            SortTopLevelTypes = true,
        };

        // Act
        var merged = baseOptions.MergeWith(overrideOptions);

        // Assert
        Assert.True(merged.AlphabeticalSort);
        Assert.True(merged.SortTopLevelTypes);
    }

    /// <summary>
    /// Tests that MergeWith creates a new instance.
    /// </summary>
    [Fact]
    public void MergeWith_CreatesNewInstance()
    {
        // Arrange
        var options1 = new FormatterOptions { AlphabeticalSort = false };
        var options2 = new FormatterOptions { AlphabeticalSort = true };

        // Act
        var merged = options1.MergeWith(options2);

        // Assert - merged should be a different instance
        Assert.NotSame(options1, merged);
        Assert.NotSame(options2, merged);
    }

    /// <summary>
    /// Tests that original options are not modified by MergeWith.
    /// </summary>
    [Fact]
    public void MergeWith_DoesNotModifyOriginal()
    {
        // Arrange
        var options1 = new FormatterOptions { AlphabeticalSort = false };
        var options2 = new FormatterOptions { AlphabeticalSort = true };

        // Act
        var merged = options1.MergeWith(options2);

        // Assert - original should be unchanged
        Assert.False(options1.AlphabeticalSort);
        Assert.True(merged.AlphabeticalSort);
    }

    /// <summary>
    /// Tests that custom access level order can be set.
    /// </summary>
    [Fact]
    public void CustomAccessLevelOrder_CanBeSet()
    {
        // Arrange & Act
        var options = new FormatterOptions
        {
            AccessLevelOrder = new List<string> { "Private", "Public" },
        };

        // Assert
        Assert.NotNull(options.AccessLevelOrder);
        Assert.Equal(2, options.AccessLevelOrder.Count);
        Assert.Equal("Private", options.AccessLevelOrder[0]);
        Assert.Equal("Public", options.AccessLevelOrder[1]);
    }

    /// <summary>
    /// Tests that custom member type order can be set.
    /// </summary>
    [Fact]
    public void CustomMemberTypeOrder_CanBeSet()
    {
        // Arrange & Act
        var options = new FormatterOptions
        {
            MemberTypeOrder = new List<string> { "Method", "Property", "Field" },
        };

        // Assert
        Assert.NotNull(options.MemberTypeOrder);
        Assert.Equal(3, options.MemberTypeOrder.Count);
        Assert.Equal("Method", options.MemberTypeOrder[0]);
    }

    /// <summary>
    /// Tests that MergeWith preserves custom orders when other has null values.
    /// </summary>
    [Fact]
    public void MergeWith_PreservesCustomOrdersWhenOtherIsNull()
    {
        // Arrange
        var baseOptions = new FormatterOptions
        {
            AccessLevelOrder = new List<string> { "Private", "Public" },
            MemberTypeOrder = new List<string> { "Method", "Field" },
        };

        var overrideOptions = new FormatterOptions { AlphabeticalSort = true };

        // Act
        var merged = baseOptions.MergeWith(overrideOptions);

        // Assert
        Assert.NotNull(merged.AccessLevelOrder);
        Assert.Equal(2, merged.AccessLevelOrder.Count);
        Assert.NotNull(merged.MemberTypeOrder);
        Assert.Equal(2, merged.MemberTypeOrder.Count);
    }

    /// <summary>
    /// Tests that MergeWith overrides custom orders when other has values.
    /// </summary>
    [Fact]
    public void MergeWith_OverridesCustomOrdersWhenOtherHasValues()
    {
        // Arrange
        var baseOptions = new FormatterOptions
        {
            AccessLevelOrder = new List<string> { "Private", "Public" },
        };

        var overrideOptions = new FormatterOptions
        {
            AccessLevelOrder = new List<string> { "Public", "Private", "Internal" },
        };

        // Act
        var merged = baseOptions.MergeWith(overrideOptions);

        // Assert
        Assert.NotNull(merged.AccessLevelOrder);
        Assert.Equal(3, merged.AccessLevelOrder.Count);
        Assert.Equal("Public", merged.AccessLevelOrder[0]);
        Assert.Equal("Internal", merged.AccessLevelOrder[2]);
    }
}

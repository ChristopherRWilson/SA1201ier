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
}

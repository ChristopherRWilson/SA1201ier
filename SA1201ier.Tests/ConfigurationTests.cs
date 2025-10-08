using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Tests for the configuration system and new formatting options.
/// </summary>
public class ConfigurationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ConfigurationLoader _loader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationTests"/> class.
    /// </summary>
    public ConfigurationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SA1201ier_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _loader = new ConfigurationLoader();
    }

    /// <summary>
    /// Cleans up test directory.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tests that default options are returned when no config file exists.
    /// </summary>
    [Fact]
    public void LoadConfiguration_NoConfigFile_ReturnsDefaults()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "Test.cs");

        // Act
        var options = _loader.LoadConfiguration(filePath);

        // Assert
        Assert.False(options.AlphabeticalSort);
        Assert.False(options.SortTopLevelTypes);
    }

    /// <summary>
    /// Tests that config file is loaded correctly.
    /// </summary>
    [Fact]
    public void LoadConfiguration_WithConfigFile_LoadsOptions()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, ".sa1201ierrc");
        File.WriteAllText(configPath, @"{""alphabeticalSort"": true, ""sortTopLevelTypes"": true}");
        var filePath = Path.Combine(_testDirectory, "Test.cs");

        // Act
        var options = _loader.LoadConfiguration(filePath);

        // Assert
        Assert.True(options.AlphabeticalSort);
        Assert.True(options.SortTopLevelTypes);
    }

    /// <summary>
    /// Tests hierarchical configuration merging.
    /// </summary>
    [Fact]
    public void LoadConfiguration_HierarchicalConfigs_MergesCorrectly()
    {
        // Arrange
        var rootConfig = Path.Combine(_testDirectory, ".sa1201ierrc");
        File.WriteAllText(
            rootConfig,
            @"{""alphabeticalSort"": false, ""sortTopLevelTypes"": false}"
        );

        var subDir = Path.Combine(_testDirectory, "SubDir");
        Directory.CreateDirectory(subDir);
        var subConfig = Path.Combine(subDir, ".sa1201ierrc");
        File.WriteAllText(subConfig, @"{""alphabeticalSort"": true}");

        var filePath = Path.Combine(subDir, "Test.cs");

        // Act
        var options = _loader.LoadConfiguration(filePath);

        // Assert
        Assert.True(options.AlphabeticalSort); // Overridden by child
        Assert.False(options.SortTopLevelTypes); // Inherited from parent
    }

    /// <summary>
    /// Tests that invalid JSON is handled gracefully.
    /// </summary>
    [Fact]
    public void LoadConfiguration_InvalidJson_ReturnsDefaults()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, ".sa1201ierrc");
        File.WriteAllText(configPath, @"{invalid json}");
        var filePath = Path.Combine(_testDirectory, "Test.cs");

        // Act
        var options = _loader.LoadConfiguration(filePath);

        // Assert - should fall back to defaults
        Assert.False(options.AlphabeticalSort);
        Assert.False(options.SortTopLevelTypes);
    }

    /// <summary>
    /// Tests that config caching works.
    /// </summary>
    [Fact]
    public void LoadConfiguration_MultipleCalls_UsesCaching()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, ".sa1201ierrc");
        File.WriteAllText(configPath, @"{""alphabeticalSort"": true}");
        var filePath = Path.Combine(_testDirectory, "Test.cs");

        // Act
        var options1 = _loader.LoadConfiguration(filePath);
        var options2 = _loader.LoadConfiguration(filePath);

        // Assert - both should return same result
        Assert.True(options1.AlphabeticalSort);
        Assert.True(options2.AlphabeticalSort);
    }

    /// <summary>
    /// Tests CreateSampleConfig creates a valid config file.
    /// </summary>
    [Fact]
    public void CreateSampleConfig_CreatesValidFile()
    {
        // Act
        ConfigurationLoader.CreateSampleConfig(_testDirectory);

        // Assert
        var configPath = Path.Combine(_testDirectory, ".sa1201ierrc");
        Assert.True(File.Exists(configPath));

        var content = File.ReadAllText(configPath);
        Assert.Contains("alphabeticalSort", content);
        Assert.Contains("sortTopLevelTypes", content);
    }

    /// <summary>
    /// Tests CreateSampleConfig throws when file already exists.
    /// </summary>
    [Fact]
    public void CreateSampleConfig_FileExists_ThrowsException()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, ".sa1201ierrc");
        File.WriteAllText(configPath, "{}");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.CreateSampleConfig(_testDirectory)
        );
    }
}

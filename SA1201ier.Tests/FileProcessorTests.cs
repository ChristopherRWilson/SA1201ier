using SA1201ier.Core;

namespace SA1201ier.Tests;

/// <summary>
/// Tests for the FileProcessor class.
/// </summary>
public class FileProcessorTests : IDisposable
{
    private readonly FileProcessor _processor;

    private readonly string _tempDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessorTests" /> class.
    /// </summary>
    public FileProcessorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"SA1201Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _processor = new FileProcessor();
    }

    /// <summary>
    /// Cleans up temporary test files.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Tests processing a directory with multiple files.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_Directory_ProcessesAllFiles()
    {
        // Arrange
        var file1 = Path.Combine(_tempDirectory, "test1.cs");
        var file2 = Path.Combine(_tempDirectory, "test2.cs");

        var content1 =
            @"
public class TestClass1
{
    private int PrivateField;
    public int PublicField;
}";
        var content2 =
            @"
public class TestClass2
{
    private void PrivateMethod() { }
    public void PublicMethod() { }
}";

        await File.WriteAllTextAsync(file1, content1);
        await File.WriteAllTextAsync(file2, content2);

        // Act
        var results = await _processor.ProcessAsync(
            _tempDirectory,
            check: false,
            writeChanges: true
        );

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.HasChanges));
    }

    /// <summary>
    /// Tests that format mode without write changes doesn't modify files.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_FormatModeWithoutWriteChanges_DoesNotModifyFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.cs");
        var content =
            @"
public class TestClass
{
    private int PrivateField;
    public int PublicField;
}";
        await File.WriteAllTextAsync(filePath, content);
        var originalContent = await File.ReadAllTextAsync(filePath);

        // Act
        var results = await _processor.ProcessAsync(filePath, check: false, writeChanges: false);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].HasChanges);

        // Verify file was NOT written
        var currentContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(originalContent, currentContent);
    }

    /// <summary>
    /// Tests that non-CS files are ignored.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_NonCsFile_ThrowsException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(filePath, "not a C# file");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _processor.ProcessAsync(filePath, check: true, writeChanges: false);
        });
    }

    /// <summary>
    /// Tests that nonexistent paths throw an exception.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_NonexistentPath_ThrowsException()
    {
        // Arrange
        var nonexistentPath = Path.Combine(_tempDirectory, "nonexistent.cs");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await _processor.ProcessAsync(nonexistentPath, check: true, writeChanges: false);
        });
    }

    /// <summary>
    /// Tests processing a properly formatted file.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_ProperlyFormattedFile_NoChanges()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.cs");
        var content =
            @"
public class TestClass
{
    public const int PublicConst = 1;
    private const int PrivateConst = 2;

    public int PublicField;
    private int PrivateField;

    public TestClass() { }

    public int PublicProperty { get; set; }
    private int PrivateProperty { get; set; }

    public void PublicMethod() { }
    private void PrivateMethod() { }
}";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var results = await _processor.ProcessAsync(filePath, check: false, writeChanges: true);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].HasChanges);
        Assert.Empty(results[0].Violations);
    }

    /// <summary>
    /// Tests processing a single file in check mode.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_SingleFile_CheckMode_DetectsViolations()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.cs");
        var content =
            @"
public class TestClass
{
    private int PrivateField;
    public int PublicField;
}";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var results = await _processor.ProcessAsync(filePath, check: true, writeChanges: false);

        // Assert
        Assert.Single(results);
        Assert.NotEmpty(results[0].Violations);
        Assert.False(results[0].HasChanges);
    }

    /// <summary>
    /// Tests processing a single file in format mode.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_SingleFile_FormatMode_FormatsFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.cs");
        var content =
            @"
public class TestClass
{
    private int PrivateField;
    public int PublicField;
}";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var results = await _processor.ProcessAsync(filePath, check: false, writeChanges: true);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].HasChanges);

        // Verify file was actually written
        var updatedContent = await File.ReadAllTextAsync(filePath);
        Assert.NotEqual(content, updatedContent);

        var publicIndex = updatedContent.IndexOf(
            "public int PublicField",
            StringComparison.Ordinal
        );
        var privateIndex = updatedContent.IndexOf(
            "private int PrivateField",
            StringComparison.Ordinal
        );
        Assert.True(publicIndex < privateIndex);
    }

    /// <summary>
    /// Tests processing a subdirectory.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_Subdirectory_ProcessesRecursively()
    {
        // Arrange
        var subDir = Path.Combine(_tempDirectory, "subdir");
        Directory.CreateDirectory(subDir);

        var file1 = Path.Combine(_tempDirectory, "test1.cs");
        var file2 = Path.Combine(subDir, "test2.cs");

        var content =
            @"
public class TestClass
{
    private int PrivateField;
    public int PublicField;
}";

        await File.WriteAllTextAsync(file1, content);
        await File.WriteAllTextAsync(file2, content);

        // Act
        var results = await _processor.ProcessAsync(
            _tempDirectory,
            check: true,
            writeChanges: false
        );

        // Assert
        Assert.Equal(2, results.Count);
    }
}

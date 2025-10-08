using System.Text.Json;

namespace SA1201ier.Core;

/// <summary>
/// Loads configuration from .sa1201ierrc files in a hierarchical manner.
/// </summary>
public class ConfigurationLoader
{
    private const string ConfigFileName = ".sa1201ierrc";
    private readonly Dictionary<string, FormatterOptions> _configCache = new();

    /// <summary>
    /// Loads the effective configuration for a given file path.
    /// Searches up the directory hierarchy for .sa1201ierrc files and merges them.
    /// </summary>
    /// <param name="filePath">The path to the file being formatted.</param>
    /// <returns>The merged configuration options.</returns>
    public FormatterOptions LoadConfiguration(string filePath)
    {
        var fileDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (string.IsNullOrEmpty(fileDirectory))
        {
            return FormatterOptions.Default;
        }

        return LoadConfigurationForDirectory(fileDirectory);
    }

    /// <summary>
    /// Loads configuration for a directory, searching up the hierarchy.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    /// <returns>The merged configuration options.</returns>
    private FormatterOptions LoadConfigurationForDirectory(string directoryPath)
    {
        // Check cache first
        if (_configCache.TryGetValue(directoryPath, out var cachedOptions))
        {
            return cachedOptions;
        }

        var options = FormatterOptions.Default;
        var currentDir = directoryPath;
        var configsToMerge = new Stack<FormatterOptions>();

        // Walk up the directory tree collecting config files
        while (!string.IsNullOrEmpty(currentDir))
        {
            var configPath = Path.Combine(currentDir, ConfigFileName);
            if (File.Exists(configPath))
            {
                var fileOptions = LoadConfigFile(configPath);
                if (fileOptions != null)
                {
                    configsToMerge.Push(fileOptions);
                }
            }

            var parent = Path.GetDirectoryName(currentDir);
            if (parent == currentDir) // Reached root
            {
                break;
            }
            currentDir = parent;
        }

        // Merge configs from root to leaf (leaf overrides root)
        while (configsToMerge.Count > 0)
        {
            options = options.MergeWith(configsToMerge.Pop());
        }

        // Cache the result
        _configCache[directoryPath] = options;

        return options;
    }

    /// <summary>
    /// Loads a configuration file.
    /// </summary>
    /// <param name="configPath">The path to the config file.</param>
    /// <returns>The loaded options, or null if loading failed.</returns>
    private FormatterOptions? LoadConfigFile(string configPath)
    {
        try
        {
            var json = File.ReadAllText(configPath);
            var options = JsonSerializer.Deserialize<FormatterOptions>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                }
            );
            return options ?? FormatterOptions.Default;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Warning: Failed to load config file '{configPath}': {ex.Message}"
            );
            return null;
        }
    }

    /// <summary>
    /// Clears the configuration cache. Useful for testing or when config files change.
    /// </summary>
    public void ClearCache()
    {
        _configCache.Clear();
    }

    /// <summary>
    /// Creates a sample .sa1201ierrc file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory to create the config file in.</param>
    public static void CreateSampleConfig(string directoryPath)
    {
        var configPath = Path.Combine(directoryPath, ConfigFileName);
        if (File.Exists(configPath))
        {
            throw new InvalidOperationException($"Config file already exists at: {configPath}");
        }

        var sampleConfig = new
        {
            alphabeticalSort = false,
            sortTopLevelTypes = false,
            _comment = "SA1201ier configuration file. See documentation for available options.",
        };

        var json = JsonSerializer.Serialize(
            sampleConfig,
            new JsonSerializerOptions { WriteIndented = true }
        );

        File.WriteAllText(configPath, json);
    }
}

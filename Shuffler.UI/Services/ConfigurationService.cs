using System.Text.Json;
using Shuffler.Core;

namespace Shuffler.UI.Services;

public class ConfigurationService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var shufflerPath = Path.Combine(appDataPath, "Shuffler");
        Directory.CreateDirectory(shufflerPath);
        _configPath = Path.Combine(shufflerPath, "config.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public ShufflerConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            var defaultConfig = DefaultConfig.Create();
            var json = JsonSerializer.Serialize(defaultConfig, _jsonOptions);
            File.WriteAllText(_configPath, json);
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<ShufflerConfig>(json, _jsonOptions);
            return config ?? DefaultConfig.Create();
        }
        catch (Exception)
        {
            return DefaultConfig.Create();
        }
    }

    public async Task SaveAsync(ShufflerConfig config)
    {
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(_configPath, json);
    }
}
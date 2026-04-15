using System;
using System.IO;
using System.Text.Json;
using SuperVibe.Models;

namespace SuperVibe.Services;

public class ConfigService
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SuperVibe");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaults = new AppConfig();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            ApplyDefaults(config);
            return config;
        }
        catch
        {
            // Corrupted config — fall back to defaults
            var defaults = new AppConfig();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    private static void ApplyDefaults(AppConfig config)
    {
        if (string.IsNullOrEmpty(config.SttModel))
            config.SttModel = "small";
        if (string.IsNullOrEmpty(config.LlmProvider))
            config.LlmProvider = "claude";
    }
}

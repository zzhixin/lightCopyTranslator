using System;
using System.IO;
using System.Text.Json;

namespace LightCopyTranslator.Services;

public sealed class AppConfig
{
    public List<ModelConfig> Models { get; set; } = [];
    public UiConfig Ui { get; set; } = new();
}

public sealed class ModelConfig
{
    public string Name { get; set; } = "Model";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public bool Enabled { get; set; } = true;
}

public sealed class UiConfig
{
    public bool ShowSourcePanel { get; set; } = false;
}

internal static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static AppConfig Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
        {
            var config = new AppConfig
            {
                Models = GetDefaultModels()
            };
            Save(config, path);
            return config;
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            if (config.Models.Count == 0)
            {
                var legacy = TryParseLegacyModels(json);
                config.Models = legacy.Count > 0 ? legacy : GetDefaultModels();
                Save(config, path);
            }

            return config;
        }
        catch
        {
            return new AppConfig
            {
                Models = GetDefaultModels()
            };
        }
    }

    public static void Save(AppConfig config)
    {
        Save(config, GetConfigPath());
    }

    private static void Save(AppConfig config, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static string GetConfigPath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(baseDir, "LightCopyTranslator", "config.json");
    }

    private static List<ModelConfig> GetDefaultModels()
    {
        return
        [
            new ModelConfig
            {
                Name = "DeepSeek",
                Model = "deepseek/deepseek-chat"
            },
            new ModelConfig
            {
                Name = "Llama 3.3",
                Model = "meta-llama/llama-3.3-70b-instruct:free"
            }
        ];
    }

    private static List<ModelConfig> TryParseLegacyModels(string json)
    {
        var models = new List<ModelConfig>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return models;
        }

        var legacyMap = new (string Key, string DefaultModel, string DefaultName)[]
        {
            ("DeepSeek", "deepseek/deepseek-chat", "DeepSeek"),
            ("Llama", "meta-llama/llama-3.3-70b-instruct:free", "Llama 3.3"),
            ("Kimi", "moonshotai/kimi-k2:free", "Kimi"),
            ("Glm", "z-ai/glm-4.5-air:free", "GLM-4.5 Air"),
            ("Qwen", "qwen/qwen3-4b:free", "Qwen")
        };

        foreach (var legacy in legacyMap)
        {
            if (!doc.RootElement.TryGetProperty(legacy.Key, out var node) || node.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var apiKey = GetString(node, "apiKey");
            var model = GetString(node, "model");
            var baseUrl = GetString(node, "baseUrl");

            models.Add(new ModelConfig
            {
                Name = legacy.DefaultName,
                ApiKey = apiKey,
                Model = string.IsNullOrWhiteSpace(model) ? legacy.DefaultModel : model,
                BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://openrouter.ai/api/v1" : baseUrl,
                Enabled = true
            });
        }

        return models;
    }

    private static string GetString(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? "";
        }

        return "";
    }
}

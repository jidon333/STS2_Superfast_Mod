using System.Text.Json;

namespace Sts2Speed.Core.Configuration;

public static class SettingsLoader
{
    public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static SettingsLoadResult LoadFromFile(string? configPath, IReadOnlyDictionary<string, string?> environment)
    {
        var source = string.IsNullOrWhiteSpace(configPath) ? "<defaults>" : configPath!;
        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
        {
            return LoadFromJson(null, source, environment);
        }

        var json = File.ReadAllText(configPath);
        return LoadFromJson(json, source, environment);
    }

    public static SettingsLoadResult LoadFromJson(string? json, string source, IReadOnlyDictionary<string, string?> environment)
    {
        var warnings = new List<string>();
        var configuration = WorkspaceConfiguration.CreateLocalDefault();

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var partial = JsonSerializer.Deserialize<PartialWorkspaceConfiguration>(json, JsonOptions);
                configuration = configuration.With(partial);
            }
            catch (JsonException exception)
            {
                warnings.Add($"Ignored configuration document from {source}: {exception.Message}");
            }
        }

        var envOverrides = EnvironmentOverrideReader.Read(environment);
        warnings.AddRange(envOverrides.Warnings);
        configuration = configuration with
        {
            Settings = configuration.Settings.With(envOverrides.Settings),
        };

        return new SettingsLoadResult
        {
            Configuration = configuration,
            ConfigurationSource = source,
            AppliedEnvironmentOverrides = envOverrides.AppliedOverrideNames,
            Warnings = warnings,
        };
    }

    public static string Serialize(WorkspaceConfiguration configuration)
    {
        return JsonSerializer.Serialize(configuration, JsonOptions);
    }
}

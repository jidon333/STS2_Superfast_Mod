using System.Globalization;
using System.Text.Json;

namespace Sts2Speed.Core.Configuration;

public sealed record RuntimeSpeedSettings
{
    public string ModDirectory { get; init; } = string.Empty;

    public string ConfigPath { get; init; } = string.Empty;

    public string LegacySpeedFilePath { get; init; } = string.Empty;

    public bool Enabled { get; init; }

    public double BaseSpeed { get; init; } = 3.0;

    public double SpineSpeed { get; init; } = 3.0;

    public double QueueSpeed { get; init; } = 3.0;

    public double EffectSpeed { get; init; } = 3.0;

    public double CombatUiSpeed { get; init; } = 3.0;

    public double CombatVfxSpeed { get; init; } = 3.0;

    public bool CombatOnly { get; init; } = true;

    public IReadOnlyList<string> Sources { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public static class RuntimeSettingsLoader
{
    public const string RuntimeConfigFileName = "Sts2Speed.config.json";
    public const string LegacySpeedFileName = "Sts2Speed.speed.txt";

    public static RuntimeSpeedSettings Load(string modDirectory)
    {
        var configPath = Path.Combine(modDirectory, RuntimeConfigFileName);
        var legacySpeedFilePath = Path.Combine(modDirectory, LegacySpeedFileName);
        var warnings = new List<string>();
        var sources = new List<string>();
        var settings = SpeedModSettings.Defaults;

        var documentSettings = TryReadRuntimeConfig(configPath, warnings);
        if (documentSettings is not null)
        {
            settings = settings.With(documentSettings);
            sources.Add(RuntimeConfigFileName);
        }

        var legacySharedSpeed = TryReadLegacySharedSpeed(legacySpeedFilePath, warnings);
        if (legacySharedSpeed.HasValue && documentSettings is null)
        {
            settings = settings with
            {
                BaseSpeed = legacySharedSpeed.Value,
                Enabled = !IsApproximately(legacySharedSpeed.Value, 1.0),
            };
            sources.Add(LegacySpeedFileName);
            warnings.Add($"Loaded legacy fallback from {LegacySpeedFileName}. Prefer {RuntimeConfigFileName} for flat speed tuning.");
        }
        else if (legacySharedSpeed.HasValue)
        {
            warnings.Add($"Ignored {LegacySpeedFileName} because {RuntimeConfigFileName} is present.");
        }

        var environment = ReadRelevantEnvironment();
        var environmentOverrides = EnvironmentOverrideReader.Read(environment);
        warnings.AddRange(environmentOverrides.Warnings);
        if (environmentOverrides.AppliedOverrideNames.Count > 0)
        {
            settings = settings.With(environmentOverrides.Settings);
            sources.Add("environment");
        }

        return new RuntimeSpeedSettings
        {
            ModDirectory = modDirectory,
            ConfigPath = configPath,
            LegacySpeedFilePath = legacySpeedFilePath,
            Enabled = settings.Enabled,
            BaseSpeed = settings.BaseSpeed,
            SpineSpeed = settings.EffectiveSpineSpeed,
            QueueSpeed = settings.EffectiveQueueSpeed,
            EffectSpeed = settings.EffectiveEffectSpeed,
            CombatUiSpeed = settings.EffectiveCombatUiSpeed,
            CombatVfxSpeed = settings.EffectiveCombatVfxSpeed,
            CombatOnly = settings.CombatOnly,
            Sources = sources.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Warnings = warnings,
        };
    }

    private static IReadOnlyDictionary<string, string?> ReadRelevantEnvironment()
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [EnvironmentOverrideNames.Enabled] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.Enabled),
            [EnvironmentOverrideNames.BaseSpeed] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.BaseSpeed),
            [EnvironmentOverrideNames.CombatOnly] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.CombatOnly),
            [EnvironmentOverrideNames.SpineSpeed] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.SpineSpeed),
            [EnvironmentOverrideNames.QueueSpeed] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.QueueSpeed),
            [EnvironmentOverrideNames.EffectSpeed] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.EffectSpeed),
            [EnvironmentOverrideNames.CombatUiSpeed] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.CombatUiSpeed),
            [EnvironmentOverrideNames.CombatVfxSpeed] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.CombatVfxSpeed),
            [EnvironmentOverrideNames.LegacySpineFactor] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.LegacySpineFactor),
            [EnvironmentOverrideNames.LegacyQueueWaitFactor] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.LegacyQueueWaitFactor),
            [EnvironmentOverrideNames.LegacyEffectDelayFactor] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.LegacyEffectDelayFactor),
            [EnvironmentOverrideNames.LegacyCombatUiDeltaFactor] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.LegacyCombatUiDeltaFactor),
            [EnvironmentOverrideNames.LegacyCombatVfxDeltaFactor] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.LegacyCombatVfxDeltaFactor),
        };
    }

    private static PartialSpeedModSettings? TryReadRuntimeConfig(string path, ICollection<string> warnings)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PartialSpeedModSettings>(File.ReadAllText(path), SettingsLoader.JsonOptions);
        }
        catch (JsonException exception)
        {
            warnings.Add($"Ignored {path}: {exception.Message}");
            return null;
        }
    }

    private static double? TryReadLegacySharedSpeed(string path, ICollection<string> warnings)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var raw = File.ReadAllText(path).Trim();
        if (raw.Length == 0)
        {
            warnings.Add($"Ignored {path}: file was empty.");
            return null;
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        warnings.Add($"Ignored {path}: expected invariant floating-point number but received '{raw}'.");
        return null;
    }

    private static bool IsApproximately(double left, double right)
    {
        return Math.Abs(left - right) < 0.0001;
    }
}

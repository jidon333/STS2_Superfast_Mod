using System.Globalization;

namespace Sts2Speed.Core.Configuration;

public sealed record RuntimeSpeedSettings
{
    public string ModDirectory { get; init; } = string.Empty;

    public string SharedSpeedFilePath { get; init; } = string.Empty;

    public bool Enabled { get; init; }

    public double SpineTimeScale { get; init; } = 1.0;

    public double QueueWaitScale { get; init; } = 1.0;

    public double EffectDelayScale { get; init; } = 1.0;

    public bool CombatOnly { get; init; } = true;

    public bool VerboseLogging { get; init; }

    public IReadOnlyList<string> Sources { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public static class RuntimeSettingsLoader
{
    public static RuntimeSpeedSettings Load(string modDirectory)
    {
        var sharedSpeedFilePath = Path.Combine(modDirectory, "Sts2Speed.speed.txt");
        var warnings = new List<string>();
        var sources = new List<string>();
        var environment = ReadRelevantEnvironment();
        var environmentOverrides = EnvironmentOverrideReader.Read(environment);
        var settings = SpeedModSettings.Defaults.With(environmentOverrides.Settings);

        warnings.AddRange(environmentOverrides.Warnings);
        if (environmentOverrides.AppliedOverrideNames.Count > 0)
        {
            sources.Add("environment");
        }

        var sharedMultiplier = TryReadSharedMultiplier(sharedSpeedFilePath, warnings);
        if (sharedMultiplier.HasValue)
        {
            sources.Add("Sts2Speed.speed.txt");
            settings = ApplySharedMultiplierFallback(settings, sharedMultiplier.Value, environmentOverrides.AppliedOverrideNames);
        }

        if (!environmentOverrides.AppliedOverrideNames.Contains(EnvironmentOverrideNames.Enabled, StringComparer.OrdinalIgnoreCase)
            && ShouldImplicitlyEnable(settings, sharedMultiplier, environmentOverrides.AppliedOverrideNames))
        {
            settings = settings with { Enabled = true };
            sources.Add("implicit-enable");
        }

        return new RuntimeSpeedSettings
        {
            ModDirectory = modDirectory,
            SharedSpeedFilePath = sharedSpeedFilePath,
            Enabled = settings.Enabled,
            SpineTimeScale = settings.SpineTimeScale,
            QueueWaitScale = settings.QueueWaitScale,
            EffectDelayScale = settings.EffectDelayScale,
            CombatOnly = settings.CombatOnly,
            VerboseLogging = settings.VerboseLogging,
            Sources = sources.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Warnings = warnings,
        };
    }

    private static IReadOnlyDictionary<string, string?> ReadRelevantEnvironment()
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [EnvironmentOverrideNames.Enabled] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.Enabled),
            [EnvironmentOverrideNames.AnimationScale] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.AnimationScale),
            [EnvironmentOverrideNames.SpineTimeScale] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.SpineTimeScale),
            [EnvironmentOverrideNames.QueueWaitScale] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.QueueWaitScale),
            [EnvironmentOverrideNames.EffectDelayScale] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.EffectDelayScale),
            [EnvironmentOverrideNames.FastModeOverride] = Environment.GetEnvironmentVariable(EnvironmentOverrideNames.FastModeOverride),
        };
    }

    private static double? TryReadSharedMultiplier(string path, ICollection<string> warnings)
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

    private static SpeedModSettings ApplySharedMultiplierFallback(
        SpeedModSettings settings,
        double sharedMultiplier,
        IReadOnlyCollection<string> appliedOverrides)
    {
        var partial = new PartialSpeedModSettings
        {
            SpineTimeScale = appliedOverrides.Contains(EnvironmentOverrideNames.SpineTimeScale, StringComparer.OrdinalIgnoreCase)
                ? null
                : sharedMultiplier,
            QueueWaitScale = appliedOverrides.Contains(EnvironmentOverrideNames.QueueWaitScale, StringComparer.OrdinalIgnoreCase)
                ? null
                : sharedMultiplier,
            EffectDelayScale = appliedOverrides.Contains(EnvironmentOverrideNames.EffectDelayScale, StringComparer.OrdinalIgnoreCase)
                ? null
                : sharedMultiplier,
        };

        return settings.With(partial);
    }

    private static bool ShouldImplicitlyEnable(
        SpeedModSettings settings,
        double? sharedMultiplier,
        IReadOnlyCollection<string> appliedOverrides)
    {
        if (sharedMultiplier.HasValue && !IsApproximately(sharedMultiplier.Value, 1.0))
        {
            return true;
        }

        return appliedOverrides.Any(name =>
                name is EnvironmentOverrideNames.SpineTimeScale
                    or EnvironmentOverrideNames.QueueWaitScale
                    or EnvironmentOverrideNames.EffectDelayScale)
            && (!IsApproximately(settings.SpineTimeScale, 1.0)
                || !IsApproximately(settings.QueueWaitScale, 1.0)
                || !IsApproximately(settings.EffectDelayScale, 1.0));
    }

    private static bool IsApproximately(double left, double right)
    {
        return Math.Abs(left - right) < 0.0001;
    }
}

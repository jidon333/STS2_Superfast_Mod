namespace Sts2Speed.Core.Configuration;

public static class EnvironmentOverrideNames
{
    public const string Prefix = "STS2_SPEED_";
    public const string Enabled = Prefix + "ENABLED";
    public const string SpineTimeScale = Prefix + "SPINE_TIME_SCALE";
    public const string QueueWaitScale = Prefix + "QUEUE_WAIT_SCALE";
    public const string EffectDelayScale = Prefix + "EFFECT_DELAY_SCALE";
}

public sealed record EnvironmentOverrideResult
{
    public PartialSpeedModSettings Settings { get; init; } = new();

    public IReadOnlyList<string> AppliedOverrideNames { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public static class EnvironmentOverrideReader
{
    public static EnvironmentOverrideResult Read(IReadOnlyDictionary<string, string?> environment)
    {
        var applied = new List<string>();
        var warnings = new List<string>();
        var map = new Dictionary<string, string?>(environment, StringComparer.OrdinalIgnoreCase);
        var settings = new PartialSpeedModSettings
        {
            Enabled = ReadBoolean(map, EnvironmentOverrideNames.Enabled, applied, warnings),
            SpineTimeScale = ReadDouble(map, EnvironmentOverrideNames.SpineTimeScale, applied, warnings),
            QueueWaitScale = ReadDouble(map, EnvironmentOverrideNames.QueueWaitScale, applied, warnings),
            EffectDelayScale = ReadDouble(map, EnvironmentOverrideNames.EffectDelayScale, applied, warnings),
        };

        return new EnvironmentOverrideResult
        {
            Settings = settings,
            AppliedOverrideNames = applied,
            Warnings = warnings,
        };
    }

    private static bool? ReadBoolean(
        IReadOnlyDictionary<string, string?> environment,
        string name,
        ICollection<string> applied,
        ICollection<string> warnings)
    {
        if (!environment.TryGetValue(name, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (bool.TryParse(raw, out var value))
        {
            applied.Add(name);
            return value;
        }

        warnings.Add($"Ignored {name}: expected boolean but received '{raw}'.");
        return null;
    }

    private static double? ReadDouble(
        IReadOnlyDictionary<string, string?> environment,
        string name,
        ICollection<string> applied,
        ICollection<string> warnings)
    {
        if (!environment.TryGetValue(name, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            applied.Add(name);
            return value;
        }

        warnings.Add($"Ignored {name}: expected number but received '{raw}'.");
        return null;
    }
}

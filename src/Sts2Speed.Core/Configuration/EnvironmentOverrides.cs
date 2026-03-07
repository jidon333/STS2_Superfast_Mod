using System.Globalization;

namespace Sts2Speed.Core.Configuration;

public static class EnvironmentOverrideNames
{
    public const string Prefix = "STS2_SPEED_";
    public const string Enabled = Prefix + "ENABLED";
    public const string BaseSpeed = Prefix + "BASE_SPEED";
    public const string CombatOnly = Prefix + "COMBAT_ONLY";
    public const string SpineSpeed = Prefix + "SPINE_SPEED";
    public const string QueueSpeed = Prefix + "QUEUE_SPEED";
    public const string EffectSpeed = Prefix + "EFFECT_SPEED";
    public const string CombatUiSpeed = Prefix + "COMBAT_UI_SPEED";
    public const string CombatVfxSpeed = Prefix + "COMBAT_VFX_SPEED";

    public const string LegacySpineFactor = Prefix + "SPINE_FACTOR";
    public const string LegacyQueueWaitFactor = Prefix + "QUEUE_WAIT_FACTOR";
    public const string LegacyEffectDelayFactor = Prefix + "EFFECT_DELAY_FACTOR";
    public const string LegacyCombatUiDeltaFactor = Prefix + "COMBAT_UI_DELTA_FACTOR";
    public const string LegacyCombatVfxDeltaFactor = Prefix + "COMBAT_VFX_DELTA_FACTOR";
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
            BaseSpeed = ReadDouble(map, EnvironmentOverrideNames.BaseSpeed, applied, warnings),
            CombatOnly = ReadBoolean(map, EnvironmentOverrideNames.CombatOnly, applied, warnings),
            SpineSpeed = ReadDoubleWithLegacy(
                map,
                EnvironmentOverrideNames.SpineSpeed,
                EnvironmentOverrideNames.LegacySpineFactor,
                applied,
                warnings),
            QueueSpeed = ReadDoubleWithLegacy(
                map,
                EnvironmentOverrideNames.QueueSpeed,
                EnvironmentOverrideNames.LegacyQueueWaitFactor,
                applied,
                warnings),
            EffectSpeed = ReadDoubleWithLegacy(
                map,
                EnvironmentOverrideNames.EffectSpeed,
                EnvironmentOverrideNames.LegacyEffectDelayFactor,
                applied,
                warnings),
            CombatUiSpeed = ReadDoubleWithLegacy(
                map,
                EnvironmentOverrideNames.CombatUiSpeed,
                EnvironmentOverrideNames.LegacyCombatUiDeltaFactor,
                applied,
                warnings),
            CombatVfxSpeed = ReadDoubleWithLegacy(
                map,
                EnvironmentOverrideNames.CombatVfxSpeed,
                EnvironmentOverrideNames.LegacyCombatVfxDeltaFactor,
                applied,
                warnings),
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

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            applied.Add(name);
            return value;
        }

        warnings.Add($"Ignored {name}: expected number but received '{raw}'.");
        return null;
    }

    private static double? ReadDoubleWithLegacy(
        IReadOnlyDictionary<string, string?> environment,
        string primaryName,
        string legacyName,
        ICollection<string> applied,
        ICollection<string> warnings)
    {
        var primary = ReadDouble(environment, primaryName, applied, warnings);
        if (primary.HasValue)
        {
            return primary;
        }

        var legacy = ReadDouble(environment, legacyName, applied, warnings);
        if (legacy.HasValue)
        {
            warnings.Add($"Loaded legacy environment override {legacyName}. Prefer {primaryName}.");
        }

        return legacy;
    }
}

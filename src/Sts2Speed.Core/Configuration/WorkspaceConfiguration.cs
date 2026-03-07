using System.Text.Json.Serialization;

namespace Sts2Speed.Core.Configuration;

public sealed record WorkspaceConfiguration
{
    public GamePathOptions GamePaths { get; init; } = GamePathOptions.CreateLocalDefault();

    public SpeedModSettings Settings { get; init; } = SpeedModSettings.Defaults;

    public static WorkspaceConfiguration CreateLocalDefault()
    {
        return new WorkspaceConfiguration();
    }

    public WorkspaceConfiguration With(PartialWorkspaceConfiguration? partial)
    {
        if (partial is null)
        {
            return this;
        }

        return this with
        {
            GamePaths = GamePaths.With(partial.GamePaths),
            Settings = Settings.With(partial.Settings),
        };
    }
}

public sealed record PartialWorkspaceConfiguration
{
    public PartialGamePathOptions? GamePaths { get; init; }

    public PartialSpeedModSettings? Settings { get; init; }
}

public sealed record GamePathOptions
{
    public string GameDirectory { get; init; } = string.Empty;

    public string UserDataRoot { get; init; } = string.Empty;

    public string SteamAccountId { get; init; } = string.Empty;

    public int ProfileIndex { get; init; } = 1;

    public string ArtifactsRoot { get; init; } = "artifacts";

    public static GamePathOptions CreateLocalDefault()
    {
        return new GamePathOptions
        {
            GameDirectory = @"D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
            UserDataRoot = @"C:\Users\jidon\AppData\Roaming\SlayTheSpire2",
            SteamAccountId = "76561198206882255",
            ProfileIndex = 1,
            ArtifactsRoot = "artifacts",
        };
    }

    public GamePathOptions With(PartialGamePathOptions? partial)
    {
        if (partial is null)
        {
            return this;
        }

        return this with
        {
            GameDirectory = partial.GameDirectory ?? GameDirectory,
            UserDataRoot = partial.UserDataRoot ?? UserDataRoot,
            SteamAccountId = partial.SteamAccountId ?? SteamAccountId,
            ProfileIndex = partial.ProfileIndex ?? ProfileIndex,
            ArtifactsRoot = partial.ArtifactsRoot ?? ArtifactsRoot,
        };
    }
}

public sealed record PartialGamePathOptions
{
    public string? GameDirectory { get; init; }

    public string? UserDataRoot { get; init; }

    public string? SteamAccountId { get; init; }

    public int? ProfileIndex { get; init; }

    public string? ArtifactsRoot { get; init; }
}

public sealed record LegacySpeedGroupSettings
{
    public double? Spine { get; init; }

    public double? QueueWait { get; init; }

    public double? EffectDelay { get; init; }

    public double? CombatUiDelta { get; init; }

    public double? CombatVfxDelta { get; init; }
}

public sealed record SpeedModSettings
{
    public bool Enabled { get; init; }

    public double BaseSpeed { get; init; }

    public bool CombatOnly { get; init; }

    public double SpineSpeed { get; init; } = 1.0;

    public double QueueSpeed { get; init; } = 1.0;

    public double EffectSpeed { get; init; } = 1.0;

    public double CombatUiSpeed { get; init; } = 1.0;

    public double CombatVfxSpeed { get; init; } = 1.0;

    [JsonIgnore]
    public double EffectiveSpineSpeed => BaseSpeed * SpineSpeed;

    [JsonIgnore]
    public double EffectiveQueueSpeed => BaseSpeed * QueueSpeed;

    [JsonIgnore]
    public double EffectiveEffectSpeed => BaseSpeed * EffectSpeed;

    [JsonIgnore]
    public double EffectiveCombatUiSpeed => BaseSpeed * CombatUiSpeed;

    [JsonIgnore]
    public double EffectiveCombatVfxSpeed => BaseSpeed * CombatVfxSpeed;

    public static SpeedModSettings Defaults { get; } = new SpeedModSettings
    {
        Enabled = true,
        BaseSpeed = 3.0,
        CombatOnly = true,
    };

    public SpeedModSettings With(PartialSpeedModSettings? partial)
    {
        if (partial is null)
        {
            return this;
        }

        return this with
        {
            Enabled = partial.Enabled ?? Enabled,
            BaseSpeed = partial.BaseSpeed ?? BaseSpeed,
            CombatOnly = partial.CombatOnly ?? CombatOnly,
            SpineSpeed = ResolveLegacyOrNew(partial.SpineSpeed, partial.Groups?.Spine, SpineSpeed),
            QueueSpeed = ResolveLegacyOrNew(partial.QueueSpeed, partial.Groups?.QueueWait, QueueSpeed),
            EffectSpeed = ResolveLegacyOrNew(partial.EffectSpeed, partial.Groups?.EffectDelay, EffectSpeed),
            CombatUiSpeed = ResolveLegacyOrNew(partial.CombatUiSpeed, partial.Groups?.CombatUiDelta, CombatUiSpeed),
            CombatVfxSpeed = ResolveLegacyOrNew(partial.CombatVfxSpeed, partial.Groups?.CombatVfxDelta, CombatVfxSpeed),
        };
    }

    private static double ResolveLegacyOrNew(double? updatedValue, double? legacyValue, double currentValue)
    {
        return updatedValue ?? legacyValue ?? currentValue;
    }
}

public sealed record PartialSpeedModSettings
{
    public bool? Enabled { get; init; }

    public double? BaseSpeed { get; init; }

    public bool? CombatOnly { get; init; }

    public double? SpineSpeed { get; init; }

    public double? QueueSpeed { get; init; }

    public double? EffectSpeed { get; init; }

    public double? CombatUiSpeed { get; init; }

    public double? CombatVfxSpeed { get; init; }

    public LegacySpeedGroupSettings? Groups { get; init; }
}

public sealed record SettingsLoadResult
{
    public WorkspaceConfiguration Configuration { get; init; } = WorkspaceConfiguration.CreateLocalDefault();

    public string ConfigurationSource { get; init; } = "<defaults>";

    public IReadOnlyList<string> AppliedEnvironmentOverrides { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

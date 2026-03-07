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

public sealed record SpeedModSettings
{
    public bool Enabled { get; init; }

    public string? FastModeOverride { get; init; }

    public double AnimationScale { get; init; }

    public double SpineTimeScale { get; init; }

    public double QueueWaitScale { get; init; }

    public double EffectDelayScale { get; init; }

    public bool CombatOnly { get; init; }

    public bool PreserveGameSettings { get; init; }

    public bool VerboseLogging { get; init; }

    public static SpeedModSettings Defaults { get; } = new SpeedModSettings
    {
        Enabled = false,
        FastModeOverride = null,
        AnimationScale = 1.0,
        SpineTimeScale = 1.0,
        QueueWaitScale = 1.0,
        EffectDelayScale = 1.0,
        CombatOnly = true,
        PreserveGameSettings = true,
        VerboseLogging = false,
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
            FastModeOverride = NormalizeFastModeOverride(partial.FastModeOverride, FastModeOverride),
            AnimationScale = partial.AnimationScale ?? AnimationScale,
            SpineTimeScale = partial.SpineTimeScale ?? SpineTimeScale,
            QueueWaitScale = partial.QueueWaitScale ?? QueueWaitScale,
            EffectDelayScale = partial.EffectDelayScale ?? EffectDelayScale,
            CombatOnly = partial.CombatOnly ?? CombatOnly,
            PreserveGameSettings = partial.PreserveGameSettings ?? PreserveGameSettings,
            VerboseLogging = partial.VerboseLogging ?? VerboseLogging,
        };
    }

    public static string? NormalizeFastModeOverride(string? candidate, string? fallback = null)
    {
        if (candidate is null)
        {
            return fallback;
        }

        var normalized = candidate.Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        normalized = normalized.ToLowerInvariant();
        return normalized switch
        {
            "none" => "none",
            "normal" => "normal",
            "fast" => "fast",
            "instant" => "instant",
            _ => fallback,
        };
    }
}

public sealed record PartialSpeedModSettings
{
    public bool? Enabled { get; init; }

    public string? FastModeOverride { get; init; }

    public double? AnimationScale { get; init; }

    public double? SpineTimeScale { get; init; }

    public double? QueueWaitScale { get; init; }

    public double? EffectDelayScale { get; init; }

    public bool? CombatOnly { get; init; }

    public bool? PreserveGameSettings { get; init; }

    public bool? VerboseLogging { get; init; }
}

public sealed record SettingsLoadResult
{
    public WorkspaceConfiguration Configuration { get; init; } = WorkspaceConfiguration.CreateLocalDefault();

    public string ConfigurationSource { get; init; } = "<defaults>";

    public IReadOnlyList<string> AppliedEnvironmentOverrides { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

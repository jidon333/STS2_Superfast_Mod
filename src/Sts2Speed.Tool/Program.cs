using System.Text.Json;
using Sts2Speed.Core.Configuration;
using Sts2Speed.Core.Planning;
using Sts2Speed.ModSkeleton;

var command = args.Length == 0 ? "help" : args[0].ToLowerInvariant();
var options = ParseOptions(args.Skip(1).ToArray());
var workspaceRoot = Directory.GetCurrentDirectory();
var configPath = ResolveConfigPath(options, workspaceRoot);
var loadResult = SettingsLoader.LoadFromFile(configPath, ReadEnvironment());
var configuration = ApplyPathOverrides(loadResult.Configuration, options);

switch (command)
{
    case "show-config":
        PrintJson(new
        {
            loadResult.ConfigurationSource,
            loadResult.AppliedEnvironmentOverrides,
            loadResult.Warnings,
            Configuration = configuration,
        });
        return 0;

    case "dry-run-package":
        {
            var plan = SpeedModEntryPoint.CreateDryRunPackagePlan(
                configuration,
                Path.GetFullPath(configuration.GamePaths.ArtifactsRoot, workspaceRoot));

            PrintJson(plan);
            return 0;
        }

    case "dry-run-snapshot":
        {
            var snapshotRoot = options.TryGetValue("--snapshot-root", out var overrideRoot)
                ? Path.GetFullPath(overrideRoot, workspaceRoot)
                : Path.GetFullPath(SnapshotPlanner.BuildSnapshotRoot(configuration.GamePaths, DateTimeOffset.Now), workspaceRoot);

            var plan = SnapshotPlanner.CreateDefaultPlan(configuration.GamePaths, snapshotRoot);
            PrintJson(plan);
            return 0;
        }

    case "dry-run-restore":
        {
            var snapshotRoot = options.TryGetValue("--snapshot-root", out var overrideRoot)
                ? Path.GetFullPath(overrideRoot, workspaceRoot)
                : Path.GetFullPath(SnapshotPlanner.BuildSnapshotRoot(configuration.GamePaths, DateTimeOffset.Now), workspaceRoot);

            var snapshotPlan = SnapshotPlanner.CreateDefaultPlan(configuration.GamePaths, snapshotRoot);
            var restorePlan = SnapshotPlanner.CreateRestorePlan(snapshotPlan);
            PrintJson(restorePlan);
            return 0;
        }

    default:
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- show-config [--config path]");
        Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- dry-run-package [--config path] [--artifacts-root path]");
        Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- dry-run-snapshot [--config path] [--snapshot-root path]");
        Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- dry-run-restore [--config path] [--snapshot-root path]");
        return 0;
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < args.Length; index += 1)
    {
        var current = args[index];
        if (!current.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            options[current] = args[index + 1];
            index += 1;
            continue;
        }

        options[current] = "true";
    }

    return options;
}

static string? ResolveConfigPath(IReadOnlyDictionary<string, string> options, string workspaceRoot)
{
    if (options.TryGetValue("--config", out var explicitPath))
    {
        return Path.GetFullPath(explicitPath, workspaceRoot);
    }

    var samplePath = Path.Combine(workspaceRoot, "config", "sts2speed.sample.json");
    return File.Exists(samplePath) ? samplePath : null;
}

static IReadOnlyDictionary<string, string?> ReadEnvironment()
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

static WorkspaceConfiguration ApplyPathOverrides(WorkspaceConfiguration configuration, IReadOnlyDictionary<string, string> options)
{
    var gamePaths = configuration.GamePaths.With(new PartialGamePathOptions
    {
        GameDirectory = options.TryGetValue("--game-dir", out var gameDirectory) ? gameDirectory : null,
        UserDataRoot = options.TryGetValue("--user-data-root", out var userDataRoot) ? userDataRoot : null,
        SteamAccountId = options.TryGetValue("--steam-account-id", out var steamAccountId) ? steamAccountId : null,
        ProfileIndex = options.TryGetValue("--profile-index", out var profileIndexRaw)
            && int.TryParse(profileIndexRaw, out var profileIndex)
            ? profileIndex
            : null,
        ArtifactsRoot = options.TryGetValue("--artifacts-root", out var artifactsRoot) ? artifactsRoot : null,
    });

    return configuration with
    {
        GamePaths = gamePaths,
    };
}

static void PrintJson<T>(T value)
{
    var serializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    Console.WriteLine(JsonSerializer.Serialize(value, serializerOptions));
}

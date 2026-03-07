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

try
{
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
                var outputRoot = ResolveArtifactsRoot(configuration, workspaceRoot);
                var plan = SpeedModEntryPoint.CreateDryRunPackagePlan(configuration, outputRoot);
                PrintJson(plan);
                return 0;
            }

        case "materialize-package":
            {
                var outputRoot = ResolveArtifactsRoot(configuration, workspaceRoot);
                var runtimeAssemblyRoot = ResolveRuntimeAssemblyRoot(options, workspaceRoot);
                var result = SpeedModEntryPoint.MaterializePackage(configuration, outputRoot, runtimeAssemblyRoot);
                PrintJson(result);
                return 0;
            }

        case "materialize-gumm-game-entry":
            {
                var outputRoot = ResolveArtifactsRoot(configuration, workspaceRoot);
                var result = GummIntegration.MaterializeGameDescriptor(outputRoot);
                PrintJson(result);
                return 0;
            }

        case "deploy-package":
            {
                var modRoot = RequireAbsolutePath(options, "--mod-root", workspaceRoot);
                var outputRoot = ResolveArtifactsRoot(configuration, workspaceRoot);
                var runtimeAssemblyRoot = ResolveRuntimeAssemblyRoot(options, workspaceRoot);
                var package = SpeedModEntryPoint.MaterializePackage(configuration, outputRoot, runtimeAssemblyRoot);
                var result = SpeedModEntryPoint.DeployPackage(package, modRoot);
                PrintJson(result);
                return 0;
            }

        case "install-gumm-loader":
            {
                var packageRoot = ResolvePackageRoot(options, configuration, workspaceRoot);
                var gummRepositoryRoot = ResolveGummRepositoryRoot(options, workspaceRoot);
                var result = GummIntegration.InstallLoader(configuration.GamePaths, packageRoot, gummRepositoryRoot);
                PrintJson(result);
                return 0;
            }

        case "discover-mod-path":
            {
                var result = ModPathDiscovery.Discover(configuration.GamePaths);
                PrintJson(result);
                return result.RecommendedPath is null ? 1 : 0;
            }

        case "dry-run-snapshot":
            {
                var snapshotRoot = ResolveSnapshotRoot(options, configuration, workspaceRoot);
                var plan = SnapshotPlanner.CreateDefaultPlan(configuration.GamePaths, snapshotRoot);
                PrintJson(plan);
                return 0;
            }

        case "snapshot":
            {
                var snapshotRoot = ResolveSnapshotRoot(options, configuration, workspaceRoot);
                var plan = SnapshotPlanner.CreateDefaultPlan(configuration.GamePaths, snapshotRoot);
                var result = SnapshotExecutor.ExecuteSnapshot(plan);
                PrintJson(result);
                return 0;
            }

        case "dry-run-restore":
            {
                var snapshotRoot = ResolveSnapshotRoot(options, configuration, workspaceRoot);
                var plan = ResolveRestorePlan(snapshotRoot, configuration.GamePaths);
                PrintJson(plan);
                return 0;
            }

        case "restore":
            {
                var snapshotRoot = ResolveSnapshotRoot(options, configuration, workspaceRoot);
                var plan = ResolveRestorePlan(snapshotRoot, configuration.GamePaths);
                var result = SnapshotExecutor.ExecuteRestore(plan);
                PrintJson(result);
                return 0;
            }

        case "verify-snapshot":
            {
                var snapshotRoot = ResolveSnapshotRoot(options, configuration, workspaceRoot);
                var snapshot = SnapshotExecutor.LoadSnapshotExecutionResult(snapshotRoot);
                var result = SnapshotExecutor.VerifySnapshot(snapshot);
                PrintJson(result);
                return result.AllEntriesMatch ? 0 : 1;
            }

        default:
            WriteUsage();
            return 0;
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
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

static string ResolveArtifactsRoot(WorkspaceConfiguration configuration, string workspaceRoot)
{
    return Path.GetFullPath(configuration.GamePaths.ArtifactsRoot, workspaceRoot);
}

static string ResolveRuntimeAssemblyRoot(IReadOnlyDictionary<string, string> options, string workspaceRoot)
{
    if (options.TryGetValue("--runtime-assembly-root", out var explicitRoot))
    {
        return Path.GetFullPath(explicitRoot, workspaceRoot);
    }

    return AppContext.BaseDirectory;
}

static string ResolvePackageRoot(
    IReadOnlyDictionary<string, string> options,
    WorkspaceConfiguration configuration,
    string workspaceRoot)
{
    if (options.TryGetValue("--package-root", out var explicitRoot))
    {
        return Path.GetFullPath(explicitRoot, workspaceRoot);
    }

    return Path.Combine(ResolveArtifactsRoot(configuration, workspaceRoot), "package-layout", "Sts2Speed");
}

static string ResolveGummRepositoryRoot(IReadOnlyDictionary<string, string> options, string workspaceRoot)
{
    if (options.TryGetValue("--gumm-repo-root", out var explicitRoot))
    {
        return Path.GetFullPath(explicitRoot, workspaceRoot);
    }

    return Path.GetFullPath(Path.Combine(workspaceRoot, "artifacts", "tools", "Godot-Universal-Mod-Manager"));
}

static string ResolveSnapshotRoot(
    IReadOnlyDictionary<string, string> options,
    WorkspaceConfiguration configuration,
    string workspaceRoot)
{
    if (options.TryGetValue("--snapshot-root", out var explicitRoot))
    {
        return Path.GetFullPath(explicitRoot, workspaceRoot);
    }

    return Path.GetFullPath(
        SnapshotPlanner.BuildSnapshotRoot(configuration.GamePaths, DateTimeOffset.Now),
        workspaceRoot);
}

static RestorePlan ResolveRestorePlan(string snapshotRoot, GamePathOptions gamePaths)
{
    var reportPath = SnapshotPlanner.BuildSnapshotReportPath(snapshotRoot);
    if (File.Exists(reportPath))
    {
        var snapshot = SnapshotExecutor.LoadSnapshotExecutionResult(snapshotRoot);
        return SnapshotPlanner.CreateRestorePlan(snapshot);
    }

    var plan = SnapshotPlanner.CreateDefaultPlan(gamePaths, snapshotRoot);
    return SnapshotPlanner.CreateRestorePlan(plan);
}

static string RequireAbsolutePath(IReadOnlyDictionary<string, string> options, string optionName, string workspaceRoot)
{
    if (!options.TryGetValue(optionName, out var value) || string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Missing required option: {optionName}");
    }

    return Path.GetFullPath(value, workspaceRoot);
}

static void WriteUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- show-config [--config path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- dry-run-package [--config path] [--artifacts-root path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- materialize-package [--config path] [--artifacts-root path] [--runtime-assembly-root path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- materialize-gumm-game-entry [--config path] [--artifacts-root path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- deploy-package --mod-root path [--config path] [--artifacts-root path] [--runtime-assembly-root path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- install-gumm-loader [--config path] [--package-root path] [--gumm-repo-root path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- discover-mod-path [--config path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- dry-run-snapshot [--config path] [--snapshot-root path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- snapshot [--config path] [--snapshot-root path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- dry-run-restore [--config path] [--snapshot-root path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- restore [--config path] [--snapshot-root path]");
    Console.WriteLine("  dotnet run --project src/Sts2Speed.Tool -- verify-snapshot [--snapshot-root path]");
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

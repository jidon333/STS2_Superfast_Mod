using Sts2Speed.Core.Configuration;
using Sts2Speed.Core.Planning;
using Sts2Speed.ModSkeleton;

var failures = new List<string>();

Run("environment overrides win over config", TestEnvironmentOverrides, failures);
Run("missing fast mode env override is ignored", TestMissingFastModeOverride, failures);
Run("preserveGameSettings blocks live mutation", TestMutationPolicy, failures);
Run("snapshot planner includes required files", TestSnapshotPlanner, failures);
Run("restore plan mirrors snapshot entries", TestRestorePlan, failures);
Run("manifest contains expected metadata", TestManifestTemplate, failures);

if (failures.Count == 0)
{
    Console.WriteLine("All self-tests passed.");
    return 0;
}

Console.Error.WriteLine($"{failures.Count} self-test(s) failed.");
foreach (var failure in failures)
{
    Console.Error.WriteLine($"  - {failure}");
}

return 1;

static void Run(string name, Action test, ICollection<string> failures)
{
    try
    {
        test();
        Console.WriteLine($"[PASS] {name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{name}: {exception.Message}");
        Console.WriteLine($"[FAIL] {name}");
    }
}

static void TestEnvironmentOverrides()
{
    const string json = """
    {
      "settings": {
        "enabled": false,
        "animationScale": 1.0,
        "spineTimeScale": 1.0,
        "queueWaitScale": 1.0,
        "effectDelayScale": 1.0,
        "preserveGameSettings": true
      }
    }
    """;

    var environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
    {
        [EnvironmentOverrideNames.Enabled] = "true",
        [EnvironmentOverrideNames.AnimationScale] = "1.75",
        [EnvironmentOverrideNames.FastModeOverride] = "instant",
    };

    var result = SettingsLoader.LoadFromJson(json, "inline", environment);

    Assert(result.Configuration.Settings.Enabled, "Expected env override to enable the mod.");
    Assert(Math.Abs(result.Configuration.Settings.AnimationScale - 1.75) < 0.0001, "Expected animationScale env override.");
    Assert(result.Configuration.Settings.FastModeOverride == "instant", "Expected fast mode override to normalize to instant.");
}

static void TestMutationPolicy()
{
    var settings = SpeedModSettings.Defaults.With(new PartialSpeedModSettings
    {
        Enabled = true,
        FastModeOverride = "instant",
        PreserveGameSettings = true,
    });

    Assert(!MutationPolicy.ShouldMutateLiveGameSettings(settings), "preserveGameSettings=true must block live mutation.");
}

static void TestMissingFastModeOverride()
{
    var environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
    {
        [EnvironmentOverrideNames.FastModeOverride] = null,
    };

    var result = SettingsLoader.LoadFromJson(null, "inline", environment);

    Assert(result.AppliedEnvironmentOverrides.Count == 0, "Null fast mode environment values should not be reported as applied.");
}

static void TestSnapshotPlanner()
{
    var options = new GamePathOptions
    {
        GameDirectory = @"D:\Fake\Slay the Spire 2",
        UserDataRoot = @"C:\Users\Test\AppData\Roaming\SlayTheSpire2",
        SteamAccountId = "1234567890",
        ProfileIndex = 1,
        ArtifactsRoot = "artifacts",
    };

    var probe = new FakeProbe(
        Path.Combine(options.GameDirectory, "release_info.json"),
        Path.Combine(options.UserDataRoot, "steam", options.SteamAccountId, "settings.save"));

    var plan = SnapshotPlanner.CreateDefaultPlan(options, @"C:\workspace\artifacts\snapshots\test", probe);

    Assert(plan.Entries.Count == 9, $"Expected 9 snapshot entries but received {plan.Entries.Count}.");
    Assert(plan.Entries.Any(entry => entry.BackupPath.EndsWith(Path.Combine("game", "release_info.json"), StringComparison.OrdinalIgnoreCase)), "Missing release_info snapshot entry.");
    Assert(plan.Entries.Count(entry => entry.Exists) == 2, "Fake probe should mark exactly two files as existing.");
}

static void TestRestorePlan()
{
    var options = GamePathOptions.CreateLocalDefault();
    var plan = SnapshotPlanner.CreateDefaultPlan(options, @"C:\workspace\artifacts\snapshots\test", new FakeProbe());
    var restore = SnapshotPlanner.CreateRestorePlan(plan, new FakeProbe(plan.Entries[0].BackupPath));

    Assert(restore.Entries.Count == plan.Entries.Count, "Restore plan should mirror snapshot entry count.");
    Assert(restore.Entries[0].DestinationPath == plan.Entries[0].SourcePath, "Restore destination should map back to the original source path.");
    Assert(restore.Entries[0].BackupExists, "Fake probe should mark the first backup path as existing.");
}

static void TestManifestTemplate()
{
    var descriptor = SpeedModEntryPoint.CreateDescriptor();
    var manifest = SpeedModEntryPoint.CreateManifestJson(descriptor);

    Assert(manifest.Contains("\"pckName\": \"sts2-speed-skeleton.pck\"", StringComparison.Ordinal), "Expected manifest to include pckName.");
    Assert(manifest.Contains("\"name\": \"STS2 Speed Skeleton\"", StringComparison.Ordinal), "Expected manifest to include the default name.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class FakeProbe : IFileStateProbe
{
    private readonly HashSet<string> existingPaths;

    public FakeProbe(params string[] existingPaths)
    {
        this.existingPaths = new HashSet<string>(existingPaths, StringComparer.OrdinalIgnoreCase);
    }

    public bool FileExists(string path)
    {
        return existingPaths.Contains(path);
    }
}

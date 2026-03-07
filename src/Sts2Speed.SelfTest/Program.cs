using Sts2Speed.Core.Configuration;
using Sts2Speed.Core.Planning;
using Sts2Speed.ModSkeleton;

var failures = new List<string>();

Run("environment overrides win over config", TestEnvironmentOverrides, failures);
Run("runtime settings infer enablement from shared speed file", TestRuntimeSettingsSharedMultiplier, failures);
Run("speed multiplier semantics shrink wait durations", TestSpeedMultiplierSemantics, failures);
Run("snapshot planner includes required files", TestSnapshotPlanner, failures);
Run("snapshot execution copies and verifies files", TestSnapshotExecutionAndVerification, failures);
Run("strict restore removes files created after snapshot", TestStrictRestoreRemovesCreatedFiles, failures);
Run("modded profile sync mirrors vanilla profile after backing up destination", TestModdedProfileSync, failures);
Run("restore plan mirrors snapshot entries", TestRestorePlan, failures);
Run("native package staging captures missing pck requirement", TestMaterializeNativePackage, failures);
Run("native package writes the configured shared multiplier", TestNativePackageSharedMultiplier, failures);

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
        "spineTimeScale": 2.0,
        "queueWaitScale": 2.0,
        "effectDelayScale": 2.0,
        "combatOnly": true
      }
    }
    """;

    var environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
    {
        [EnvironmentOverrideNames.Enabled] = "true",
        [EnvironmentOverrideNames.SpineTimeScale] = "1.75",
        [EnvironmentOverrideNames.EffectDelayScale] = "1.5",
    };

    var result = SettingsLoader.LoadFromJson(json, "inline", environment);

    Assert(result.Configuration.Settings.Enabled, "Expected env override to enable the mod.");
    Assert(Math.Abs(result.Configuration.Settings.SpineTimeScale - 1.75) < 0.0001, "Expected spineTimeScale env override.");
    Assert(Math.Abs(result.Configuration.Settings.QueueWaitScale - 2.0) < 0.0001, "Expected queueWaitScale to remain at the config value.");
    Assert(Math.Abs(result.Configuration.Settings.EffectDelayScale - 1.5) < 0.0001, "Expected effectDelayScale env override.");
    Assert(result.AppliedEnvironmentOverrides.Contains(EnvironmentOverrideNames.SpineTimeScale), "Expected spineTimeScale to be reported as an applied override.");
}

static void TestRuntimeSettingsSharedMultiplier()
{
    var root = CreateTempDirectory();
    try
    {
        var modRoot = Path.Combine(root, "mods", "Sts2Speed");
        Directory.CreateDirectory(modRoot);
        File.WriteAllText(Path.Combine(modRoot, "Sts2Speed.speed.txt"), "2.0");

        var settings = RuntimeSettingsLoader.Load(modRoot);

        Assert(settings.Enabled, "Shared multiplier should implicitly enable the runtime payload when no explicit enabled override exists.");
        Assert(Math.Abs(settings.SpineTimeScale - 2.0) < 0.0001, "Shared multiplier should set spineTimeScale.");
        Assert(Math.Abs(settings.QueueWaitScale - 2.0) < 0.0001, "Shared multiplier should set queueWaitScale.");
        Assert(Math.Abs(settings.EffectDelayScale - 2.0) < 0.0001, "Shared multiplier should set effectDelayScale.");
    }
    finally
    {
        SafeDeleteDirectory(root);
    }
}

static void TestSpeedMultiplierSemantics()
{
    Assert(Math.Abs(SpeedScaleMath.ApplyAnimationSpeedMultiplier(1.0f, 2.0) - 2.0f) < 0.0001, "Animation multipliers should scale up directly.");
    Assert(Math.Abs(SpeedScaleMath.ApplyDurationSpeedMultiplier(1.0f, 2.0) - 0.5f) < 0.0001, "A 2.0 speed multiplier should halve float wait durations.");
    Assert(Math.Abs(SpeedScaleMath.ApplyDurationSpeedMultiplier(1.0, 0.5) - 2.0) < 0.0001, "A 0.5 speed multiplier should double double wait durations.");
    Assert(Math.Abs(SpeedScaleMath.ApplyDurationSpeedMultiplier(1.0, 0.0) - 1.0) < 0.0001, "Invalid zero multipliers should leave durations unchanged.");
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

    Assert(plan.Entries.Count == 11, $"Expected 11 snapshot entries but received {plan.Entries.Count}.");
    Assert(plan.Entries.Any(entry => entry.BackupPath.EndsWith(Path.Combine("game", "release_info.json"), StringComparison.OrdinalIgnoreCase)), "Missing release_info snapshot entry.");
    Assert(plan.Entries.Any(entry => entry.BackupPath.EndsWith(Path.Combine("game", "override.cfg"), StringComparison.OrdinalIgnoreCase)), "Missing override.cfg snapshot entry.");
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

static void TestSnapshotExecutionAndVerification()
{
    var root = CreateTempDirectory();
    try
    {
        var gameDirectory = Path.Combine(root, "game");
        var userDataRoot = Path.Combine(root, "userdata");
        var steamDirectory = Path.Combine(userDataRoot, "steam", "1234567890");
        var saveDirectory = Path.Combine(steamDirectory, "profile1", "saves");
        Directory.CreateDirectory(gameDirectory);
        Directory.CreateDirectory(saveDirectory);

        var releaseInfoPath = Path.Combine(gameDirectory, "release_info.json");
        var settingsPath = Path.Combine(steamDirectory, "settings.save");
        var prefsPath = Path.Combine(saveDirectory, "prefs.save");
        File.WriteAllText(releaseInfoPath, """{"version":"test"}""");
        File.WriteAllText(settingsPath, """{"screenshake":"true"}""");
        File.WriteAllText(prefsPath, """{"fast_mode":"fast"}""");

        var options = new GamePathOptions
        {
            GameDirectory = gameDirectory,
            UserDataRoot = userDataRoot,
            SteamAccountId = "1234567890",
            ProfileIndex = 1,
            ArtifactsRoot = Path.Combine(root, "artifacts"),
        };

        var snapshotRoot = SnapshotPlanner.BuildSnapshotRoot(options, new DateTimeOffset(2026, 3, 7, 10, 0, 0, TimeSpan.Zero));
        var plan = SnapshotPlanner.CreateDefaultPlan(options, snapshotRoot);
        var snapshot = SnapshotExecutor.ExecuteSnapshot(plan);

        Assert(snapshot.Entries.Count(entry => entry.Status == "copied") == 3, "Expected three copied files in the snapshot result.");
        Assert(File.Exists(Path.Combine(snapshotRoot, "game", "release_info.json")), "Snapshot should contain release_info.json.");

        var verification = SnapshotExecutor.VerifySnapshot(snapshot);
        Assert(verification.AllEntriesMatch, "Unchanged files should verify successfully.");

        File.WriteAllText(settingsPath, """{"screenshake":"false"}""");
        var changedVerification = SnapshotExecutor.VerifySnapshot(snapshot);
        Assert(!changedVerification.AllEntriesMatch, "Changing a source file should fail verification.");
        Assert(changedVerification.Entries.Any(entry => entry.SourcePath == settingsPath && entry.Status == "changed"), "Expected settings.save to be marked as changed.");

        var restorePlan = SnapshotPlanner.CreateRestorePlan(snapshot);
        SnapshotExecutor.ExecuteRestore(restorePlan);
        Assert(File.ReadAllText(settingsPath).Contains("true", StringComparison.Ordinal), "Restore should put the original settings file back.");
    }
    finally
    {
        SafeDeleteDirectory(root);
    }
}

static void TestStrictRestoreRemovesCreatedFiles()
{
    var root = CreateTempDirectory();
    try
    {
        var snapshotRoot = Path.Combine(root, "snapshot");
        var createdFilePath = Path.Combine(root, "game", "override.cfg");
        Directory.CreateDirectory(Path.GetDirectoryName(createdFilePath)!);
        File.WriteAllText(createdFilePath, "created later");

        var snapshot = new SnapshotExecutionResult(
            snapshotRoot,
            Path.Combine(snapshotRoot, "snapshot-report.json"),
            DateTimeOffset.UtcNow,
            new[]
            {
                new SnapshotExecutionEntry(
                    "GameInstall",
                    createdFilePath,
                    Path.Combine(snapshotRoot, "game", "override.cfg"),
                    false,
                    "missing",
                    null,
                    null),
            },
            Array.Empty<string>());

        var restore = SnapshotExecutor.ExecuteRestoreToSnapshotState(snapshot);

        Assert(!File.Exists(createdFilePath), "Strict restore should delete files that did not exist at snapshot time.");
        Assert(restore.Entries.Any(entry => entry.Status == "deleted-created-after-snapshot"), "Strict restore should report deletion of created files.");
    }
    finally
    {
        SafeDeleteDirectory(root);
    }
}

static void TestModdedProfileSync()
{
    var root = CreateTempDirectory();
    try
    {
        var userDataRoot = Path.Combine(root, "userdata");
        var steamRoot = Path.Combine(userDataRoot, "steam", "1234567890");
        var sourceRoot = Path.Combine(steamRoot, "profile1");
        var destinationRoot = Path.Combine(steamRoot, "modded", "profile1");
        var sourceSaves = Path.Combine(sourceRoot, "saves");
        var destinationSaves = Path.Combine(destinationRoot, "saves");
        Directory.CreateDirectory(sourceSaves);
        Directory.CreateDirectory(destinationSaves);

        File.WriteAllText(Path.Combine(sourceSaves, "progress.save"), "vanilla-progress");
        File.WriteAllText(Path.Combine(sourceSaves, "prefs.save"), "vanilla-prefs");
        File.WriteAllText(Path.Combine(destinationSaves, "progress.save"), "old-modded-progress");

        var options = new GamePathOptions
        {
            GameDirectory = Path.Combine(root, "game"),
            UserDataRoot = userDataRoot,
            SteamAccountId = "1234567890",
            ProfileIndex = 1,
            ArtifactsRoot = Path.Combine(root, "artifacts"),
        };

        var result = ModdedProfileSync.SyncVanillaToModded(options, options.ArtifactsRoot, new DateTimeOffset(2026, 3, 7, 12, 0, 0, TimeSpan.Zero));

        Assert(File.Exists(Path.Combine(result.BackupRoot, "modded-profile-before-sync", "saves", "progress.save")), "Sync should back up the previous modded progress.");
        Assert(File.ReadAllText(Path.Combine(destinationSaves, "progress.save")) == "vanilla-progress", "Sync should replace modded progress with vanilla progress.");
        Assert(File.ReadAllText(Path.Combine(destinationSaves, "prefs.save")) == "vanilla-prefs", "Sync should copy additional vanilla files.");
        Assert(result.Files.Count == 2, $"Expected two copied files but received {result.Files.Count}.");
    }
    finally
    {
        SafeDeleteDirectory(root);
    }
}

static void TestMaterializeNativePackage()
{
    var root = CreateTempDirectory();
    try
    {
        var configuration = WorkspaceConfiguration.CreateLocalDefault() with
        {
            GamePaths = WorkspaceConfiguration.CreateLocalDefault().GamePaths with
            {
                GameDirectory = Path.Combine(root, "game"),
                ArtifactsRoot = Path.Combine(root, "artifacts"),
            },
        };

        var result = SpeedModEntryPoint.MaterializeNativePackage(configuration, configuration.GamePaths.ArtifactsRoot, AppContext.BaseDirectory, "subdir");

        Assert(File.Exists(Path.Combine(result.PackageRoot, "Sts2Speed.speed.txt")), "Native staging package should include a text multiplier file.");
        Assert(File.Exists(Path.Combine(result.PackageRoot, "sts2-speed-skeleton.dll")), "Native staging package should include the pck-matching managed payload.");
        Assert(!File.Exists(Path.Combine(result.PackageRoot, "mod_manifest.json")), "Native staging package should keep mod_manifest.json inside the generated pck rather than as a loose file.");
        Assert(!File.Exists(Path.Combine(result.PackageRoot, "README.native.txt")), "Native staging package should not ship documentation files into the live mods directory.");
        Assert(result.MissingArtifacts.Any(artifact => artifact.RelativePath.EndsWith(".pck", StringComparison.OrdinalIgnoreCase)), "Native staging package should report that a .pck artifact is still missing.");
        Assert(result.LayoutKind == "subdir", "Native staging package should normalize the requested layout kind.");
    }
    finally
    {
        SafeDeleteDirectory(root);
    }
}

static void TestNativePackageSharedMultiplier()
{
    var root = CreateTempDirectory();
    try
    {
        var configuration = WorkspaceConfiguration.CreateLocalDefault() with
        {
            GamePaths = WorkspaceConfiguration.CreateLocalDefault().GamePaths with
            {
                GameDirectory = Path.Combine(root, "game"),
                ArtifactsRoot = Path.Combine(root, "artifacts"),
            },
            Settings = SpeedModSettings.Defaults with
            {
                SpineTimeScale = 3.0,
                QueueWaitScale = 3.0,
                EffectDelayScale = 3.0,
            },
        };

        var result = SpeedModEntryPoint.MaterializeNativePackage(configuration, configuration.GamePaths.ArtifactsRoot, AppContext.BaseDirectory, "flat");
        var sharedSpeed = File.ReadAllText(Path.Combine(result.PackageRoot, "Sts2Speed.speed.txt")).Trim();

        Assert(sharedSpeed == "3", $"Expected packaged shared multiplier to be 3 but received '{sharedSpeed}'.");
    }
    finally
    {
        SafeDeleteDirectory(root);
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static string CreateTempDirectory()
{
    var path = Path.Combine(Path.GetTempPath(), "Sts2SpeedTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    return path;
}

static void SafeDeleteDirectory(string path)
{
    if (!Directory.Exists(path))
    {
        return;
    }

    Directory.Delete(path, recursive: true);
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

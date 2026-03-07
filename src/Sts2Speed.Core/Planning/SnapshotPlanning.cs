using Sts2Speed.Core.Configuration;

namespace Sts2Speed.Core.Planning;

public interface IFileStateProbe
{
    bool FileExists(string path);
}

public sealed class PhysicalFileStateProbe : IFileStateProbe
{
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }
}

public sealed record SnapshotEntry(
    string Category,
    string SourcePath,
    string BackupPath,
    bool Exists);

public sealed record SnapshotPlan(
    string SnapshotRoot,
    IReadOnlyList<SnapshotEntry> Entries,
    IReadOnlyList<string> FailureCriteria);

public sealed record RestoreEntry(
    string BackupPath,
    string DestinationPath,
    bool BackupExists);

public sealed record RestorePlan(
    string SnapshotRoot,
    IReadOnlyList<RestoreEntry> Entries);

public static class SnapshotPlanner
{
    private static readonly IReadOnlyList<string> DefaultFailureCriteria = new List<string>
    {
        "Boot failure",
        "Infinite loading",
        "Combat or reward screen soft lock",
        "Save schema corruption",
        "Save write failure",
    };

    public static SnapshotPlan CreateDefaultPlan(
        GamePathOptions options,
        string snapshotRoot,
        IFileStateProbe? probe = null)
    {
        probe ??= new PhysicalFileStateProbe();

        var entries = BuildEntries(options)
            .Select(spec => new SnapshotEntry(
                spec.Category,
                spec.SourcePath,
                Path.Combine(snapshotRoot, spec.RelativeBackupPath),
                probe.FileExists(spec.SourcePath)))
            .ToList();

        return new SnapshotPlan(snapshotRoot, entries, DefaultFailureCriteria);
    }

    public static RestorePlan CreateRestorePlan(SnapshotPlan snapshotPlan, IFileStateProbe? probe = null)
    {
        probe ??= new PhysicalFileStateProbe();
        var entries = snapshotPlan.Entries
            .Select(entry => new RestoreEntry(
                entry.BackupPath,
                entry.SourcePath,
                probe.FileExists(entry.BackupPath)))
            .ToList();

        return new RestorePlan(snapshotPlan.SnapshotRoot, entries);
    }

    public static RestorePlan CreateRestorePlan(SnapshotExecutionResult snapshotExecution, IFileStateProbe? probe = null)
    {
        probe ??= new PhysicalFileStateProbe();
        var entries = snapshotExecution.Entries
            .Select(entry => new RestoreEntry(
                entry.BackupPath,
                entry.SourcePath,
                probe.FileExists(entry.BackupPath)))
            .ToList();

        return new RestorePlan(snapshotExecution.SnapshotRoot, entries);
    }

    public static string BuildSnapshotRoot(GamePathOptions options, DateTimeOffset now)
    {
        var stamp = now.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(options.ArtifactsRoot, "snapshots", stamp);
    }

    public static string BuildSnapshotReportPath(string snapshotRoot)
    {
        return Path.Combine(snapshotRoot, "snapshot-report.json");
    }

    public static string BuildRestoreReportPath(string snapshotRoot)
    {
        return Path.Combine(snapshotRoot, "restore-report.json");
    }

    private static IEnumerable<(string Category, string SourcePath, string RelativeBackupPath)> BuildEntries(GamePathOptions options)
    {
        var profileDirectory = Path.Combine(
            options.UserDataRoot,
            "steam",
            options.SteamAccountId,
            $"profile{options.ProfileIndex}",
            "saves");

        yield return (
            "GameInstall",
            Path.Combine(options.GameDirectory, "release_info.json"),
            Path.Combine("game", "release_info.json"));
        yield return (
            "UserSettings",
            Path.Combine(options.UserDataRoot, "steam", options.SteamAccountId, "settings.save"),
            Path.Combine("user", "steam", options.SteamAccountId, "settings.save"));
        yield return (
            "UserSettings",
            Path.Combine(options.UserDataRoot, "steam", options.SteamAccountId, "settings.save.backup"),
            Path.Combine("user", "steam", options.SteamAccountId, "settings.save.backup"));
        yield return (
            "ProfileSave",
            Path.Combine(profileDirectory, "prefs.save"),
            Path.Combine("user", "steam", options.SteamAccountId, $"profile{options.ProfileIndex}", "saves", "prefs.save"));
        yield return (
            "ProfileSave",
            Path.Combine(profileDirectory, "prefs.save.backup"),
            Path.Combine("user", "steam", options.SteamAccountId, $"profile{options.ProfileIndex}", "saves", "prefs.save.backup"));
        yield return (
            "ProfileSave",
            Path.Combine(profileDirectory, "progress.save"),
            Path.Combine("user", "steam", options.SteamAccountId, $"profile{options.ProfileIndex}", "saves", "progress.save"));
        yield return (
            "ProfileSave",
            Path.Combine(profileDirectory, "progress.save.backup"),
            Path.Combine("user", "steam", options.SteamAccountId, $"profile{options.ProfileIndex}", "saves", "progress.save.backup"));
        yield return (
            "RunSave",
            Path.Combine(profileDirectory, "current_run.save"),
            Path.Combine("user", "steam", options.SteamAccountId, $"profile{options.ProfileIndex}", "saves", "current_run.save"));
        yield return (
            "RunSave",
            Path.Combine(profileDirectory, "current_run.save.backup"),
            Path.Combine("user", "steam", options.SteamAccountId, $"profile{options.ProfileIndex}", "saves", "current_run.save.backup"));
    }
}

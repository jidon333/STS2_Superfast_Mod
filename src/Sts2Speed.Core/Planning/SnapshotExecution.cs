using System.Security.Cryptography;
using System.Text.Json;

namespace Sts2Speed.Core.Planning;

public sealed record SnapshotExecutionEntry(
    string Category,
    string SourcePath,
    string BackupPath,
    bool SourceExistedAtSnapshot,
    string Status,
    long? Size,
    string? Sha256);

public sealed record SnapshotExecutionResult(
    string SnapshotRoot,
    string ReportPath,
    DateTimeOffset CapturedAt,
    IReadOnlyList<SnapshotExecutionEntry> Entries,
    IReadOnlyList<string> FailureCriteria);

public sealed record RestoreExecutionEntry(
    string BackupPath,
    string DestinationPath,
    bool BackupExists,
    string Status,
    long? Size,
    string? Sha256);

public sealed record RestoreExecutionResult(
    string SnapshotRoot,
    string ReportPath,
    DateTimeOffset RestoredAt,
    IReadOnlyList<RestoreExecutionEntry> Entries);

public sealed record SnapshotVerificationEntry(
    string SourcePath,
    string BackupPath,
    bool SourceExistedAtSnapshot,
    bool SourceExistsNow,
    string Status,
    string? SnapshotSha256,
    string? CurrentSha256);

public sealed record SnapshotVerificationResult(
    string SnapshotRoot,
    bool AllEntriesMatch,
    IReadOnlyList<SnapshotVerificationEntry> Entries);

public static class SnapshotExecutor
{
    public static SnapshotExecutionResult ExecuteSnapshot(SnapshotPlan plan, string? reportPath = null)
    {
        Directory.CreateDirectory(plan.SnapshotRoot);

        var entries = new List<SnapshotExecutionEntry>();
        foreach (var entry in plan.Entries)
        {
            if (!entry.Exists)
            {
                entries.Add(new SnapshotExecutionEntry(
                    entry.Category,
                    entry.SourcePath,
                    entry.BackupPath,
                    false,
                    "missing",
                    null,
                    null));
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(entry.BackupPath)!);
            File.Copy(entry.SourcePath, entry.BackupPath, overwrite: true);

            var info = new FileInfo(entry.BackupPath);
            entries.Add(new SnapshotExecutionEntry(
                entry.Category,
                entry.SourcePath,
                entry.BackupPath,
                true,
                "copied",
                info.Length,
                ComputeSha256(entry.BackupPath)));
        }

        var finalReportPath = reportPath ?? SnapshotPlanner.BuildSnapshotReportPath(plan.SnapshotRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(finalReportPath)!);

        var result = new SnapshotExecutionResult(
            plan.SnapshotRoot,
            finalReportPath,
            DateTimeOffset.UtcNow,
            entries,
            plan.FailureCriteria);

        WriteJson(finalReportPath, result);
        return result;
    }

    public static RestoreExecutionResult ExecuteRestore(RestorePlan plan, string? reportPath = null)
    {
        Directory.CreateDirectory(plan.SnapshotRoot);

        var entries = new List<RestoreExecutionEntry>();
        foreach (var entry in plan.Entries)
        {
            if (!entry.BackupExists)
            {
                entries.Add(new RestoreExecutionEntry(
                    entry.BackupPath,
                    entry.DestinationPath,
                    false,
                    "missing-backup",
                    null,
                    null));
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(entry.DestinationPath)!);
            File.Copy(entry.BackupPath, entry.DestinationPath, overwrite: true);

            var info = new FileInfo(entry.DestinationPath);
            entries.Add(new RestoreExecutionEntry(
                entry.BackupPath,
                entry.DestinationPath,
                true,
                "restored",
                info.Length,
                ComputeSha256(entry.DestinationPath)));
        }

        var finalReportPath = reportPath ?? SnapshotPlanner.BuildRestoreReportPath(plan.SnapshotRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(finalReportPath)!);

        var result = new RestoreExecutionResult(
            plan.SnapshotRoot,
            finalReportPath,
            DateTimeOffset.UtcNow,
            entries);

        WriteJson(finalReportPath, result);
        return result;
    }

    public static RestoreExecutionResult ExecuteRestoreToSnapshotState(SnapshotExecutionResult snapshot, string? reportPath = null)
    {
        Directory.CreateDirectory(snapshot.SnapshotRoot);

        var entries = new List<RestoreExecutionEntry>();
        foreach (var entry in snapshot.Entries)
        {
            if (entry.SourceExistedAtSnapshot)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(entry.SourcePath)!);
                File.Copy(entry.BackupPath, entry.SourcePath, overwrite: true);

                var info = new FileInfo(entry.SourcePath);
                entries.Add(new RestoreExecutionEntry(
                    entry.BackupPath,
                    entry.SourcePath,
                    true,
                    "restored",
                    info.Length,
                    ComputeSha256(entry.SourcePath)));
                continue;
            }

            if (File.Exists(entry.SourcePath))
            {
                File.Delete(entry.SourcePath);
                entries.Add(new RestoreExecutionEntry(
                    entry.BackupPath,
                    entry.SourcePath,
                    false,
                    "deleted-created-after-snapshot",
                    null,
                    null));
                continue;
            }

            entries.Add(new RestoreExecutionEntry(
                entry.BackupPath,
                entry.SourcePath,
                false,
                "already-missing",
                null,
                null));
        }

        var finalReportPath = reportPath ?? SnapshotPlanner.BuildRestoreReportPath(snapshot.SnapshotRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(finalReportPath)!);

        var result = new RestoreExecutionResult(
            snapshot.SnapshotRoot,
            finalReportPath,
            DateTimeOffset.UtcNow,
            entries);

        WriteJson(finalReportPath, result);
        return result;
    }

    public static SnapshotVerificationResult VerifySnapshot(SnapshotExecutionResult snapshot)
    {
        var entries = new List<SnapshotVerificationEntry>();
        foreach (var entry in snapshot.Entries)
        {
            var sourceExistsNow = File.Exists(entry.SourcePath);
            string status;
            string? currentSha = null;

            if (!entry.SourceExistedAtSnapshot && !sourceExistsNow)
            {
                status = "still-missing";
            }
            else if (!entry.SourceExistedAtSnapshot && sourceExistsNow)
            {
                currentSha = ComputeSha256(entry.SourcePath);
                status = "created-after-snapshot";
            }
            else if (entry.SourceExistedAtSnapshot && !sourceExistsNow)
            {
                status = "missing-current";
            }
            else
            {
                currentSha = ComputeSha256(entry.SourcePath);
                status = string.Equals(entry.Sha256, currentSha, StringComparison.OrdinalIgnoreCase)
                    ? "unchanged"
                    : "changed";
            }

            entries.Add(new SnapshotVerificationEntry(
                entry.SourcePath,
                entry.BackupPath,
                entry.SourceExistedAtSnapshot,
                sourceExistsNow,
                status,
                entry.Sha256,
                currentSha));
        }

        var allEntriesMatch = entries.All(entry =>
            entry.Status is "unchanged" or "still-missing");

        return new SnapshotVerificationResult(snapshot.SnapshotRoot, allEntriesMatch, entries);
    }

    public static SnapshotExecutionResult LoadSnapshotExecutionResult(string snapshotRoot)
    {
        var reportPath = SnapshotPlanner.BuildSnapshotReportPath(snapshotRoot);
        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException("Snapshot report was not found.", reportPath);
        }

        var json = File.ReadAllText(reportPath);
        var result = JsonSerializer.Deserialize<SnapshotExecutionResult>(json, JsonOptions);
        if (result is null)
        {
            throw new InvalidOperationException($"Unable to deserialize snapshot report: {reportPath}");
        }

        return result;
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    private static void WriteJson<T>(string path, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}

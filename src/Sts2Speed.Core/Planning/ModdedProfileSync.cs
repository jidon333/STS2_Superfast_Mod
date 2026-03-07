using System.Security.Cryptography;
using System.Text.Json;
using Sts2Speed.Core.Configuration;

namespace Sts2Speed.Core.Planning;

public sealed record ModdedProfileSyncFile(
    string RelativePath,
    string SourcePath,
    string DestinationPath,
    string Status,
    long Size,
    string Sha256);

public sealed record ModdedProfileSyncResult(
    string SourceRoot,
    string DestinationRoot,
    string BackupRoot,
    string ReportPath,
    IReadOnlyList<ModdedProfileSyncFile> Files,
    IReadOnlyList<string> Warnings);

public static class ModdedProfileSync
{
    public static ModdedProfileSyncResult SyncVanillaToModded(
        GamePathOptions options,
        string artifactsRoot,
        DateTimeOffset? now = null)
    {
        var sourceRoot = Path.Combine(
            options.UserDataRoot,
            "steam",
            options.SteamAccountId,
            $"profile{options.ProfileIndex}");
        var destinationRoot = Path.Combine(
            options.UserDataRoot,
            "steam",
            options.SteamAccountId,
            "modded",
            $"profile{options.ProfileIndex}");

        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Vanilla profile root was not found: {sourceRoot}");
        }

        var stamp = (now ?? DateTimeOffset.Now).ToString("yyyyMMdd-HHmmss");
        var backupRoot = Path.Combine(artifactsRoot, "profile-sync-backups", stamp);
        var destinationBackupRoot = Path.Combine(backupRoot, "modded-profile-before-sync");
        var reportPath = Path.Combine(backupRoot, "profile-sync-report.json");
        var warnings = new List<string>
        {
            "This command overwrites modded/profileN with the current vanilla profile after backing the destination up.",
            "Run it only while Slay the Spire 2 is closed.",
        };

        Directory.CreateDirectory(backupRoot);
        if (Directory.Exists(destinationRoot))
        {
            CopyDirectory(destinationRoot, destinationBackupRoot);
        }

        RecreateDirectory(destinationRoot);
        CopyDirectory(sourceRoot, destinationRoot);

        var files = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Select(sourcePath =>
            {
                var relativePath = Path.GetRelativePath(sourceRoot, sourcePath);
                var destinationPath = Path.Combine(destinationRoot, relativePath);
                var info = new FileInfo(destinationPath);
                return new ModdedProfileSyncFile(
                    relativePath,
                    sourcePath,
                    destinationPath,
                    "copied",
                    info.Length,
                    ComputeSha256(destinationPath));
            })
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new ModdedProfileSyncResult(
            sourceRoot,
            destinationRoot,
            backupRoot,
            reportPath,
            files,
            warnings);

        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(result, JsonOptions));
        return result;
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);

        foreach (var directory in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, directory);
            Directory.CreateDirectory(Path.Combine(destinationRoot, relativePath));
        }

        foreach (var file in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, file);
            var destinationPath = Path.Combine(destinationRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    private static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}

using System.Text.RegularExpressions;
using Sts2Speed.Core.Configuration;

namespace Sts2Speed.Core.Planning;

public sealed record ModPathCandidate(
    string Path,
    bool Exists,
    bool DiscoveredFromLog,
    string Evidence,
    bool ParentDirectoryExists);

public sealed record ModPathDiscoveryResult(
    string LogPath,
    string? RecommendedPath,
    IReadOnlyList<ModPathCandidate> Candidates,
    IReadOnlyList<string> Warnings);

public static class ModPathDiscovery
{
    public static ModPathDiscoveryResult Discover(GamePathOptions options)
    {
        var logPath = Path.Combine(options.UserDataRoot, "logs", "godot.log");
        var warnings = new List<string>();
        var candidates = new Dictionary<string, ModPathCandidate>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(logPath))
        {
            foreach (var candidatePath in ExtractPathsFromLog(ReadTextWithSharedAccess(logPath)))
            {
                AddCandidate(candidates, candidatePath, discoveredFromLog: true, "Found in godot.log.");
            }
        }
        else
        {
            warnings.Add($"Log file not found: {logPath}");
        }

        foreach (var candidatePath in BuildHeuristicCandidates(options))
        {
            AddCandidate(candidates, candidatePath, discoveredFromLog: false, "Heuristic candidate.");
        }

        var orderedCandidates = candidates.Values
            .OrderByDescending(candidate => candidate.DiscoveredFromLog)
            .ThenByDescending(candidate => candidate.Exists)
            .ThenBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        string? recommendedPath = orderedCandidates
            .FirstOrDefault(candidate => candidate.DiscoveredFromLog && (candidate.Exists || candidate.ParentDirectoryExists))
            ?.Path;

        if (recommendedPath is null)
        {
            warnings.Add("No exact mod path was discovered from logs. Confirm the mod scan path in the in-game Modding UI before deploying.");
        }

        return new ModPathDiscoveryResult(logPath, recommendedPath, orderedCandidates, warnings);
    }

    private static IEnumerable<string> ExtractPathsFromLog(string logContents)
    {
        const string pattern = @"(?i)([A-Z]:\\[^""\r\n]*(?:mods?|workshop\\content\\2868840)[^""\r\n]*)";
        foreach (Match match in Regex.Matches(logContents, pattern))
        {
            var candidate = match.Groups[1].Value.Trim().TrimEnd('.', ',', ';', ')', ']');
            if (candidate.Length > 0)
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<string> BuildHeuristicCandidates(GamePathOptions options)
    {
        yield return Path.Combine(options.GameDirectory, "mods");
        yield return Path.Combine(options.GameDirectory, "Mods");
        yield return Path.Combine(options.UserDataRoot, "mods");
        yield return Path.Combine(options.UserDataRoot, "Mods");
        yield return Path.Combine(options.UserDataRoot, "steam", options.SteamAccountId, "mods");
        yield return Path.Combine(options.UserDataRoot, "steam", options.SteamAccountId, "Mods");

        var commonDirectory = Directory.GetParent(options.GameDirectory);
        var steamAppsDirectory = commonDirectory?.Parent;
        if (steamAppsDirectory is not null)
        {
            yield return Path.Combine(steamAppsDirectory.FullName, "workshop", "content", "2868840");
        }
    }

    private static void AddCandidate(
        IDictionary<string, ModPathCandidate> candidates,
        string candidatePath,
        bool discoveredFromLog,
        string evidence)
    {
        var normalizedPath = Path.GetFullPath(candidatePath);
        var exists = Directory.Exists(normalizedPath);
        var parentDirectory = Directory.GetParent(normalizedPath);
        var parentExists = parentDirectory is not null && Directory.Exists(parentDirectory.FullName);

        if (candidates.TryGetValue(normalizedPath, out var existing))
        {
            candidates[normalizedPath] = existing with
            {
                Exists = existing.Exists || exists,
                DiscoveredFromLog = existing.DiscoveredFromLog || discoveredFromLog,
                Evidence = existing.DiscoveredFromLog
                    ? existing.Evidence
                    : evidence,
                ParentDirectoryExists = existing.ParentDirectoryExists || parentExists,
            };

            return;
        }

        candidates[normalizedPath] = new ModPathCandidate(
            normalizedPath,
            exists,
            discoveredFromLog,
            evidence,
            parentExists);
    }

    private static string ReadTextWithSharedAccess(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

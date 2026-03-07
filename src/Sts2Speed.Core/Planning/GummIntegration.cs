using System.Text;
using System.Text.Json;
using Sts2Speed.Core.Configuration;

namespace Sts2Speed.Core.Planning;

public sealed record GummGameDescriptor(
    string Title,
    string GodotVersion,
    string MainScene);

public sealed record GummGameDescriptorResult(
    string OutputRoot,
    string DescriptorRoot,
    string GameConfigPath,
    IReadOnlyList<string> Warnings);

public sealed record GummInstallFile(
    string Path,
    string Status,
    string Description);

public sealed record GummInstallResult(
    string GameDirectory,
    string PackageRoot,
    string OverrideConfigPath,
    string LoaderScenePath,
    string ReportPath,
    IReadOnlyList<GummInstallFile> Files,
    IReadOnlyList<string> Warnings);

public static class GummIntegration
{
    public static GummGameDescriptor CreateSts2GameDescriptor()
    {
        return new GummGameDescriptor(
            "Slay the Spire 2",
            "4.x",
            "res://scenes/game.tscn");
    }

    public static GummGameDescriptorResult MaterializeGameDescriptor(string outputRoot)
    {
        var descriptor = CreateSts2GameDescriptor();
        var descriptorRoot = Path.Combine(outputRoot, "gumm-game-entry", descriptor.Title);
        var gameConfigPath = Path.Combine(descriptorRoot, "game.cfg");
        Directory.CreateDirectory(descriptorRoot);

        File.WriteAllText(gameConfigPath, BuildGameConfig(descriptor));

        return new GummGameDescriptorResult(
            outputRoot,
            descriptorRoot,
            gameConfigPath,
            new[]
            {
                "GUMM game entry generated for manual import.",
                "Main scene path is based on the public STS2 GUMM guide: res://scenes/game.tscn.",
            });
    }

    public static GummInstallResult InstallLoader(
        GamePathOptions gamePaths,
        string packageRoot,
        string gummRepositoryRoot)
    {
        packageRoot = Path.GetFullPath(packageRoot);
        gummRepositoryRoot = Path.GetFullPath(gummRepositoryRoot);

        var descriptor = CreateSts2GameDescriptor();
        var overrideConfigPath = Path.Combine(gamePaths.GameDirectory, "override.cfg");
        var loaderScenePath = Path.Combine(gamePaths.GameDirectory, "GUMM_mod_loader.tscn");
        var sourceLoaderScenePath = Path.Combine(gummRepositoryRoot, "System", descriptor.GodotVersion, "GUMM_mod_loader.tscn");
        var reportPath = Path.Combine(gamePaths.ArtifactsRoot, "gumm-install-report.json");

        if (!Directory.Exists(packageRoot))
        {
            throw new DirectoryNotFoundException($"Package root was not found: {packageRoot}");
        }

        if (!File.Exists(Path.Combine(packageRoot, "mod.cfg")))
        {
            throw new FileNotFoundException("mod.cfg was not found in the package root.", Path.Combine(packageRoot, "mod.cfg"));
        }

        if (!File.Exists(sourceLoaderScenePath))
        {
            throw new FileNotFoundException("GUMM loader scene was not found.", sourceLoaderScenePath);
        }

        var warnings = new List<string>();
        var files = new List<GummInstallFile>();

        var document = OverrideConfigDocument.Load(overrideConfigPath);
        document.SetValue("application", "run/main_scene", Quote("res://GUMM_mod_loader.tscn"));
        document.SetValue("gumm", "main_scene", Quote(descriptor.MainScene));
        document.SetValue("gumm", "mod_list", BuildPackedStringArray(packageRoot));
        File.WriteAllText(overrideConfigPath, document.Serialize());
        files.Add(new GummInstallFile(overrideConfigPath, "written", "Merged or created override.cfg with GUMM settings."));

        File.Copy(sourceLoaderScenePath, loaderScenePath, overwrite: true);
        files.Add(new GummInstallFile(loaderScenePath, "copied", "Copied GUMM 4.x loader scene into the game directory."));

        if (document.WasLoadedFromExistingFile)
        {
            warnings.Add("Existing override.cfg was merged. Review the written file if the game already used custom override settings.");
        }

        var result = new GummInstallResult(
            gamePaths.GameDirectory,
            packageRoot,
            overrideConfigPath,
            loaderScenePath,
            Path.GetFullPath(reportPath),
            files,
            warnings);

        Directory.CreateDirectory(Path.GetDirectoryName(result.ReportPath)!);
        File.WriteAllText(result.ReportPath, JsonSerializer.Serialize(result, JsonOptions));
        return result;
    }

    private static string BuildGameConfig(GummGameDescriptor descriptor)
    {
        var lines = new[]
        {
            "[Godot Game]",
            string.Empty,
            $"title={Quote(descriptor.Title)}",
            $"godot_version={Quote(descriptor.GodotVersion)}",
            $"main_scene={Quote(descriptor.MainScene)}",
        };

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string BuildPackedStringArray(params string[] values)
    {
        var quotedValues = values
            .Select(value => Quote(value.Replace('\\', '/')))
            .ToArray();

        return $"PackedStringArray({string.Join(", ", quotedValues)})";
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    private static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}

internal sealed class OverrideConfigDocument
{
    private readonly Dictionary<string, Dictionary<string, string>> sections;
    private readonly List<string> sectionOrder;

    private OverrideConfigDocument(
        Dictionary<string, Dictionary<string, string>> sections,
        List<string> sectionOrder,
        bool wasLoadedFromExistingFile)
    {
        this.sections = sections;
        this.sectionOrder = sectionOrder;
        WasLoadedFromExistingFile = wasLoadedFromExistingFile;
    }

    public bool WasLoadedFromExistingFile { get; }

    public static OverrideConfigDocument Load(string path)
    {
        if (!File.Exists(path))
        {
            return new OverrideConfigDocument(
                new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase),
                new List<string>(),
                false);
        }

        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var sectionOrder = new List<string>();
        string? currentSection = null;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                EnsureSection(sections, sectionOrder, currentSection);
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || currentSection is null)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            EnsureSection(sections, sectionOrder, currentSection);
            sections[currentSection][key] = value;
        }

        return new OverrideConfigDocument(sections, sectionOrder, true);
    }

    public void SetValue(string section, string key, string value)
    {
        EnsureSection(sections, sectionOrder, section);
        sections[section][key] = value;
    }

    public string Serialize()
    {
        var builder = new StringBuilder();
        for (var index = 0; index < sectionOrder.Count; index += 1)
        {
            var section = sectionOrder[index];
            builder.Append('[').Append(section).Append(']').AppendLine().AppendLine();

            foreach (var entry in sections[section])
            {
                builder.Append(entry.Key).Append('=').Append(entry.Value).AppendLine();
            }

            if (index < sectionOrder.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static void EnsureSection(
        IDictionary<string, Dictionary<string, string>> sections,
        ICollection<string> sectionOrder,
        string section)
    {
        if (sections.ContainsKey(section))
        {
            return;
        }

        sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        sectionOrder.Add(section);
    }
}

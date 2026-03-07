using System.Text.Json;
using Sts2Speed.Core.Configuration;

namespace Sts2Speed.ModSkeleton;

public sealed record NativePackageFile(
    string RelativePath,
    string OutputPath,
    string SourceKind,
    string Status);

public sealed record MissingNativeArtifact(
    string RelativePath,
    string Reason);

public sealed record NativePackageResult(
    string OutputRoot,
    string LayoutKind,
    string ModsRoot,
    string PackageRoot,
    string ReportPath,
    IReadOnlyList<NativePackageFile> Files,
    IReadOnlyList<MissingNativeArtifact> MissingArtifacts,
    IReadOnlyList<string> Warnings);

public static partial class SpeedModEntryPoint
{
    public static NativePackageResult MaterializeNativePackage(
        WorkspaceConfiguration configuration,
        string outputRoot,
        string runtimeAssemblyRoot,
        string layoutKind)
    {
        var normalizedLayout = NormalizeNativeLayoutKind(layoutKind);
        var descriptor = CreateDescriptor();
        var modsRoot = Path.Combine(outputRoot, "native-package-layout", normalizedLayout, "mods");
        var packageRoot = normalizedLayout == "flat"
            ? modsRoot
            : Path.Combine(modsRoot, "Sts2Speed");
        var reportPath = Path.Combine(outputRoot, "native-package-layout", normalizedLayout, "native-package-report.json");

        Directory.CreateDirectory(packageRoot);

        var files = new List<NativePackageFile>();
        var warnings = new List<string>
        {
            "Native STS2 layout is inferred from community examples and local binary strings.",
            "The live-verified GUMM path remains available as a fallback and diagnostics route.",
            "A real .pck artifact is still required before native mods-folder deployment can be validated.",
        };

        files.Add(WriteNativeTextFile(
            packageRoot,
            "manifest.json",
            CreateManifestJson(descriptor),
            "generated"));
        files.Add(WriteNativeTextFile(
            packageRoot,
            "README.native.txt",
            BuildNativePackageReadme(configuration, normalizedLayout),
            "generated"));
        files.Add(WriteNativeTextFile(
            packageRoot,
            "Sts2Speed.speed.txt",
            "1.0" + Environment.NewLine,
            "generated"));
        files.Add(WriteNativeTextFile(
            packageRoot,
            "native-loader-hints.json",
            BuildNativeLoaderHintsJson(descriptor, normalizedLayout),
            "generated"));

        foreach (var assemblyName in new[]
                 {
                     "Sts2Speed.ModSkeleton.dll",
                     "Sts2Speed.Core.dll",
                 })
        {
            var sourcePath = Path.Combine(runtimeAssemblyRoot, assemblyName);
            if (!File.Exists(sourcePath))
            {
                warnings.Add($"Runtime build artifact not found: {sourcePath}");
                continue;
            }

            files.Add(CopyNativeFile(packageRoot, assemblyName, sourcePath, "build-artifact"));
        }

        var missingArtifacts = new List<MissingNativeArtifact>
        {
            new(
                descriptor.PckName,
                "A Godot .pck file is expected by the native mods-folder route, but this workspace does not yet generate one."),
        };

        var result = new NativePackageResult(
            outputRoot,
            normalizedLayout,
            modsRoot,
            packageRoot,
            reportPath,
            files,
            missingArtifacts,
            warnings);

        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(result, JsonOptions));
        return result;
    }

    private static string NormalizeNativeLayoutKind(string? layoutKind)
    {
        if (string.Equals(layoutKind, "flat", StringComparison.OrdinalIgnoreCase))
        {
            return "flat";
        }

        return "subdir";
    }

    private static NativePackageFile WriteNativeTextFile(
        string packageRoot,
        string relativePath,
        string contents,
        string sourceKind)
    {
        var outputPath = Path.Combine(packageRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, contents);
        return new NativePackageFile(relativePath, outputPath, sourceKind, "written");
    }

    private static NativePackageFile CopyNativeFile(
        string packageRoot,
        string relativePath,
        string sourcePath,
        string sourceKind)
    {
        var outputPath = Path.Combine(packageRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.Copy(sourcePath, outputPath, overwrite: true);
        return new NativePackageFile(relativePath, outputPath, sourceKind, "copied");
    }

    private static string BuildNativePackageReadme(WorkspaceConfiguration configuration, string layoutKind)
    {
        var lines = new List<string>
        {
            "STS2 Native Mods-Folder Staging Package",
            string.Empty,
            $"Layout kind: {layoutKind}",
            $"Game directory: {configuration.GamePaths.GameDirectory}",
            $"Expected native mods root: {Path.Combine(configuration.GamePaths.GameDirectory, "mods")}",
            string.Empty,
            "Community-observed shape:",
            "  - mods/<something>.pck",
            "  - mods/<something>.dll",
            "  - mods/<something>.txt",
            string.Empty,
            "This staging package intentionally does not claim to be deployable yet.",
            "A real .pck is still missing, so the native route is not live-validated from this workspace.",
            string.Empty,
            "Why this package still exists:",
            "  - it fixes the target folder layout",
            "  - it fixes the text-config convention for the future payload",
            "  - it documents which artifact is still missing before live deployment",
            string.Empty,
            "Current config convention:",
            "  - Sts2Speed.speed.txt contains a single floating-point multiplier",
            "  - Example values: 2.0, 3.0, 0.5",
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildNativeLoaderHintsJson(SpeedModDescriptor descriptor, string layoutKind)
    {
        var document = new
        {
            approach = "native-mods-folder",
            layoutKind,
            expectedSignals = new[]
            {
                "TryLoadModFromPck",
                "LoadMods",
                "ModsDirectory",
                "SteamWorkshop",
                "ModInitializerAttribute",
            },
            inferredPackageShape = new
            {
                modsRoot = "Slay the Spire 2/mods",
                pckName = descriptor.PckName,
                primaryAssemblies = new[]
                {
                    "Sts2Speed.ModSkeleton.dll",
                    "Sts2Speed.Core.dll",
                },
                textConfig = "Sts2Speed.speed.txt",
            },
        };

        return JsonSerializer.Serialize(document, JsonOptions);
    }
}

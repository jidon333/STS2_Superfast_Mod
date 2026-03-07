using System.Globalization;
using System.Text.Json;
using Sts2Speed.Core.Configuration;

namespace Sts2Speed.ModSkeleton;

public sealed record RecommendedTestProfile(
    string Name,
    bool Enabled,
    double? SpineTimeScale,
    double? QueueWaitScale,
    double? EffectDelayScale,
    string Purpose);

public sealed record MaterializedPackageFile(
    string RelativePath,
    string OutputPath,
    string SourceKind,
    string Status);

public sealed record MaterializedPackageResult(
    string OutputRoot,
    string PackageRoot,
    string ReportPath,
    IReadOnlyList<MaterializedPackageFile> Files,
    IReadOnlyList<RecommendedTestProfile> TestProfiles,
    IReadOnlyList<string> Warnings);

public sealed record DeploymentFile(
    string SourcePath,
    string DestinationPath,
    string Status);

public sealed record DeploymentResult(
    string ModRoot,
    string DeployedRoot,
    string ReportPath,
    IReadOnlyList<DeploymentFile> Files,
    IReadOnlyList<string> Warnings);

public static partial class SpeedModEntryPoint
{
    public static IReadOnlyList<RecommendedTestProfile> CreateRecommendedTestProfiles()
    {
        return new List<RecommendedTestProfile>
        {
            new("vanilla", false, null, null, null, "Control run with all STS2_SPEED_* overrides cleared."),
            new("spine125", true, 1.25, null, null, "First runtime validation for animation speed only."),
            new("spine150", true, 1.5, null, null, "Second runtime validation for animation speed only."),
            new("spine175", true, 1.75, null, null, "Upper animation-only validation before queue tuning."),
            new("queue085", true, 1.75, 0.85, null, "Queue wait reduction after animation-only runs are stable."),
            new("queue070", true, 1.75, 0.7, null, "More aggressive queue wait reduction after queue085 is stable."),
            new("effect085", true, 1.75, 0.85, 0.85, "Introduce effect delay reduction after conservative queue tuning is stable."),
            new("effect070", true, 1.75, 0.85, 0.7, "More aggressive effect delay reduction once effect085 is stable."),
        };
    }

    public static MaterializedPackageResult MaterializePackage(
        WorkspaceConfiguration configuration,
        string outputRoot,
        string runtimeAssemblyRoot)
    {
        var descriptor = CreateDescriptor();
        var packageRoot = Path.Combine(outputRoot, "package-layout", "Sts2Speed");
        var reportPath = Path.Combine(outputRoot, "package-layout", "package-report.json");
        var dryRunPlan = CreateDryRunPackagePlan(configuration, outputRoot);
        var warnings = new List<string>(dryRunPlan.Warnings);
        var files = new List<MaterializedPackageFile>();

        Directory.CreateDirectory(packageRoot);

        files.Add(WriteTextFile(
            packageRoot,
            "manifest.json",
            CreateManifestJson(descriptor),
            "generated"));
        files.Add(WriteTextFile(
            packageRoot,
            "mod.cfg",
            BuildGummModConfig(descriptor),
            "generated"));
        files.Add(WriteTextFile(
            packageRoot,
            Path.Combine("config", "sts2speed.sample.json"),
            SettingsLoader.Serialize(configuration),
            "generated"));
        files.Add(WriteTextFile(
            packageRoot,
            "README.txt",
            BuildPackageReadme(Initialize(configuration)),
            "generated"));

        var testProfiles = CreateRecommendedTestProfiles();
        files.Add(WriteTextFile(
            packageRoot,
            Path.Combine("scripts", "test-profiles.json"),
            JsonSerializer.Serialize(testProfiles, JsonOptions),
            "generated"));
        files.Add(WriteTextFile(
            packageRoot,
            Path.Combine("scripts", "Start-Sts2SpeedTest.ps1"),
            BuildLaunchScript(configuration.GamePaths.GameDirectory, testProfiles),
            "generated"));
        files.Add(WriteTextFile(
            packageRoot,
            "GUMM_mod.gd",
            BuildGummBaseScript(),
            "generated"));
        files.Add(WriteTextFile(
            packageRoot,
            "mod.gd",
            BuildGummBootstrapScript(),
            "generated"));

        foreach (var assemblyName in new[]
                 {
                     "Sts2Speed.ModSkeleton.dll",
                     "Sts2Speed.Core.dll",
                     "Sts2Speed.ModSkeleton.pdb",
                     "Sts2Speed.Core.pdb",
                 })
        {
            var sourcePath = Path.Combine(runtimeAssemblyRoot, assemblyName);
            var relativePath = Path.Combine("bin", assemblyName);
            if (!File.Exists(sourcePath))
            {
                warnings.Add($"Runtime build artifact not found: {sourcePath}");
                continue;
            }

            files.Add(CopyFileToPackage(packageRoot, relativePath, sourcePath));
        }

        var result = new MaterializedPackageResult(
            outputRoot,
            packageRoot,
            reportPath,
            files,
            testProfiles,
            warnings);

        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(result, JsonOptions));
        return result;
    }

    public static DeploymentResult DeployPackage(MaterializedPackageResult package, string modRoot)
    {
        var deployedRoot = Path.Combine(modRoot, "Sts2Speed");
        var reportPath = Path.Combine(package.OutputRoot, "package-layout", "deploy-report.json");
        var warnings = new List<string>();
        var files = new List<DeploymentFile>();

        if (!modRoot.Contains("mod", StringComparison.OrdinalIgnoreCase) &&
            !modRoot.Contains(Path.Combine("workshop", "content"), StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("The selected mod root does not contain 'mod' in its path. Confirm the destination before starting the game.");
        }

        Directory.CreateDirectory(deployedRoot);
        foreach (var file in package.Files)
        {
            var sourcePath = file.OutputPath;
            var destinationPath = Path.Combine(deployedRoot, file.RelativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(sourcePath, destinationPath, overwrite: true);
            files.Add(new DeploymentFile(sourcePath, destinationPath, "copied"));
        }

        var result = new DeploymentResult(modRoot, deployedRoot, reportPath, files, warnings);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(result, JsonOptions));
        return result;
    }

    private static MaterializedPackageFile WriteTextFile(
        string packageRoot,
        string relativePath,
        string contents,
        string sourceKind)
    {
        var outputPath = Path.Combine(packageRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, contents);
        return new MaterializedPackageFile(relativePath, outputPath, sourceKind, "written");
    }

    private static MaterializedPackageFile CopyFileToPackage(
        string packageRoot,
        string relativePath,
        string sourcePath)
    {
        var outputPath = Path.Combine(packageRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.Copy(sourcePath, outputPath, overwrite: true);
        return new MaterializedPackageFile(relativePath, outputPath, "build-artifact", "copied");
    }

    private static string BuildLaunchScript(string gameDirectory, IReadOnlyList<RecommendedTestProfile> testProfiles)
    {
        var validProfiles = string.Join("', '", testProfiles.Select(profile => profile.Name));
        var gameExe = Path.Combine(gameDirectory, "SlayTheSpire2.exe");

        return $$"""
        param(
            [ValidateSet('{{validProfiles}}')]
            [string]$Profile = 'vanilla',
            [string]$Enabled = '',
            [double]$SpineTimeScale = [double]::NaN,
            [double]$QueueWaitScale = [double]::NaN,
            [double]$EffectDelayScale = [double]::NaN
        )

        $ErrorActionPreference = 'Stop'
        $profilePath = Join-Path (Split-Path -Parent $PSCommandPath) 'test-profiles.json'
        $profiles = Get-Content $profilePath -Raw | ConvertFrom-Json
        $selected = $profiles | Where-Object { $_.name -eq $Profile } | Select-Object -First 1
        if (-not $selected) {
            throw "Unknown profile: $Profile"
        }

        $gameExe = '{{gameExe}}'
        if (-not (Test-Path $gameExe)) {
            throw "Game executable not found: $gameExe"
        }

        function Set-Or-Clear([string]$name, [string]$value) {
            if ([string]::IsNullOrWhiteSpace($value)) {
                Remove-Item "Env:$name" -ErrorAction SilentlyContinue
            } else {
                Set-Item "Env:$name" $value
            }
        }

        function Convert-ToInvariant([double]$number) {
            return [Convert]::ToString($number, [System.Globalization.CultureInfo]::InvariantCulture)
        }

        $resolvedEnabled = if (-not [string]::IsNullOrWhiteSpace($Enabled)) {
            $Enabled
        } elseif ($null -ne $selected.enabled) {
            $selected.enabled.ToString().ToLowerInvariant()
        } else {
            ''
        }

        $resolvedSpine = if (-not [double]::IsNaN($SpineTimeScale)) {
            Convert-ToInvariant $SpineTimeScale
        } elseif ($null -ne $selected.spineTimeScale) {
            Convert-ToInvariant ([double]$selected.spineTimeScale)
        } else {
            ''
        }

        $resolvedQueue = if (-not [double]::IsNaN($QueueWaitScale)) {
            Convert-ToInvariant $QueueWaitScale
        } elseif ($null -ne $selected.queueWaitScale) {
            Convert-ToInvariant ([double]$selected.queueWaitScale)
        } else {
            ''
        }

        $resolvedEffect = if (-not [double]::IsNaN($EffectDelayScale)) {
            Convert-ToInvariant $EffectDelayScale
        } elseif ($null -ne $selected.effectDelayScale) {
            Convert-ToInvariant ([double]$selected.effectDelayScale)
        } else {
            ''
        }

        Set-Or-Clear 'STS2_SPEED_ENABLED' $resolvedEnabled
        Remove-Item 'Env:STS2_SPEED_ANIMATION_SCALE' -ErrorAction SilentlyContinue
        Set-Or-Clear 'STS2_SPEED_SPINE_TIME_SCALE' $resolvedSpine
        Set-Or-Clear 'STS2_SPEED_QUEUE_WAIT_SCALE' $resolvedQueue
        Set-Or-Clear 'STS2_SPEED_EFFECT_DELAY_SCALE' $resolvedEffect
        Remove-Item 'Env:STS2_SPEED_FAST_MODE_OVERRIDE' -ErrorAction SilentlyContinue

        Start-Process -FilePath $gameExe -WorkingDirectory (Split-Path -Parent $gameExe)
        """;
    }

    private static string BuildGummModConfig(SpeedModDescriptor descriptor)
    {
        return $$"""
        [Godot Mod]

        game="Slay the Spire 2"
        name="{{descriptor.Name}}"
        description="{{descriptor.Description}}"
        version="{{descriptor.Version}}"
        """;
    }

    private static string BuildGummBaseScript()
    {
        return """
        extends RefCounted

        var base_path: String

        func initialize(mod_path: String, scene_tree: SceneTree) -> void:
        	base_path = mod_path
        	_initialize(scene_tree)

        func _initialize(scene_tree: SceneTree) -> void:
        	pass
        """;
    }

    private static string BuildGummBootstrapScript()
    {
        return """
        extends "GUMM_mod.gd"

        func _initialize(scene_tree: SceneTree) -> void:
        	var enabled := OS.get_environment("STS2_SPEED_ENABLED")
        	var spine := OS.get_environment("STS2_SPEED_SPINE_TIME_SCALE")
        	var queue := OS.get_environment("STS2_SPEED_QUEUE_WAIT_SCALE")
        	var effect := OS.get_environment("STS2_SPEED_EFFECT_DELAY_SCALE")
        	print("STS2 Speed Skeleton GUMM bootstrap loaded. enabled=%s spine=%s queue=%s effect=%s" % [enabled, spine, queue, effect])
        """;
    }
}

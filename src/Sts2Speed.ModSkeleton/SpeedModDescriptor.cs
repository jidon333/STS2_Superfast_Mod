using System.Text.Json;
using Sts2Speed.Core.Configuration;
using Sts2Speed.Core.Planning;
using Sts2Speed.ModSkeleton.Integration;

namespace Sts2Speed.ModSkeleton;

public sealed record SpeedModDescriptor(
    string Name,
    string Author,
    string Version,
    string Description,
    string PckName);

public sealed record PatchDirective(
    string Category,
    string GameTypeName,
    string MethodName,
    double RequestedScale,
    string ActivationCondition,
    string Notes);

public sealed record ModInitializationPlan(
    SpeedModDescriptor Descriptor,
    IReadOnlyList<PatchDirective> Patches,
    IReadOnlyList<string> Warnings);

public sealed record SkeletonPackageFile(
    string RelativePath,
    string Description,
    string PreviewContent);

public sealed record DryRunPackagePlan(
    string OutputRoot,
    IReadOnlyList<SkeletonPackageFile> Files,
    IReadOnlyList<string> Warnings);

public static class SpeedModEntryPoint
{
    public static ModInitializationPlan Initialize(WorkspaceConfiguration configuration)
    {
        var descriptor = CreateDescriptor();
        var directives = KnownPatchTargets.All
            .Select(target => new PatchDirective(
                target.Category,
                target.GameTypeName,
                target.MethodName,
                ResolveScale(target.ScaleSetting, configuration.Settings),
                configuration.Settings.Enabled
                    ? "Apply only in single-player runtime once the game is closed and a safe copy is used."
                    : "Disabled until config.settings.enabled=true or STS2_SPEED_ENABLED=true.",
                target.Description))
            .ToList();

        var warnings = new List<string>(MutationPolicy.Describe(configuration.Settings))
        {
            $"Expected initializer attribute: {ExpectedGameContracts.ModInitializerAttribute}",
            "This project intentionally avoids live Steam install writes.",
        };

        return new ModInitializationPlan(descriptor, directives, warnings);
    }

    public static SpeedModDescriptor CreateDescriptor()
    {
        return new SpeedModDescriptor(
            "STS2 Speed Skeleton",
            "jidon + Codex",
            "0.1.0-skeleton",
            "Non-invasive animation and wait acceleration scaffold for Slay the Spire 2.",
            "sts2-speed-skeleton.pck");
    }

    public static string CreateManifestJson(SpeedModDescriptor descriptor)
    {
        var manifest = new
        {
            pckName = descriptor.PckName,
            name = descriptor.Name,
            author = descriptor.Author,
            description = descriptor.Description,
            version = descriptor.Version,
        };

        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    public static DryRunPackagePlan CreateDryRunPackagePlan(WorkspaceConfiguration configuration, string outputRoot)
    {
        var descriptor = CreateDescriptor();
        var initializationPlan = Initialize(configuration);
        var configJson = SettingsLoader.Serialize(configuration);
        var manifestJson = CreateManifestJson(descriptor);
        var readmeText = BuildPackageReadme(initializationPlan);

        var files = new List<SkeletonPackageFile>
        {
            new(
                Path.Combine("package-layout", "Sts2Speed", "manifest.json"),
                "Template manifest for the future in-game mod package.",
                manifestJson),
            new(
                Path.Combine("package-layout", "Sts2Speed", "config", "sts2speed.sample.json"),
                "Sample configuration preserving the user's first-play settings.",
                configJson),
            new(
                Path.Combine("package-layout", "Sts2Speed", "README.txt"),
                "Package notes and rollback reminders.",
                readmeText),
            new(
                Path.Combine("package-layout", "Sts2Speed", "bin", "Sts2Speed.ModSkeleton.dll"),
                "Placeholder for the future compiled mod assembly.",
                "// build output placeholder"),
            new(
                Path.Combine("package-layout", "Sts2Speed", "bin", "Sts2Speed.Core.dll"),
                "Placeholder for shared configuration and planning types.",
                "// build output placeholder"),
        };

        return new DryRunPackagePlan(
            outputRoot,
            files,
            initializationPlan.Warnings);
    }

    private static string BuildPackageReadme(ModInitializationPlan plan)
    {
        var lines = new List<string>
        {
            "STS2 Speed Skeleton",
            string.Empty,
            "Expected game contracts:",
            $"  - {ExpectedGameContracts.ModManager}",
            $"  - {ExpectedGameContracts.ModManifest}",
            $"  - {ExpectedGameContracts.ModdingScreen}",
            string.Empty,
            "Safety reminders:",
            "  - Do not copy this package into the live Steam install while the game is running.",
            "  - Snapshot game and user save files before the first real integration attempt.",
            "  - Keep preserveGameSettings=true during the first-play period.",
            string.Empty,
            "Planned patch directives:",
        };

        lines.AddRange(plan.Patches.Select(patch =>
            $"  - {patch.GameTypeName}.{patch.MethodName} [{patch.Category}] => scale {patch.RequestedScale:0.###}"));

        return string.Join(Environment.NewLine, lines);
    }

    private static double ResolveScale(string settingName, SpeedModSettings settings)
    {
        return settingName switch
        {
            "AnimationScale" => settings.AnimationScale,
            "SpineTimeScale" => settings.SpineTimeScale,
            "QueueWaitScale" => settings.QueueWaitScale,
            "EffectDelayScale" => settings.EffectDelayScale,
            _ => 1.0,
        };
    }

    private static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}

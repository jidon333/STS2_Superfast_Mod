using System.Text.Json;

namespace Sts2Speed.ModSkeleton;

public sealed record SpeedModDescriptor(
    string Name,
    string Author,
    string Version,
    string Description,
    string PckName);

public static partial class SpeedModEntryPoint
{
    public static SpeedModDescriptor CreateDescriptor()
    {
        return new SpeedModDescriptor(
            "STS2 Speed Skeleton",
            "jidon + Codex",
            "0.1.0-skeleton",
            "Non-invasive animation and wait acceleration scaffold for Slay the Spire 2.",
            "sts2-speed-skeleton.pck");
    }

    internal static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}

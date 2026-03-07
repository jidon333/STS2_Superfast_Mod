using Sts2Speed.Core.Configuration;

namespace Sts2Speed.Core.Planning;

public static class MutationPolicy
{
    public static bool ShouldMutateLiveGameSettings(SpeedModSettings settings)
    {
        if (settings.PreserveGameSettings)
        {
            return false;
        }

        return settings.FastModeOverride is not null;
    }

    public static IReadOnlyList<string> Describe(SpeedModSettings settings)
    {
        var notes = new List<string>();
        if (settings.PreserveGameSettings)
        {
            notes.Add("Live game settings remain untouched while preserveGameSettings=true.");
        }
        else
        {
            notes.Add("Live settings mutation is allowed once explicit integration hooks are implemented.");
        }

        if (!settings.Enabled)
        {
            notes.Add("All runtime patch directives stay inert until the mod is explicitly enabled.");
        }

        notes.Add("Multiplayer synchronization targets remain excluded.");
        return notes;
    }
}

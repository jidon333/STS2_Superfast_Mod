namespace Sts2Speed.Core.Planning;

public sealed record KnownPatchTarget(
    string Category,
    string GameTypeName,
    string MethodName,
    string ScaleSetting,
    string Description);

public static class KnownPatchTargets
{
    public static IReadOnlyList<KnownPatchTarget> All { get; } = new List<KnownPatchTarget>
    {
        new(
            "Animation",
            "MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaAnimationState",
            "SetTimeScale",
            "SpineTimeScale",
            "Primary Spine animation time scale hook for combat and UI motion."),
        new(
            "Animation",
            "MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaTrackEntry",
            "SetTimeScale",
            "SpineTimeScale",
            "Per-track animation speed hook for fine-grained sequence acceleration."),
        new(
            "CombatWait",
            "MegaCrit.Sts2.Core.Combat.CombatManager",
            "WaitForActionThenEndTurn",
            "QueueWaitScale",
            "Late-turn wait reduction after queued actions complete."),
        new(
            "CombatWait",
            "MegaCrit.Sts2.Core.Combat.CombatManager",
            "WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction",
            "QueueWaitScale",
            "Queue drain wait reduction in single-player combat."),
        new(
            "DeferredEffect",
            "MegaCrit.Sts2.Core.Combat.CombatState",
            "GodotTimerTask",
            "EffectDelayScale",
            "General timer task acceleration without mutating live game settings."),
        new(
            "ActionQueue",
            "MegaCrit.Sts2.Core.GameActions.ActionExecutor",
            "ExecuteActions",
            "QueueWaitScale",
            "Action queue pacing adjustment once explicit runtime hooks are added."),
    };

    public static IReadOnlyList<string> ExcludedGroups { get; } = new List<string>
    {
        "MegaCrit.Sts2.Core.GameActions.Multiplayer.ActionQueueSynchronizer",
        "MegaCrit.Sts2.Core.GameActions.Multiplayer.INetAction",
        "MegaCrit.Sts2.Core.Entities.Multiplayer.*",
    };
}

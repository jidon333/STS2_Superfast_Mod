using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace Sts2Speed.ModSkeleton.Runtime;

[HarmonyPatch]
internal static class SpineTimeScalePatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var candidate in new[]
                 {
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaAnimationState", "SetTimeScale", typeof(float)),
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaTrackEntry", "SetTimeScale", typeof(float)),
                 })
        {
            if (candidate is not null)
            {
                yield return candidate;
            }
        }
    }

    [HarmonyPrefix]
    private static void Prefix(ref float scale)
    {
        if (RuntimePatchContext.TryApplySpineScale(ref scale))
        {
            RuntimePatchContext.LogAppliedOnce("spine-scale", $"spine time scale applied. effective argument={scale:0.###}");
        }
    }
}

[HarmonyPatch]
internal static class CustomScaledWaitPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var method = RuntimePatchContext.TryResolveMethod(
            "MegaCrit.Sts2.Core.Commands.Cmd",
            "CustomScaledWait",
            typeof(float),
            typeof(float),
            typeof(bool),
            typeof(CancellationToken));

        if (method is not null)
        {
            yield return method;
        }
    }

    [HarmonyPrefix]
    private static void Prefix(ref float fastSeconds, ref float standardSeconds)
    {
        if (RuntimePatchContext.TryApplyQueueWaitScale(ref fastSeconds, ref standardSeconds))
        {
            RuntimePatchContext.LogAppliedOnce("queue-wait", $"queue wait scale applied. fast={fastSeconds:0.###} standard={standardSeconds:0.###}");
        }
    }
}

[HarmonyPatch]
internal static class CombatStateTimerPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var method = RuntimePatchContext.TryResolveMethod(
            "MegaCrit.Sts2.Core.Combat.CombatState",
            "GodotTimerTask",
            typeof(double));

        if (method is not null)
        {
            yield return method;
        }
    }

    [HarmonyPrefix]
    private static void Prefix(ref double timeSec)
    {
        if (RuntimePatchContext.TryApplyEffectDelayScale(ref timeSec))
        {
            RuntimePatchContext.LogAppliedOnce("effect-delay", $"effect delay scale applied. seconds={timeSec:0.###}");
        }
    }
}

[HarmonyPatch]
internal static class CombatUiDeltaPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var candidate in new[]
                 {
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Nodes.Combat.NTargetingArrow", "_Process", typeof(double)),
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Nodes.Combat.NIntent", "_Process", typeof(double)),
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Nodes.Combat.NStarCounter", "_Process", typeof(double)),
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Nodes.Combat.NEnergyCounter", "_Process", typeof(double)),
                 })
        {
            if (candidate is not null)
            {
                yield return candidate;
            }
        }
    }

    [HarmonyPrefix]
    private static void Prefix(ref double delta)
    {
        if (RuntimePatchContext.TryApplyCombatUiDelta(ref delta))
        {
            RuntimePatchContext.LogAppliedOnce("combat-ui-delta", $"combat ui delta scale applied. effective delta={delta:0.###}");
        }
    }
}

[HarmonyPatch]
internal static class CombatVfxDeltaPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var candidate in new[]
                 {
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Nodes.Vfx.NBezierTrail", "_Process", typeof(double)),
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Nodes.Vfx.NCardTrail", "_Process", typeof(double)),
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Nodes.Vfx.NDamageNumVfx", "_Process", typeof(double)),
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Nodes.Vfx.NHealNumVfx", "_Process", typeof(double)),
                 })
        {
            if (candidate is not null)
            {
                yield return candidate;
            }
        }
    }

    [HarmonyPrefix]
    private static void Prefix(ref double delta)
    {
        if (RuntimePatchContext.TryApplyCombatVfxDelta(ref delta))
        {
            RuntimePatchContext.LogAppliedOnce("combat-vfx-delta", $"combat vfx delta scale applied. effective delta={delta:0.###}");
        }
    }
}

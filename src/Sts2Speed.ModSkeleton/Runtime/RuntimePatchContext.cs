using System.Reflection;
using HarmonyLib;
using Sts2Speed.Core.Configuration;

namespace Sts2Speed.ModSkeleton.Runtime;

internal static class RuntimePatchContext
{
    private static readonly object Sync = new();
    private static readonly string ModDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
    private static readonly HashSet<string> MissingTargets = new(StringComparer.Ordinal);
    private static readonly HashSet<string> AppliedLogs = new(StringComparer.Ordinal);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(500);
    private static RuntimeSpeedSettings? _cachedSettings;
    private static DateTimeOffset _lastLoadedAt = DateTimeOffset.MinValue;
    private static DateTime _lastObservedConfigWriteUtc = DateTime.MinValue;
    private static string? _lastSettingsSignature;
    private static bool _moduleAnnouncementWritten;

    public static RuntimeSpeedSettings GetSettings(bool forceRefresh = false)
    {
        var cached = _cachedSettings;
        if (!forceRefresh
            && cached is not null
            && DateTimeOffset.UtcNow - _lastLoadedAt < RefreshInterval)
        {
            return cached;
        }

        lock (Sync)
        {
            cached = _cachedSettings;
            if (!forceRefresh
                && cached is not null
                && DateTimeOffset.UtcNow - _lastLoadedAt < RefreshInterval)
            {
                return cached;
            }

            var configWriteUtc = GetConfigWriteUtc();
            if (!forceRefresh
                && cached is not null
                && configWriteUtc == _lastObservedConfigWriteUtc)
            {
                _lastLoadedAt = DateTimeOffset.UtcNow;
                return cached;
            }

            _cachedSettings = RuntimeSettingsLoader.Load(ModDirectory);
            _lastLoadedAt = DateTimeOffset.UtcNow;
            _lastObservedConfigWriteUtc = configWriteUtc;

            if (!_moduleAnnouncementWritten)
            {
                WriteLine($"module loaded from '{ModDirectory}'. initial settings: {Describe(_cachedSettings)}");
                foreach (var warning in _cachedSettings.Warnings)
                {
                    WriteLine($"settings warning: {warning}");
                }

                _moduleAnnouncementWritten = true;
            }

            var signature = Describe(_cachedSettings);
            if (!string.Equals(_lastSettingsSignature, signature, StringComparison.Ordinal))
            {
                WriteLine($"settings refreshed: {signature}");
                _lastSettingsSignature = signature;
            }

            return _cachedSettings;
        }
    }

    public static bool ShouldApply(RuntimeSpeedSettings settings)
    {
        if (!settings.Enabled)
        {
            return false;
        }

        if (!settings.CombatOnly)
        {
            return true;
        }

        return IsCombatInProgress();
    }

    public static bool TryApplySpineScale(ref float scale)
    {
        var settings = GetSettings();
        if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.SpineSpeed))
        {
            return false;
        }

        scale = SpeedScaleMath.ApplyAnimationSpeedMultiplier(scale, settings.SpineSpeed);
        return true;
    }

    public static bool TryApplyCombatUiDelta(ref double delta)
    {
        var settings = GetSettings();
        if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.CombatUiSpeed))
        {
            return false;
        }

        delta = SpeedScaleMath.ApplyFrameDeltaSpeedMultiplier(delta, settings.CombatUiSpeed);
        return true;
    }

    public static bool TryApplyCombatVfxDelta(ref double delta)
    {
        var settings = GetSettings();
        if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.CombatVfxSpeed))
        {
            return false;
        }

        delta = SpeedScaleMath.ApplyFrameDeltaSpeedMultiplier(delta, settings.CombatVfxSpeed);
        return true;
    }

    public static bool TryApplyQueueWaitScale(ref float fastSeconds, ref float standardSeconds)
    {
        var settings = GetSettings();
        if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.QueueSpeed))
        {
            return false;
        }

        fastSeconds = SpeedScaleMath.ApplyDurationSpeedMultiplier(fastSeconds, settings.QueueSpeed);
        standardSeconds = SpeedScaleMath.ApplyDurationSpeedMultiplier(standardSeconds, settings.QueueSpeed);
        return true;
    }

    public static bool TryApplyEffectDelayScale(ref double timeSec)
    {
        var settings = GetSettings();
        if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.EffectSpeed))
        {
            return false;
        }

        timeSec = SpeedScaleMath.ApplyDurationSpeedMultiplier(timeSec, settings.EffectSpeed);
        return true;
    }

    public static MethodBase? TryResolveMethod(string typeName, string methodName, params Type[] parameterTypes)
    {
        GetSettings();

        var type = AccessTools.TypeByName(typeName);
        if (type is null)
        {
            ReportMissingTarget($"{typeName}.{methodName}", $"type '{typeName}' was not found.");
            return null;
        }

        var method = AccessTools.Method(type, methodName, parameterTypes);
        if (method is null)
        {
            ReportMissingTarget($"{typeName}.{methodName}", $"method '{methodName}' was not found.");
            return null;
        }

        return method;
    }

    public static void LogAppliedOnce(string key, string detail)
    {
        lock (Sync)
        {
            if (!AppliedLogs.Add(key))
            {
                return;
            }
        }

        WriteLine(detail);
    }

    public static void LogInfo(string detail)
    {
        WriteLine(detail);
    }

    private static DateTime GetConfigWriteUtc()
    {
        var configWriteUtc = File.Exists(Path.Combine(ModDirectory, RuntimeSettingsLoader.RuntimeConfigFileName))
            ? File.GetLastWriteTimeUtc(Path.Combine(ModDirectory, RuntimeSettingsLoader.RuntimeConfigFileName))
            : DateTime.MinValue;
        var legacyWriteUtc = File.Exists(Path.Combine(ModDirectory, RuntimeSettingsLoader.LegacySpeedFileName))
            ? File.GetLastWriteTimeUtc(Path.Combine(ModDirectory, RuntimeSettingsLoader.LegacySpeedFileName))
            : DateTime.MinValue;
        return configWriteUtc >= legacyWriteUtc ? configWriteUtc : legacyWriteUtc;
    }

    private static void ReportMissingTarget(string key, string detail)
    {
        lock (Sync)
        {
            if (!MissingTargets.Add(key))
            {
                return;
            }
        }

        WriteLine($"target skipped: {key} ({detail})");
    }

    private static bool IsCombatInProgress()
    {
        var managerType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Combat.CombatManager");
        if (managerType is null)
        {
            return false;
        }

        var instance = AccessTools.Property(managerType, "Instance")?.GetValue(null);
        if (instance is null)
        {
            return false;
        }

        return AccessTools.Property(managerType, "IsInProgress")?.GetValue(instance) as bool? ?? false;
    }

    private static string Describe(RuntimeSpeedSettings settings)
    {
        return $"enabled={settings.Enabled} base={settings.BaseSpeed:0.###} spine={settings.SpineSpeed:0.###} queue={settings.QueueSpeed:0.###} effect={settings.EffectSpeed:0.###} combatUi={settings.CombatUiSpeed:0.###} combatVfx={settings.CombatVfxSpeed:0.###} combatOnly={settings.CombatOnly} sources=[{string.Join(", ", settings.Sources)}]";
    }

    private static void WriteLine(string message)
    {
        var formatted = $"[STS2Speed] {message}";
        Console.WriteLine(formatted);

        try
        {
            var logPath = Path.Combine(ModDirectory, "sts2speed.runtime.log");
            File.AppendAllText(logPath, formatted + Environment.NewLine);
        }
        catch
        {
            // Keep the live mod resilient even if file logging is unavailable.
        }
    }
}

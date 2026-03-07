using System.Reflection;
using HarmonyLib;
using Sts2Speed.Core.Configuration;

namespace Sts2Speed.ModSkeleton.Runtime;

internal static class RuntimePatchContext
{
    private static readonly object Sync = new();
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
        lock (Sync)
        {
            var modDirectory = GetModDirectory();
            var configWriteUtc = GetConfigWriteUtc(modDirectory);
            var shouldRefresh = forceRefresh
                || _cachedSettings is null
                || DateTimeOffset.UtcNow - _lastLoadedAt >= RefreshInterval
                || configWriteUtc != _lastObservedConfigWriteUtc;

            if (!shouldRefresh)
            {
                return _cachedSettings!;
            }

            _cachedSettings = RuntimeSettingsLoader.Load(modDirectory);
            _lastLoadedAt = DateTimeOffset.UtcNow;
            _lastObservedConfigWriteUtc = configWriteUtc;

            if (!_moduleAnnouncementWritten)
            {
                WriteLine($"module loaded from '{modDirectory}'. initial settings: {Describe(_cachedSettings)}");
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
        if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.SpineTimeScale))
        {
            return false;
        }

        scale = SpeedScaleMath.ApplyAnimationSpeedMultiplier(scale, settings.SpineTimeScale);
        return true;
    }

    public static bool TryApplyQueueWaitScale(ref float fastSeconds, ref float standardSeconds)
    {
        var settings = GetSettings();
        if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.QueueWaitScale))
        {
            return false;
        }

        fastSeconds = SpeedScaleMath.ApplyDurationSpeedMultiplier(fastSeconds, settings.QueueWaitScale);
        standardSeconds = SpeedScaleMath.ApplyDurationSpeedMultiplier(standardSeconds, settings.QueueWaitScale);
        return true;
    }

    public static bool TryApplyEffectDelayScale(ref double timeSec)
    {
        var settings = GetSettings();
        if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.EffectDelayScale))
        {
            return false;
        }

        timeSec = SpeedScaleMath.ApplyDurationSpeedMultiplier(timeSec, settings.EffectDelayScale);
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

    private static string GetModDirectory()
    {
        var location = Assembly.GetExecutingAssembly().Location;
        return Path.GetDirectoryName(location) ?? AppContext.BaseDirectory;
    }

    private static DateTime GetConfigWriteUtc(string modDirectory)
    {
        var configPath = Path.Combine(modDirectory, "Sts2Speed.speed.txt");
        return File.Exists(configPath)
            ? File.GetLastWriteTimeUtc(configPath)
            : DateTime.MinValue;
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
        return $"enabled={settings.Enabled} spine={settings.SpineTimeScale:0.###} queue={settings.QueueWaitScale:0.###} effect={settings.EffectDelayScale:0.###} combatOnly={settings.CombatOnly} sources=[{string.Join(", ", settings.Sources)}]";
    }

    private static void WriteLine(string message)
    {
        var formatted = $"[STS2Speed] {message}";
        Console.WriteLine(formatted);

        try
        {
            var logPath = Path.Combine(GetModDirectory(), "sts2speed.runtime.log");
            File.AppendAllText(logPath, formatted + Environment.NewLine);
        }
        catch
        {
            // Keep the live mod resilient even if file logging is unavailable.
        }
    }
}

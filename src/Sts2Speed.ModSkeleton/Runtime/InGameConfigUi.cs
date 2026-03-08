using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using Sts2Speed.Core.Configuration;

namespace Sts2Speed.ModSkeleton.Runtime;

internal static class InGameConfigUi
{
    private const string ValueFormat = "0.##x";
    private const double BaseSpeedStep = 0.25;
    private const double GroupSpeedStep = 0.1;
    private const double MinBaseSpeed = 0.25;
    private const double MaxBaseSpeed = 8.0;
    private const double MinGroupSpeed = 0.1;
    private const double MaxGroupSpeed = 3.0;
    private const float PanelOffsetX = 16f;
    private const float PanelTopGap = 16f;
    private const float RowSpacing = 8f;
    private const float ButtonWidth = 40f;
    private const float ValueWidth = 72f;
    private const float TitleWidth = 170f;
    private const float RowHeight = 36f;
    private static readonly string ModDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
    private static readonly string ConfigPath = Path.Combine(ModDirectory, RuntimeSettingsLoader.RuntimeConfigFileName);
    private static readonly string LegacySpeedFilePath = Path.Combine(ModDirectory, RuntimeSettingsLoader.LegacySpeedFileName);
    private static readonly string TargetPckName = Path.GetFileNameWithoutExtension(SpeedModEntryPoint.CreateDescriptor().PckName);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly ConditionalWeakTable<object, PanelState> Panels = new();

    public static void RefreshForSelection(object infoContainer, object? mod)
    {
        try
        {
            var state = Panels.GetValue(infoContainer, _ => new PanelState());
            EnsurePanelExists(infoContainer, state);

            if (!IsTargetMod(mod))
            {
                if (state.Root is not null)
                {
                    SetVisible(state.Root, visible: false);
                }

                SetDescriptionVisible(infoContainer, visible: true);

                return;
            }

            var settings = LoadEditableSettings();
            SetDescriptionVisible(infoContainer, visible: false);
            UpdateLayout(infoContainer, state);
            UpdateTexts(state, settings);
            if (state.Root is not null)
            {
                SetVisible(state.Root, visible: true);
            }
        }
        catch (Exception exception)
        {
            RuntimePatchContext.LogAppliedOnce("ui-refresh-failed", $"config ui refresh failed: {exception.Message}");
        }
    }

    private static void EnsurePanelExists(object infoContainer, PanelState state)
    {
        if (state.Root is not null)
        {
            return;
        }

        var root = New("Godot.VBoxContainer");
        var enabledButton = CreateActionButton(string.Empty);
        var combatOnlyButton = CreateActionButton(string.Empty);
        AddChild(root, enabledButton);

        var rows = new[]
        {
            CreateAdjustRow(root, "Base speed", BaseSpeedStep, MinBaseSpeed, MaxBaseSpeed, settings => settings.BaseSpeed, (settings, value) => settings with { BaseSpeed = value }),
            CreateAdjustRow(root, "Spine speed", GroupSpeedStep, MinGroupSpeed, MaxGroupSpeed, settings => settings.SpineSpeed, (settings, value) => settings with { SpineSpeed = value }),
            CreateAdjustRow(root, "Queue speed", GroupSpeedStep, MinGroupSpeed, MaxGroupSpeed, settings => settings.QueueSpeed, (settings, value) => settings with { QueueSpeed = value }),
            CreateAdjustRow(root, "Effect speed", GroupSpeedStep, MinGroupSpeed, MaxGroupSpeed, settings => settings.EffectSpeed, (settings, value) => settings with { EffectSpeed = value }),
            CreateAdjustRow(root, "Combat UI speed", GroupSpeedStep, MinGroupSpeed, MaxGroupSpeed, settings => settings.CombatUiSpeed, (settings, value) => settings with { CombatUiSpeed = value }),
            CreateAdjustRow(root, "Combat VFX speed", GroupSpeedStep, MinGroupSpeed, MaxGroupSpeed, settings => settings.CombatVfxSpeed, (settings, value) => settings with { CombatVfxSpeed = value }),
        };

        foreach (var row in rows)
        {
            AddChild(root, row.Root);
        }

        AddChild(root, combatOnlyButton);

        BindPressed(enabledButton, () =>
        {
            var settings = LoadEditableSettings();
            var updated = settings with { Enabled = !settings.Enabled };
            SaveEditableSettings(updated);
            UpdateTexts(state, updated);
        });

        BindPressed(combatOnlyButton, () =>
        {
            var settings = LoadEditableSettings();
            var updated = settings with { CombatOnly = !settings.CombatOnly };
            SaveEditableSettings(updated);
            UpdateTexts(state, updated);
        });

        AddChild(infoContainer, root);
        SetVisible(root, visible: false);

        state.Root = root;
        state.EnabledButton = enabledButton;
        state.CombatOnlyButton = combatOnlyButton;
        state.Rows = rows;

        RuntimePatchContext.LogAppliedOnce("ui-panel-created", "config ui injected into modding screen.");
    }

    private static AdjustableRow CreateAdjustRow(
        object root,
        string title,
        double step,
        double minimum,
        double maximum,
        Func<SpeedModSettings, double> getter,
        Func<SpeedModSettings, double, SpeedModSettings> setter)
    {
        var row = New("Godot.HBoxContainer");
        var titleLabel = CreateLabel(title);
        var minusButton = CreateActionButton("-");
        var valueButton = CreateActionButton("0");
        var plusButton = CreateActionButton("+");

        SetCustomMinimumSize(row, 0f, RowHeight);
        SetCustomMinimumSize(titleLabel, TitleWidth, RowHeight);
        SetCustomMinimumSize(minusButton, ButtonWidth, RowHeight);
        SetCustomMinimumSize(valueButton, ValueWidth, RowHeight);
        SetCustomMinimumSize(plusButton, ButtonWidth, RowHeight);

        AddChild(row, titleLabel);
        AddChild(row, minusButton);
        AddChild(row, valueButton);
        AddChild(row, plusButton);

        BindPressed(minusButton, () =>
        {
            var settings = LoadEditableSettings();
            var updatedValue = ClampAndRound(getter(settings) - step, minimum, maximum);
            var updated = setter(settings, updatedValue);
            SaveEditableSettings(updated);
            SetText(valueButton, updatedValue.ToString(ValueFormat));
            RefreshPanel(root);
        });

        BindPressed(plusButton, () =>
        {
            var settings = LoadEditableSettings();
            var updatedValue = ClampAndRound(getter(settings) + step, minimum, maximum);
            var updated = setter(settings, updatedValue);
            SaveEditableSettings(updated);
            SetText(valueButton, updatedValue.ToString(ValueFormat));
            RefreshPanel(root);
        });

        return new AdjustableRow(title, row, valueButton, getter);
    }

    private static void RefreshPanel(object root)
    {
        var parent = Invoke(root, "GetParent");
        if (parent is null)
        {
            return;
        }

        var state = Panels.TryGetValue(parent, out var existing) ? existing : null;
        if (state is null)
        {
            return;
        }

        var settings = LoadEditableSettings();
        UpdateTexts(state, settings);
        UpdateLayout(parent, state);
    }

    private static void UpdateTexts(PanelState state, SpeedModSettings settings)
    {
        if (state.EnabledButton is not null)
        {
            SetText(state.EnabledButton, settings.Enabled ? "Enabled: On" : "Enabled: Off");
        }

        if (state.CombatOnlyButton is not null)
        {
            SetText(state.CombatOnlyButton, settings.CombatOnly ? "Combat only: On" : "Combat only: Off");
        }

        foreach (var row in state.Rows)
        {
            SetText(row.ValueLabel, row.Getter(settings).ToString(ValueFormat));
        }
    }

    private static void UpdateLayout(object infoContainer, PanelState state)
    {
        var title = AccessTools.Field(infoContainer.GetType(), "_title")?.GetValue(infoContainer);
        if (title is null || state.Root is null)
        {
            return;
        }

        var hostSize = GetVector2(infoContainer, "Size");
        var width = Math.Max(260f, hostSize.X - (PanelOffsetX * 2f));
        var titlePosition = GetVector2(title, "Position");
        var titleSize = GetVector2(title, "Size");
        var panelY = titlePosition.Y + Math.Max(titleSize.Y, 72f) + PanelTopGap;

        SetPosition(state.Root, PanelOffsetX, panelY);
        SetCustomMinimumSize(state.Root, width, 0f);

        var currentY = 0f;
        foreach (var child in GetChildren(state.Root))
        {
            SetPosition(child, 0f, currentY);
            SetCustomMinimumSize(child, width, 0f);
            currentY += GetHeight(child) + RowSpacing;
        }
    }

    private static IEnumerable<object> GetChildren(object node)
    {
        if (Invoke(node, "GetChildren") is not IEnumerable<object> typed)
        {
            if (Invoke(node, "GetChildren") is System.Collections.IEnumerable untyped)
            {
                foreach (var child in untyped)
                {
                    if (child is not null)
                    {
                        yield return child;
                    }
                }
            }

            yield break;
        }

        foreach (var child in typed)
        {
            yield return child;
        }
    }

    private static float GetHeight(object node)
    {
        var size = GetVector2(node, "Size");
        if (size.Y > 0f)
        {
            return size.Y;
        }

        var minimumSize = GetVector2(node, "CustomMinimumSize");
        return minimumSize.Y > 0f ? minimumSize.Y : 28f;
    }

    private static bool IsTargetMod(object? mod)
    {
        var pckName = GetMemberValue(mod, "pckName") as string;
        if (string.IsNullOrWhiteSpace(pckName))
        {
            return false;
        }

        return string.Equals(
            Path.GetFileNameWithoutExtension(pckName),
            TargetPckName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static SpeedModSettings LoadEditableSettings()
    {
        var settings = SpeedModSettings.Defaults;
        if (File.Exists(ConfigPath))
        {
            try
            {
                var partial = JsonSerializer.Deserialize<PartialSpeedModSettings>(File.ReadAllText(ConfigPath), JsonOptions);
                settings = settings.With(partial);
            }
            catch (JsonException exception)
            {
                RuntimePatchContext.LogAppliedOnce("ui-config-read-warning", $"config ui ignored invalid json: {exception.Message}");
            }
        }
        else if (File.Exists(LegacySpeedFilePath))
        {
            var runtimeSettings = RuntimePatchContext.GetSettings(forceRefresh: true);
            settings = settings with
            {
                BaseSpeed = runtimeSettings.BaseSpeed,
                Enabled = runtimeSettings.Enabled,
            };
        }

        return settings;
    }

    private static void SaveEditableSettings(SpeedModSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(ToEditableDocument(settings), JsonOptions));
        RuntimePatchContext.LogInfo($"config ui saved settings to '{ConfigPath}'.");
    }

    private static object ToEditableDocument(SpeedModSettings settings)
    {
        return new
        {
            settings.Enabled,
            settings.BaseSpeed,
            settings.CombatOnly,
            settings.SpineSpeed,
            settings.QueueSpeed,
            settings.EffectSpeed,
            settings.CombatUiSpeed,
            settings.CombatVfxSpeed,
        };
    }

    private static object CreateLabel(string text)
    {
        var label = New("Godot.Label");
        SetText(label, text);
        return label;
    }

    private static object CreateActionButton(string text)
    {
        var button = New("Godot.Button");
        SetText(button, text);
        return button;
    }

    private static void BindPressed(object button, Action handler)
    {
        var pressed = button.GetType().GetEvent("Pressed");
        if (pressed is null || pressed.EventHandlerType is null)
        {
            RuntimePatchContext.LogAppliedOnce("ui-pressed-binding-missing", $"config ui could not bind Pressed event for '{button.GetType().FullName}'.");
            return;
        }

        var callback = Delegate.CreateDelegate(pressed.EventHandlerType, handler.Target, handler.Method);
        pressed.AddEventHandler(button, callback);
    }

    private static object New(string typeName)
    {
        var type = AccessTools.TypeByName(typeName)
            ?? throw new InvalidOperationException($"Unable to resolve runtime type '{typeName}'.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Unable to create runtime type '{typeName}'.");
    }

    private static void AddChild(object parent, object child)
    {
        var addChild = AccessTools.Method(parent.GetType(), "AddChild", new[] { child.GetType() })
            ?? parent.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "AddChild", StringComparison.Ordinal)
                    && method.GetParameters().Length > 0);

        if (addChild is null)
        {
            throw new MissingMethodException(parent.GetType().FullName, "AddChild");
        }

        var parameters = addChild.GetParameters();
        var args = new object?[parameters.Length];
        args[0] = child;
        for (var index = 1; index < parameters.Length; index++)
        {
            args[index] = parameters[index].HasDefaultValue
                ? parameters[index].DefaultValue
                : parameters[index].ParameterType.IsValueType
                    ? Activator.CreateInstance(parameters[index].ParameterType)
                    : null;
        }

        addChild.Invoke(parent, args);
    }

    private static object? Invoke(object target, string methodName, params object?[] args)
    {
        var method = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(candidate =>
            {
                if (candidate.ContainsGenericParameters)
                {
                    return false;
                }

                if (!string.Equals(candidate.Name, methodName, StringComparison.Ordinal))
                {
                    return false;
                }

                var parameters = candidate.GetParameters();
                return parameters.Length == args.Length;
            });

        return method?.Invoke(target, args);
    }

    private static object? GetMemberValue(object? target, string name)
    {
        if (target is null)
        {
            return null;
        }

        return AccessTools.Property(target.GetType(), name)?.GetValue(target)
            ?? AccessTools.Field(target.GetType(), name)?.GetValue(target);
    }

    private static void SetText(object control, string text)
    {
        var textProperty = AccessTools.Property(control.GetType(), "Text");
        if (textProperty is not null)
        {
            textProperty.SetValue(control, text);
            return;
        }

        var setText = control.GetType().GetMethod("SetTextAutoSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        setText?.Invoke(control, new object?[] { text });
    }

    private static void SetVisible(object control, bool visible)
    {
        var property = AccessTools.Property(control.GetType(), "Visible");
        property?.SetValue(control, visible);
    }

    private static void SetDescriptionVisible(object infoContainer, bool visible)
    {
        var description = AccessTools.Field(infoContainer.GetType(), "_description")?.GetValue(infoContainer);
        if (description is not null)
        {
            SetVisible(description, visible);
        }
    }

    private static void SetPosition(object control, float x, float y)
    {
        var property = AccessTools.Property(control.GetType(), "Position");
        property?.SetValue(control, CreateVector2(x, y));
    }

    private static void SetCustomMinimumSize(object control, float width, float height)
    {
        var property = AccessTools.Property(control.GetType(), "CustomMinimumSize");
        property?.SetValue(control, CreateVector2(width, height));
    }

    private static (float X, float Y) GetVector2(object control, string propertyName)
    {
        var property = AccessTools.Property(control.GetType(), propertyName);
        if (property?.GetValue(control) is not { } vector)
        {
            return default;
        }

        return
        (
            Convert.ToSingle(AccessTools.Property(vector.GetType(), "X")?.GetValue(vector) ?? 0f),
            Convert.ToSingle(AccessTools.Property(vector.GetType(), "Y")?.GetValue(vector) ?? 0f)
        );
    }

    private static object CreateVector2(float x, float y)
    {
        var type = AccessTools.TypeByName("Godot.Vector2")
            ?? throw new InvalidOperationException("Unable to resolve Godot.Vector2.");
        return Activator.CreateInstance(type, x, y)
            ?? throw new InvalidOperationException("Unable to create Godot.Vector2.");
    }

    private static double ClampAndRound(double value, double minimum, double maximum)
    {
        return Math.Round(Math.Clamp(value, minimum, maximum), 2, MidpointRounding.AwayFromZero);
    }

    private sealed class PanelState
    {
        public object? Root { get; set; }

        public object? EnabledButton { get; set; }

        public object? CombatOnlyButton { get; set; }

        public AdjustableRow[] Rows { get; set; } = Array.Empty<AdjustableRow>();
    }

    private sealed record AdjustableRow(
        string Title,
        object Root,
        object ValueLabel,
        Func<SpeedModSettings, double> Getter);
}

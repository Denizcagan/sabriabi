using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace ClickerApp;

internal enum ActivationMode
{
    Toggle,
    WhileKeyHeld
}

internal enum ClickTarget
{
    CurrentCursor,
    FixedPosition
}

internal enum MouseButtonChoice
{
    Left,
    Right,
    Middle,
    XButton1,
    XButton2
}

internal enum InputKind
{
    Keyboard,
    Mouse
}

internal enum AppLanguage
{
    English,
    Turkish
}

internal enum InputAction
{
    Click,
    ScrollUp,
    ScrollDown
}

internal enum ClickStyle
{
    Standard,
    Jitter,
    DragClick
}

internal enum MacroInputType
{
    Mouse,
    Keyboard
}

internal enum KeyboardStepAction
{
    Tap,
    Hold,
    Repeat,
    OpenApplication,
    Wait
}

internal sealed class KeyboardMacroStep
{
    public KeyboardStepAction Action { get; set; }
    public List<int> Keys { get; set; } = [];
    public int HoldMilliseconds { get; set; } = 100;
    public int IntervalMilliseconds { get; set; } = 100;
    public int RepeatCount { get; set; } = 1;
    public string ApplicationPath { get; set; } = "";

    public string DisplayName
    {
        get
        {
            if (Action == KeyboardStepAction.OpenApplication)
            {
                return $"Open: {Path.GetFileName(ApplicationPath)}";
            }

            if (Action == KeyboardStepAction.Wait)
            {
                return $"Wait ({HoldMilliseconds} ms)";
            }

            string keyNames = string.Join(" + ", Keys.Select(key =>
                Enum.IsDefined((Keys)key) ? ((Keys)key).ToString() : $"Key {key}"));
            return Action switch
            {
                KeyboardStepAction.Hold => $"Hold {keyNames} ({HoldMilliseconds} ms)",
                KeyboardStepAction.Repeat => $"Repeat {keyNames} × {RepeatCount}",
                _ => $"Tap {keyNames}"
            };
        }
    }
}

internal readonly record struct InputBinding(InputKind Kind, int Code)
{
    public static InputBinding Default => new(InputKind.Keyboard, (int)Keys.F6);

    public string DisplayName
    {
        get
        {
            if (Kind == InputKind.Keyboard)
            {
                return Enum.IsDefined((Keys)Code) ? ((Keys)Code).ToString() : $"Key {Code}";
            }

            return Code switch
            {
                (int)MouseButtonChoice.Left => "Mouse Left",
                (int)MouseButtonChoice.Right => "Mouse Right",
                (int)MouseButtonChoice.Middle => "Mouse Middle",
                (int)MouseButtonChoice.XButton1 => "Mouse X1",
                (int)MouseButtonChoice.XButton2 => "Mouse X2",
                _ => $"Mouse {Code}"
            };
        }
    }
}

internal sealed class ClickerSettings
{
    public decimal ClicksPerSecond { get; set; } = 10;
    public decimal DutyCyclePercent { get; set; } = 50;
    public MouseButtonChoice MouseButton { get; set; } = MouseButtonChoice.Left;
    public ActivationMode ActivationMode { get; set; } = ActivationMode.Toggle;
    public ClickTarget Target { get; set; } = ClickTarget.CurrentCursor;
    public InputBinding ActivationBinding { get; set; } = InputBinding.Default;
    public InputBinding EmergencyStopBinding { get; set; } = new(InputKind.Keyboard, (int)Keys.F12);
    public bool LimitClicks { get; set; }
    public int ClickLimit { get; set; } = 100;
    public int FixedX { get; set; }
    public int FixedY { get; set; }
    public InputAction InputAction { get; set; } = InputAction.Click;
    public ClickStyle ClickStyle { get; set; } = ClickStyle.Standard;
    public int JitterPixels { get; set; } = 2;
    public int TimingVariationPercent { get; set; } = 15;
    public List<string> AllowedApplications { get; set; } = [];
    public bool RestrictToSelectedApplications { get; set; }
    public bool StartWithWindows { get; set; }
    public string SelectedProfile { get; set; } = "Default";
    public List<ClickerProfile> Profiles { get; set; } = [];
    public List<MacroDefinition> Macros { get; set; } = [];
    public AppLanguage Language { get; set; } =
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr"
            ? AppLanguage.Turkish
            : AppLanguage.English;
}

internal sealed class ClickerProfile
{
    public string Name { get; set; } = "Default";
    public decimal ClicksPerSecond { get; set; } = 10;
    public decimal DutyCyclePercent { get; set; } = 50;
    public MouseButtonChoice MouseButton { get; set; } = MouseButtonChoice.Left;
    public ActivationMode ActivationMode { get; set; } = ActivationMode.Toggle;
    public ClickTarget Target { get; set; } = ClickTarget.CurrentCursor;
    public InputBinding ActivationBinding { get; set; } = InputBinding.Default;
    public bool LimitClicks { get; set; }
    public int ClickLimit { get; set; } = 100;
    public int FixedX { get; set; }
    public int FixedY { get; set; }
    public InputAction InputAction { get; set; } = InputAction.Click;
    public ClickStyle ClickStyle { get; set; } = ClickStyle.Standard;
    public int JitterPixels { get; set; } = 2;
    public int TimingVariationPercent { get; set; } = 15;
}

internal sealed class MacroDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Macro 1";
    public bool Enabled { get; set; } = true;
    public string ProfileName { get; set; } = "Default";
    public ClickerProfile Profile { get; set; } = new();
    public MacroInputType InputType { get; set; } = MacroInputType.Mouse;
    public List<KeyboardMacroStep> KeyboardSteps { get; set; } = [];
    public int MaxRuntimeSeconds { get; set; }
    public int MouseActionLimit { get; set; }
    public int KeyboardSequenceRepeats { get; set; } = 1;
    public decimal MaxClicksPerSecond { get; set; } = 100m;
}

internal static class SettingsStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClickerApp",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static ClickerSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new ClickerSettings();
        }

        string json = File.ReadAllText(SettingsPath);
        ClickerSettings settings = JsonSerializer.Deserialize<ClickerSettings>(json, JsonOptions)
            ?? throw new InvalidDataException("Settings file is empty or invalid.");

        settings.Macros ??= [];
        if (settings.EmergencyStopBinding == default)
        {
            settings.EmergencyStopBinding = new InputBinding(InputKind.Keyboard, (int)Keys.F12);
        }
        foreach (MacroDefinition macro in settings.Macros)
        {
            macro.KeyboardSteps ??= [];
        }
        if (settings.Macros.Count == 0)
        {
            settings.Macros.Add(CreateDefaultMacro(settings));
        }

        MigrateClickRates(settings);
        Validate(settings);
        return settings;
    }

    private static void MigrateClickRates(ClickerSettings settings)
    {
        settings.ClicksPerSecond = Math.Clamp(settings.ClicksPerSecond, 0.2m, 100m);
        if (settings.Profiles is not null)
        {
            foreach (ClickerProfile profile in settings.Profiles)
            {
                profile.ClicksPerSecond = Math.Clamp(profile.ClicksPerSecond, 0.2m, 100m);
            }
        }

        if (settings.Macros is not null)
        {
            foreach (MacroDefinition macro in settings.Macros)
            {
                if (macro.Profile is not null)
                {
                    macro.Profile.ClicksPerSecond = Math.Clamp(macro.Profile.ClicksPerSecond, 0.2m, 100m);
                }
            }
        }
    }

    public static void Save(ClickerSettings settings)
    {
        Validate(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    public static ClickerSettings Clone(ClickerSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        return JsonSerializer.Deserialize<ClickerSettings>(json, JsonOptions)
            ?? throw new InvalidDataException("Settings could not be copied.");
    }

    public static ClickerProfile CloneProfile(ClickerProfile profile)
    {
        string json = JsonSerializer.Serialize(profile, JsonOptions);
        return JsonSerializer.Deserialize<ClickerProfile>(json, JsonOptions)
            ?? throw new InvalidDataException("Profile could not be copied.");
    }

    public static ClickerProfile CreateProfile(ClickerSettings settings, string name) => new()
    {
        Name = name,
        ClicksPerSecond = settings.ClicksPerSecond,
        DutyCyclePercent = settings.DutyCyclePercent,
        MouseButton = settings.MouseButton,
        ActivationMode = settings.ActivationMode,
        Target = settings.Target,
        ActivationBinding = settings.ActivationBinding,
        LimitClicks = settings.LimitClicks,
        ClickLimit = settings.ClickLimit,
        FixedX = settings.FixedX,
        FixedY = settings.FixedY,
        InputAction = settings.InputAction,
        ClickStyle = settings.ClickStyle,
        JitterPixels = settings.JitterPixels,
        TimingVariationPercent = settings.TimingVariationPercent
    };

    public static MacroDefinition CreateDefaultMacro(ClickerSettings settings)
    {
        ClickerProfile profile = CreateProfile(settings, settings.SelectedProfile);
        return new MacroDefinition
        {
            Name = "Macro 1",
            ProfileName = profile.Name,
            Profile = profile
        };
    }

    public static void ApplyProfile(ClickerSettings settings, ClickerProfile profile)
    {
        settings.ClicksPerSecond = profile.ClicksPerSecond;
        settings.DutyCyclePercent = profile.DutyCyclePercent;
        settings.MouseButton = profile.MouseButton;
        settings.ActivationMode = profile.ActivationMode;
        settings.Target = profile.Target;
        settings.ActivationBinding = profile.ActivationBinding;
        settings.LimitClicks = profile.LimitClicks;
        settings.ClickLimit = profile.ClickLimit;
        settings.FixedX = profile.FixedX;
        settings.FixedY = profile.FixedY;
        settings.InputAction = profile.InputAction;
        settings.ClickStyle = profile.ClickStyle;
        settings.JitterPixels = profile.JitterPixels;
        settings.TimingVariationPercent = profile.TimingVariationPercent;
        settings.SelectedProfile = profile.Name;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static void Validate(ClickerSettings settings)
    {
        bool validBinding = settings.ActivationBinding.Kind switch
        {
            InputKind.Keyboard => settings.ActivationBinding.Code is > 0 and <= 255,
            InputKind.Mouse => Enum.IsDefined((MouseButtonChoice)settings.ActivationBinding.Code),
            _ => false
        };
        bool validEmergencyBinding = settings.EmergencyStopBinding.Kind switch
        {
            InputKind.Keyboard => settings.EmergencyStopBinding.Code is > 0 and <= 255,
            InputKind.Mouse => Enum.IsDefined((MouseButtonChoice)settings.EmergencyStopBinding.Code),
            _ => false
        };

        if (settings.ClicksPerSecond is < 0.2m or > 100m
            || settings.DutyCyclePercent is < 1m or > 100m
            || !Enum.IsDefined(settings.MouseButton)
            || !Enum.IsDefined(settings.ActivationMode)
            || !Enum.IsDefined(settings.Target)
            || !Enum.IsDefined(settings.Language)
            || !Enum.IsDefined(settings.InputAction)
            || !Enum.IsDefined(settings.ClickStyle)
            || settings.JitterPixels is < 0 or > 10
            || settings.TimingVariationPercent is < 0 or > 50
            || !validBinding
            || !validEmergencyBinding
            || settings.AllowedApplications is null
            || settings.Profiles is null
            || settings.Macros is null
            || settings.ClickLimit is < 1 or > 1_000_000)
        {
            throw new InvalidDataException("A setting is outside its supported range.");
        }

        if (settings.Profiles.Any(profile =>
            string.IsNullOrWhiteSpace(profile.Name)
            || profile.ClicksPerSecond is < 0.2m or > 100m
            || profile.DutyCyclePercent is < 1m or > 100m
            || !Enum.IsDefined(profile.MouseButton)
            || !Enum.IsDefined(profile.ActivationMode)
            || !Enum.IsDefined(profile.Target)
            || !Enum.IsDefined(profile.InputAction)
            || !Enum.IsDefined(profile.ClickStyle)
            || profile.JitterPixels is < 0 or > 10
            || profile.TimingVariationPercent is < 0 or > 50
            || profile.ClickLimit is < 1 or > 1_000_000
            || profile.ActivationBinding.Kind switch
            {
                InputKind.Keyboard => profile.ActivationBinding.Code is <= 0 or > 255,
                InputKind.Mouse => !Enum.IsDefined((MouseButtonChoice)profile.ActivationBinding.Code),
                _ => true
            }))
        {
            throw new InvalidDataException("A profile contains an invalid setting.");
        }

        if (settings.Macros.Count > 64 || settings.Macros.Any(macro => !IsValidMacro(macro)))
        {
            throw new InvalidDataException("A macro contains invalid settings or exceeds the supported count.");
        }

    }

    public static void ValidateMacroDefinition(MacroDefinition macro)
    {
        if (!IsValidMacro(macro))
        {
            throw new InvalidDataException("A macro contains invalid settings or exceeds the supported count.");
        }
    }

    private static bool IsValidMacro(MacroDefinition? macro) =>
        macro is not null
        && !string.IsNullOrWhiteSpace(macro.Id)
        && !string.IsNullOrWhiteSpace(macro.Name)
        && !string.IsNullOrWhiteSpace(macro.ProfileName)
        && macro.Profile is not null
        && Enum.IsDefined(macro.InputType)
        && macro.KeyboardSteps is { Count: <= 128 }
        && macro.MaxRuntimeSeconds is >= 0 and <= 86_400
        && macro.MouseActionLimit is >= 0 and <= 1_000_000
        && macro.KeyboardSequenceRepeats is >= 1 and <= 10_000
        && macro.MaxClicksPerSecond is >= 0.2m and <= 100m
        && macro.KeyboardSteps.All(step => IsValidKeyboardStep(step))
        && IsValidProfile(macro.Profile);

    private static bool IsValidKeyboardStep(KeyboardMacroStep? step) =>
        step is not null
        && Enum.IsDefined(step.Action)
        && (step.Action == KeyboardStepAction.OpenApplication
            ? !string.IsNullOrWhiteSpace(step.ApplicationPath)
            : step.Action == KeyboardStepAction.Wait
                ? step.HoldMilliseconds is >= 1 and <= 60_000
            : step.Keys is { Count: > 0 and <= 8 }
                && step.Keys.All(key => key is > 0 and <= 255)
                && step.HoldMilliseconds is >= 1 and <= 60_000
                && step.IntervalMilliseconds is >= 0 and <= 60_000
                && step.RepeatCount is >= 1 and <= 10_000);

    private static bool IsValidProfile(ClickerProfile profile)
    {
        bool validBinding = profile.ActivationBinding.Kind switch
        {
            InputKind.Keyboard => profile.ActivationBinding.Code is > 0 and <= 255,
            InputKind.Mouse => Enum.IsDefined((MouseButtonChoice)profile.ActivationBinding.Code),
            _ => false
        };

        return !string.IsNullOrWhiteSpace(profile.Name)
            && profile.ClicksPerSecond is >= 0.2m and <= 100m
            && profile.DutyCyclePercent is >= 1m and <= 100m
            && Enum.IsDefined(profile.MouseButton)
            && Enum.IsDefined(profile.ActivationMode)
            && Enum.IsDefined(profile.Target)
            && Enum.IsDefined(profile.InputAction)
            && Enum.IsDefined(profile.ClickStyle)
            && profile.JitterPixels is >= 0 and <= 10
            && profile.TimingVariationPercent is >= 0 and <= 50
            && profile.ClickLimit is >= 1 and <= 1_000_000
            && validBinding;
    }
}

using System.ComponentModel;
using Microsoft.Win32;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ClickerApp;

internal sealed class ClickerForm : Form
{
    private readonly ClickerSettings _settings;
    private readonly NumericUpDown _cpsInput = new();
    private readonly NumericUpDown _dutyInput = new();
    private readonly NumericUpDown _xInput = new();
    private readonly NumericUpDown _yInput = new();
    private readonly ComboBox _buttonInput = new();
    private readonly ComboBox _activationModeInput = new();
    private readonly ComboBox _targetInput = new();
    private readonly ComboBox _actionInput = new();
    private readonly ComboBox _languageInput = new();
    private readonly CheckBox _limitInput = new();
    private readonly NumericUpDown _countInput = new();
    private readonly Button _captureButton = new();
    private readonly Label _bindingLabel = new();
    private readonly Label _xLabel = new();
    private readonly Label _yLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _helpLabel = new();
    private readonly Button _startStopButton = new();
    private readonly Label _languageLabel = new();
    private readonly Label _cpsLabel = new();
    private readonly Label _dutyLabel = new();
    private readonly Label _buttonLabel = new();
    private readonly Label _activationModeLabel = new();
    private readonly Label _targetLabel = new();
    private readonly Label _positionRowLabel = new();
    private readonly Label _actionLabel = new();
    private readonly Label _limitRowLabel = new();
    private readonly GroupBox _clickGroup = new();
    private readonly GroupBox _activationGroup = new();
    private readonly GroupBox _targetGroup = new();
    private readonly Button _profilesButton = new();
    private readonly Button _settingsButton = new();
    private readonly Button _macrosButton = new();
    private Button _readButton = new();
    private readonly Label _macroInfoLabel = new();
    private readonly GlobalInputHook _inputHook;
    private readonly List<MacroRuntime> _macroRuntimes = [];

    private bool _captureInProgress;
    private bool _applyingLanguage;
    private bool _suspendTriggers;
    private long _lastStatusUpdateTimestamp;
    private bool _isRunning => _macroRuntimes.Any(runtime => runtime.IsRunning);

    public ClickerForm()
    {
        _settings = LoadSettings();
        if (_settings.Macros.Count == 0)
        {
            _settings.Macros.Add(SettingsStore.CreateDefaultMacro(_settings));
        }

        ConfigureWindow();
        BuildInterface();
        ApplySettingsToControls();
        ApplyLanguage();
        AttachControlEvents();

        _inputHook = new GlobalInputHook(_settings.ActivationBinding);
        _inputHook.InputChanged += OnInputChanged;
        RebuildMacroRuntimes();
        UpdateInterfaceState();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopAllMacros();
        DisposeMacroRuntimes();
        try
        {
            _inputHook.Dispose();
        }
        catch (Win32Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Input hook cleanup error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        SyncSettingsFromControls();
        try
        {
            SettingsStore.Save(_settings);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            MessageBox.Show(
                this,
                $"{TextFor("Settings could not be saved.", "Ayarlar kaydedilemedi.")} {exception.Message}",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        base.OnFormClosing(e);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_settings.StartWithWindows)
        {
            return;
        }

        try
        {
            ApplyStartupSetting(true);
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            MessageBox.Show(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static ClickerSettings LoadSettings()
    {
        try
        {
            return SettingsStore.Load();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            MessageBox.Show(
                $"Settings could not be loaded; defaults will be used. {exception.Message}",
                "Auto Clicker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return new ClickerSettings();
        }
    }

    private string TextFor(string english, string turkish) =>
        _settings.Language == AppLanguage.Turkish ? turkish : english;

    private void ConfigureWindow()
    {
        Text = $"Sabri Abi v{Application.ProductVersion}";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(500, 320);
        Size = new Size(560, 370);
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9F);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
    }

    private void BuildInterface()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(14),
            AutoScroll = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < layout.RowCount; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        Controls.Add(layout);

        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        ConfigureToolbarButton(_profilesButton, "☷ Profiles");
        ConfigureToolbarButton(_settingsButton, "⚙ Settings");
        ConfigureToolbarButton(_macrosButton, "▤ Macros");
        _readButton = DisclaimerDialog.CreateReadButton(_settings.Language == AppLanguage.Turkish);
        _profilesButton.Click += OnProfilesClick;
        _settingsButton.Click += OnSettingsClick;
        _macrosButton.Click += OnMacrosClick;
        ConfigureModernButton(_profilesButton);
        ConfigureModernButton(_settingsButton);
        ConfigureModernButton(_macrosButton);
        toolbar.Controls.Add(_profilesButton);
        toolbar.Controls.Add(_macrosButton);
        toolbar.Controls.Add(_settingsButton);
        toolbar.Controls.Add(_readButton);
        layout.Controls.Add(toolbar, 0, 0);

        var languagePanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        languagePanel.Controls.Add(_languageLabel);
        languagePanel.Controls.Add(_languageInput);
        layout.Controls.Add(languagePanel, 0, 1);

        _statusLabel.AutoSize = true;
        _statusLabel.Font = new Font(Font, FontStyle.Bold);
        _statusLabel.Margin = new Padding(3, 5, 3, 10);
        layout.Controls.Add(_statusLabel, 0, 2);

        _macroInfoLabel.AutoSize = true;
        _macroInfoLabel.MaximumSize = new Size(540, 0);
        _macroInfoLabel.Margin = new Padding(3, 5, 3, 14);
        _macroInfoLabel.ForeColor = AppTheme.MutedText;
        layout.Controls.Add(_macroInfoLabel, 0, 3);

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        _startStopButton.AutoSize = true;
        _startStopButton.MinimumSize = new Size(140, 38);
        _startStopButton.Click += OnStartStopButtonClick;
        bottomPanel.Controls.Add(_startStopButton);
        _helpLabel.AutoSize = true;
        _helpLabel.MaximumSize = new Size(520, 0);
        bottomPanel.Controls.Add(_helpLabel);
        layout.Controls.Add(bottomPanel, 0, 4);

        ConfigureInputs();
        AppTheme.Apply(this);
        AppTheme.StylePrimaryButton(_startStopButton);
    }

    private void ConfigureInputs()
    {
        _cpsInput.Minimum = 0.2m;
        _cpsInput.Maximum = 100m;
        _cpsInput.Increment = 0.1m;
        _cpsInput.DecimalPlaces = 1;
        _cpsInput.Width = 130;

        _dutyInput.Minimum = 1;
        _dutyInput.Maximum = 100;
        _dutyInput.Increment = 1;
        _dutyInput.DecimalPlaces = 0;
        _dutyInput.Width = 130;

        _countInput.Minimum = 1;
        _countInput.Maximum = 1_000_000;
        _countInput.Value = 100;
        _countInput.Width = 110;

        _captureButton.AutoSize = true;
        _bindingLabel.AutoSize = true;
        _bindingLabel.Margin = new Padding(8, 8, 0, 0);

        foreach (NumericUpDown coordinateInput in new[] { _xInput, _yInput })
        {
            coordinateInput.Minimum = -32768;
            coordinateInput.Maximum = 32767;
            coordinateInput.Width = 100;
        }

        ConfigureCombo(_languageInput, ["English", "Türkçe"], 95);
        ConfigureCombo(_buttonInput, ["Left", "Right", "Middle", "X button 1", "X button 2"], 220);
        ConfigureCombo(_activationModeInput, ["Toggle start/stop", "Run while key is held"], 280);
        ConfigureCombo(_targetInput, ["Current cursor position", "Fixed coordinates"], 240);
        ConfigureCombo(_actionInput, ["Mouse click", "Scroll up", "Scroll down"], 220);
    }

    private static void ConfigureCombo(ComboBox comboBox, string[] items, int width)
    {
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Width = width;
        comboBox.Items.AddRange(items);
    }

    private static TableLayoutPanel CreateSettingsTable()
    {
        var table = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(4)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }

    private static void AddRow(TableLayoutPanel table, Label label, Control control)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        label.AutoSize = true;
        label.Anchor = AnchorStyles.Left;
        label.Margin = new Padding(3, 7, 8, 7);
        table.Controls.Add(label, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static void ConfigureGroup(GroupBox group, TableLayoutPanel content)
    {
        group.AutoSize = true;
        group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        group.Dock = DockStyle.Top;
        group.Padding = new Padding(8);
        group.Margin = new Padding(0, 0, 0, 8);
        group.Controls.Add(content);
    }

    private Control CreateCountPanel()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
        panel.Controls.Add(_limitInput);
        panel.Controls.Add(_countInput);
        return panel;
    }

    private Control CreateCapturePanel()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
        panel.Controls.Add(_captureButton);
        panel.Controls.Add(_bindingLabel);
        return panel;
    }

    private Control CreatePositionPanel()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
        panel.Controls.Add(_xLabel);
        panel.Controls.Add(_xInput);
        panel.Controls.Add(_yLabel);
        panel.Controls.Add(_yInput);
        return panel;
    }

    private void ApplySettingsToControls()
    {
        _cpsInput.Value = _settings.ClicksPerSecond;
        _dutyInput.Value = _settings.DutyCyclePercent;
        _buttonInput.SelectedIndex = (int)_settings.MouseButton;
        _activationModeInput.SelectedIndex = (int)_settings.ActivationMode;
        _targetInput.SelectedIndex = (int)_settings.Target;
        _actionInput.SelectedIndex = (int)_settings.InputAction;
        _languageInput.SelectedIndex = (int)_settings.Language;
        _limitInput.Checked = _settings.LimitClicks;
        _countInput.Value = _settings.ClickLimit;
        _xInput.Value = Math.Clamp(_settings.FixedX, (int)_xInput.Minimum, (int)_xInput.Maximum);
        _yInput.Value = Math.Clamp(_settings.FixedY, (int)_yInput.Minimum, (int)_yInput.Maximum);
        UpdateBindingLabel();
        UpdatePositionLabel();
    }

    private void ApplyLanguage()
    {
        _applyingLanguage = true;
        try
        {
            bool turkish = _settings.Language == AppLanguage.Turkish;
            _languageLabel.Text = turkish ? "Dil:" : "Language:";
            _macroInfoLabel.Text = turkish
                ? $"Makrolar ayrı ayrı çalışır. Acil durdurma: {_settings.EmergencyStopBinding.DisplayName}."
                : $"Macros run independently. Emergency stop: {_settings.EmergencyStopBinding.DisplayName}.";
            _cpsLabel.Text = turkish ? "Saniyedeki tıklama (CPS)" : "Clicks per second";
            _dutyLabel.Text = turkish ? "Tıklama görev döngüsü (%)" : "Click duty cycle (%)";
            _limitRowLabel.Text = turkish ? "Tıklama limiti" : "Click limit";
            _actionLabel.Text = turkish ? "Eylem" : "Action";
            _xLabel.Text = "X:";
            _yLabel.Text = "Y:";
            _limitInput.Text = turkish ? "Tıklama limitini etkinleştir" : "Enable click limit";
            _captureButton.Text = _captureInProgress
                ? (turkish ? "Dinleniyor... (Esc ile iptal)" : "Listening... (Esc to cancel)")
                : (turkish ? "Tuş seç" : "Select key");

            SetComboItems(_languageInput, ["English", "Türkçe"], (int)_settings.Language);
            UpdateBindingLabel();
            UpdateInterfaceState();
        }
        finally
        {
            _applyingLanguage = false;
        }
    }

    private static void SetComboItems(ComboBox combo, string[] items, int selectedIndex)
    {
        combo.BeginUpdate();
        combo.Items.Clear();
        combo.Items.AddRange(items);
        combo.SelectedIndex = selectedIndex;
        combo.EndUpdate();
    }

    private void AttachControlEvents()
    {
        _languageInput.SelectedIndexChanged += (_, _) =>
        {
            if (_applyingLanguage)
            {
                return;
            }

            _settings.Language = (AppLanguage)_languageInput.SelectedIndex;
            ApplyLanguage();
        };
        _cpsInput.ValueChanged += (_, _) =>
        {
            _settings.ClicksPerSecond = _cpsInput.Value;
        };
        _dutyInput.ValueChanged += (_, _) =>
        {
            _settings.DutyCyclePercent = _dutyInput.Value;
        };
        _activationModeInput.SelectedIndexChanged += (_, _) =>
        {
            if (_applyingLanguage)
            {
                return;
            }

            if (_isRunning)
            {
                StopAutomation();
            }

            _settings.ActivationMode = (ActivationMode)_activationModeInput.SelectedIndex;
            _inputHook.ResetActivationState();
            UpdateInterfaceState();
        };
        _actionInput.SelectedIndexChanged += (_, _) =>
        {
            if (_applyingLanguage)
            {
                return;
            }

            if (_isRunning)
            {
                StopAutomation();
            }

            _settings.InputAction = (InputAction)_actionInput.SelectedIndex;
            UpdateInterfaceState();
        };
        _targetInput.SelectedIndexChanged += (_, _) =>
        {
            if (_applyingLanguage)
            {
                return;
            }

            _settings.Target = (ClickTarget)_targetInput.SelectedIndex;
            UpdateInterfaceState();
        };
        _limitInput.CheckedChanged += (_, _) =>
        {
            _settings.LimitClicks = _limitInput.Checked;
            UpdateInterfaceState();
        };
        _countInput.ValueChanged += (_, _) =>
        {
            _settings.ClickLimit = decimal.ToInt32(_countInput.Value);
        };
        _xInput.ValueChanged += (_, _) =>
        {
            _settings.FixedX = decimal.ToInt32(_xInput.Value);
            UpdatePositionLabel();
        };
        _yInput.ValueChanged += (_, _) =>
        {
            _settings.FixedY = decimal.ToInt32(_yInput.Value);
            UpdatePositionLabel();
        };
        _captureButton.Click += OnCaptureButtonClick;
    }

    private void OnInputChanged(InputBinding binding, bool isDown, bool isCaptured)
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(() => ProcessInputChanged(binding, isDown, isCaptured));
        }
        catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
        {
        }
    }

    private void ProcessInputChanged(InputBinding binding, bool isDown, bool isCaptured)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (binding == _settings.EmergencyStopBinding)
        {
            if (isDown)
            {
                StopAllMacros();
                _statusLabel.Text = TextFor("Emergency stop activated.", "Acil durdurma etkinleştirildi.");
                _statusLabel.ForeColor = AppTheme.Warning;
            }

            return;
        }

        if (isCaptured)
        {
            if (binding.Kind == InputKind.Keyboard && binding.Code == (int)Keys.Escape)
            {
                _captureInProgress = false;
                _inputHook.IsCapturing = false;
                UpdateInterfaceState();
                return;
            }

            _captureInProgress = false;
            _inputHook.IsCapturing = false;
            _settings.ActivationBinding = binding;
            _inputHook.ActivationBinding = binding;
            _inputHook.ResetActivationState();
            UpdateBindingLabel();
            UpdateInterfaceState();
            return;
        }

        if (_suspendTriggers)
        {
            return;
        }

        if (!IsForegroundApplicationAllowed() && isDown)
        {
            _statusLabel.Text = TextFor(
                "Blocked: active application is not selected.",
                "Engellendi: etkin uygulama seçili değil.");
            _statusLabel.ForeColor = AppTheme.Warning;
            return;
        }

        foreach (MacroRuntime runtime in _macroRuntimes.Where(runtime =>
            runtime.Definition.Enabled && runtime.Profile.ActivationBinding == binding).ToArray())
        {
            if (isDown)
            {
                StartMacro(runtime);
            }
            else
            {
                StopMacro(runtime);
            }
        }

        UpdateInterfaceState();
    }

    private void OnCaptureButtonClick(object? sender, EventArgs e)
    {
        _captureInProgress = !_captureInProgress;
        _inputHook.IsCapturing = _captureInProgress;
        UpdateInterfaceState();
    }

    private void OnStartStopButtonClick(object? sender, EventArgs e) => ToggleAutomation();

    private static void ConfigureToolbarButton(Button button, string text)
    {
        button.Text = text;
        button.AutoSize = true;
        button.MinimumSize = new Size(112, 34);
        button.FlatStyle = FlatStyle.Flat;
    }

    private static void ConfigureModernButton(Button button)
    {
        button.BackColor = AppTheme.Surface;
        button.ForeColor = AppTheme.Text;
        button.FlatAppearance.BorderColor = AppTheme.Border;
        button.FlatAppearance.MouseOverBackColor = AppTheme.AccentTint;
        button.Cursor = Cursors.Hand;
    }

    private void OnProfilesClick(object? sender, EventArgs e)
    {
        SyncSettingsFromControls();
        _suspendTriggers = true;
        try
        {
            using var dialog = new ClickerProfileForm(_settings);
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultSettings is not null)
            {
                ApplySettingsSnapshot(dialog.ResultSettings);
            }
        }
        finally
        {
            _suspendTriggers = false;
        }
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        SyncSettingsFromControls();
        _suspendTriggers = true;
        try
        {
            using var dialog = new ClickerOptionsForm(_settings, ApplySettingsSnapshot);
            dialog.ShowDialog(this);
        }
        finally
        {
            _suspendTriggers = false;
        }
    }

    private void OnMacrosClick(object? sender, EventArgs e)
    {
        SyncSettingsFromControls();
        _suspendTriggers = true;
        try
        {
            using var dialog = new MacroManagerForm(_settings);
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultMacros is not null)
            {
                StopAllMacros();
                _settings.Macros = dialog.ResultMacros;
                _settings.EmergencyStopBinding = dialog.EmergencyStopBinding;
                RebuildMacroRuntimes();
                SaveSettings();
                UpdateInterfaceState();
            }
        }
        finally
        {
            _suspendTriggers = false;
        }
    }

    private void ApplySettingsSnapshot(ClickerSettings source)
    {
        _settings.ClicksPerSecond = source.ClicksPerSecond;
        _settings.DutyCyclePercent = source.DutyCyclePercent;
        _settings.MouseButton = source.MouseButton;
        _settings.ActivationMode = source.ActivationMode;
        _settings.Target = source.Target;
        _settings.ActivationBinding = source.ActivationBinding;
        _settings.EmergencyStopBinding = source.EmergencyStopBinding;
        _settings.LimitClicks = source.LimitClicks;
        _settings.ClickLimit = source.ClickLimit;
        _settings.FixedX = source.FixedX;
        _settings.FixedY = source.FixedY;
        _settings.InputAction = source.InputAction;
        _settings.ClickStyle = source.ClickStyle;
        _settings.JitterPixels = source.JitterPixels;
        _settings.TimingVariationPercent = source.TimingVariationPercent;
        _settings.Language = source.Language;
        _settings.AllowedApplications = source.AllowedApplications;
        _settings.RestrictToSelectedApplications = source.RestrictToSelectedApplications;
        _settings.StartWithWindows = source.StartWithWindows;
        _settings.SelectedProfile = source.SelectedProfile;
        _settings.Profiles = source.Profiles;
        _settings.Macros = source.Macros;

        _inputHook.ActivationBinding = _settings.ActivationBinding;
        _inputHook.ResetActivationState();
        StopAllMacros();
        RebuildMacroRuntimes();
        ApplySettingsToControls();
        ApplyLanguage();

        try
        {
            ApplyStartupSetting(_settings.StartWithWindows);
            SaveSettings();
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            MessageBox.Show(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void ApplyStartupSetting(bool enabled)
    {
        using RegistryKey runKey = Registry.CurrentUser.CreateSubKey(
            "Software\\Microsoft\\Windows\\CurrentVersion\\Run",
            writable: true)
            ?? throw new IOException("Unable to open the current-user startup registry key.");
        const string valueName = "SabriAbi";
        if (enabled)
        {
            runKey.SetValue(valueName, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            runKey.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }

    private void ToggleAutomation()
    {
        if (_isRunning)
        {
            StopAllMacros();
        }
        else
        {
            StartAllMacros();
        }
    }

    private void StartAllMacros()
    {
        if (_captureInProgress)
        {
            return;
        }

        if (!IsForegroundApplicationAllowed())
        {
            _statusLabel.Text = TextFor(
                "Blocked: active application is not selected.",
                "Engellendi: etkin uygulama seçili değil.");
            _statusLabel.ForeColor = AppTheme.Warning;
            return;
        }

        foreach (MacroRuntime runtime in _macroRuntimes.Where(runtime => runtime.Definition.Enabled))
        {
            StartMacro(runtime);
        }

        UpdateInterfaceState();
    }

    private void StopAllMacros()
    {
        foreach (MacroRuntime runtime in _macroRuntimes)
        {
            StopMacro(runtime);
        }

        UpdateInterfaceState();
    }

    private void StopAutomation() => StopAllMacros();

    private void StartMacro(MacroRuntime runtime)
    {
        lock (runtime.SyncRoot)
        {
            if (runtime.IsRunning || !runtime.Definition.Enabled || _captureInProgress
                || !IsForegroundApplicationAllowed())
            {
                return;
            }

            runtime.CompletedClicks = 0;
            runtime.CurrentStep = 0;
            runtime.CurrentSequence = 0;
            runtime.LastProgressUiTimestamp = 0;
            runtime.StopAfterCurrentClick = false;
            runtime.IsRunning = true;
            runtime.LimitTimer.Change(
                runtime.Definition.MaxRuntimeSeconds > 0
                    ? TimeSpan.FromSeconds(runtime.Definition.MaxRuntimeSeconds)
                    : Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);
            if (runtime.Definition.InputType == MacroInputType.Keyboard)
            {
                var cancellation = new CancellationTokenSource();
                runtime.KeyboardCancellation = cancellation;
                runtime.KeyboardTask = Task.Run(() => RunKeyboardMacroAsync(runtime, cancellation));
            }
            else
            {
                try
                {
                    PerformMacroAction(runtime);
                }
                catch (Win32Exception exception)
                {
                    StopMacro(runtime);
                    ShowInputError(exception);
                }
            }
        }

        UpdateInterfaceState();
    }

    private void StopMacro(MacroRuntime runtime)
    {
        lock (runtime.SyncRoot)
        {
            if (runtime.IsDisposed)
            {
                return;
            }

            runtime.ClickTimer.Change(Timeout.Infinite, Timeout.Infinite);
            runtime.DutyTimer.Change(Timeout.Infinite, Timeout.Infinite);
            runtime.LimitTimer.Change(Timeout.Infinite, Timeout.Infinite);
            runtime.StopAfterCurrentClick = false;
            runtime.IsRunning = false;
            runtime.KeyboardCancellation?.Cancel();
            try
            {
                ReleaseMacroButton(runtime);
            }
            catch (Win32Exception exception)
            {
                ShowInputError(exception);
            }
        }

        UpdateInterfaceState();
    }

    private async Task RunKeyboardMacroAsync(MacroRuntime runtime, CancellationTokenSource cancellation)
    {
        CancellationToken token = cancellation.Token;
        var pressedKeys = new HashSet<int>();
        Exception? failure = null;
        try
        {
            for (int sequence = 0; sequence < runtime.Definition.KeyboardSequenceRepeats; sequence++)
            {
                runtime.CurrentSequence = sequence + 1;
                for (int index = 0; index < runtime.Definition.KeyboardSteps.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    if (!IsForegroundApplicationAllowed())
                    {
                        return;
                    }

                    runtime.CurrentStep = index + 1;
                    RequestKeyboardProgressUpdate(runtime);
                    KeyboardMacroStep step = runtime.Definition.KeyboardSteps[index];
                    if (step.Action == KeyboardStepAction.OpenApplication)
                    {
                        if (Process.Start(new ProcessStartInfo
                        {
                            FileName = step.ApplicationPath,
                            UseShellExecute = true
                        }) is null)
                        {
                            throw new InvalidOperationException("The selected application could not be started.");
                        }
                    }
                    else if (step.Action == KeyboardStepAction.Wait)
                    {
                        await Task.Delay(step.HoldMilliseconds, token).ConfigureAwait(false);
                    }
                    else
                    {
                        int repetitions = step.Action == KeyboardStepAction.Repeat ? step.RepeatCount : 1;
                        for (int repeat = 0; repeat < repetitions; repeat++)
                        {
                            token.ThrowIfCancellationRequested();
                            KeyboardInput.SetKeys(step.Keys, true);
                            pressedKeys.UnionWith(step.Keys);
                            await Task.Delay(step.HoldMilliseconds, token).ConfigureAwait(false);
                            KeyboardInput.SetKeys(step.Keys, false);
                            pressedKeys.ExceptWith(step.Keys);
                            if (repeat + 1 < repetitions && step.IntervalMilliseconds > 0)
                            {
                                await Task.Delay(step.IntervalMilliseconds, token).ConfigureAwait(false);
                            }
                        }
                    }

                    runtime.CompletedClicks++;
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException or IOException
                or UnauthorizedAccessException or ArgumentException)
        {
            failure = exception;
        }
        finally
        {
            try
            {
                if (pressedKeys.Count > 0)
                {
                    KeyboardInput.SetKeys(pressedKeys, false);
                }
            }
            catch (Win32Exception exception)
            {
                failure ??= exception;
            }

            lock (runtime.SyncRoot)
            {
                if (ReferenceEquals(runtime.KeyboardCancellation, cancellation))
                {
                    runtime.KeyboardCancellation = null;
                    runtime.KeyboardTask = null;
                    runtime.IsRunning = false;
                    if (!runtime.IsDisposed)
                    {
                        runtime.LimitTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }

            cancellation.Dispose();
            if (!IsDisposed && !Disposing && IsHandleCreated)
            {
                TryBeginInvoke(() =>
                {
                    UpdateInterfaceState();
                    if (failure is Win32Exception inputException)
                    {
                        ShowInputError(inputException);
                    }
                    else if (failure is not null)
                    {
                        MessageBox.Show(this, failure.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
        }
    }

    private void OnMacroClickTimerTick(MacroRuntime runtime)
    {
        if (Interlocked.Exchange(ref runtime.ClickCallbackRunning, 1) != 0)
        {
            return;
        }

        try
        {
            lock (runtime.SyncRoot)
            {
                if (!runtime.IsRunning)
                {
                    return;
                }

                if (ShouldCheckApplicationScope(runtime) && !IsForegroundApplicationAllowed())
                {
                    StopMacro(runtime);
                    return;
                }

                PerformMacroAction(runtime);
            }
        }
        catch (Win32Exception exception)
        {
            StopMacro(runtime);
            ShowInputError(exception);
        }
        finally
        {
            Volatile.Write(ref runtime.ClickCallbackRunning, 0);
        }
    }

    private void OnMacroRuntimeLimit(MacroRuntime runtime)
    {
        lock (runtime.SyncRoot)
        {
            if (!runtime.IsRunning)
            {
                return;
            }

            StopMacro(runtime);
        }
    }

    private void RequestKeyboardProgressUpdate(MacroRuntime runtime)
    {
        long now = Stopwatch.GetTimestamp();
        long lastUpdate = Volatile.Read(ref runtime.LastProgressUiTimestamp);
        if (Stopwatch.GetElapsedTime(lastUpdate, now) >= TimeSpan.FromMilliseconds(250)
            && Interlocked.CompareExchange(ref runtime.LastProgressUiTimestamp, now, lastUpdate) == lastUpdate)
        {
            TryBeginInvoke(UpdateInterfaceState);
        }
    }

    private void PerformMacroAction(MacroRuntime runtime)
    {
        ClickerProfile profile = runtime.Profile;
        if (runtime.MouseButtonHeld)
        {
            runtime.DutyTimer.Change(Timeout.Infinite, Timeout.Infinite);
            ReleaseMacroButton(runtime);
        }

        if (profile.Target == ClickTarget.FixedPosition)
        {
            MouseInput.MoveTo(profile.FixedX, profile.FixedY);
        }

        if (profile.InputAction is InputAction.ScrollUp or InputAction.ScrollDown)
        {
            MouseInput.Scroll(profile.InputAction == InputAction.ScrollUp ? 120 : -120);
        }
        else
        {
            if (profile.ClickStyle == ClickStyle.Jitter && profile.JitterPixels > 0)
            {
                Point origin = profile.Target == ClickTarget.FixedPosition
                    ? new Point(profile.FixedX, profile.FixedY)
                    : MouseInput.GetCursorPosition();
                int offsetX = Random.Shared.Next(-profile.JitterPixels, profile.JitterPixels + 1);
                int offsetY = Random.Shared.Next(-profile.JitterPixels, profile.JitterPixels + 1);
                runtime.JitterRestorePosition = origin;
                runtime.RestoreAfterJitter = true;
                Rectangle screen = SystemInformation.VirtualScreen;
                MouseInput.MoveTo(
                    Math.Clamp(origin.X + offsetX, screen.Left, screen.Right - 1),
                    Math.Clamp(origin.Y + offsetY, screen.Top, screen.Bottom - 1));
            }

            MouseInput.SetButton(profile.MouseButton, true);
            runtime.MouseButtonHeld = true;
            runtime.DutyTimer.Change(GetDutyInterval(profile, GetEffectiveClicksPerSecond(runtime)), Timeout.Infinite);
        }

        runtime.CompletedClicks++;

        int clickLimit = profile.LimitClicks ? profile.ClickLimit : 0;
        int actionLimit = runtime.Definition.MouseActionLimit;
        bool reachedActionLimit = (clickLimit > 0 && runtime.CompletedClicks >= clickLimit)
            || (actionLimit > 0 && runtime.CompletedClicks >= actionLimit);
        if (reachedActionLimit)
        {
            runtime.ClickTimer.Change(Timeout.Infinite, Timeout.Infinite);
            runtime.StopAfterCurrentClick = true;
            if (!runtime.MouseButtonHeld)
            {
                StopMacro(runtime);
            }
        }
        else
        {
            runtime.ClickTimer.Change(GetClickInterval(profile, GetEffectiveClicksPerSecond(runtime)), Timeout.Infinite);
        }

        long now = Stopwatch.GetTimestamp();
        long lastUpdate = Volatile.Read(ref _lastStatusUpdateTimestamp);
        if (Stopwatch.GetElapsedTime(lastUpdate, now) >= TimeSpan.FromSeconds(1)
            && Interlocked.CompareExchange(ref _lastStatusUpdateTimestamp, now, lastUpdate) == lastUpdate)
        {
            UpdateInterfaceState();
        }
    }

    private bool ShouldCheckApplicationScope(MacroRuntime runtime)
    {
        if (!_settings.RestrictToSelectedApplications)
        {
            return false;
        }

        long now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(runtime.LastScopeCheckTimestamp, now) < TimeSpan.FromMilliseconds(100))
        {
            return false;
        }

        runtime.LastScopeCheckTimestamp = now;
        return true;
    }

    private void OnMacroDutyTimerTick(MacroRuntime runtime)
    {
        try
        {
            lock (runtime.SyncRoot)
            {
                if (!runtime.IsRunning)
                {
                    return;
                }

                runtime.DutyTimer.Change(Timeout.Infinite, Timeout.Infinite);
                ReleaseMacroButton(runtime);
                if (runtime.StopAfterCurrentClick)
                {
                    StopMacro(runtime);
                }
            }
        }
        catch (Win32Exception exception)
        {
            StopMacro(runtime);
            ShowInputError(exception);
        }
    }

    private static void ReleaseMacroButton(MacroRuntime runtime)
    {
        if (runtime.MouseButtonHeld)
        {
            MouseInput.SetButton(runtime.Profile.MouseButton, false);
            runtime.MouseButtonHeld = false;
        }

        if (runtime.RestoreAfterJitter)
        {
            MouseInput.MoveTo(runtime.JitterRestorePosition.X, runtime.JitterRestorePosition.Y);
            runtime.RestoreAfterJitter = false;
        }
    }

    private static int GetClickInterval(ClickerProfile profile, decimal clicksPerSecond)
    {
        int baseInterval = Math.Max(10, decimal.ToInt32(decimal.Round(
            1000m / clicksPerSecond, 0, MidpointRounding.AwayFromZero)));
        if (profile.TimingVariationPercent == 0)
        {
            return baseInterval;
        }

        int variation = baseInterval * profile.TimingVariationPercent / 100;
        return Math.Max(10, baseInterval + Random.Shared.Next(-variation, variation + 1));
    }

    private static int GetDutyInterval(ClickerProfile profile, decimal clicksPerSecond)
    {
        decimal duty = profile.ClickStyle == ClickStyle.DragClick
            ? Math.Min(profile.DutyCyclePercent, 15m)
            : profile.DutyCyclePercent;
        return Math.Max(1, decimal.ToInt32(decimal.Round(
            1000m / clicksPerSecond * duty / 100m,
            0,
            MidpointRounding.AwayFromZero)));
    }

    private static decimal GetEffectiveClicksPerSecond(MacroRuntime runtime) =>
        Math.Min(runtime.Profile.ClicksPerSecond, runtime.Definition.MaxClicksPerSecond);

    private void RebuildMacroRuntimes()
    {
        DisposeMacroRuntimes();
        foreach (MacroDefinition definition in _settings.Macros)
        {
            var runtime = new MacroRuntime(definition);
            runtime.ClickTimer = new System.Threading.Timer(
                _ => OnMacroClickTimerTick(runtime),
                null,
                Timeout.Infinite,
                Timeout.Infinite);
            runtime.DutyTimer = new System.Threading.Timer(
                _ => OnMacroDutyTimerTick(runtime),
                null,
                Timeout.Infinite,
                Timeout.Infinite);
            runtime.LimitTimer = new System.Threading.Timer(
                _ => OnMacroRuntimeLimit(runtime),
                null,
                Timeout.Infinite,
                Timeout.Infinite);
            _macroRuntimes.Add(runtime);
        }
    }

    private void DisposeMacroRuntimes()
    {
        foreach (MacroRuntime runtime in _macroRuntimes)
        {
            runtime.Dispose();
        }

        _macroRuntimes.Clear();
    }

    private void SaveSettings()
    {
        try
        {
            SettingsStore.Save(_settings);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            MessageBox.Show(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private sealed class MacroRuntime : IDisposable
    {
        public MacroRuntime(MacroDefinition definition)
        {
            Definition = definition;
        }

        public MacroDefinition Definition { get; }
        public ClickerProfile Profile => Definition.Profile;
        public object SyncRoot { get; } = new();
        public System.Threading.Timer ClickTimer { get; set; } = null!;
        public System.Threading.Timer DutyTimer { get; set; } = null!;
        public System.Threading.Timer LimitTimer { get; set; } = null!;
        public int ClickCallbackRunning;
        public long LastScopeCheckTimestamp;
        public bool IsRunning { get; set; }
        public bool MouseButtonHeld { get; set; }
        public bool StopAfterCurrentClick { get; set; }
        public int CompletedClicks { get; set; }
        public int CurrentStep { get; set; }
        public int CurrentSequence { get; set; }
        public long LastProgressUiTimestamp;
        public Point JitterRestorePosition { get; set; }
        public bool RestoreAfterJitter { get; set; }
        public bool IsDisposed { get; private set; }
        public CancellationTokenSource? KeyboardCancellation { get; set; }
        public Task? KeyboardTask { get; set; }

        public void Dispose()
        {
            lock (SyncRoot)
            {
                IsDisposed = true;
                KeyboardCancellation?.Cancel();
                ClickTimer.Dispose();
                DutyTimer.Dispose();
                LimitTimer.Dispose();
            }
        }
    }

    private void UpdatePositionLabel()
    {
        _xInput.Value = Math.Clamp(_settings.FixedX, (int)_xInput.Minimum, (int)_xInput.Maximum);
        _yInput.Value = Math.Clamp(_settings.FixedY, (int)_yInput.Minimum, (int)_yInput.Maximum);
    }

    private void UpdateBindingLabel()
    {
        _bindingLabel.Text = _settings.ActivationBinding.Kind == InputKind.Mouse
            && _settings.Language == AppLanguage.Turkish
                ? (MouseButtonChoice)_settings.ActivationBinding.Code switch
                {
                    MouseButtonChoice.Left => "Sol fare",
                    MouseButtonChoice.Right => "Sağ fare",
                    MouseButtonChoice.Middle => "Orta fare",
                    MouseButtonChoice.XButton1 => "Fare X1",
                    MouseButtonChoice.XButton2 => "Fare X2",
                    _ => _settings.ActivationBinding.DisplayName
                }
                : _settings.ActivationBinding.DisplayName;
    }

    private void SyncSettingsFromControls()
    {
        _settings.ClicksPerSecond = _cpsInput.Value;
        _settings.DutyCyclePercent = _dutyInput.Value;
        _settings.MouseButton = (MouseButtonChoice)_buttonInput.SelectedIndex;
        _settings.ActivationMode = (ActivationMode)_activationModeInput.SelectedIndex;
        _settings.Target = (ClickTarget)_targetInput.SelectedIndex;
        _settings.LimitClicks = _limitInput.Checked;
        _settings.ClickLimit = decimal.ToInt32(_countInput.Value);
        _settings.Language = (AppLanguage)_languageInput.SelectedIndex;
        _settings.FixedX = decimal.ToInt32(_xInput.Value);
        _settings.FixedY = decimal.ToInt32(_yInput.Value);
    }

    private bool IsForegroundApplicationAllowed()
    {
        if (!_settings.RestrictToSelectedApplications)
        {
            return true;
        }

        if (_settings.AllowedApplications.Count == 0)
        {
            return false;
        }

        IntPtr foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(foreground, out uint processId);
        try
        {
            using Process process = Process.GetProcessById((int)processId);
            return _settings.AllowedApplications.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    private void UpdateInterfaceState()
    {
        if (InvokeRequired)
        {
            TryBeginInvoke(UpdateInterfaceState);
            return;
        }

        bool turkish = _settings.Language == AppLanguage.Turkish;
        _profilesButton.Text = turkish ? "☷ Profiller" : "☷ Profiles";
        _settingsButton.Text = turkish ? "⚙ Ayarlar" : "⚙ Settings";
        _readButton.Text = turkish ? "Oku" : "Read";
        _macroInfoLabel.Text = turkish
            ? $"Makrolar ayrı ayrı çalışır. Acil durdurma: {_settings.EmergencyStopBinding.DisplayName}."
            : $"Macros run independently. Emergency stop: {_settings.EmergencyStopBinding.DisplayName}.";
        _profilesButton.Enabled = !_isRunning;
        _settingsButton.Enabled = !_isRunning;
        _languageInput.Enabled = true;

        if (_isRunning)
        {
            MacroRuntime[] running = _macroRuntimes.Where(runtime => runtime.IsRunning).ToArray();
            string[] names = running.Take(3).Select(runtime =>
                runtime.Definition.InputType == MacroInputType.Keyboard
                    ? $"{runtime.Definition.Name} [{runtime.Profile.ActivationBinding.DisplayName}] ({runtime.CurrentSequence}/{runtime.Definition.KeyboardSequenceRepeats}, {(turkish ? "adım" : "step")} {runtime.CurrentStep}/{runtime.Definition.KeyboardSteps.Count})"
                    : $"{runtime.Definition.Name} [{runtime.Profile.ActivationBinding.DisplayName}] ({runtime.CompletedClicks} {(turkish ? "eylem" : "actions")})").ToArray();
            string remaining = running.Length > names.Length
                ? (turkish ? $" +{running.Length - names.Length} makro" : $" +{running.Length - names.Length} macros")
                : "";
            _statusLabel.Text = $"{(turkish ? "Çalışıyor" : "Running")} — {string.Join(", ", names)}{remaining}";
            _statusLabel.ForeColor = AppTheme.Success;
            _startStopButton.Text = turkish ? "Durdur" : "Stop";
            _startStopButton.Enabled = true;
        }
        else if (_captureInProgress)
        {
            _statusLabel.Text = turkish
                ? "Bir klavye veya fare tuşuna basın..."
                : "Press any keyboard or mouse button...";
            _statusLabel.ForeColor = AppTheme.Accent;
            _startStopButton.Text = turkish ? "Dinleme açık" : "Listening";
            _startStopButton.Enabled = false;
        }
        else
        {
            int enabledMacros = _macroRuntimes.Count(runtime => runtime.Definition.Enabled);
            _statusLabel.Text = turkish
                ? $"Hazır — {enabledMacros} makro etkin"
                : $"Ready — {enabledMacros} macros enabled";
            _statusLabel.ForeColor = AppTheme.Text;
            _startStopButton.Text = turkish ? "Başlat" : "Start";
            _startStopButton.Enabled = enabledMacros > 0;
        }

        _helpLabel.Text = turkish
            ? "Makroları açarak her birinin fare tuşunu, tıklama hızını, kısayolunu ve profilini ayrı ayrı ayarlayın."
            : "Open Macros to set each macro's mouse button, click rate, hotkey, and profile independently.";
    }

    private void ShowInputError(Win32Exception exception)
    {
        if (InvokeRequired)
        {
            TryBeginInvoke(() => ShowInputError(exception));
            return;
        }

        MessageBox.Show(
            this,
            $"{TextFor("Mouse input could not be sent.", "Fare girdisi gönderilemedi.")} {exception.Message}",
            Text,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private bool TryBeginInvoke(Action action)
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return false;
        }

        try
        {
            BeginInvoke(action);
            return true;
        }
        catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
        {
            return false;
        }
    }

}

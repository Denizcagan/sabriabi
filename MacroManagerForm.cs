using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClickerApp;

internal sealed class MacroManagerForm : Form
{
    private readonly ClickerSettings _draft;
    private readonly ListBox _macros = new();
    private readonly ListBox _keyboardSteps = new();
    private readonly Label _emptyMacroLabel = new();
    private readonly Label _emptyEditorLabel = new();
    private readonly TextBox _name = new();
    private readonly TextBox _stepKeys = new();
    private readonly TextBox _applicationPath = new();
    private readonly Label _emergencyBinding = new();
    private readonly Label _stepHoldLabel = new();
    private readonly ComboBox _profile = new();
    private readonly ComboBox _activation = new();
    private readonly ComboBox _stepAction = new();
    private readonly Panel _macroTypePanel = new();
    private readonly Panel _mouseSettingsPanel = new();
    private Control? _keyboardSettingsPanel;
    private readonly NumericUpDown _cps = new();
    private readonly NumericUpDown _duty = new();
    private readonly ComboBox _clickStyle = new();
    private readonly NumericUpDown _jitterPixels = new();
    private readonly NumericUpDown _timingVariation = new();
    private readonly ComboBox _action = new();
    private readonly ComboBox _target = new();
    private readonly CheckBox _limitEnabled = new();
    private readonly NumericUpDown _limit = new();
    private readonly NumericUpDown _x = new();
    private readonly NumericUpDown _y = new();
    private readonly NumericUpDown _stepHold = new();
    private readonly NumericUpDown _stepInterval = new();
    private readonly NumericUpDown _stepRepeat = new();
    private readonly NumericUpDown _maxRuntimeSeconds = new();
    private readonly NumericUpDown _mouseActionLimit = new();
    private readonly NumericUpDown _keyboardSequenceRepeats = new();
    private readonly NumericUpDown _maxClicksPerSecond = new();
    private readonly MousePreviewControl _mousePreview = new();
    private readonly CheckBox _enabled = new();
    private readonly Button _capture = new();
    private readonly Button _add = new();
    private readonly Button _remove = new();
    private readonly Button _preview = new();
    private readonly Button _moveStepUp = new();
    private readonly Button _moveStepDown = new();
    private readonly Button _captureEmergencyStop = new();
    private readonly Button _importMacro = new();
    private readonly Button _exportMacro = new();
    private readonly Button _apply = new();
    private readonly Button _exit = new();
    private readonly Button _addStep = new();
    private readonly Button _updateStep = new();
    private readonly Button _removeStep = new();
    private readonly Button _browseApplication = new();
    private readonly Label _binding = new();
    private readonly ToolTip _modeHelp = new();
    private readonly bool _turkish;
    private readonly GlobalInputHook _captureHook;
    private TableLayoutPanel? _macroEditor;
    private MacroDefinition? _editing;
    private bool _capturing;
    private bool _capturingEmergencyStop;
    private bool _updating;

    public MacroManagerForm(ClickerSettings settings)
    {
        _draft = SettingsStore.Clone(settings);
        EmergencyStopBinding = _draft.EmergencyStopBinding;
        _turkish = settings.Language == AppLanguage.Turkish;
        if (_draft.Macros.Count == 0)
        {
            _draft.Macros.Add(SettingsStore.CreateDefaultMacro(settings));
        }

        _captureHook = new GlobalInputHook(settings.ActivationBinding);
        _captureHook.InputChanged += OnInputChanged;
        ConfigureWindow();
        BuildInterface();
        _emergencyBinding.Text = $"{(_turkish ? "Acil durdurma:" : "Emergency stop:")} {EmergencyStopBinding.DisplayName}";
        RefreshMacroList(_draft.Macros.FirstOrDefault()?.Id);
    }

    public List<MacroDefinition>? ResultMacros { get; private set; }
    public InputBinding EmergencyStopBinding { get; private set; }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _captureHook.Dispose();
        _modeHelp.Dispose();
        base.OnFormClosed(e);
    }

    private void ConfigureWindow()
    {
        Text = _turkish ? "Sabri Abi — Makrolar" : "Sabri Abi — Macros";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 680);
        Size = new Size(1020, 780);
        Font = new Font("Segoe UI", 9F);
    }

    private void BuildInterface()
    {
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 2,
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 14, 0) };
        _macros.Dock = DockStyle.Fill;
        _macros.BackColor = AppTheme.Surface;
        _macros.ForeColor = ForeColor;
        _macros.BorderStyle = BorderStyle.None;
        _macros.FormattingEnabled = true;
        _emptyMacroLabel.Dock = DockStyle.Fill;
        _emptyMacroLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyMacroLabel.ForeColor = AppTheme.MutedText;
        _emptyMacroLabel.Visible = false;
        listPanel.Controls.Add(_emptyMacroLabel);
        _macros.SelectedIndexChanged += (_, _) =>
        {
            SaveEditor();
            LoadSelectedMacro();
        };
        _macros.Format += (_, e) =>
        {
            if (e.ListItem is MacroDefinition macro)
            {
                string type = macro.InputType == MacroInputType.Mouse
                    ? (_turkish ? "Fare" : "Mouse")
                    : (_turkish ? "Klavye" : "Keyboard");
                e.Value = $"{macro.Name}  ·  {type}";
            }
        };
        listPanel.Controls.Add(_macros);

        var listButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 112, WrapContents = true };
        StyleButton(_add, _turkish ? "＋ Makro" : "＋ Add macro");
        StyleButton(_remove, _turkish ? "− Sil" : "− Remove");
        StyleButton(_preview, _turkish ? "Önizle" : "Preview");
        StyleButton(_importMacro, _turkish ? "JSON içe aktar" : "Import JSON");
        StyleButton(_exportMacro, _turkish ? "JSON dışa aktar" : "Export JSON");
        _add.Click += OnAddClick;
        _remove.Click += OnRemoveClick;
        _importMacro.Click += OnImportMacroClick;
        _exportMacro.Click += OnExportMacroClick;
        listButtons.Controls.AddRange([_add, _remove, _preview, _importMacro, _exportMacro]);
        listPanel.Controls.Add(listButtons);
        root.Controls.Add(listPanel, 0, 0);

        var editorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppTheme.Surface
        };
        var editor = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(12),
            BackColor = AppTheme.Surface
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (int i = 0; i < editor.RowCount; i++)
        {
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        AddEditorRow(editor, _turkish ? "Makro adı" : "Macro name", _name);
        AddEditorRow(editor, _turkish ? "Profil" : "Profile", _profile);
        AddEditorRow(editor, _turkish ? "Kısayol" : "Activation key", CreateBindingPanel());
        AddEditorRow(editor, _turkish ? "Çalışma biçimi" : "Activation mode", _activation);
        _enabled.Text = _turkish ? "Etkin" : "Enabled";
        _enabled.AutoSize = true;
        AddEditorRow(editor, "", _enabled);

        editorPanel.Controls.Add(editor);
        _macroEditor = editor;
        _emptyEditorLabel.Dock = DockStyle.Fill;
        _emptyEditorLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyEditorLabel.ForeColor = AppTheme.MutedText;
        _emptyEditorLabel.Visible = false;
        editorPanel.Controls.Add(_emptyEditorLabel);
        root.Controls.Add(editorPanel, 1, 0);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft
        };
        StyleButton(_exit, _turkish ? "İptal" : "Cancel");
        StyleButton(_apply, _turkish ? "Uygula" : "Apply");
        StyleButton(_captureEmergencyStop, _turkish ? "Acil durdurma tuşu seç" : "Set emergency stop key");
        _emergencyBinding.AutoSize = true;
        _emergencyBinding.Margin = new Padding(6, 9, 8, 0);
        _captureEmergencyStop.Click += (_, _) =>
        {
            _capturingEmergencyStop = true;
            _capturing = true;
            _captureHook.IsCapturing = true;
            _captureEmergencyStop.Text = _turkish ? "Dinleniyor… Esc iptal" : "Listening… Esc cancels";
        };
        _apply.Click += OnApplyClick;
        _exit.Click += (_, _) => Close();
        bottom.Controls.AddRange([
            _exit,
            _apply,
            DisclaimerDialog.CreateReadButton(_turkish),
            _emergencyBinding,
            _captureEmergencyStop
        ]);
        root.Controls.Add(bottom, 0, 1);
        root.SetColumnSpan(bottom, 2);

        _profile.DropDownStyle = ComboBoxStyle.DropDownList;
        _profile.Width = 300;
        _profile.SelectedIndexChanged += OnProfileChanged;
        _activation.DropDownStyle = ComboBoxStyle.DropDownList;
        _activation.Items.AddRange(_turkish
            ? ["Aç/kapat (tuş basılıyken çalışır)", "Basılı tut"]
            : ["Toggle (runs while held)", "Hold"]);
        _activation.SelectedIndexChanged += (_, _) =>
        {
            if (!_updating && _editing is not null && _activation.SelectedIndex >= 0)
            {
                _editing.Profile.ActivationMode = (ActivationMode)_activation.SelectedIndex;
            }
        };
        _cps.Minimum = 0.2m;
        _cps.Maximum = 100m;
        _cps.DecimalPlaces = 1;
        _cps.Increment = 1m;
        _cps.Width = 110;
        _duty.Minimum = 1;
        _duty.Maximum = 100;
        _duty.Width = 110;
        _clickStyle.DropDownStyle = ComboBoxStyle.DropDownList;
        _clickStyle.Items.AddRange(_turkish
            ? ["Standart", "Jitter", "Drag click"]
            : ["Standard", "Jitter", "Drag click"]);
        _modeHelp.SetToolTip(_clickStyle, _turkish
            ? "Jitter: imleci seçilen piksel aralığında titretir. Drag click: kısa basış-bırakış darbeleri kullanır."
            : "Jitter varies cursor position within the selected pixel range. Drag click uses short press/release pulses.");
        _modeHelp.SetToolTip(_timingVariation, _turkish
            ? "Makronun tıklama aralığına eklenecek rastgele yüzde."
            : "Random percentage added to this macro's click interval.");
        _jitterPixels.Minimum = 0;
        _jitterPixels.Maximum = 10;
        _jitterPixels.Width = 90;
        _timingVariation.Minimum = 0;
        _timingVariation.Maximum = 50;
        _timingVariation.Width = 90;
        _action.DropDownStyle = ComboBoxStyle.DropDownList;
        _action.Items.AddRange(_turkish
            ? ["Fare tıklaması", "Yukarı kaydır", "Aşağı kaydır"]
            : ["Mouse click", "Scroll up", "Scroll down"]);
        _target.DropDownStyle = ComboBoxStyle.DropDownList;
        _target.Items.AddRange(_turkish
            ? ["İmlecin bulunduğu yer", "Sabit koordinat"]
            : ["Current cursor position", "Fixed coordinates"]);
        _stepAction.DropDownStyle = ComboBoxStyle.DropDownList;
        _stepAction.Items.AddRange(_turkish
            ? ["Bir kez bas", "Basılı tut", "Tekrarla", "Uygulama aç", "Bekle"]
            : ["Tap", "Hold", "Repeat", "Open application", "Wait"]);
        _stepAction.SelectedIndex = 0;
        _stepAction.SelectedIndexChanged += (_, _) => UpdateKeyboardStepState();
        _keyboardSteps.SelectedIndexChanged += (_, _) => LoadKeyboardStepIntoEditor();
        _addStep.Click += (_, _) => AddKeyboardStep();
        _updateStep.Click += (_, _) => UpdateKeyboardStep();
        _removeStep.Click += (_, _) => RemoveKeyboardStep();
        _moveStepUp.Click += (_, _) => MoveKeyboardStep(-1);
        _moveStepDown.Click += (_, _) => MoveKeyboardStep(1);
        _preview.Click += (_, _) => PreviewSelectedMacro();
        _browseApplication.Click += (_, _) => BrowseForApplication();
        _stepHold.Minimum = 1;
        _stepHold.Maximum = 60_000;
        _stepHold.Value = 100;
        _stepInterval.Minimum = 0;
        _stepInterval.Maximum = 60_000;
        _stepInterval.Value = 100;
        _stepRepeat.Minimum = 1;
        _stepRepeat.Maximum = 10_000;
        _stepRepeat.Value = 5;
        _maxRuntimeSeconds.Minimum = 0;
        _maxRuntimeSeconds.Maximum = 86_400;
        _maxRuntimeSeconds.Width = 100;
        _mouseActionLimit.Minimum = 0;
        _mouseActionLimit.Maximum = 1_000_000;
        _mouseActionLimit.Width = 100;
        _keyboardSequenceRepeats.Minimum = 1;
        _keyboardSequenceRepeats.Maximum = 10_000;
        _keyboardSequenceRepeats.Value = 1;
        _keyboardSequenceRepeats.Width = 100;
        _maxClicksPerSecond.Minimum = 0.2m;
        _maxClicksPerSecond.Maximum = 100m;
        _maxClicksPerSecond.DecimalPlaces = 1;
        _maxClicksPerSecond.Value = 100m;
        _maxClicksPerSecond.Width = 100;
        ConfigureCoordinate(_x);
        ConfigureCoordinate(_y);
        BuildMouseSettings();
        _keyboardSettingsPanel = CreateKeyboardEditor();
        _macroTypePanel.AutoSize = true;
        _macroTypePanel.Dock = DockStyle.Top;
        _macroTypePanel.BackColor = AppTheme.Surface;
        _mouseSettingsPanel.AutoSize = true;
        _mouseSettingsPanel.Dock = DockStyle.Top;
        _mouseSettingsPanel.BackColor = AppTheme.Surface;
        _macroTypePanel.Controls.Add(_keyboardSettingsPanel);
        _macroTypePanel.Controls.Add(_mouseSettingsPanel);
        editor.Controls.Add(_macroTypePanel, 0, 5);
        editor.SetColumnSpan(_macroTypePanel, 2);
        var safetyLabel = new Label
        {
            Text = _turkish ? "Güvenli sınırlar" : "Safety limits",
            AutoSize = true,
            ForeColor = AppTheme.MutedText,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 10, 8, 10)
        };
        Control safetyLimits = CreateSafetyLimitsPanel();
        safetyLimits.Margin = new Padding(3, 5, 3, 5);
        safetyLimits.Anchor = AnchorStyles.Left;
        editor.Controls.Add(safetyLabel, 0, 6);
        editor.Controls.Add(safetyLimits, 1, 6);
        _mousePreview.MouseButtonSelected += button =>
        {
            if (!_updating && _editing is not null)
            {
                _editing.Profile.MouseButton = button;
            }
        };
        _target.SelectedIndexChanged += (_, _) => UpdateCoordinateState();
        _clickStyle.SelectedIndexChanged += (_, _) => UpdateClickStyleState();
        _limitEnabled.CheckedChanged += (_, _) => _limit.Enabled = _limitEnabled.Checked;
        ReloadProfiles();
        AppTheme.Apply(this);
        editor.BackColor = AppTheme.Surface;
        AppTheme.StylePrimaryButton(_apply);
    }

    private Control CreateKeyboardEditor()
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 4,
            Dock = DockStyle.Top,
            BackColor = AppTheme.Surface
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _keyboardSteps.Dock = DockStyle.Fill;
        _keyboardSteps.BackColor = AppTheme.Input;
        _keyboardSteps.ForeColor = AppTheme.Text;
        _keyboardSteps.BorderStyle = BorderStyle.FixedSingle;
        _keyboardSteps.DisplayMember = nameof(KeyboardMacroStep.DisplayName);
        panel.Controls.Add(_keyboardSteps, 0, 0);

        var fields = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 6,
            Dock = DockStyle.Top,
            BackColor = AppTheme.Surface
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < fields.RowCount; i++)
        {
            fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        AddEditorRow(fields, _turkish ? "Adım türü" : "Step action", _stepAction);
        AddEditorRow(fields, _turkish ? "Tuşlar (+ ile)" : "Keys (joined with +)", _stepKeys);
        AddEditorRow(fields, _turkish ? "Basılı kalma (ms)" : "Hold duration (ms)", _stepHold);
        _stepHoldLabel.Text = _turkish ? "Basılı kalma (ms)" : "Hold duration (ms)";
        _stepHoldLabel.AutoSize = true;
        _stepHoldLabel.ForeColor = AppTheme.MutedText;
        _stepHoldLabel.Anchor = AnchorStyles.Left;
        _stepHoldLabel.Margin = new Padding(3, 10, 8, 10);
        fields.Controls.RemoveAt(fields.Controls.Count - 2);
        fields.Controls.Add(_stepHoldLabel, 0, 2);
        AddEditorRow(fields, _turkish ? "Aralık (ms)" : "Repeat interval (ms)", _stepInterval);
        AddEditorRow(fields, _turkish ? "Tekrar sayısı" : "Repeat count", _stepRepeat);
        var applicationPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        _applicationPath.Width = 210;
        _applicationPath.ReadOnly = true;
        StyleButton(_browseApplication, _turkish ? "Gözat…" : "Browse…");
        applicationPanel.Controls.Add(_applicationPath);
        applicationPanel.Controls.Add(_browseApplication);
        AddEditorRow(fields, _turkish ? "Uygulama" : "Application", applicationPanel);
        panel.Controls.Add(fields, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            WrapContents = false
        };
        StyleButton(_addStep, _turkish ? "＋ Adım ekle" : "＋ Add step");
        StyleButton(_updateStep, _turkish ? "Güncelle" : "Update step");
        StyleButton(_removeStep, _turkish ? "− Adımı sil" : "− Remove step");
        _updateStep.Enabled = false;
        _removeStep.Enabled = false;
        buttons.Controls.AddRange([_addStep, _updateStep, _removeStep]);
        panel.Controls.Add(buttons, 0, 2);
        var reorderButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false };
        StyleButton(_moveStepUp, _turkish ? "↑ Yukarı" : "↑ Move up");
        StyleButton(_moveStepDown, _turkish ? "↓ Aşağı" : "↓ Move down");
        _moveStepUp.Enabled = false;
        _moveStepDown.Enabled = false;
        reorderButtons.Controls.AddRange([_moveStepUp, _moveStepDown]);
        panel.Controls.Add(reorderButtons, 0, 3);
        UpdateKeyboardStepState();
        return panel;
    }

    private void BuildMouseSettings()
    {
        var settings = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 10,
            Dock = DockStyle.Top,
            BackColor = AppTheme.Surface,
            Padding = Padding.Empty
        };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (int i = 0; i < settings.RowCount; i++)
        {
            settings.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        AddEditorRow(settings,
            _turkish ? "Tıklama / sn (0,2–100)" : "Clicks per second (0.2–100)",
            _cps);
        AddEditorRow(settings, _turkish ? "Görev döngüsü (%)" : "Duty cycle (%)", _duty);
        AddEditorRow(settings, _turkish ? "Tıklama stili" : "Click style", _clickStyle);
        AddEditorRow(settings, _turkish ? "Fare tuşu" : "Mouse button", _mousePreview);
        AddEditorRow(settings, _turkish ? "Jitter mesafesi (px)" : "Jitter distance (px)", _jitterPixels);
        AddEditorRow(settings, _turkish ? "Zaman değişimi (%)" : "Timing variation (%)", _timingVariation);
        AddEditorRow(settings, _turkish ? "Eylem" : "Action", _action);
        AddEditorRow(settings, _turkish ? "Tıklama limiti" : "Click limit", CreateLimitPanel());
        AddEditorRow(settings, _turkish ? "Hedef" : "Target", _target);
        AddEditorRow(settings, _turkish ? "Sabit konum" : "Fixed position", CreateCoordinatesPanel());
        _mouseSettingsPanel.Controls.Add(settings);
    }

    private Control CreateSafetyLimitsPanel()
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface
        };
        panel.Controls.Add(new Label
        {
            Text = _turkish ? "Maks. süre (sn, 0=sınırsız):" : "Max runtime (sec, 0=unlimited):",
            AutoSize = true,
            Margin = new Padding(3, 8, 3, 3),
            ForeColor = AppTheme.MutedText
        });
        panel.Controls.Add(_maxRuntimeSeconds);
        panel.Controls.Add(new Label
        {
            Text = _turkish ? "Maks. fare eylemi (0=sınırsız):" : "Max mouse actions (0=unlimited):",
            AutoSize = true,
            Margin = new Padding(12, 8, 3, 3),
            ForeColor = AppTheme.MutedText
        });
        panel.Controls.Add(_mouseActionLimit);
        panel.Controls.Add(new Label
        {
            Text = _turkish ? "Maks. CPS:" : "Max CPS:",
            AutoSize = true,
            Margin = new Padding(12, 8, 3, 3),
            ForeColor = AppTheme.MutedText
        });
        panel.Controls.Add(_maxClicksPerSecond);
        panel.Controls.Add(new Label
        {
            Text = _turkish ? "Klavye dizisi tekrarı:" : "Keyboard sequence repeats:",
            AutoSize = true,
            Margin = new Padding(12, 8, 3, 3),
            ForeColor = AppTheme.MutedText
        });
        panel.Controls.Add(_keyboardSequenceRepeats);
        return panel;
    }

    private void UpdateKeyboardStepState()
    {
        bool openApplication = _stepAction.SelectedIndex == (int)KeyboardStepAction.OpenApplication;
        bool wait = _stepAction.SelectedIndex == (int)KeyboardStepAction.Wait;
        bool isRepeat = _stepAction.SelectedIndex == (int)KeyboardStepAction.Repeat;
        _stepKeys.Enabled = !openApplication && !wait;
        _stepHold.Enabled = !openApplication;
        _stepHoldLabel.Text = wait
            ? (_turkish ? "Bekleme süresi (ms)" : "Wait duration (ms)")
            : (_turkish ? "Basılı kalma (ms)" : "Hold duration (ms)");
        _stepInterval.Enabled = isRepeat;
        _stepRepeat.Enabled = isRepeat;
        _applicationPath.Enabled = openApplication;
        _browseApplication.Enabled = openApplication;
    }

    private void LoadKeyboardStepIntoEditor()
    {
        if (_keyboardSteps.SelectedItem is not KeyboardMacroStep step)
        {
            _updateStep.Enabled = false;
            _removeStep.Enabled = false;
            _moveStepUp.Enabled = false;
            _moveStepDown.Enabled = false;
            return;
        }

        _stepAction.SelectedIndex = (int)step.Action;
        _stepKeys.Text = string.Join("+", step.Keys.Select(key => ((Keys)key).ToString()));
        _stepHold.Value = Math.Clamp(step.HoldMilliseconds, (int)_stepHold.Minimum, (int)_stepHold.Maximum);
        _stepInterval.Value = Math.Clamp(step.IntervalMilliseconds, (int)_stepInterval.Minimum, (int)_stepInterval.Maximum);
        _stepRepeat.Value = Math.Clamp(step.RepeatCount, (int)_stepRepeat.Minimum, (int)_stepRepeat.Maximum);
        _applicationPath.Text = step.ApplicationPath;
        _updateStep.Enabled = true;
        _removeStep.Enabled = true;
        _moveStepUp.Enabled = _keyboardSteps.SelectedIndex > 0;
        _moveStepDown.Enabled = _editing is not null
            && _keyboardSteps.SelectedIndex < _editing.KeyboardSteps.Count - 1;
    }

    private void AddKeyboardStep()
    {
        if (_editing is null)
        {
            return;
        }

        if (_editing.KeyboardSteps.Count >= 128)
        {
            MessageBox.Show(this,
                _turkish ? "Bir makro en fazla 128 adım içerebilir." : "A macro can contain at most 128 steps.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!TryReadKeyboardStep(out KeyboardMacroStep step))
        {
            return;
        }

        _editing.KeyboardSteps.Add(step);
        RefreshKeyboardSteps(step);
    }

    private void UpdateKeyboardStep()
    {
        if (_editing is null || _keyboardSteps.SelectedIndex < 0
            || !TryReadKeyboardStep(out KeyboardMacroStep step))
        {
            return;
        }

        int index = _keyboardSteps.SelectedIndex;
        _editing.KeyboardSteps[index] = step;
        RefreshKeyboardSteps(step);
    }

    private void RemoveKeyboardStep()
    {
        if (_editing is null || _keyboardSteps.SelectedItem is not KeyboardMacroStep step)
        {
            return;
        }

        int index = _keyboardSteps.SelectedIndex;
        _editing.KeyboardSteps.Remove(step);
        KeyboardMacroStep? next = _editing.KeyboardSteps.Count == 0
            ? null
            : _editing.KeyboardSteps[Math.Min(index, _editing.KeyboardSteps.Count - 1)];
        RefreshKeyboardSteps(next);
    }

    private void MoveKeyboardStep(int direction)
    {
        if (_editing is null)
        {
            return;
        }

        int index = _keyboardSteps.SelectedIndex;
        int nextIndex = index + direction;
        if (index < 0 || nextIndex < 0 || nextIndex >= _editing.KeyboardSteps.Count)
        {
            return;
        }

        (_editing.KeyboardSteps[index], _editing.KeyboardSteps[nextIndex]) =
            (_editing.KeyboardSteps[nextIndex], _editing.KeyboardSteps[index]);
        RefreshKeyboardSteps(_editing.KeyboardSteps[nextIndex]);
    }

    private void PreviewSelectedMacro()
    {
        if (_editing is null)
        {
            return;
        }

        SaveEditor();
        string details;
        if (_editing.InputType == MacroInputType.Keyboard)
        {
            string steps = _editing.KeyboardSteps.Count == 0
                ? (_turkish ? "(adım yok)" : "(no steps)")
                : string.Join(Environment.NewLine, _editing.KeyboardSteps.Select((step, index) =>
                    $"{index + 1}. {step.DisplayName}"));
            details = $"{_editing.Name} — {(_turkish ? "Klavye makrosu" : "Keyboard macro")}{Environment.NewLine}" +
                $"{(_turkish ? "Tetik:" : "Trigger:")} {_editing.Profile.ActivationBinding.DisplayName}{Environment.NewLine}" +
                $"{(_turkish ? "Dizi tekrarı:" : "Sequence repeats:")} {_editing.KeyboardSequenceRepeats}{Environment.NewLine}" +
                $"{(_turkish ? "Maksimum süre:" : "Maximum runtime:")} {FormatRuntime(_editing.MaxRuntimeSeconds)}{Environment.NewLine}{Environment.NewLine}{steps}";
        }
        else
        {
            decimal effectiveCps = Math.Min(_editing.Profile.ClicksPerSecond, _editing.MaxClicksPerSecond);
            string clickLimit = _editing.Profile.LimitClicks
                ? _editing.Profile.ClickLimit.ToString()
                : (_turkish ? "Yok" : "None");
            string actionLimit = _editing.MouseActionLimit == 0
                ? (_turkish ? "Yok" : "Unlimited")
                : _editing.MouseActionLimit.ToString();
            details = $"{_editing.Name} — {(_turkish ? "Fare makrosu" : "Mouse macro")}{Environment.NewLine}" +
                $"{(_turkish ? "Tetik:" : "Trigger:")} {_editing.Profile.ActivationBinding.DisplayName}{Environment.NewLine}" +
                $"{(_turkish ? "Fare tuşu:" : "Mouse button:")} {_editing.Profile.MouseButton}{Environment.NewLine}" +
                $"{(_turkish ? "Hız:" : "Rate:")} {effectiveCps:0.0} CPS{Environment.NewLine}" +
                $"{(_turkish ? "Tıklama limiti:" : "Click limit:")} {clickLimit}{Environment.NewLine}" +
                $"{(_turkish ? "Eylem limiti:" : "Action limit:")} {actionLimit}{Environment.NewLine}" +
                $"{(_turkish ? "Maksimum süre:" : "Maximum runtime:")} {FormatRuntime(_editing.MaxRuntimeSeconds)}";
        }

        MessageBox.Show(this, details, _turkish ? "Makro önizlemesi" : "Macro preview",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private string FormatRuntime(int seconds) => seconds == 0
        ? (_turkish ? "Sınırsız" : "Unlimited")
        : _turkish ? $"{seconds} sn" : $"{seconds} sec";

    private bool TryReadKeyboardStep(out KeyboardMacroStep step)
    {
        step = new KeyboardMacroStep();
        if (_stepAction.SelectedIndex < 0)
        {
            return false;
        }

        step = new KeyboardMacroStep
        {
            Action = (KeyboardStepAction)_stepAction.SelectedIndex,
            HoldMilliseconds = decimal.ToInt32(_stepHold.Value),
            IntervalMilliseconds = decimal.ToInt32(_stepInterval.Value),
            RepeatCount = decimal.ToInt32(_stepRepeat.Value),
            ApplicationPath = _applicationPath.Text.Trim()
        };
        if (step.Action == KeyboardStepAction.OpenApplication)
        {
            if (!File.Exists(step.ApplicationPath))
            {
                MessageBox.Show(this,
                    _turkish ? "Uygulama dosyasını seçin." : "Select an existing application file.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        else if (step.Action != KeyboardStepAction.Wait)
        {
            foreach (string part in _stepKeys.Text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                string keyName = part.ToLowerInvariant() switch
                {
                    "ctrl" or "control" => nameof(Keys.ControlKey),
                    "shift" => nameof(Keys.ShiftKey),
                    "alt" => nameof(Keys.Menu),
                    "win" or "windows" => nameof(Keys.LWin),
                    _ => part
                };
                if (!Enum.TryParse(keyName, true, out Keys key) || (int)key is <= 0 or > 255)
                {
                    MessageBox.Show(this,
                        _turkish ? $"Geçersiz tuş: {part}" : $"Invalid key: {part}",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                step.Keys.Add((int)key);
            }

            if (step.Keys.Count == 0 || step.Keys.Count > 8)
            {
                MessageBox.Show(this,
                    _turkish ? "1 ile 8 arasında tuş girin." : "Enter between 1 and 8 keys.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        return true;
    }

    private void RefreshKeyboardSteps(KeyboardMacroStep? selected)
    {
        _keyboardSteps.BeginUpdate();
        _keyboardSteps.Items.Clear();
        if (_editing is not null)
        {
            foreach (KeyboardMacroStep step in _editing.KeyboardSteps)
            {
                _keyboardSteps.Items.Add(step);
            }
        }
        _keyboardSteps.SelectedItem = selected;
        _keyboardSteps.EndUpdate();
        LoadKeyboardStepIntoEditor();
    }

    private void BrowseForApplication()
    {
        using var dialog = new OpenFileDialog
        {
            Title = _turkish ? "Uygulama seç" : "Select application",
            Filter = _turkish ? "Uygulamalar (*.exe)|*.exe|Tüm dosyalar (*.*)|*.*" : "Applications (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _applicationPath.Text = dialog.FileName;
        }
    }

    private static void ConfigureCoordinate(NumericUpDown input)
    {
        input.Minimum = -32768;
        input.Maximum = 32767;
        input.Width = 82;
    }

    private Control CreateLimitPanel()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        _limitEnabled.Text = _turkish ? "Etkin" : "Enabled";
        _limitEnabled.AutoSize = true;
        _limit.Minimum = 1;
        _limit.Maximum = 1_000_000;
        _limit.Width = 110;
        panel.Controls.Add(_limitEnabled);
        panel.Controls.Add(_limit);
        return panel;
    }

    private Control CreateCoordinatesPanel()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        panel.Controls.Add(new Label
        {
            Text = "X:",
            AutoSize = true,
            Margin = new Padding(3, 8, 3, 3),
            ForeColor = AppTheme.MutedText
        });
        panel.Controls.Add(_x);
        panel.Controls.Add(new Label
        {
            Text = "Y:",
            AutoSize = true,
            Margin = new Padding(10, 8, 3, 3),
            ForeColor = AppTheme.MutedText
        });
        panel.Controls.Add(_y);
        return panel;
    }

    private void UpdateCoordinateState()
    {
        bool fixedTarget = _target.SelectedIndex == (int)ClickTarget.FixedPosition;
        _x.Enabled = fixedTarget;
        _y.Enabled = fixedTarget;
    }

    private void UpdateClickStyleState()
    {
        bool jitter = _clickStyle.SelectedIndex == (int)ClickStyle.Jitter;
        _jitterPixels.Enabled = jitter;
        _timingVariation.Enabled = _clickStyle.SelectedIndex != (int)ClickStyle.Standard;
    }

    private Control CreateBindingPanel()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        StyleButton(_capture, _turkish ? "Tuş seç" : "Select key");
        _capture.Click += (_, _) =>
        {
            _capturingEmergencyStop = false;
            _capturing = !_capturing;
            _captureHook.IsCapturing = _capturing;
            _capture.Text = _capturing
                ? (_turkish ? "Dinleniyor… Esc iptal" : "Listening… Esc cancels")
                : (_turkish ? "Tuş seç" : "Select key");
        };
        _binding.AutoSize = true;
        _binding.Margin = new Padding(10, 9, 0, 0);
        panel.Controls.Add(_capture);
        panel.Controls.Add(_binding);
        return panel;
    }

    private static void AddEditorRow(TableLayoutPanel table, string title, Control control)
    {
        int row = table.Controls.Count / 2;
        var label = new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = AppTheme.MutedText,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 10, 8, 10)
        };
        table.Controls.Add(label, 0, row);
        control.Margin = new Padding(3, 5, 3, 5);
        control.Anchor = AnchorStyles.Left;
        if (control is TextBox textBox)
        {
            textBox.Width = 300;
            textBox.BackColor = AppTheme.Input;
            textBox.ForeColor = AppTheme.Text;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is ComboBox combo)
        {
            combo.BackColor = AppTheme.Input;
            combo.ForeColor = AppTheme.Text;
        }
        table.Controls.Add(control, 1, row);
    }

    private static void StyleButton(Button button, string text)
    {
        button.Text = text;
        button.AutoSize = true;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = AppTheme.Border;
        button.BackColor = AppTheme.Surface;
        button.ForeColor = AppTheme.Text;
        button.Margin = new Padding(3);
    }

    private void ReloadProfiles(string? selectedName = null)
    {
        _updating = true;
        _profile.Items.Clear();
        if (_draft.Profiles.Count == 0)
        {
            _draft.Profiles.Add(SettingsStore.CreateProfile(_draft, "Default"));
        }

        _profile.Items.AddRange(_draft.Profiles.Cast<object>().ToArray());
        _profile.DisplayMember = nameof(ClickerProfile.Name);
        int index = _draft.Profiles.FindIndex(profile =>
            string.Equals(profile.Name, selectedName, StringComparison.OrdinalIgnoreCase));
        _profile.SelectedIndex = index >= 0 ? index : 0;
        _updating = false;
    }

    private void RefreshMacroList(string? selectedId = null)
    {
        _updating = true;
        _macros.Items.Clear();
        foreach (MacroDefinition macro in _draft.Macros)
        {
            _macros.Items.Add(macro);
            if (macro.Id == selectedId)
            {
                _macros.SelectedItem = macro;
            }
        }
        _macros.DisplayMember = nameof(MacroDefinition.Name);
        _updating = false;
        bool hasMacros = _macros.Items.Count > 0;
        _emptyMacroLabel.Text = hasMacros
            ? ""
            : (_turkish ? "Henüz makro yok.\n＋ Makro ile fare veya klavye makrosu oluşturun." : "No macros yet.\nUse ＋ Add macro to create a mouse or keyboard macro.");
        _emptyMacroLabel.Visible = !hasMacros;
        if (hasMacros)
        {
            _macros.BringToFront();
        }
        else
        {
            _emptyMacroLabel.BringToFront();
        }
        LoadSelectedMacro();
    }

    private void LoadSelectedMacro()
    {
        if (_updating || _macros.SelectedItem is not MacroDefinition macro)
        {
            _editing = null;
            if (_macroEditor is not null)
            {
                _macroEditor.Enabled = false;
            }

            _preview.Enabled = false;
            _exportMacro.Enabled = false;
            _emptyEditorLabel.Text = _turkish
                ? "Makro oluşturmak için + Makro'ya basıp Fare veya Klavye'yi seçin."
                : "Select + Add macro, then choose Mouse or Keyboard.";
            _emptyEditorLabel.Visible = true;
            _macroTypePanel.Visible = false;
            return;
        }

        if (_macroEditor is not null)
        {
            _macroEditor.Enabled = true;
        }
        _emptyEditorLabel.Visible = false;
        _editing = macro;
        _preview.Enabled = true;
        _exportMacro.Enabled = true;
        _updating = true;
        _name.Text = macro.Name;
        _enabled.Checked = macro.Enabled;
        _maxRuntimeSeconds.Value = macro.MaxRuntimeSeconds;
        _mouseActionLimit.Value = macro.MouseActionLimit;
        _keyboardSequenceRepeats.Value = macro.KeyboardSequenceRepeats;
        _maxClicksPerSecond.Value = macro.MaxClicksPerSecond;
        UpdateMacroTypePanel(macro.InputType);
        ReloadProfiles(macro.ProfileName);
        LoadProfileIntoEditor(macro.Profile);
        _updating = false;
        RefreshKeyboardSteps(macro.KeyboardSteps.FirstOrDefault());
    }

    private void UpdateMacroTypePanel(MacroInputType inputType)
    {
        bool mouseMacro = inputType == MacroInputType.Mouse;
        _mouseSettingsPanel.Visible = mouseMacro;
        if (_keyboardSettingsPanel is not null)
        {
            _keyboardSettingsPanel.Visible = !mouseMacro;
        }

        _macroTypePanel.Visible = true;
        _mouseActionLimit.Enabled = mouseMacro;
        _maxClicksPerSecond.Enabled = mouseMacro;
        _keyboardSequenceRepeats.Enabled = !mouseMacro;
        _preview.Enabled = _editing is not null;
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        if (_updating || _editing is null || _profile.SelectedItem is not ClickerProfile selectedProfile)
        {
            return;
        }

        SaveEditor();
        _editing.Profile = SettingsStore.CloneProfile(selectedProfile);
        _editing.ProfileName = selectedProfile.Name;
        _updating = true;
        LoadProfileIntoEditor(_editing.Profile);
        _updating = false;
    }

    private void LoadProfileIntoEditor(ClickerProfile profile)
    {
        _activation.SelectedIndex = (int)profile.ActivationMode;
        _binding.Text = profile.ActivationBinding.DisplayName;
        _cps.Value = profile.ClicksPerSecond;
        _duty.Value = profile.DutyCyclePercent;
        _clickStyle.SelectedIndex = (int)profile.ClickStyle;
        _mousePreview.SelectedButton = profile.MouseButton;
        _jitterPixels.Value = profile.JitterPixels;
        _timingVariation.Value = profile.TimingVariationPercent;
        _action.SelectedIndex = (int)profile.InputAction;
        _limitEnabled.Checked = profile.LimitClicks;
        _limit.Value = profile.ClickLimit;
        _target.SelectedIndex = (int)profile.Target;
        _x.Value = Math.Clamp(profile.FixedX, (int)_x.Minimum, (int)_x.Maximum);
        _y.Value = Math.Clamp(profile.FixedY, (int)_y.Minimum, (int)_y.Maximum);
        _limit.Enabled = profile.LimitClicks;
        UpdateCoordinateState();
        UpdateClickStyleState();
    }

    private void OnAddClick(object? sender, EventArgs e)
    {
        if (_draft.Macros.Count >= 64)
        {
            MessageBox.Show(this, _turkish ? "En fazla 64 makro eklenebilir." : "Up to 64 macros are supported.", Text);
            return;
        }

        using var typeDialog = new MacroTypeDialog(_turkish);
        if (typeDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SaveEditor();
        ClickerProfile profile = _profile.SelectedItem is ClickerProfile selected
            ? SettingsStore.CloneProfile(selected)
            : SettingsStore.CreateProfile(_draft, "Default");
        var macro = new MacroDefinition
        {
            Name = $"{(_turkish ? "Makro" : "Macro")} {_draft.Macros.Count + 1}",
            ProfileName = profile.Name,
            Profile = profile,
            InputType = typeDialog.InputType
        };
        _draft.Macros.Add(macro);
        RefreshMacroList(macro.Id);
    }

    private void OnRemoveClick(object? sender, EventArgs e)
    {
        SaveEditor();
        if (_macros.SelectedItem is not MacroDefinition macro)
        {
            return;
        }

        _draft.Macros.Remove(macro);
        RefreshMacroList(_draft.Macros.LastOrDefault()?.Id);
    }

    private void OnImportMacroClick(object? sender, EventArgs e)
    {
        if (_draft.Macros.Count >= 64)
        {
            MessageBox.Show(this, _turkish ? "En fazla 64 makro eklenebilir." : "Up to 64 macros are supported.", Text);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = "JSON macro (*.json)|*.json",
            Title = _turkish ? "Makroyu JSON'dan içe aktar" : "Import macro from JSON",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            MacroDefinition macro = JsonSerializer.Deserialize<MacroDefinition>(
                File.ReadAllText(dialog.FileName), options)
                ?? throw new InvalidDataException("Macro file is empty.");
            SettingsStore.ValidateMacroDefinition(macro);
            macro.Id = Guid.NewGuid().ToString("N");
            string originalName = macro.Name;
            int suffix = 0;
            while (_draft.Macros.Any(existing =>
                string.Equals(existing.Name, macro.Name, StringComparison.OrdinalIgnoreCase)))
            {
                suffix++;
                macro.Name = $"{originalName} ({(_turkish ? "içe aktarıldı" : "imported")} {suffix})";
            }

            SaveEditor();
            _draft.Macros.Add(macro);
            RefreshMacroList(macro.Id);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            MessageBox.Show(this,
                $"{(_turkish ? "Makro içe aktarılamadı." : "Macro import failed.")}{Environment.NewLine}{exception.Message}",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnExportMacroClick(object? sender, EventArgs e)
    {
        if (_editing is null)
        {
            return;
        }

        SaveEditor();
        string fileName = _editing.Name;
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid, '_');
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "JSON macro (*.json)|*.json",
            FileName = $"{fileName}.json",
            Title = _turkish ? "Makroyu JSON olarak dışa aktar" : "Export macro as JSON",
            AddExtension = true,
            DefaultExt = "json"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new JsonStringEnumConverter());
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(_editing, options));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this,
                $"{(_turkish ? "Makro dışa aktarılamadı." : "Macro export failed.")}{Environment.NewLine}{exception.Message}",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnInputChanged(InputBinding binding, bool isDown, bool isCaptured)
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(() => ProcessCapturedInput(binding, isDown, isCaptured));
        }
        catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
        {
        }
    }

    private void ProcessCapturedInput(InputBinding binding, bool isDown, bool isCaptured)
    {
        if (!_capturing || !isDown || !isCaptured || (!_capturingEmergencyStop && _editing is null))
        {
            return;
        }

        if (binding.Kind == InputKind.Keyboard && binding.Code == (int)Keys.Escape)
        {
            _capturing = false;
            _capturingEmergencyStop = false;
            _captureHook.IsCapturing = false;
            _capture.Text = _turkish ? "Tuş seç" : "Select key";
            _captureEmergencyStop.Text = _turkish ? "Acil durdurma tuşu seç" : "Set emergency stop key";
            return;
        }

        if (_capturingEmergencyStop)
        {
            if (_draft.Macros.Any(macro => macro.Enabled && macro.Profile.ActivationBinding == binding))
            {
                MessageBox.Show(this,
                    _turkish
                        ? "Acil durdurma tuşu etkin bir makronun kısayoluyla aynı olamaz."
                        : "The emergency stop key cannot match an enabled macro hotkey.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EmergencyStopBinding = binding;
            _draft.EmergencyStopBinding = binding;
            _emergencyBinding.Text = $"{(_turkish ? "Acil durdurma:" : "Emergency stop:")} {binding.DisplayName}";
            _capturingEmergencyStop = false;
            _capturing = false;
            _captureHook.IsCapturing = false;
            _captureEmergencyStop.Text = _turkish ? "Acil durdurma tuşu seç" : "Set emergency stop key";
            return;
        }

        if (_editing is null)
        {
            return;
        }

        _editing.Profile.ActivationBinding = binding;
        _capturingEmergencyStop = false;
        _binding.Text = binding.DisplayName;
        _capturing = false;
        _captureHook.IsCapturing = false;
        _capture.Text = _turkish ? "Tuş seç" : "Select key";
    }

    private void SaveEditor()
    {
        if (_editing is null)
        {
            return;
        }

        string name = _name.Text.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            _editing.Name = name;
        }
        _editing.Enabled = _enabled.Checked;
        ClickerProfile profile = _editing.Profile;
        _editing.MaxRuntimeSeconds = decimal.ToInt32(_maxRuntimeSeconds.Value);
        if (_editing.InputType == MacroInputType.Mouse)
        {
            profile.ClicksPerSecond = _cps.Value;
            profile.DutyCyclePercent = _duty.Value;
            profile.ClickStyle = (ClickStyle)_clickStyle.SelectedIndex;
            profile.MouseButton = _mousePreview.SelectedButton;
            profile.JitterPixels = decimal.ToInt32(_jitterPixels.Value);
            profile.TimingVariationPercent = decimal.ToInt32(_timingVariation.Value);
            profile.InputAction = (InputAction)_action.SelectedIndex;
            profile.LimitClicks = _limitEnabled.Checked;
            profile.ClickLimit = decimal.ToInt32(_limit.Value);
            profile.Target = (ClickTarget)_target.SelectedIndex;
            profile.FixedX = decimal.ToInt32(_x.Value);
            profile.FixedY = decimal.ToInt32(_y.Value);
            _editing.MouseActionLimit = decimal.ToInt32(_mouseActionLimit.Value);
            _editing.MaxClicksPerSecond = _maxClicksPerSecond.Value;
        }
        else
        {
            _editing.KeyboardSequenceRepeats = decimal.ToInt32(_keyboardSequenceRepeats.Value);
        }
        if (_activation.SelectedIndex >= 0)
        {
            profile.ActivationMode = (ActivationMode)_activation.SelectedIndex;
        }
    }

    private void OnApplyClick(object? sender, EventArgs e)
    {
        SaveEditor();
        if (_draft.Macros.Count == 0)
        {
            MessageBox.Show(this, _turkish ? "En az bir makro oluşturun." : "Add at least one macro.", Text);
            return;
        }

        MacroDefinition? emptyKeyboardMacro = _draft.Macros.FirstOrDefault(macro =>
            macro.Enabled && macro.InputType == MacroInputType.Keyboard && macro.KeyboardSteps.Count == 0);
        if (emptyKeyboardMacro is not null)
        {
            MessageBox.Show(this,
                _turkish
                    ? $"'{emptyKeyboardMacro.Name}' klavye makrosuna en az bir adım ekleyin."
                    : $"Add at least one step to the '{emptyKeyboardMacro.Name}' keyboard macro.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshMacroList(emptyKeyboardMacro.Id);
            return;
        }

        if (_draft.Macros.Any(macro =>
            macro.Enabled && macro.Profile.ActivationBinding == EmergencyStopBinding))
        {
            MessageBox.Show(this,
                _turkish
                    ? "Acil durdurma tuşu etkin bir makronun kısayoluyla aynı olamaz."
                    : "The emergency stop key cannot match an enabled macro hotkey.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ResultMacros = _draft.Macros;
        _draft.EmergencyStopBinding = EmergencyStopBinding;
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class MacroTypeDialog : Form
{
    public MacroTypeDialog(bool turkish)
    {
        Text = turkish ? "Makro türü" : "Macro type";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 140);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        Font = new Font("Segoe UI", 9F);

        var prompt = new Label
        {
            Text = turkish ? "Oluşturmak istediğiniz makro türünü seçin:" : "Choose the type of macro to create:",
            AutoSize = true,
            Location = new Point(18, 18),
            ForeColor = AppTheme.Text
        };
        var mouse = CreateButton(turkish ? "Fare" : "Mouse");
        mouse.Location = new Point(18, 66);
        mouse.Click += (_, _) => SelectType(MacroInputType.Mouse);
        var keyboard = CreateButton(turkish ? "Klavye" : "Keyboard");
        keyboard.Location = new Point(132, 66);
        keyboard.Click += (_, _) => SelectType(MacroInputType.Keyboard);
        var cancel = CreateButton(turkish ? "İptal" : "Cancel");
        cancel.Location = new Point(246, 66);
        cancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange([prompt, mouse, keyboard, cancel]);
        AcceptButton = mouse;
        CancelButton = cancel;
        AppTheme.Apply(this);
    }

    public MacroInputType InputType { get; private set; }

    private static Button CreateButton(string text) => new()
    {
        Text = text,
        Size = new Size(96, 36),
        FlatStyle = FlatStyle.Flat,
        BackColor = AppTheme.Surface,
        ForeColor = AppTheme.Text,
        FlatAppearance = { BorderColor = AppTheme.Border }
    };

    private void SelectType(MacroInputType inputType)
    {
        InputType = inputType;
        DialogResult = DialogResult.OK;
        Close();
    }
}

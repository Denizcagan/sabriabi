using System.Diagnostics;

namespace ClickerApp;

internal sealed class ClickerOptionsForm : Form
{
    private readonly ClickerSettings _draft;
    private readonly Action<ClickerSettings> _apply;
    private readonly bool _turkish;
    private readonly CheckedListBox _applications = new();
    private readonly CheckBox _restrictApps = new();
    private readonly CheckBox _startup = new();
    private readonly Label _status = new();
    private readonly Button _addApp = new();
    private readonly Button _reset = new();
    private readonly Button _applyButton = new();
    private readonly Button _okButton = new();
    private readonly Button _exitButton = new();

    public ClickerOptionsForm(ClickerSettings settings, Action<ClickerSettings> apply)
    {
        _draft = SettingsStore.Clone(settings);
        _apply = apply;
        _turkish = settings.Language == AppLanguage.Turkish;
        ConfigureWindow();
        BuildInterface();
        LoadApplications();
    }

    private void ConfigureWindow()
    {
        Text = _turkish ? "Sabri Abi — Ayarlar" : "Sabri Abi — Settings";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(540, 500);
        Size = new Size(620, 560);
        Font = new Font("Segoe UI", 9F);
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 5
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        _startup.Text = _turkish ? "Windows ile otomatik başlat" : "Start with Windows";
        _startup.AutoSize = true;
        _startup.Checked = _draft.StartWithWindows;
        root.Controls.Add(_startup, 0, 0);

        _restrictApps.Text = _turkish
            ? "Yalnızca seçili uygulamalar ön plandayken çalıştır"
            : "Run only while a selected app is in the foreground";
        _restrictApps.AutoSize = true;
        _restrictApps.Checked = _draft.RestrictToSelectedApplications;
        root.Controls.Add(_restrictApps, 0, 1);

        var appPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true
        };
        appPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        appPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        appPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _applications.Dock = DockStyle.Fill;
        appPanel.Controls.Add(_applications, 0, 0);

        var appButtons = new FlowLayoutPanel { AutoSize = true };
        _addApp.Text = _turkish ? "＋ Uygulama ekle" : "＋ Add application";
        _addApp.AutoSize = true;
        _addApp.Click += OnAddApplicationClick;
        appButtons.Controls.Add(_addApp);
        appPanel.Controls.Add(appButtons, 0, 1);
        root.Controls.Add(appPanel, 0, 2);

        _status.AutoSize = true;
        _status.Text = _turkish
            ? "Uygulama listesi boşsa kapsam kısıtlaması tüm eylemleri engeller."
            : "When the allowlist is empty, the scope restriction blocks all actions.";
        root.Controls.Add(_status, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        ConfigureButton(_okButton, "✓ OK");
        ConfigureButton(_applyButton, _turkish ? "Uygula" : "Apply");
        ConfigureButton(_reset, _turkish ? "Sıfırla" : "Reset settings");
        ConfigureButton(_exitButton, _turkish ? "Çıkış" : "Exit");
        _okButton.Click += OnOkClick;
        _applyButton.Click += OnApplyClick;
        _reset.Click += OnResetClick;
        _exitButton.Click += (_, _) => Close();
        buttons.Controls.AddRange([
            _exitButton,
            _okButton,
            DisclaimerDialog.CreateReadButton(_turkish),
            _applyButton,
            DisclaimerDialog.CreateReadButton(_turkish),
            _reset
        ]);
        root.Controls.Add(buttons, 0, 4);
        AppTheme.Apply(this);
        AppTheme.StylePrimaryButton(_okButton);
        AppTheme.StylePrimaryButton(_applyButton);
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.AutoSize = true;
        button.Margin = new Padding(3);
    }

    private void LoadApplications()
    {
        var appNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string app in _draft.AllowedApplications)
        {
            if (!string.IsNullOrWhiteSpace(app))
            {
                appNames.Add(app);
            }
        }

        foreach (Process process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero
                        && !string.IsNullOrWhiteSpace(process.MainWindowTitle))
                    {
                        appNames.Add(process.ProcessName);
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
            }
        }

        foreach (string app in appNames)
        {
            int index = _applications.Items.Add(app);
            _applications.SetItemChecked(index, _draft.AllowedApplications.Contains(app, StringComparer.OrdinalIgnoreCase));
        }
    }

    private void OnAddApplicationClick(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Applications (*.exe)|*.exe",
            Title = _turkish ? "İzin verilecek uygulamayı seç" : "Select an allowed application"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string processName = Path.GetFileNameWithoutExtension(dialog.FileName);
        for (int i = 0; i < _applications.Items.Count; i++)
        {
            if (string.Equals(_applications.Items[i]?.ToString(), processName, StringComparison.OrdinalIgnoreCase))
            {
                _applications.SetItemChecked(i, true);
                return;
            }
        }

        int index = _applications.Items.Add(processName);
        _applications.SetItemChecked(index, true);
    }

    private void OnResetClick(object? sender, EventArgs e)
    {
        DialogResult confirmation = MessageBox.Show(
            this,
            _turkish
                ? "Ayarlar varsayılana sıfırlansın mı? Profiller silinmez."
                : "Reset click settings to defaults? Saved profiles will be kept.",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        _draft.ClicksPerSecond = 10;
        _draft.DutyCyclePercent = 50;
        _draft.MouseButton = MouseButtonChoice.Left;
        _draft.ActivationMode = ActivationMode.Toggle;
        _draft.Target = ClickTarget.CurrentCursor;
        _draft.ActivationBinding = InputBinding.Default;
        _draft.LimitClicks = false;
        _draft.ClickLimit = 100;
        _draft.FixedX = 0;
        _draft.FixedY = 0;
        _draft.InputAction = InputAction.Click;
        _draft.RestrictToSelectedApplications = false;
        _draft.AllowedApplications.Clear();
        _draft.StartWithWindows = false;
        _restrictApps.Checked = false;
        _startup.Checked = false;
        _applications.Items.Clear();
        _status.Text = _turkish ? "Varsayılanlar hazır. Kaydetmek için Uygula veya OK seçin." : "Defaults ready. Choose Apply or OK to save.";
    }

    private void OnApplyClick(object? sender, EventArgs e)
    {
        CopyControlsToDraft();
        _apply(SettingsStore.Clone(_draft));
        _status.Text = _turkish ? "Ayarlar uygulandı." : "Settings applied.";
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        CopyControlsToDraft();
        _apply(SettingsStore.Clone(_draft));
        DialogResult = DialogResult.OK;
        Close();
    }

    private void CopyControlsToDraft()
    {
        _draft.StartWithWindows = _startup.Checked;
        _draft.RestrictToSelectedApplications = _restrictApps.Checked;
        _draft.AllowedApplications = _applications.CheckedItems
            .Cast<object>()
            .Select(item => item.ToString()!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

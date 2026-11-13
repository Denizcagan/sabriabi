using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClickerApp;

internal sealed class ClickerProfileForm : Form
{
    private readonly ClickerSettings _draft;
    private readonly bool _turkish;
    private readonly ListBox _profiles = new();
    private readonly TextBox _name = new();
    private readonly Label _description = new();
    private readonly Button _saveButton = new();
    private readonly Button _applyButton = new();
    private readonly Button _importButton = new();
    private readonly Button _exportButton = new();
    private readonly Button _deleteButton = new();
    private readonly Button _closeButton = new();

    public ClickerProfileForm(ClickerSettings settings)
    {
        _draft = SettingsStore.Clone(settings);
        _turkish = settings.Language == AppLanguage.Turkish;
        ConfigureWindow();
        BuildInterface();
        RefreshProfiles(_draft.SelectedProfile);
    }

    public ClickerSettings? ResultSettings { get; private set; }

    private void ConfigureWindow()
    {
        Text = _turkish
            ? "Sabri Abi — Profiller"
            : "Sabri Abi — Profiles";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(540, 380);
        Font = new Font("Segoe UI", 9F);
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 4
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        _profiles.Dock = DockStyle.Fill;
        _profiles.SelectedIndexChanged += (_, _) =>
        {
            if (_profiles.SelectedItem is ClickerProfile profile)
            {
                _name.Text = profile.Name;
                _description.Text = _turkish
                    ? $"CPS {profile.ClicksPerSecond:0.0} · Görev döngüsü %{profile.DutyCyclePercent:0}"
                    : $"CPS {profile.ClicksPerSecond:0.0} · Duty cycle {profile.DutyCyclePercent:0}%";
            }
        };
        root.Controls.Add(_profiles, 0, 0);

        var namePanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        namePanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = _turkish ? "Profil adı:" : "Profile name:",
            Margin = new Padding(3, 7, 8, 3)
        });
        _name.Width = 260;
        namePanel.Controls.Add(_name);
        root.Controls.Add(namePanel, 0, 1);

        _description.AutoSize = true;
        _description.Margin = new Padding(3, 8, 3, 8);
        root.Controls.Add(_description, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        ConfigureButton(_saveButton, _turkish ? "＋ Kaydet" : "＋ Save current");
        ConfigureButton(_applyButton, _turkish ? "✓ Uygula" : "✓ Apply");
        ConfigureButton(_importButton, _turkish ? "↓ İçe aktar" : "↓ Import");
        ConfigureButton(_exportButton, _turkish ? "↑ Dışa aktar" : "↑ Export");
        ConfigureButton(_deleteButton, _turkish ? "× Sil" : "× Delete");
        ConfigureButton(_closeButton, _turkish ? "Çıkış" : "Exit");
        _saveButton.Click += OnSaveCurrentClick;
        _applyButton.Click += OnApplyClick;
        _importButton.Click += OnImportClick;
        _exportButton.Click += OnExportClick;
        _deleteButton.Click += OnDeleteClick;
        _closeButton.Click += (_, _) => Close();
        buttons.Controls.AddRange([
            _saveButton, DisclaimerDialog.CreateReadButton(_turkish),
            _applyButton, DisclaimerDialog.CreateReadButton(_turkish),
            _importButton, _exportButton, _deleteButton, _closeButton
        ]);
        root.Controls.Add(buttons, 0, 3);
        AppTheme.Apply(this);
        AppTheme.StylePrimaryButton(_applyButton);
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.AutoSize = true;
        button.Margin = new Padding(3);
    }

    private void RefreshProfiles(string? selectedName = null)
    {
        _profiles.BeginUpdate();
        _profiles.Items.Clear();
        _profiles.DisplayMember = nameof(ClickerProfile.Name);
        foreach (ClickerProfile profile in _draft.Profiles)
        {
            _profiles.Items.Add(profile);
            if (string.Equals(profile.Name, selectedName, StringComparison.OrdinalIgnoreCase))
            {
                _profiles.SelectedItem = profile;
            }
        }
        _profiles.EndUpdate();
        _deleteButton.Enabled = _profiles.Items.Count > 0;
        _exportButton.Enabled = _profiles.Items.Count > 0;
        _applyButton.Enabled = _profiles.Items.Count > 0;
    }

    private void OnSaveCurrentClick(object? sender, EventArgs e)
    {
        string name = _name.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError(_turkish ? "Bir profil adı girin." : "Enter a profile name.");
            return;
        }

        ClickerProfile? existing = _draft.Profiles.FirstOrDefault(profile =>
            string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null && _profiles.SelectedItem != existing)
        {
            ShowError(_turkish ? "Bu profil adı zaten var." : "A profile with that name already exists.");
            return;
        }

        ClickerProfile current = SettingsStore.CreateProfile(_draft, name);
        if (existing is null)
        {
            _draft.Profiles.Add(current);
        }
        else
        {
            _draft.Profiles[_draft.Profiles.IndexOf(existing)] = current;
        }

        _draft.SelectedProfile = name;
        RefreshProfiles(name);
    }

    private void OnApplyClick(object? sender, EventArgs e)
    {
        if (_profiles.SelectedItem is not ClickerProfile profile)
        {
            ShowError(_turkish ? "Önce bir profil seçin." : "Select a profile first.");
            return;
        }

        SettingsStore.ApplyProfile(_draft, profile);
        ResultSettings = SettingsStore.Clone(_draft);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnImportClick(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON profile (*.json)|*.json",
            Title = _turkish ? "Profil içe aktar" : "Import profile"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            ClickerProfile profile = JsonSerializer.Deserialize<ClickerProfile>(json, options)
                ?? throw new InvalidDataException("Profile file is empty.");
            ValidateProfile(profile);
            ClickerProfile? duplicate = _draft.Profiles.FirstOrDefault(item =>
                string.Equals(item.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
            {
                profile.Name = $"{profile.Name} (imported)";
            }

            _draft.Profiles.Add(profile);
            RefreshProfiles(profile.Name);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            ShowError($"{(_turkish ? "Profil içe aktarılamadı." : "Profile import failed.")}\n{exception.Message}");
        }
    }

    private void OnExportClick(object? sender, EventArgs e)
    {
        if (_profiles.SelectedItem is not ClickerProfile profile)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "JSON profile (*.json)|*.json",
            FileName = $"{MakeSafeFileName(profile.Name)}.json",
            Title = _turkish ? "Profili dışa aktar" : "Export profile"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new JsonStringEnumConverter());
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(profile, options));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError($"{(_turkish ? "Profil dışa aktarılamadı." : "Profile export failed.")}\n{exception.Message}");
        }
    }

    private void OnDeleteClick(object? sender, EventArgs e)
    {
        if (_profiles.SelectedItem is not ClickerProfile profile)
        {
            return;
        }

        _draft.Profiles.Remove(profile);
        if (_draft.SelectedProfile == profile.Name)
        {
            _draft.SelectedProfile = "Default";
        }

        RefreshProfiles();
    }

    private static void ValidateProfile(ClickerProfile profile)
    {
        bool validBinding = profile.ActivationBinding.Kind switch
        {
            InputKind.Keyboard => profile.ActivationBinding.Code is > 0 and <= 255,
            InputKind.Mouse => Enum.IsDefined((MouseButtonChoice)profile.ActivationBinding.Code),
            _ => false
        };

        if (string.IsNullOrWhiteSpace(profile.Name)
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
            || !validBinding)
        {
            throw new InvalidDataException("Profile settings are invalid.");
        }
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return name;
    }

    private void ShowError(string message) =>
        MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

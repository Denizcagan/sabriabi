using System.Diagnostics;

namespace ClickerApp;

internal static class DisclaimerDialog
{
    private const string RepositoryUrl = "https://github.com/Denizcagan/sabriabi";

    public static Button CreateReadButton(bool turkish)
    {
        var button = new Button
        {
            Text = turkish ? "Oku" : "Read",
            AutoSize = true,
            Margin = new Padding(3)
        };
        button.Click += (_, _) => Show(button.FindForm(), turkish);
        return button;
    }

    private static void Show(IWin32Window? owner, bool turkish)
    {
        using var dialog = new Form
        {
            Text = turkish ? "Sabri Abi — Kullanım bildirimi" : "Sabri Abi — Usage notice",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(540, 270),
            BackColor = AppTheme.Background,
            ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI", 9F)
        };

        var message = new Label
        {
            Text = turkish
                ? "Sabri Abi, sahibi olduğunuz veya geliştirdiğiniz oyunları denemek için hazırlanmış bir makro programıdır. Haksız avantaj sağlamak veya anti-cheat sistemlerini atlatmak için tasarlanmamıştır. Kendi Minecraft sunucumun anti-cheat sistemi üzerinde denedim.\r\n\r\nYalnızca izin verilen yerlerde ve ilgili oyun, sunucu ve platform kurallarına uygun kullanın. Test amacıyla kullanım da kısıtlanabilir. Yazılımı kullanma ve kullanımından doğan sonuçların sorumluluğu size aittir."
                : "Sabri Abi is a macro program intended for testing games that you own or develop. It is not designed to provide an unfair advantage or bypass anti-cheat systems. I tested it on the anti-cheat system of my own Minecraft server.\r\n\r\nUse it only where permitted and in accordance with the relevant game, server, and platform rules. Testing use may also be restricted. You are responsible for your use of the software and its consequences.",
            Location = new Point(18, 18),
            Size = new Size(504, 166),
            ForeColor = AppTheme.Text
        };
        var repository = new LinkLabel
        {
            Text = RepositoryUrl,
            Location = new Point(18, 196),
            AutoSize = true,
            LinkColor = AppTheme.Accent
        };
        repository.LinkClicked += (_, _) => OpenRepository(dialog, turkish);
        var close = new Button
        {
            Text = turkish ? "Kapat" : "Close",
            AutoSize = true,
            DialogResult = DialogResult.OK,
            Location = new Point(440, 220),
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.Text,
            FlatStyle = FlatStyle.Flat
        };
        close.FlatAppearance.BorderColor = AppTheme.Border;
        dialog.Controls.AddRange([message, repository, close]);
        dialog.AcceptButton = close;
        AppTheme.Apply(dialog);
        dialog.ShowDialog(owner);
    }

    private static void OpenRepository(IWin32Window owner, bool turkish)
    {
        try
        {
            Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(owner,
                turkish ? $"GitHub bağlantısı açılamadı.\r\n{exception.Message}" : $"Could not open the GitHub link.\r\n{exception.Message}",
                turkish ? "Bağlantı hatası" : "Link error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

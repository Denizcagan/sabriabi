namespace ClickerApp;

internal static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(242, 245, 248);
    public static readonly Color Surface = Color.White;
    public static readonly Color Input = Color.FromArgb(248, 250, 252);
    public static readonly Color Border = Color.FromArgb(203, 213, 225);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color MutedText = Color.FromArgb(100, 116, 139);
    public static readonly Color Accent = Color.FromArgb(15, 118, 110);
    public static readonly Color AccentHover = Color.FromArgb(17, 94, 89);
    public static readonly Color AccentTint = Color.FromArgb(220, 243, 240);
    public static readonly Color Success = Color.FromArgb(21, 128, 61);
    public static readonly Color Warning = Color.FromArgb(180, 83, 9);

    public static void Apply(Control root)
    {
        ApplyControl(root, root.Parent?.BackColor ?? Background);
        root.BackColor = Background;
    }

    public static void StylePrimaryButton(Button button)
    {
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.FlatAppearance.BorderColor = Accent;
        button.FlatAppearance.MouseOverBackColor = AccentHover;
        button.FlatAppearance.MouseDownBackColor = AccentHover;
    }

    private static void ApplyControl(Control control, Color inheritedBackground)
    {
        Color childBackground = inheritedBackground;
        switch (control)
        {
            case Form form:
                form.BackColor = Background;
                form.ForeColor = Text;
                childBackground = Background;
                break;
            case GroupBox group:
                group.BackColor = Surface;
                group.ForeColor = Accent;
                group.FlatStyle = FlatStyle.Flat;
                childBackground = Surface;
                break;
            case TableLayoutPanel table:
                table.BackColor = inheritedBackground;
                childBackground = inheritedBackground;
                break;
            case FlowLayoutPanel flow:
                flow.BackColor = inheritedBackground;
                childBackground = inheritedBackground;
                break;
            case Panel panel:
                panel.BackColor = Surface;
                childBackground = Surface;
                break;
            case Button button:
                button.BackColor = Surface;
                button.ForeColor = Text;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.MouseOverBackColor = AccentTint;
                button.FlatAppearance.MouseDownBackColor = AccentTint;
                button.Cursor = Cursors.Hand;
                break;
            case ComboBox combo:
                combo.BackColor = Input;
                combo.ForeColor = Text;
                combo.FlatStyle = FlatStyle.Flat;
                break;
            case NumericUpDown numeric:
                numeric.BackColor = Input;
                numeric.ForeColor = Text;
                break;
            case TextBox textBox:
                textBox.BackColor = Input;
                textBox.ForeColor = Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case CheckedListBox checkedList:
                checkedList.BackColor = Surface;
                checkedList.ForeColor = Text;
                checkedList.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListBox listBox:
                listBox.BackColor = Surface;
                listBox.ForeColor = Text;
                listBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case CheckBox checkBox:
                checkBox.BackColor = inheritedBackground;
                checkBox.ForeColor = Text;
                break;
            case Label label:
                label.BackColor = Color.Transparent;
                label.ForeColor = Text;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyControl(child, childBackground);
        }
    }
}

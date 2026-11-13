using System.Drawing.Drawing2D;

namespace ClickerApp;

internal sealed class MousePreviewControl : Control
{
    private MouseButtonChoice _selectedButton;

    public MousePreviewControl()
    {
        DoubleBuffered = true;
        Size = new Size(190, 185);
        Cursor = Cursors.Hand;
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.Text;
        AccessibleName = "Select a mouse button";
    }

    public event Action<MouseButtonChoice>? MouseButtonSelected;

    public MouseButtonChoice SelectedButton
    {
        get => _selectedButton;
        set
        {
            if (_selectedButton == value)
            {
                return;
            }

            _selectedButton = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        Rectangle mouseBounds = new(
            (ClientSize.Width - 92) / 2,
            7,
            92,
            Math.Max(150, ClientSize.Height - 18));
        using var mousePath = CreateRoundedMousePath(mouseBounds);

        using var bodyBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
        e.Graphics.FillPath(bodyBrush, mousePath);

        using var outline = new Pen(AppTheme.Border, 2f);
        e.Graphics.DrawPath(outline, mousePath);

        Rectangle leftButton = new(mouseBounds.X + 3, mouseBounds.Y + 3, 42, 60);
        Rectangle rightButton = new(mouseBounds.X + 47, mouseBounds.Y + 3, 42, 60);
        DrawButton(e.Graphics, leftButton, MouseButtonChoice.Left);
        DrawButton(e.Graphics, rightButton, MouseButtonChoice.Right);

        using var centerDivider = new Pen(AppTheme.MutedText, 2f);
        e.Graphics.DrawLine(centerDivider, mouseBounds.X + mouseBounds.Width / 2, mouseBounds.Y + 3,
            mouseBounds.X + mouseBounds.Width / 2, mouseBounds.Y + 61);

        Rectangle wheel = new(mouseBounds.X + 39, mouseBounds.Y + 20, 14, 29);
        using var wheelBrush = new SolidBrush(AppTheme.Accent);
        e.Graphics.FillRectangle(wheelBrush, wheel);
        using var wheelLine = new Pen(Color.White, 1.5f);
        e.Graphics.DrawLine(wheelLine, wheel.X + 7, wheel.Y + 6, wheel.X + 7, wheel.Y + 13);
        e.Graphics.DrawLine(wheelLine, wheel.X + 7, wheel.Y + 18, wheel.X + 7, wheel.Y + 23);

        DrawSideButton(e.Graphics, mouseBounds, MouseButtonChoice.XButton1, true);
        DrawSideButton(e.Graphics, mouseBounds, MouseButtonChoice.XButton2, false);

        using var labelBrush = new SolidBrush(AppTheme.MutedText);
        using var labelFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        e.Graphics.DrawString("MIDDLE", labelFont, labelBrush,
            mouseBounds.X + 21, mouseBounds.Y + 73);
        e.Graphics.DrawString("X1", labelFont, labelBrush,
            mouseBounds.X + 4, mouseBounds.Y + 116);
        e.Graphics.DrawString("X2", labelFont, labelBrush,
            mouseBounds.X + 69, mouseBounds.Y + 116);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        Rectangle mouseBounds = new((ClientSize.Width - 92) / 2, 7, 92, Math.Max(150, ClientSize.Height - 18));
        Rectangle leftButton = new(mouseBounds.X + 3, mouseBounds.Y + 3, 42, 60);
        Rectangle rightButton = new(mouseBounds.X + 47, mouseBounds.Y + 3, 42, 60);
        Rectangle wheel = new(mouseBounds.X + 39, mouseBounds.Y + 20, 14, 29);
        Rectangle sideButton1 = new(mouseBounds.X - 5, mouseBounds.Y + 91, 8, 34);
        Rectangle sideButton2 = new(mouseBounds.Right - 2, mouseBounds.Y + 91, 8, 34);

        MouseButtonChoice? selected = e.Button == MouseButtons.Left
            ? sideButton1.Contains(e.Location) ? MouseButtonChoice.XButton1
            : sideButton2.Contains(e.Location) ? MouseButtonChoice.XButton2
            : wheel.Contains(e.Location) ? MouseButtonChoice.Middle
            : leftButton.Contains(e.Location) ? MouseButtonChoice.Left
            : rightButton.Contains(e.Location) ? MouseButtonChoice.Right
            : null
            : null;

        if (selected is MouseButtonChoice button)
        {
            SelectedButton = button;
            MouseButtonSelected?.Invoke(button);
        }
    }

    private void DrawButton(Graphics graphics, Rectangle bounds, MouseButtonChoice button)
    {
        Rectangle adjusted = bounds;
        adjusted.Inflate(-1, -1);
        using var brush = new SolidBrush(button == _selectedButton
            ? AppTheme.Accent
            : AppTheme.Border);
        graphics.FillRectangle(brush, adjusted);
    }

    private void DrawSideButton(Graphics graphics, Rectangle mouseBounds, MouseButtonChoice button, bool left)
    {
        Rectangle side = new(
            left ? mouseBounds.X - 5 : mouseBounds.Right - 2,
            mouseBounds.Y + 91,
            8,
            34);
        using var brush = new SolidBrush(button == _selectedButton
            ? AppTheme.Accent
            : AppTheme.MutedText);
        graphics.FillRectangle(brush, side);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedMousePath(Rectangle bounds)
    {
        float radius = bounds.Width / 2f;
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, bounds.Width, bounds.Width, 180, 180);
        path.AddLine(bounds.Right, bounds.Y + radius, bounds.Right, bounds.Bottom - radius);
        path.AddArc(bounds.X, bounds.Bottom - bounds.Width, bounds.Width, bounds.Width, 0, 180);
        path.CloseFigure();
        return path;
    }
}

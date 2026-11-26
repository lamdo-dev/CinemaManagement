using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Windows.Forms;

namespace CinemaManagement.Utils;

internal sealed class NeonThemeButton : Button
{
    private bool _isHovered;
    private bool _isPressed;
    private bool _isDarkTheme = true;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value)
                return;

            _isDarkTheme = value;
            AccessibleDescription = value
                ? "Giao diện tối đang bật. Nhấn để chuyển sang giao diện sáng."
                : "Giao diện sáng đang bật. Nhấn để chuyển sang giao diện tối.";
            Invalidate();
        }
    }

    public NeonThemeButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        AccessibleName = "Chuyển đổi giao diện sáng tối";
        AccessibleDescription = "Giao diện tối đang bật. Nhấn để chuyển sang giao diện sáng.";
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Color background = Parent?.BackColor ?? SystemColors.Control;
        using var brush = new SolidBrush(background);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle bounds = Rectangle.Inflate(ClientRectangle, -3, -3);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        int radius = bounds.Height / 2;
        using GraphicsPath trackPath = CreateRoundedPath(bounds, radius);
        Color trackColor = _isDarkTheme
            ? Color.FromArgb(25, 37, 61)
            : Color.FromArgb(250, 204, 21);
        Color borderColor = _isHovered
            ? (_isDarkTheme ? Color.FromArgb(108, 157, 224) : Color.FromArgb(202, 138, 4))
            : (_isDarkTheme ? Color.FromArgb(57, 75, 108) : Color.FromArgb(234, 179, 8));

        using (var trackBrush = new SolidBrush(trackColor))
            graphics.FillPath(trackBrush, trackPath);

        using (var borderPen = new Pen(borderColor, _isHovered ? 1.6F : 1F))
            graphics.DrawPath(borderPen, trackPath);

        int thumbSize = Math.Max(16, bounds.Height - 8);
        int thumbX = _isDarkTheme
            ? bounds.Right - thumbSize - 4
            : bounds.Left + 4;
        int thumbY = bounds.Top + (bounds.Height - thumbSize) / 2 + (_isPressed ? 1 : 0);
        var thumbBounds = new Rectangle(thumbX, thumbY, thumbSize, thumbSize);

        using (var shadowBrush = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
        {
            Rectangle shadowBounds = thumbBounds;
            shadowBounds.Offset(0, 1);
            graphics.FillEllipse(shadowBrush, shadowBounds);
        }

        Color thumbColor = _isDarkTheme
            ? Color.FromArgb(248, 250, 252)
            : Color.FromArgb(255, 244, 184);
        using (var thumbBrush = new SolidBrush(thumbColor))
            graphics.FillEllipse(thumbBrush, thumbBounds);

        if (_isDarkTheme)
            DrawMoon(graphics, thumbBounds, thumbColor);
        else
            DrawSun(graphics, thumbBounds);

        if (_isDarkTheme)
            DrawStars(graphics, bounds, thumbBounds);

        if (Focused && ShowFocusCues)
        {
            Rectangle focusBounds = Rectangle.Inflate(bounds, -2, -2);
            ControlPaint.DrawFocusRectangle(graphics, focusBounds, Color.White, trackColor);
        }
    }

    private static void DrawSun(Graphics graphics, Rectangle thumbBounds)
    {
        float centerX = thumbBounds.Left + thumbBounds.Width / 2F;
        float centerY = thumbBounds.Top + thumbBounds.Height / 2F;
        float innerRadius = thumbBounds.Width * 0.19F;
        float outerRadius = thumbBounds.Width * 0.32F;
        using var rayPen = new Pen(Color.FromArgb(217, 119, 6), Math.Max(1F, thumbBounds.Width * 0.055F));

        for (int angle = 0; angle < 360; angle += 45)
        {
            double radians = angle * Math.PI / 180;
            float startX = centerX + (float)Math.Cos(radians) * innerRadius;
            float startY = centerY + (float)Math.Sin(radians) * innerRadius;
            float endX = centerX + (float)Math.Cos(radians) * outerRadius;
            float endY = centerY + (float)Math.Sin(radians) * outerRadius;
            graphics.DrawLine(rayPen, startX, startY, endX, endY);
        }

        float sunSize = thumbBounds.Width * 0.26F;
        using var sunBrush = new SolidBrush(Color.FromArgb(245, 158, 11));
        graphics.FillEllipse(
            sunBrush,
            centerX - sunSize / 2F,
            centerY - sunSize / 2F,
            sunSize,
            sunSize);
    }

    private static void DrawMoon(Graphics graphics, Rectangle thumbBounds, Color thumbColor)
    {
        float moonSize = thumbBounds.Width * 0.43F;
        float moonX = thumbBounds.Left + (thumbBounds.Width - moonSize) / 2F;
        float moonY = thumbBounds.Top + (thumbBounds.Height - moonSize) / 2F;
        using var moonBrush = new SolidBrush(Color.FromArgb(51, 65, 85));
        using var cutoutBrush = new SolidBrush(thumbColor);
        graphics.FillEllipse(moonBrush, moonX, moonY, moonSize, moonSize);
        graphics.FillEllipse(
            cutoutBrush,
            moonX + moonSize * 0.38F,
            moonY - moonSize * 0.16F,
            moonSize,
            moonSize);
    }

    private static void DrawStars(Graphics graphics, Rectangle bounds, Rectangle thumbBounds)
    {
        using var starBrush = new SolidBrush(Color.FromArgb(190, 211, 235));
        float starY = bounds.Top + bounds.Height / 2F;
        graphics.FillEllipse(starBrush, bounds.Left + bounds.Width * 0.28F, starY - 1.4F, 2.8F, 2.8F);
        graphics.FillEllipse(starBrush, bounds.Left + bounds.Width * 0.41F, starY - 3.2F, 2F, 2F);
        graphics.FillEllipse(starBrush, thumbBounds.Left - 8F, starY + 2F, 1.8F, 1.8F);
    }

    protected override void OnMouseEnter(System.EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(System.EventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _isPressed = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _isPressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2F;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
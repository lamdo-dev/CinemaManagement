using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CinemaManagement.Utils;

internal sealed class NeonThemeButton : Button
{
    private bool _isHovered;
    private bool _isPressed;

    internal Color NeonColor = Color.FromArgb(0, 245, 212);

    public NeonThemeButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
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

        Rectangle bounds = ClientRectangle;
        bounds.Inflate(-5, -5);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        using GraphicsPath path = CreateRoundedPath(bounds, 9);
        using var backgroundBrush = new SolidBrush(BackColor);
        graphics.FillPath(backgroundBrush, path);

        int outerAlpha = _isPressed ? 38 : _isHovered ? 48 : 25;
        int middleAlpha = _isPressed ? 80 : _isHovered ? 100 : 62;
        using var outerGlow = new Pen(Color.FromArgb(outerAlpha, NeonColor), 9F);
        using var middleGlow = new Pen(Color.FromArgb(middleAlpha, NeonColor), 5F);
        using var neonBorder = new Pen(NeonColor, _isHovered ? 2.2F : 1.6F);
        graphics.DrawPath(outerGlow, path);
        graphics.DrawPath(middleGlow, path);
        graphics.DrawPath(neonBorder, path);

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        if (Focused && ShowFocusCues)
        {
            Rectangle focusBounds = Rectangle.Inflate(bounds, -5, -5);
            ControlPaint.DrawFocusRectangle(graphics, focusBounds, ForeColor, BackColor);
        }
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
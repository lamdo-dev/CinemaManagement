using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace CinemaManagement.Utils;

internal static class ThemeColorApplicator
{
    private sealed class OriginalColors
    {
        public OriginalColors(Color backColor, Color foreColor)
        {
            BackColor = backColor;
            ForeColor = foreColor;
        }

        public Color BackColor { get; }
        public Color ForeColor { get; }
        public bool IsDark { get; set; }
    }

    private static readonly ConditionalWeakTable<Control, OriginalColors> OriginalColorsByControl = new();

    public static void Apply(Control control, bool isDark)
    {
        OriginalColors colors = OriginalColorsByControl.GetValue(
            control,
            item => new OriginalColors(item.BackColor, item.ForeColor));
        colors.IsDark = isDark;

        control.ControlAdded -= OnControlAdded;
        control.ControlAdded += OnControlAdded;

        control.BackColor = GetBackgroundColor(colors.BackColor, control, isDark);
        control.ForeColor = GetForegroundColor(colors.ForeColor, colors.BackColor, isDark);

        if (control is DateTimePicker dateTimePicker)
        {
            dateTimePicker.CalendarMonthBackground = isDark
                ? Color.FromArgb(21, 29, 48)
                : Color.White;
            dateTimePicker.CalendarForeColor = isDark
                ? Color.FromArgb(226, 232, 240)
                : Color.FromArgb(30, 41, 59);
            dateTimePicker.CalendarTitleBackColor = isDark
                ? Color.FromArgb(27, 38, 59)
                : Color.FromArgb(226, 232, 240);
            dateTimePicker.CalendarTitleForeColor = isDark
                ? Color.FromArgb(226, 232, 240)
                : Color.FromArgb(30, 41, 59);
        }

        if (control is DataGridView grid)
            ApplyGridTheme(grid, isDark);

        foreach (Control child in control.Controls)
            Apply(child, isDark);
    }

    private static void OnControlAdded(object? sender, ControlEventArgs e)
    {
        if (sender is Control parent &&
            OriginalColorsByControl.TryGetValue(parent, out OriginalColors? colors) &&
            colors != null &&
            e.Control is Control child)
        {
            Apply(child, colors.IsDark);
        }
    }

    private static Color GetBackgroundColor(Color original, Control control, bool isDark)
    {
        if (original.IsEmpty || original.A == 0 || IsAccentColor(original))
            return original;

        double luminance = GetLuminance(original);
        if (isDark)
        {
            if (luminance >= 0.88)
                return Color.FromArgb(27, 38, 59);
            if (luminance >= 0.42)
                return Color.FromArgb(27, 38, 59);
            if (luminance >= 0.16)
                return Color.FromArgb(21, 29, 48);

            return control is UserControl or Form
                ? Color.FromArgb(11, 16, 32)
                : Color.FromArgb(21, 29, 48);
        }

        if (luminance < 0.16)
            return control is UserControl or Form
                ? Color.FromArgb(241, 245, 249)
                : Color.White;
        if (luminance < 0.40)
            return Color.White;
        if (luminance < 0.72)
            return Color.FromArgb(226, 232, 240);

        return original;
    }

    private static Color GetForegroundColor(Color original, Color originalBackColor, bool isDark)
    {
        if (original.IsEmpty || IsAccentColor(original) || IsAccentColor(originalBackColor))
            return original;

        double luminance = GetLuminance(original);
        if (isDark && luminance <= 0.82)
            return Color.FromArgb(203, 213, 225);

        if (luminance < 0.36 || luminance > 0.82)
            return isDark
                ? Color.FromArgb(226, 232, 240)
                : Color.FromArgb(30, 41, 59);

        if (!isDark && luminance < 0.60)
            return Color.FromArgb(71, 85, 105);

        return original;
    }

    private static void ApplyGridTheme(DataGridView grid, bool isDark)
    {
        Color background = isDark ? Color.FromArgb(11, 16, 32) : Color.White;
        Color surface = isDark ? Color.FromArgb(21, 29, 48) : Color.White;
        Color elevated = isDark ? Color.FromArgb(27, 38, 59) : Color.FromArgb(226, 232, 240);
        Color text = isDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);
        Color border = isDark ? Color.FromArgb(42, 56, 86) : Color.FromArgb(203, 213, 225);

        grid.BackgroundColor = background;
        grid.GridColor = border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = elevated;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = text;
        grid.RowHeadersDefaultCellStyle.BackColor = elevated;
        grid.RowHeadersDefaultCellStyle.ForeColor = text;
        grid.DefaultCellStyle.BackColor = surface;
        grid.DefaultCellStyle.ForeColor = text;
        grid.DefaultCellStyle.SelectionBackColor = isDark
            ? Color.FromArgb(6, 78, 59)
            : Color.FromArgb(209, 250, 229);
        grid.DefaultCellStyle.SelectionForeColor = isDark
            ? Color.White
            : Color.FromArgb(6, 78, 59);
        grid.AlternatingRowsDefaultCellStyle.BackColor = isDark
            ? Color.FromArgb(27, 38, 59)
            : Color.FromArgb(248, 250, 252);
        grid.AlternatingRowsDefaultCellStyle.ForeColor = text;
    }

    private static bool IsAccentColor(Color color)
    {
        return color.A != 0 && color.GetSaturation() >= 0.35f && color.GetBrightness() >= 0.30f;
    }

    private static double GetLuminance(Color color)
    {
        return (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
    }
}
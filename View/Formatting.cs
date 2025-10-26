using System;
using System.Drawing;

namespace View;
public static class Formatting
{
    public static string TightTime(TimeSpan ts)
        => $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
    public static string Fire(double v, int digits = 2) => $"{v.ToString($"F{digits}")} 火";
}

public static class Styles
{
    public static readonly Color Bg = Color.FromArgb(35, 35, 35);
    public static readonly Color Card = Color.FromArgb(45, 45, 45);
    public static readonly Color Text = Color.White;
    public static readonly Color Accent = Color.FromArgb(0, 150, 255);
    public static readonly Color Profit = Color.FromArgb(0, 220, 120);
    public static readonly Color Grid = Color.FromArgb(70, 70, 70);
}
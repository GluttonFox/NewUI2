// Utils/ControlsUtil.cs
namespace Utils;
using System.Drawing;
using System.Windows.Forms;

public static class ControlsUtil
{
    public static void EnableDoubleBuffer(Control c)
        => typeof(Control).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        )?.SetValue(c, true, null);

    public static void ApplyRoundedRegion(Control c, int radius)
    {
        if (c.Width <= 0 || c.Height <= 0) return;
        using var path = DrawingExtensions.RoundedRect(new Rectangle(0, 0, c.Width, c.Height), radius);
        c.Region?.Dispose();
        c.Region = new Region(path);
    }
}

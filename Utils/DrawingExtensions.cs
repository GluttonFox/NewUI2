namespace Utils;
using System.Drawing;
using System.Drawing.Drawing2D;

public static class DrawingExtensions
{
    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var gp = new GraphicsPath();
        int d = radius * 2;
        gp.AddArc(r.X, r.Y, d, d, 180, 90);
        gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        gp.CloseFigure();
        return gp;
    }

    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle r, int radius)
    {
        using var path = RoundedRect(r, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle r, int radius)
    {
        using var path = RoundedRect(r, radius);
        g.DrawPath(pen, path);
    }
}

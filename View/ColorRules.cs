// View/ColorRules.cs
namespace View;
using System.Drawing;

public static class ColorRules
{
    public static Color ProfitColor(double v) =>
        v < 0 ? Color.Red :
        v == 0 ? Color.Gray :
        v < 1000 ? Color.LimeGreen :
        v < 2000 ? Color.Orange : Color.Gold;
}

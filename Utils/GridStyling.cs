namespace Utils;
using System.Drawing;
using System.Windows.Forms;

public static class GridStyling
{
    public static void ApplyDarkSkin(DataGridView g)
    {
        g.BackgroundColor = Color.FromArgb(45, 45, 45);
        g.BorderStyle = BorderStyle.None;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.ReadOnly = true;
        g.RowHeadersVisible = false;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.EnableHeadersVisualStyles = false;
        g.GridColor = Color.FromArgb(70, 70, 70);
        g.RowTemplate.Height = 28;

        g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        g.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            SelectionBackColor = Color.FromArgb(70, 70, 70),
            SelectionForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 9f),
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
    }
}

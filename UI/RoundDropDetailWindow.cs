using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static NewUI.Managers.CurrentDropManager;

public class RoundDropDetailWindow : Form
{
    private readonly DropRound _round;
    private DataGridView _grid;

    public RoundDropDetailWindow(DropRound round)
    {
        _round = round;
        Text = $"第 {_round.RoundNumber} 轮 · {_round.SceneName} · 详情";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Size = new Size(560, 420);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(35, 35, 35);

        BuildUI();
        LoadData();
    }

    private void BuildUI()
    {
        var header = new Label
        {
            Dock = DockStyle.Top,
            Height = 48,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
            Padding = new Padding(12, 0, 12, 0),
            Text = $"地图：{_round.SceneName}    用时：{FormatTimeSpan(_round.Duration)}"
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(45, 45, 45),
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false
        };

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            SelectionBackColor = Color.FromArgb(70, 70, 70),
            SelectionForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 9f),
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        _grid.GridColor = Color.FromArgb(70, 70, 70);
        _grid.RowTemplate.Height = 28;

        _grid.Columns.Add("ItemName", "物品");
        _grid.Columns.Add("Quantity", "数量");
        _grid.Columns.Add("TotalValue", "总价值(火)");

        // 禁用排序（避免误点排序导致顺序变化）
        foreach (DataGridViewColumn col in _grid.Columns)
            col.SortMode = DataGridViewColumnSortMode.NotSortable;

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(12)
        };
        var closeBtn = new Button
        {
            Text = "关闭",
            Dock = DockStyle.Right,
            Width = 88
        };
        closeBtn.Click += (_, __) => Close();
        footer.Controls.Add(closeBtn);

        Controls.Add(_grid);
        Controls.Add(footer);
        Controls.Add(header);
    }

    private void LoadData()
    {
        _grid.Rows.Clear();

        if (_round.DropItems != null)
        {
            foreach (var it in _round.DropItems.OrderByDescending(x => x.TotalValue))
            {
                _grid.Rows.Add(it.ItemName, it.Quantity, it.TotalValue.ToString("F3"));
            }
        }

        // 汇总行
        double total = _round.DropItems?.Sum(i => i.TotalValue) ?? 0;
        _grid.Rows.Add("—— 合计 ——", "", total.ToString("F3"));
        _grid.Rows[_grid.Rows.Count - 1].DefaultCellStyle.Font =
            new Font("Microsoft YaHei", 9f, FontStyle.Bold);
    }

    // 你已有的 FormatTimeSpan，可直接用；如果不在当前作用域，也可以再写一份：
    private static string FormatTimeSpan(TimeSpan ts)
        => $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
}

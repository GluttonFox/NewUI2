using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using GameLogMonitor;
using NewUI.Managers;
using NewUI.UI;
using Utils;
using View;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using static NewUI.Managers.CurrentDropManager;


namespace NewUI
{
    /// <summary>
    /// 左侧标签 + 内容区；多个页面（收益/交易/刷图/物价/设置）统一风格
    /// 纯 Dock 布局，避免初始化尺寸造成的空白
    /// </summary>
    public class NewStatsDetailControl : UserControl
    {

        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        // 主题色
        private readonly Color _backgroundColor = Color.FromArgb(35, 35, 35);
        private readonly Color _textColor = Color.White;
        private readonly Color _accentColor = Color.FromArgb(0, 150, 255);
        private readonly Color _profitColor = Color.FromArgb(0, 220, 120);
        private readonly Color _cardBackground = Color.FromArgb(45, 45, 45);

        // 左侧栏 + 内容区
        private Panel _tabBar;
        private Panel _contentHost;

        // ===== 标签按钮 =====
        private Button _tabRevenue;
        private Button _tabTrading;
        private Button _tabFarming;
        private Button _tabPrices;   // 新增：物价
        private Button _tabSettings; // 新增：设置
        private Button _tabClose; // 新增：关闭

        // ===== 根面板 =====
        private Panel _revenueRoot;
        private Panel _tradingRoot;
        private Panel _farmingRoot;
        private Panel _pricesRoot;   // 新增
        private Panel _settingsRoot; // 新增

        // 对外视图枚举/属性（新增 Prices/Settings）
        public enum DetailView { Revenue, Trading, Farming, Prices, Settings }
        public DetailView SelectedView { get; private set; } = DetailView.Revenue;

        // 数据控件引用（用于实时更新）
        private Label _revenueSummaryInfo;
        private Label _tradingSummaryInfo;
        private Label _farmingSummaryInfo;
        private DataGridView _pricesGrid;
        private Label _pricesSummaryInfo;
        private DataGridView _tradingItemsGrid;
        private DataGridView _farmingItemsGrid;
        private DataGridView _revenueItemsGrid;
        // 收益页：覆盖层（就地显示详情）
        private Panel _revenueOverlay;               // 盖在蓝框区域之上的层
        private DataGridView _revenueDetailGrid;     // 详情表格
        private Label _revenueOverlayTitle;          // 标题
        private DropRound _revenueOverlayRound;      // 当前显示的轮次

        private CustomScrollBar _farmingVBar;
        private CustomScrollBar _tradingVBar;
        private CustomScrollBar _pricesVBar;
        private CustomScrollBar _revenueVBar;   // 覆盖层详情表格用


        private FlowLayoutPanel _revenueCardsPanel;
        private ScrollContainer _revenueCardsContainer;


        public NewStatsDetailControl()
        {
            InitializeComponent();
        }

        public void SelectView(DetailView dv) => ShowView(dv);

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = _backgroundColor;
            Font = new Font("Microsoft YaHei", 9f);

            // 先添加 Fill，再添加 Left（Dock 顺序很重要）
            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _backgroundColor,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            Controls.Add(_contentHost);

            _tabBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 120,
                BackColor = _cardBackground
            };
            Controls.Add(_tabBar);

            // 标签按钮（竖直堆叠）
            _tabRevenue = MakeTabButton("收益详情", (_, __) => ShowView(DetailView.Revenue));
            _tabTrading = MakeTabButton("交易详情", (_, __) => ShowView(DetailView.Trading));
            _tabFarming = MakeTabButton("成本详情", (_, __) => ShowView(DetailView.Farming));
            _tabPrices = MakeTabButton("物价详情", (_, __) => ShowView(DetailView.Prices));
            _tabSettings = MakeTabButton("设置", (_, __) => ShowView(DetailView.Settings));
            _tabClose = MakeTabButton("关闭", (_, __) =>
            {
                var form = FindForm();
                form?.Close();
            });

            // 关键：把“关闭”固定在左下角
            _tabClose.Dock = DockStyle.Bottom;
            // 设置按钮也在底部（在“关闭”之上）
            _tabSettings.Dock = DockStyle.Bottom;

            // 关闭按钮（放在设置下面）
            _tabClose = MakeTabButton("关闭", (_, __) =>
            {
                var form = FindForm();
                form?.Close();
            });
            _tabClose.Dock = DockStyle.Bottom;

            // 设置按钮也靠底部
            _tabSettings.Dock = DockStyle.Bottom;

            // 重新排列 Dock 顺序
            _tabBar.Controls.Clear();

            _tabBar.Controls.Add(_tabSettings);  // 紧挨上方
            _tabBar.Controls.Add(_tabClose);     // 最底部

            // 然后是顶部区域（刷图→收益→物价→交易）
            _tabBar.Controls.Add(_tabTrading);
            _tabBar.Controls.Add(_tabPrices);
            _tabBar.Controls.Add(_tabRevenue);
            _tabBar.Controls.Add(_tabFarming);



            // 根面板（先全加到内容区，只显示一个）
            _revenueRoot = MakeRoot();
            _tradingRoot = MakeRoot();
            _farmingRoot = MakeRoot();
            _pricesRoot = MakeRoot();   // 新增
            _settingsRoot = MakeRoot();   // 新增

            _contentHost.Controls.AddRange(new Control[] {
                _revenueRoot, _tradingRoot, _farmingRoot, _pricesRoot, _settingsRoot
            });

            // 构建各页面
            BuildRevenueView();
            BuildTradingView();
            BuildFarmingView();
            BuildPricesView();   // 新增
            BuildSettingsView(); // 新增

            // 默认显示“收益”
            ShowView(DetailView.Revenue);

            ResumeLayout(performLayout: true);
        }

        // ===== UI 片段 =====
        private Panel MakeRoot() => new()
        {
            Dock = DockStyle.Fill,
            BackColor = _backgroundColor,
            AutoScroll = true,
            Padding = new Padding(0)
        };

        private Button MakeTabButton(string text, System.EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(12, 4, 12, 4),
                FlatStyle = FlatStyle.Flat,
                ForeColor = _textColor,
                BackColor = _cardBackground,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);
            btn.Click += onClick;
            return btn;
        }

        private Label Title(string text) => new()
        {
            Text = text,
            ForeColor = _accentColor,
            Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        private Panel Card(int height = 0)
        {
            var p = new Panel
            {
                BackColor = _cardBackground,
                //BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };
            p.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                using var pen = new Pen(Color.FromArgb(60, 60, 60)); // 深灰细线
                var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                g.DrawRectangle(pen, rect);
            };
            if (height > 0)
            {
                p.Dock = DockStyle.Top;
                p.Height = height;
            }
            else
            {
                p.Dock = DockStyle.Fill;
            }
            return p;
        }

        // ===== 页面：收益 =====
        private void BuildRevenueView()
        {
            var summary = Card(80);
            summary.Controls.Add(Title("收益详情"));

            _revenueSummaryInfo = new Label
            {
                Name = "rvSummaryInfo",
                Text = "总掉落: 0火 | 净利润: 0火 | 刷图时间: 0:00:00 | 平均每轮耗时：0:00:00",
                ForeColor = _profitColor,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft
            };
            summary.Controls.Add(_revenueSummaryInfo);

            var items = Card(); // Fill
            

            var revenuebody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 0), // ← 调这里控制 “标题 与 内容” 的垂直距离（8/12/16）
                BackColor = _cardBackground
            };


            // 用自定义滚动容器包住卡片区
            _revenueCardsContainer = new ScrollContainer
            {
                Dock = DockStyle.Fill,
            };

            // FlowPanel 只负责排布，不自己滚动；让它按内容自动增高，容器负责滚动
            _revenueCardsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,                 // 重要：Top + AutoSize 让容器能量出内容高度
                AutoScroll = false,                   // 关闭自身滚动
                AutoSize = true,                      // 随内容增高
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(12, 6, 12, 12),
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            // 把卡片面板放进容器的 ContentPanel，然后把容器放进 revenuebody
            _revenueCardsContainer.ContentPanel.Controls.Add(_revenueCardsPanel);
            revenuebody.Controls.Add(_revenueCardsContainer);

            items.Controls.Add(revenuebody);
            items.Controls.Add(Title("💰 每轮掉落详细物品"));
            EnsureRevenueOverlay(revenuebody);

            _revenueRoot.Controls.Add(items);
            _revenueRoot.Controls.Add(summary);
        }

        private void EnsureRevenueOverlay(Control host)
        {
            if (_revenueOverlay != null) return;

            _revenueOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.FromArgb(30, 0, 0, 0),   // 半透明遮罩感
                Padding = new Padding(12)
            };

            // 中间内容卡片
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                Padding = new Padding(12),
                Margin = new Padding(0)
            };

            // 标题 + 返回按钮
            var top = new Panel { Dock = DockStyle.Top, Height = 44 };
            _revenueOverlayTitle = new Label
            {
                Text = "本轮掉落详情",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold)
            };
            var escTip = new Label
            {
                Text = "按 ESC 返回",
                Dock = DockStyle.Right,
                Width = 100,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Regular),
                Padding = new Padding(0, 0, 8, 0),
                BackColor = Color.Transparent
            };


            top.Controls.Add(escTip);
            top.Controls.Add(_revenueOverlayTitle);

            // 表格
            _revenueDetailGrid = new DataGridView
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
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(70, 70, 70)
            };
            //_revenueDetailGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            //{
            //    BackColor = Color.FromArgb(60, 60, 60),
            //    ForeColor = Color.White,
            //    Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
            //    Alignment = DataGridViewContentAlignment.MiddleCenter
            //};
            //_revenueDetailGrid.DefaultCellStyle = new DataGridViewCellStyle
            //{
            //    BackColor = Color.FromArgb(40, 40, 40),
            //    ForeColor = Color.White,
            //    SelectionBackColor = Color.FromArgb(70, 70, 70),
            //    SelectionForeColor = Color.White,
            //    Font = new Font("Microsoft YaHei", 9f),
            //    Alignment = DataGridViewContentAlignment.MiddleCenter
            //};
            //_revenueDetailGrid.RowTemplate.Height = 28;
            GridStyling.ApplyDarkSkin(_revenueDetailGrid);

            _revenueDetailGrid.AllowUserToOrderColumns = false; // 禁止用户拖拽列，防止顺序变化
            _revenueDetailGrid.Columns.Add("ItemName", "物品");
            _revenueDetailGrid.Columns.Add("Quantity", "数量");
            _revenueDetailGrid.Columns.Add("UnitValue", "单价(火)");
            _revenueDetailGrid.Columns.Add("TotalValue", "总价(火)");
            foreach (DataGridViewColumn c in _revenueDetailGrid.Columns)
                c.SortMode = DataGridViewColumnSortMode.NotSortable;

            // 双击覆盖层任意处也返回
            _revenueOverlay.DoubleClick += (_, __) => HideRevenueOverlay();

            card.Controls.Add(_revenueDetailGrid);
            _revenueDetailGrid.ScrollBars = ScrollBars.None;

            _revenueVBar = new CustomScrollBar
            {
                Dock = DockStyle.Right,
                Orientation = ScrollOrientationEx.Vertical,
                Width = 10,
                Thickness = 8,
                ThumbMinLength = 30,
                TrackColor = Color.FromArgb(70, 70, 70),
                ThumbColor = _accentColor,
                ThumbHoverColor = Color.FromArgb(30, 170, 255)
            };
            card.Controls.Add(_revenueVBar);
            _revenueVBar.BringToFront();

            void RecalcRevenueDetailScroll()
            {
                int rowCount = _revenueDetailGrid.RowCount;
                int visibleRows = Math.Max(1, _revenueDetailGrid.DisplayedRowCount(false));
                int maxFirst = Math.Max(0, rowCount - visibleRows);

                _revenueVBar.Minimum = 0;
                _revenueVBar.Maximum = maxFirst;
                _revenueVBar.LargeChange = visibleRows;
                _revenueVBar.SmallChange = Math.Max(1, visibleRows / 3);
                _revenueVBar.Enabled = maxFirst > 0;

                if (rowCount > 0)
                {
                    try
                    {
                        int first = _revenueDetailGrid.FirstDisplayedScrollingRowIndex;
                        _revenueVBar.Value = Math.Min(Math.Max(first, _revenueVBar.Minimum), _revenueVBar.Maximum);
                    }
                    catch { _revenueVBar.Value = 0; }
                }
            }

            _revenueVBar.ValueChanged += (_, __) =>
            {
                try
                {
                    _revenueDetailGrid.FirstDisplayedScrollingRowIndex =
                        Math.Min(_revenueVBar.Value, Math.Max(0, _revenueDetailGrid.RowCount - 1));
                }
                catch { }
            };

            _revenueDetailGrid.Scroll += (_, e) =>
            {
                if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
                {
                    try
                    {
                        int idx = _revenueDetailGrid.FirstDisplayedScrollingRowIndex;
                        if (idx >= _revenueVBar.Minimum && idx <= _revenueVBar.Maximum)
                            _revenueVBar.Value = idx;
                    }
                    catch { }
                }
            };

            _revenueDetailGrid.MouseWheel += (_, e) =>
            {
                if (_revenueVBar.Enabled)
                    _revenueVBar.Value += e.Delta > 0 ? -_revenueVBar.SmallChange : _revenueVBar.SmallChange;
            };
            _revenueDetailGrid.MouseEnter += (_, __) => _revenueDetailGrid.Focus();

            _revenueDetailGrid.DataBindingComplete += (_, __) => RecalcRevenueDetailScroll();
            _revenueDetailGrid.Resize += (_, __) => RecalcRevenueDetailScroll();
            HandleCreated += (_, __) => BeginInvoke(new Action(RecalcRevenueDetailScroll));

            card.Controls.Add(top);
            _revenueOverlay.Controls.Add(card);

            // 盖在卡片容器之上
            host.Controls.Add(_revenueOverlay);
            _revenueOverlay.BringToFront();
        }

        private void ShowRevenueOverlay(DropRound round)
        {
            //_revenueOverlayRound = round;
            //_revenueOverlayTitle.Text = $"第 {round.RoundNumber} 轮 · {round.SceneName} · 详情";

            //_revenueDetailGrid.Rows.Clear();
            //if (round.DropItems != null)
            //{
            //    foreach (var it in round.DropItems.OrderByDescending(x => x.TotalValue))
            //    {
            //        double unitValue = it.Quantity > 0 ? it.TotalValue / it.Quantity : 0;
            //        _revenueDetailGrid.Rows.Add(
            //            it.ItemName,
            //            it.Quantity,
            //            unitValue.ToString("F3"),
            //            it.TotalValue.ToString("F3")
            //        );
            //    }
            //}
            //double total = round.DropItems?.Sum(i => i.TotalValue) ?? 0;
            //int sumRowIndex = _revenueDetailGrid.Rows.Add("—— 合计 ——", "", "", total.ToString("F3"));

            //var sumRow = _revenueDetailGrid.Rows[sumRowIndex];
            //sumRow.DefaultCellStyle.Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold);
            //// 让“合计”更好看一点的对齐方式
            //sumRow.Cells["ItemName"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //sumRow.Cells["Quantity"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //sumRow.Cells["UnitValue"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //sumRow.Cells["TotalValue"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _revenueOverlayRound = round;
            _revenueOverlayTitle.Text = $"第 {round.RoundNumber} 轮 · {round.SceneName} · 详情";

            _revenueDetailGrid.Rows.Clear();

            if (round.DropItems != null)
            {
                foreach (var it in round.DropItems.OrderByDescending(x => x.TotalValue))
                {
                    double unit = it.Quantity > 0 ? it.TotalValue / it.Quantity : 0;
                    _revenueDetailGrid.Rows.Add(
                        it.ItemName,
                        it.Quantity.ToString(),
                        Formatting.Fire(unit, 3),
                        Formatting.Fire(it.TotalValue, 3)
                    );
                }
            }

            double total = round.DropItems?.Sum(i => i.TotalValue) ?? 0;
            int sumRowIndex = _revenueDetailGrid.Rows.Add("—— 合计 ——", "", "", Formatting.Fire(total, 3));
            var sumRow = _revenueDetailGrid.Rows[sumRowIndex];
            sumRow.DefaultCellStyle.Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold);
            sumRow.Cells["ItemName"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            sumRow.Cells["Quantity"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            sumRow.Cells["UnitValue"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            sumRow.Cells["TotalValue"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;


            // 隐藏卡片容器，仅显示覆盖层
            if (_revenueCardsPanel != null) _revenueCardsPanel.Visible = false;
            _revenueOverlay.Visible = true;
            _revenueOverlay.BringToFront();

            // ESC 返回
            var form = this.FindForm();
            if (form != null)
            {
                form.KeyPreview = true;
                form.KeyDown -= RevenueEscCloseHandler; // 防重复绑定
                form.KeyDown += RevenueEscCloseHandler;
            }
        }

        private void HideRevenueOverlay()
        {
            _revenueOverlay.Visible = false;
            if (_revenueCardsPanel != null) _revenueCardsPanel.Visible = true;

            var form = this.FindForm();
            if (form != null) form.KeyDown -= RevenueEscCloseHandler;
        }

        private void RevenueEscCloseHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && _revenueOverlay?.Visible == true)
            {
                e.Handled = true;
                HideRevenueOverlay();
            }
        }



        private void RenderRevenueCards(IEnumerable<DropRound> rounds)
        {
            if (_revenueCardsPanel == null) return;
            _revenueCardsPanel.SuspendLayout();
            _revenueCardsPanel.Controls.Clear();

            foreach (var r in rounds.OrderBy(x => x.RoundNumber))
            {
                // 统计本轮掉落总值
                double total = r.DropItems?.Sum(i => i.TotalValue) ?? 0;
                double totalDrop = r.DropItems?.Sum(i => i.TotalValue) ?? 0;
                double netProfit = totalDrop; // 目前没有每轮成本，先等同于掉落合计

                var card = CreateRoundCard(
    r,
    $"第 {r.RoundNumber} 轮 | {r.SceneName}",
    $"掉落合计：{totalDrop:F3} 火",
    $"净利润：{netProfit:F3} 火",
    $"用时：{Formatting.TightTime(r.Duration)}"
);
                _revenueCardsPanel.Controls.Add(card);
            }

            _revenueCardsPanel.ResumeLayout();
        }

        private Control CreateRoundCard(DropRound round, string title, string dropTotal, string netProfit, string duration)
        {
            var panel = new Panel
            {
                Width = 148,
                Height = 96,
                MinimumSize = new Size(148, 96),
                Margin = new Padding(8, 6, 8, 8),
                BackColor = Color.FromArgb(50, 50, 50)
            };

            // —— 圆角 Region（确保是真圆角）
            void ApplyRoundedRegion()
            {
                if (panel.Width <= 0 || panel.Height <= 0) return;
                using var rp = DrawingExtensions.RoundedRect(new Rectangle(0, 0, panel.Width, panel.Height), 10);
                panel.Region?.Dispose();
                panel.Region = new Region(rp);
                
            }
            panel.Resize += (s, e) => ApplyRoundedRegion();
            ApplyRoundedRegion();

            // 双缓冲避免闪烁
            //typeof(Control).GetProperty("DoubleBuffered",
            //    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            //)?.SetValue(panel, true, null);
            ControlsUtil.EnableDoubleBuffer(panel);

            // —— 悬停动画状态（0 = 普通，1 = 高亮）
            double t = 0.0;      // 当前动画进度
            double target = 0.0; // 目标进度
            var anim = new Timer { Interval = 16 }; // ~60fps

            anim.Tick += (s, e) =>
            {
                // 指数插值，平滑过渡
                t += (target - t) * 0.18;
                if (Math.Abs(target - t) < 0.01) { t = target; anim.Stop(); }
                panel.Invalidate();
            };

            panel.MouseEnter += (_, __) => { target = 1.0; anim.Start(); };
            panel.MouseLeave += (_, __) => { target = 0.0; anim.Start(); };

            // —— 颜色插值工具
            static Color Lerp(Color a, Color b, double u)
            {
                byte L(byte x, byte y) => (byte)(x + (y - x) * u);
                return Color.FromArgb(
                    L(a.A, b.A), L(a.R, b.R), L(a.G, b.G), L(a.B, b.B));
            }

            // —— 主体绘制（去掉顶部蓝线，仅保留圆角+细边框）
            var borderBase = Color.FromArgb(110, 110, 110); // 常态边框
            var borderHover = Color.FromArgb(190, 190, 190); // 悬停更亮

            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using var path = DrawingExtensions.RoundedRect(rect, 10);

                // 背景：柔和渐变
                using (var bg = new System.Drawing.Drawing2D.LinearGradientBrush(
                    rect, Color.FromArgb(58, 58, 58), Color.FromArgb(42, 42, 42), 90f))
                    g.FillPath(bg, path);

                // 细边框：根据 t 动态插值颜色（实现动画）
                using (var pen = new Pen(Lerp(borderBase, borderHover, t), 1.2f))
                    g.DrawPath(pen, path);

                // 内侧轻微高光（固定）
                using var innerPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1);
                var inner = new Rectangle(1, 1, panel.Width - 3, panel.Height - 3);
                using var innerPath = DrawingExtensions.RoundedRect(inner, 9);
                g.DrawPath(innerPen, innerPath);
            };

            // —— 子 Label 的生成（透明背景，避免方角）
            Label MakeLabel(string text, DockStyle dock, int h, Color color, Font f) => new Label
            {
                Text = text,
                Dock = dock,
                Height = h,
                ForeColor = color,
                Font = f,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // 标题 / 掉落合计（蓝）/ 净利润（带分级颜色）/ 用时（底部）
            var lblTitle = MakeLabel(title, DockStyle.Top, 22, _textColor,
                new Font("Microsoft YaHei", 8.5f, FontStyle.Bold));

            var lblDrop = MakeLabel(dropTotal, DockStyle.Top, 18, Color.DeepSkyBlue,
                new Font("Microsoft YaHei", 8.5f, FontStyle.Bold));

            // 解析净利润并分级上色（你之前的规则）
            double profitVal = 0;
            double.TryParse(new string(netProfit.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray()), out profitVal);
            var profitColor =
                profitVal < 0 ? Color.Red :
                profitVal == 0 ? Color.Gray :
                profitVal < 1000 ? Color.LimeGreen :
                profitVal < 2000 ? Color.Orange : Color.Gold;

            var lblProfit = MakeLabel(netProfit, DockStyle.Top, 18, profitColor,
                new Font("Microsoft YaHei", 8.5f, FontStyle.Bold));

            var lblDur = MakeLabel(duration, DockStyle.Bottom, 18, Color.FromArgb(185, 185, 185),
                new Font("Microsoft YaHei", 8f, FontStyle.Regular));

            // 注意 Dock=Top 的添加顺序（最后添加的在最上面）
            panel.Controls.Add(lblProfit);
            panel.Controls.Add(lblDrop);
            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblDur);

            // —— 双击打开覆盖层详情 —— 
            void OpenDetail(object? s, EventArgs e) => ShowRevenueOverlay(round);

            panel.DoubleClick += OpenDetail;
            panel.Cursor = Cursors.Hand;
            foreach (Control c in panel.Controls)
            {
                c.DoubleClick += OpenDetail;
                c.Cursor = Cursors.Hand;
            }


            return panel;
        }


        // —— 小工具：画圆角矩形 Path —— 
        //private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
        //{
        //    var gp = new System.Drawing.Drawing2D.GraphicsPath();
        //    int d = radius * 2;
        //    gp.AddArc(r.X, r.Y, d, d, 180, 90);
        //    gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        //    gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        //    gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        //    gp.CloseFigure();
        //    return gp;
        //}


        // ===== 页面：交易 =====
        private void BuildTradingView()
        {
            var summary = Card(80);
            summary.Controls.Add(Title("交易详情"));

            _tradingSummaryInfo = new Label
            {
                Name = "trSummaryInfo",
                Text = "购买: 0火 | 获得: 0火 | 净收益: 0火",
                ForeColor = _profitColor,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 24
            };
            summary.Controls.Add(_tradingSummaryInfo);

            var records = Card(); // Fill
            

            // 创建交易物品数据表格
            _tradingItemsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackColor = _cardBackground,
                ForeColor = _textColor,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = true,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                Font = new Font("Microsoft YaHei", 9f),
                GridColor = Color.FromArgb(60, 60, 60),
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = _textColor,
                    Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = _cardBackground,
                    ForeColor = _textColor,
                    SelectionBackColor = Color.FromArgb(70, 70, 70),
                    SelectionForeColor = _textColor,
                    WrapMode = DataGridViewTriState.True
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 40),
                    WrapMode = DataGridViewTriState.True
                },
                ColumnHeadersHeight = 24,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                ColumnHeadersVisible = true,
            };

            // 添加列
            _tradingItemsGrid.Columns.Add("SaleId", "交易ID");
            _tradingItemsGrid.Columns.Add("Status", "交易状态");
            _tradingItemsGrid.Columns.Add("Details", "详细信息");
            _tradingItemsGrid.Columns.Add("CreateTime", "创建时间");

            // 设置列宽

            _tradingItemsGrid.Columns["Details"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _tradingItemsGrid.Columns["Details"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _tradingItemsGrid.Columns["Details"].SortMode = DataGridViewColumnSortMode.NotSortable;
            _tradingItemsGrid.Columns["Details"].Width = 300;
            _tradingItemsGrid.Columns["SaleId"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _tradingItemsGrid.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _tradingItemsGrid.Columns["CreateTime"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            var tradingbody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 0), // ← 想要多少间距就改这里：12、16、20 都行
                BackColor = _cardBackground
            };
            tradingbody.Controls.Add(_tradingItemsGrid);
            // 隐藏系统滚动条，用自定义条
            _tradingItemsGrid.ScrollBars = ScrollBars.None;

            // 右侧自定义滚动条
            _tradingVBar = new CustomScrollBar
            {
                Dock = DockStyle.Right,
                Orientation = ScrollOrientationEx.Vertical,
                Width = 10,
                Thickness = 8,
                ThumbMinLength = 30,
                TrackColor = Color.FromArgb(70, 70, 70),
                ThumbColor = _accentColor,
                ThumbHoverColor = Color.FromArgb(30, 170, 255)
            };
            tradingbody.Controls.Add(_tradingVBar);
            _tradingVBar.BringToFront();

            // —— 同步逻辑
            void RecalcTradingGridScroll()
            {
                int rowCount = _tradingItemsGrid.RowCount;
                int visibleRows = Math.Max(1, _tradingItemsGrid.DisplayedRowCount(false));
                int maxFirst = Math.Max(0, rowCount - visibleRows);

                _tradingVBar.Minimum = 0;
                _tradingVBar.Maximum = maxFirst;
                _tradingVBar.LargeChange = visibleRows;
                _tradingVBar.SmallChange = Math.Max(1, visibleRows / 3);
                _tradingVBar.Enabled = maxFirst > 0;

                if (rowCount > 0)
                {
                    try
                    {
                        int first = _tradingItemsGrid.FirstDisplayedScrollingRowIndex;
                        _tradingVBar.Value = Math.Min(Math.Max(first, _tradingVBar.Minimum), _tradingVBar.Maximum);
                    }
                    catch { _tradingVBar.Value = 0; }
                }
            }

            _tradingVBar.ValueChanged += (_, __) =>
            {
                try
                {
                    _tradingItemsGrid.FirstDisplayedScrollingRowIndex =
                        Math.Min(_tradingVBar.Value, Math.Max(0, _tradingItemsGrid.RowCount - 1));
                }
                catch { }
            };

            _tradingItemsGrid.Scroll += (_, e) =>
            {
                if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
                {
                    try
                    {
                        int idx = _tradingItemsGrid.FirstDisplayedScrollingRowIndex;
                        if (idx >= _tradingVBar.Minimum && idx <= _tradingVBar.Maximum)
                            _tradingVBar.Value = idx;
                    }
                    catch { }
                }
            };

            _tradingItemsGrid.MouseWheel += (_, e) =>
            {
                if (_tradingVBar.Enabled)
                    _tradingVBar.Value += e.Delta > 0 ? -_tradingVBar.SmallChange : _tradingVBar.SmallChange;
            };
            _tradingItemsGrid.MouseEnter += (_, __) => _tradingItemsGrid.Focus();

            _tradingItemsGrid.DataBindingComplete += (_, __) => RecalcTradingGridScroll();
            _tradingItemsGrid.Resize += (_, __) => RecalcTradingGridScroll();
            HandleCreated += (_, __) => BeginInvoke(new Action(RecalcTradingGridScroll));

            records.Controls.Add(tradingbody);
            records.Controls.Add(Title("📊 交易记录详情"));

            _tradingRoot.Controls.Add(records);
            _tradingRoot.Controls.Add(summary);

        }

        // ===== 页面：成本 =====
        private void BuildFarmingView()
        {
            var summary = Card(80);
            summary.Controls.Add(Title("成本详情"));

            _farmingSummaryInfo = new Label
            {
                Name = "fmSummaryInfo",
                Text = "总计时间: 0:00:00 | 总计消耗: 0火 | 总计轮次: 0 | 当前轮次: 0",
                ForeColor = _profitColor,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 24
            };
            summary.Controls.Add(_farmingSummaryInfo);

            var rounds = Card(); // Fill

            _farmingItemsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackColor = _cardBackground,
                ForeColor = _textColor,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = true,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                Font = new Font("Microsoft YaHei", 9f),
                GridColor = Color.FromArgb(60, 60, 60),
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = _textColor,
                    Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                ColumnHeadersHeight = 24,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                ColumnHeadersVisible = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = _cardBackground,
                    ForeColor = _textColor,
                    SelectionBackColor = Color.FromArgb(70, 70, 70),
                    SelectionForeColor = _textColor,
                    WrapMode = DataGridViewTriState.True
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 40),
                    WrapMode = DataGridViewTriState.True
                }
            };

            // 添加列
            _farmingItemsGrid.Columns.Add("RoundNumber", "轮次编号");
            _farmingItemsGrid.Columns.Add("SceneType", "地图类型");
            _farmingItemsGrid.Columns.Add("CostName", "成本名称");
            _farmingItemsGrid.Columns.Add("CostCount", "成本数量");
            _farmingItemsGrid.Columns.Add("CostUnitPrice", "成本单价");
            _farmingItemsGrid.Columns.Add("CostTotal", "成本总价");
            _farmingItemsGrid.Columns.Add("RoundDuration", "轮次用时");

            // =======================
            // 固定宽度的列
            // =======================
            _farmingItemsGrid.Columns["CostName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _farmingItemsGrid.Columns["CostName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _farmingItemsGrid.Columns["CostName"].SortMode = DataGridViewColumnSortMode.NotSortable;
            _farmingItemsGrid.Columns["CostName"].Width = 200;
            _farmingItemsGrid.Columns["RoundNumber"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _farmingItemsGrid.Columns["SceneType"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _farmingItemsGrid.Columns["SceneType"].SortMode = DataGridViewColumnSortMode.NotSortable;
            _farmingItemsGrid.Columns["CostCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _farmingItemsGrid.Columns["CostTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _farmingItemsGrid.Columns["CostUnitPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _farmingItemsGrid.Columns["RoundDuration"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _farmingItemsGrid.ScrollBars = ScrollBars.None;
            var farmingbody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 0), // ← 想要多少间距就改这里：12、16、20 都行
                BackColor = _cardBackground
            };
            farmingbody.Controls.Add(_farmingItemsGrid);
            _farmingVBar = new CustomScrollBar
            {
                Dock = DockStyle.Right,
                Orientation = ScrollOrientationEx.Vertical,
                Width = 10,
                Thickness = 8,
                ThumbMinLength = 30,
                TrackColor = Color.FromArgb(50, 50, 50),  // 可改
                ThumbColor = _accentColor,                // 主题色跟随
                ThumbHoverColor = Color.FromArgb(30, 170, 255)
            };
            farmingbody.Controls.Add(_farmingVBar);
            _farmingVBar.BringToFront();
            rounds.Controls.Add(farmingbody);
            rounds.Controls.Add(Title("⚔️ 每轮成本详情"));
            // 计算范围
            void RecalcFarmingGridScroll()
            {
                try
                {
                    if (_farmingItemsGrid.RowCount <= 0)
                    {
                        _farmingVBar.Enabled = false;
                        _farmingVBar.Minimum = 0;
                        _farmingVBar.Maximum = 0;
                        _farmingVBar.Value = 0;
                        return;
                    }

                    int visibleRows = Math.Max(1, _farmingItemsGrid.DisplayedRowCount(false));
                    int maxFirst = Math.Max(0, _farmingItemsGrid.RowCount - visibleRows);

                    _farmingVBar.Minimum = 0;
                    _farmingVBar.Maximum = maxFirst;
                    _farmingVBar.LargeChange = visibleRows;                 // 一页=可视行数
                    _farmingVBar.SmallChange = Math.Max(1, visibleRows / 3);
                    _farmingVBar.Enabled = maxFirst > 0;

                    // 同步当前首行
                    try
                    {
                        int first = _farmingItemsGrid.FirstDisplayedScrollingRowIndex;
                        if (first >= _farmingVBar.Minimum && first <= _farmingVBar.Maximum)
                            _farmingVBar.Value = first;
                    }
                    catch { }
                }
                catch { }
            }

            // 自定义条 -> DGV
            _farmingVBar.ValueChanged += (_, __) =>
            {
                try
                {
                    _farmingItemsGrid.FirstDisplayedScrollingRowIndex =
                        Math.Min(_farmingVBar.Value, _farmingItemsGrid.RowCount - 1);
                }
                catch { /* 忽略边界异常 */ }
            };

            // DGV 自己滚动（键盘/PageDown等）-> 自定义条
            _farmingItemsGrid.Scroll += (_, e) =>
            {
                if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
                {
                    try
                    {
                        int idx = _farmingItemsGrid.FirstDisplayedScrollingRowIndex;
                        if (idx >= _farmingVBar.Minimum && idx <= _farmingVBar.Maximum)
                            _farmingVBar.Value = idx;
                    }
                    catch { }
                }
            };

            // 鼠标在表格上滚动 -> 驱动自定义滚动条
            _farmingItemsGrid.MouseWheel += (_, e) =>
            {
                if (!_farmingVBar.Enabled) return;

                // 向上滚为正，向下为负 —— 我们把它映射为 SmallChange
                _farmingVBar.Value += e.Delta > 0 ? -_farmingVBar.SmallChange : _farmingVBar.SmallChange;
            };

            // 进入表格时让它获得焦点，确保能收到 MouseWheel 事件
            _farmingItemsGrid.MouseEnter += (_, __) => _farmingItemsGrid.Focus();


            // 数据或尺寸变化时重算
            _farmingItemsGrid.DataBindingComplete += (_, __) => RecalcFarmingGridScroll();
            _farmingItemsGrid.Resize += (_, __) => RecalcFarmingGridScroll();

            // 首次进入页面也算一次
            this.HandleCreated += (_, __) => BeginInvoke(new Action(RecalcFarmingGridScroll));

            // ② 控件加载时算一遍（UserControl 有 Load 事件）
            this.Load += (_, __) => RecalcFarmingGridScroll();
            RecalcFarmingGridScroll();

            _farmingRoot.Controls.Add(rounds);
            _farmingRoot.Controls.Add(summary);
        }

        // ===== 页面：物价（新增） =====
        private void BuildPricesView()
        {
            var summary = Card(80);
            summary.Controls.Add(Title("物价详情"));

            _pricesSummaryInfo = new Label
            {
                Name = "prSummaryInfo",
                Text = "物品总数: 0 | 平均价格: 0.000火 | 最高价格: 0.000火 | 最低价格: 0.000火",
                ForeColor = _profitColor,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft
            };
            summary.Controls.Add(_pricesSummaryInfo);

            var list = Card(); // Fill
            //list.Controls.Add(Title("💹 物品价格列表"));

            // 预留：可以在此加入一个 DataGridView
            _pricesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackColor = _cardBackground,
                ForeColor = _textColor,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = true,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                Font = new Font("Microsoft YaHei", 9f),
                GridColor = Color.FromArgb(60, 60, 60),
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = _textColor,
                    Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.BottomCenter
                },
                ColumnHeadersHeight = 24,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                ColumnHeadersVisible = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = _cardBackground,
                    ForeColor = _textColor,
                    SelectionBackColor = Color.FromArgb(70, 70, 70),
                    SelectionForeColor = _textColor,
                    WrapMode = DataGridViewTriState.True
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 40),
                    WrapMode = DataGridViewTriState.True
                }
            };
            _pricesGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(55, 55, 55);
            _pricesGrid.ColumnHeadersDefaultCellStyle.ForeColor = _textColor;
            _pricesGrid.DefaultCellStyle.BackColor = _cardBackground;
            _pricesGrid.DefaultCellStyle.ForeColor = _textColor;

            //编号，名称，价格，
            _pricesGrid.Columns.Add("colName", "名称");
            _pricesGrid.Columns.Add("colPrice", "价格");
            _pricesGrid.Columns["colPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _pricesGrid.Columns.Add("colType", "类型");
            _pricesGrid.Columns["colType"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _pricesGrid.Columns.Add("colLastTime", "更新时间");


            var _pricesbody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 0), // ← 想要多少间距就改这里：12、16、20 都行
                BackColor = _cardBackground
            };
            _pricesbody.Controls.Add(_pricesGrid);
            _pricesGrid.ScrollBars = ScrollBars.None;

            _pricesVBar = new CustomScrollBar
            {
                Dock = DockStyle.Right,
                Orientation = ScrollOrientationEx.Vertical,
                Width = 10,
                Thickness = 8,
                ThumbMinLength = 30,
                TrackColor = Color.FromArgb(70, 70, 70),
                ThumbColor = _accentColor,
                ThumbHoverColor = Color.FromArgb(30, 170, 255)
            };
            _pricesbody.Controls.Add(_pricesVBar);
            _pricesVBar.BringToFront();

            void RecalcPricesGridScroll()
            {
                int rowCount = _pricesGrid.RowCount;
                int visibleRows = Math.Max(1, _pricesGrid.DisplayedRowCount(false));
                int maxFirst = Math.Max(0, rowCount - visibleRows);

                _pricesVBar.Minimum = 0;
                _pricesVBar.Maximum = maxFirst;
                _pricesVBar.LargeChange = visibleRows;
                _pricesVBar.SmallChange = Math.Max(1, visibleRows / 3);
                _pricesVBar.Enabled = maxFirst > 0;

                if (rowCount > 0)
                {
                    try
                    {
                        int first = _pricesGrid.FirstDisplayedScrollingRowIndex;
                        _pricesVBar.Value = Math.Min(Math.Max(first, _pricesVBar.Minimum), _pricesVBar.Maximum);
                    }
                    catch { _pricesVBar.Value = 0; }
                }
            }

            _pricesVBar.ValueChanged += (_, __) =>
            {
                try
                {
                    _pricesGrid.FirstDisplayedScrollingRowIndex =
                        Math.Min(_pricesVBar.Value, Math.Max(0, _pricesGrid.RowCount - 1));
                }
                catch { }
            };

            _pricesGrid.Scroll += (_, e) =>
            {
                if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
                {
                    try
                    {
                        int idx = _pricesGrid.FirstDisplayedScrollingRowIndex;
                        if (idx >= _pricesVBar.Minimum && idx <= _pricesVBar.Maximum)
                            _pricesVBar.Value = idx;
                    }
                    catch { }
                }
            };

            _pricesGrid.MouseWheel += (_, e) =>
            {
                if (_pricesVBar.Enabled)
                    _pricesVBar.Value += e.Delta > 0 ? -_pricesVBar.SmallChange : _pricesVBar.SmallChange;
            };
            _pricesGrid.MouseEnter += (_, __) => _pricesGrid.Focus();

            _pricesGrid.DataBindingComplete += (_, __) => RecalcPricesGridScroll();
            _pricesGrid.Resize += (_, __) => RecalcPricesGridScroll();
            HandleCreated += (_, __) => BeginInvoke(new Action(RecalcPricesGridScroll));

            list.Controls.Add(_pricesbody);
            list.Controls.Add(Title("💹 物品价格列表"));

            //list.Controls.Add(_pricesGrid);

            _pricesRoot.Controls.Add(list);
            _pricesRoot.Controls.Add(summary);
        }

        // ===== 页面：设置（新增） =====
        private void BuildSettingsView()
        {
            var basic = Card(120);
            basic.Controls.Add(Title("基础设置"));

            var chkIncludeTax = new CheckBox
            {
                Name = "stIncludeTax",
                Text = "收益计算包含税率",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = _textColor
            };
            var chkDeductCost = new CheckBox
            {
                Name = "stDeductCost",
                Text = "收益计算扣除成本",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = _textColor
            };
            var btnReset = new Button
            {
                Name = "stReset",
                Text = "恢复默认设置",
                Dock = DockStyle.Top,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                ForeColor = _textColor,
                BackColor = Color.FromArgb(60, 60, 60)
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += (_, __) =>
            {
                chkDeductCost.Checked = true;
                chkIncludeTax.Checked = true;
                MessageBox.Show("已恢复默认设置。", "设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // 注意添加顺序（后加的在更上层）
            basic.Controls.Add(btnReset);
            basic.Controls.Add(chkDeductCost);
            basic.Controls.Add(chkIncludeTax);

            var about = Card(); // Fill
            about.Controls.Add(Title("关于"));
            about.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = _textColor,
                Font = new Font("Microsoft YaHei", 9.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "版本：v1.0.0\n主题：深色\n说明：这里可展示版本、数据来源与更新日志等信息。"
            });

            _settingsRoot.Controls.Add(about);
            _settingsRoot.Controls.Add(basic);



            // 关键顺序：Top -> Bottom -> Fill(最后)
            _settingsRoot.Controls.Add(basic);    // Dock = Top
            _settingsRoot.Controls.Add(about);    // Dock = Fill（必须最后）

        }

        // ===== 切换视图 =====
        public void ShowView(DetailView view)
        {
            _revenueRoot.Visible = false;
            _tradingRoot.Visible = false;
            _farmingRoot.Visible = false;
            _pricesRoot.Visible = false;   // 新增
            _settingsRoot.Visible = false;   // 新增

            switch (view)
            {
                case DetailView.Revenue:
                    _revenueRoot.Visible = true; _revenueRoot.BringToFront(); SetActiveTab(_tabRevenue); break;
                case DetailView.Trading:
                    _tradingRoot.Visible = true; _tradingRoot.BringToFront(); SetActiveTab(_tabTrading); break;
                case DetailView.Farming:
                    _farmingRoot.Visible = true; _farmingRoot.BringToFront(); SetActiveTab(_tabFarming); break;
                case DetailView.Prices:   // 新增
                    _pricesRoot.Visible = true; _pricesRoot.BringToFront(); SetActiveTab(_tabPrices); break;
                case DetailView.Settings: // 新增
                    _settingsRoot.Visible = true; _settingsRoot.BringToFront(); SetActiveTab(_tabSettings); break;
            }
            SelectedView = view;
        }

        private void SetActiveTab(Button active)
        {
            foreach (var b in new[] { _tabRevenue, _tabTrading, _tabFarming, _tabPrices, _tabSettings }) // 覆盖新增
            {
                if (b == null) continue;
                b.BackColor = (b == active) ? Color.FromArgb(60, 60, 60) : _cardBackground;
                b.ForeColor = (b == active) ? _accentColor : _textColor;
            }
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ApplyBorderlessHostForm();
            MakeTabBarDraggable();
        }

        private void ApplyBorderlessHostForm()
        {
            var form = FindForm();
            if (form == null) return;

            // 无边框 + 保持无系统按钮
            form.FormBorderStyle = FormBorderStyle.None;
            form.ControlBox = false;

            // 关键：给个非空标题，让“窗口截图”能识别这扇窗
            form.Text = "详细统计";          // <- 以前是空字符串

            // 确保它是标准顶层窗体（出现在任务栏和窗口列表中）
            form.ShowInTaskbar = true;       // <- 新增
            form.TopMost = true;             // 设置为置顶，与主窗口保持一致
            form.Padding = new Padding(1);
            form.BackColor = Color.FromArgb(210, 210, 210);
        }


        private void MakeTabBarDraggable()
        {
            if (_tabBar == null) return;
            _tabBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    var form = FindForm();
                    if (form == null) return;
                    ReleaseCapture();
                    SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                }
            };
        }

        /// <summary>
        /// 更新所有统计数据
        /// </summary>
        public void UpdateAllStats()
        {
            UpdateRevenueStats();
            UpdateTradingStats();
            UpdateFarmingStats();
            UpdatePricesStats();
        }

        /// <summary>
        /// 更新收益统计
        /// </summary>
        public void UpdateRevenueStats()
        {
            try
            {
                if (_revenueSummaryInfo == null) return;

                var currentDropManager = ServiceLocator.Instance.Get<CurrentDropManager>();
                var currentDropSummary = currentDropManager.GetCurrentDropSummary();

                string timeStr = Formatting.TightTime(currentDropSummary.ActiveTime);
                string avgTimeStr = CalculateAverageTimePerRound(currentDropSummary);

                _revenueSummaryInfo.Text = $"总掉落: {currentDropSummary.TotalValue:F2}火 | 净利润: {currentDropSummary.NetProfit:F2}火 | 刷图时间: {timeStr} | 平均每轮耗时: {avgTimeStr}";

                // 更新掉落物品表格（每个轮次一行，详细信息在Details列中）
                //            if (_revenueItemsGrid != null)
                //            {
                //                _revenueItemsGrid.Rows.Clear();

                //                //var dropRounds = currentDropManager.GetAllDropRounds();
                //                IEnumerable<DropRound> dropRounds =
                //currentDropManager.GetAllDropRounds() as IEnumerable<DropRound>
                //?? Enumerable.Empty<DropRound>();

                //                foreach (var round in dropRounds.OrderByDescending(x => x.RoundNumber))
                //                {
                //                    // 构建详细信息
                //                    var details = new List<string>();

                //                    foreach (var item in round.DropItems.OrderByDescending(x => x.TotalValue))
                //                    {
                //                        details.Add($"  -{item.ItemName} x{item.Quantity} ({item.TotalValue:F2}火)");
                //                    }

                //                    // 添加一行记录
                //                    _revenueItemsGrid.Rows.Add(
                //                        round.RoundNumber,
                //                        round.SceneName,
                //                        string.Join("\n", details),
                //                        FormatTimeSpan(round.Duration)
                //                    );
                //                }
                //                RenderRevenueCards(dropRounds);
                //            }
                RenderRevenueCards(currentDropManager.GetAllDropRounds() as IEnumerable<DropRound> ?? Enumerable.Empty<DropRound>());

            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新收益统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新交易统计
        /// </summary>
        public void UpdateTradingStats()
        {
            try
            {
                if (_tradingSummaryInfo == null) return;

                var tradingManager = ServiceLocator.Instance.Get<TradingManager>();
                var tradingSummary = tradingManager.GetTradingSummary();

                _tradingSummaryInfo.Text = $"购买: {tradingSummary.TotalBuyConsumeValue:F2}火 | 获得: {tradingSummary.TotalReceiveValue:F2}火 | 净收益: {tradingSummary.NetTradingProfit:F2}火";

                // 更新交易记录表格（每个ID一行，详细信息在Details列中）
                if (_tradingItemsGrid != null)
                {
                    _tradingItemsGrid.Rows.Clear();

                    var priceManager = ServiceLocator.Instance.Get<PriceManager>();

                    foreach (var record in tradingSummary.TradingRecords.OrderByDescending(x => x.CreateTime))
                    {
                        // 判断交易状态
                        string status;
                        if (record.BuyRecords.Any() && record.ReceiveRecords.Any())
                        {
                            status = "购买记录";
                        }
                        else if (record.ReceiveRecords.Any())
                        {
                            status = "掉落卖出或历史记录";
                        }
                        else if (record.BuyRecords.Any())
                        {
                            status = "购买未领取";
                        }
                        else
                        {
                            status = "未知状态";
                        }

                        // 构建详细信息
                        var details = new List<string>();

                        // 添加购买记录（支出物品）
                        foreach (var buyRecord in record.BuyRecords)
                        {
                            var priceInfo = priceManager.GetItemPriceInfo(buyRecord.ItemBaseId);
                            double unitPrice = priceInfo?.Price ?? 0.0;
                            double buyValue = Math.Abs(buyRecord.Quantity) * unitPrice;
                            details.Add($"  -支出: {buyRecord.ItemName} x{Math.Abs(buyRecord.Quantity)} ({buyValue:F2}火)");
                        }

                        // 添加接收记录（接收物品）
                        foreach (var receiveRecord in record.ReceiveRecords)
                        {
                            var priceInfo = priceManager.GetItemPriceInfo(receiveRecord.ItemBaseId);
                            double unitPrice = priceInfo?.Price ?? 0.0;
                            double receiveValue = receiveRecord.Quantity * unitPrice;
                            details.Add($"  -接收: {receiveRecord.ItemName} x{receiveRecord.Quantity} ({receiveValue:F2}火)");
                        }

                        // 添加一行记录
                        _tradingItemsGrid.Rows.Add(
                            record.SaleId,
                            status,
                            string.Join("\n", details),
                            record.CreateTime.ToString("HH:mm:ss")
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新交易统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新刷图统计
        /// </summary>
        public void UpdateFarmingStats()
        {
            try
            {
                if (_farmingSummaryInfo == null) return;

                var farmingManager = ServiceLocator.Instance.Get<FarmingCostManager>();
                var currentDropManager = ServiceLocator.Instance.Get<CurrentDropManager>();

                var farmingSummary = farmingManager.GetFarmingSummary();
                var currentDropSummary = currentDropManager.GetCurrentDropSummary();

                double totalCost = farmingSummary.Sum(x => x.TotalValue);
                int totalRounds = farmingSummary.Count > 0 ? farmingSummary.Max(x => x.RunCount) : 0;
                string timeStr = Formatting.TightTime(currentDropSummary.ActiveTime);

                _farmingSummaryInfo.Text = $"总计时间: {timeStr} | 总计成本: {totalCost:F2}火 | 总计轮次: {totalRounds}";

                // 更新成本物品表格（每个轮次一行，详细信息在Details列中）
                if (_farmingItemsGrid != null)
                {
                    _farmingItemsGrid.Rows.Clear();

                    var farmingRounds = farmingManager.GetAllFarmingRounds();
                    var priceManager = ServiceLocator.Instance.Get<PriceManager>();

                    foreach (var round in farmingRounds.OrderByDescending(x => x.RoundNumber))
                    {
                        // 构建详细信息
                        var details = new List<string>();

                        foreach (var itemUsage in round.ItemUsage.OrderByDescending(x => x.Value))
                        {
                            string itemName = priceManager.GetItemName(itemUsage.Key);
                            double unitPrice = priceManager.GetItemUnitPriceWithoutTax(itemUsage.Key);
                            double totalValue = unitPrice * itemUsage.Value;
                            details.Add($"  -{itemName} x{itemUsage.Value} ({totalValue:F2}火)");
                        }

                        // 添加一行记录
                        _farmingItemsGrid.Rows.Add(
                            round.RoundNumber,
                            round.RunType,
                            string.Join("\n", details),
                            Formatting.TightTime(round.Duration)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新刷图统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新物价统计
        /// </summary>
        public void UpdatePricesStats()
        {
            try
            {
                if (_pricesGrid == null) return;

                var priceManager = ServiceLocator.Instance.Get<PriceManager>();

                // 获取价格数据统计
                var priceSummary = priceManager.GetPriceDataSummary();

                // 更新概览信息
                if (_pricesSummaryInfo != null)
                {
                    string lastUpdateStr = FormatTimestamp(priceSummary.LastUpdateTime);
                    _pricesSummaryInfo.Text = $"物品总数: {priceSummary.TotalItems} | 平均价格: {priceSummary.AveragePrice:F3}火 | 最高价格: {priceSummary.MaxPrice:F3}火 | 最低价格: {priceSummary.MinPrice:F3}火 | 最后更新: {lastUpdateStr}";
                }

                // 获取所有价格数据
                var allPriceData = priceManager.GetAllPriceData();

                // 清空现有数据
                _pricesGrid.Rows.Clear();

                // 添加真实价格数据
                foreach (var priceInfo in allPriceData)
                {
                    // 格式化时间显示
                    string timeStr = FormatTimestamp(priceInfo.LastTime);

                    _pricesGrid.Rows.Add(
                        priceInfo.Name,
                        Formatting.Fire(priceInfo.Price,3),
                        //priceInfo.Price.ToString("F3"),
                        priceInfo.Type,
                        timeStr
                    );
                }

                //ConsoleLogger.Instance.LogInfo($"已更新物价数据，共 {allPriceData.Count} 个物品");
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新物价统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 格式化时间跨度显示
        /// </summary>
        /// <param name="timeSpan">时间跨度</param>
        /// <returns>格式化后的时间字符串</returns>
        //private string FormatTimeSpan(TimeSpan timeSpan)
        //{
        //    if (timeSpan.TotalDays >= 1)
        //    {
        //        return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        //    }
        //    else
        //    {
        //        return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        //    }
        //}

        /// <summary>
        /// 格式化时间戳显示
        /// </summary>
        /// <param name="timestamp">Unix时间戳</param>
        /// <returns>格式化后的时间字符串</returns>
        private string FormatTimestamp(long timestamp)
        {
            try
            {
                var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "未知时间";
            }
        }

        /// <summary>
        /// 计算平均每轮耗时
        /// </summary>
        /// <param name="currentDropSummary">当前掉落摘要</param>
        /// <returns>平均每轮耗时字符串</returns>
        private string CalculateAverageTimePerRound(CurrentDropSummary currentDropSummary)
        {
            try
            {
                var farmingManager = ServiceLocator.Instance.Get<FarmingCostManager>();
                var farmingSummary = farmingManager.GetFarmingSummary();

                if (farmingSummary.Count == 0)
                {
                    return "0:00:00";
                }

                int totalRounds = farmingSummary.Max(x => x.RunCount);
                if (totalRounds == 0)
                {
                    return "0:00:00";
                }

                TimeSpan avgTime = TimeSpan.FromTicks(currentDropSummary.ActiveTime.Ticks / totalRounds);
                return Formatting.TightTime(avgTime);
            }
            catch
            {
                return "0:00:00";
            }
        }

    }
}

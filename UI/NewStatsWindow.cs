using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GameLogMonitor;
using NewUI.Managers;
using System.Linq;
using System.Diagnostics;


namespace NewUI
{
    /// <summary>
    /// 统一的统计窗口（将“刷图消耗”“收益统计”“交易统计”三窗体合并为单窗体）
    /// - 顶部标签按钮切换页面
    /// - 具备左右滑动的页面切换动画
    /// - 沿用各页面原有的配色与元素布局
    /// - 双击页面主体打开对应的详情窗口
    /// - 右键隐藏窗口；支持拖拽移动；无边框、圆角
    /// </summary>
    public class NewStatsWindow : Form
    {
        private enum Page
        {
            Farming,
            Revenue,
            Trading
        }

        // —— 导航 ——
        private Panel _navBar;
        private Button _tabFarming;
        private Button _tabRevenue;
        private Button _tabTrading;
        private Panel _accentUnderline;

        // —— 内容容器（做切换动画用：当前/下一页两个容器滑动） ——
        private Panel _contentCurrent;
        private Panel _contentNext;

        // —— 动画 ——
        private System.Windows.Forms.Timer _slideTimer;
        private int _animationDx;   // 每帧移动像素
        private int _animationLeftStartCurrent;
        private int _animationLeftStartNext;
        private int _animationTargetLeftCurrent;
        private int _animationTargetLeftNext;

        private Page _currentPage = Page.Revenue;
        private Page _nextPage = Page.Revenue;

        // —— 样式常量（参考仓库中的三块面板配色，统一在此处集中管理） ——
        private readonly Color _cardBackground = Color.FromArgb(30, 30, 30);
        private readonly Color _textColor = Color.FromArgb(255, 255, 255);
        private readonly Color _secondaryTextColor = Color.FromArgb(180, 180, 180);

        private readonly Color _accentFarming = Color.FromArgb(220, 53, 69);   // 红色（成本）
        private readonly Color _accentRevenue = Color.FromArgb(40, 167, 69);   // 绿色（收益）
        private readonly Color _accentTrading = Color.FromArgb(255, 193, 7);   // 黄色（交易）

        private bool _isDragging = false;
        private Point _mouseDownPoint;

        private Point _pressPointScreen;   // 按下那一刻的屏幕坐标
        private bool _dragStarted;         // 是否已经触发了原生拖拽
        private const int DRAG_THRESHOLD = 4; // 触发拖拽的移动阈值(像素)

        private int _lastOpenTick;           // 上一次打开详情的时刻（毫秒）

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;

        // 跟踪已经绑定过交互事件的控件，避免重复绑定
        private readonly HashSet<Control> _boundControls = new HashSet<Control>();

        private Form _detailForm;

        // 数据标签引用（用于实时更新）
        private Label _farmingTotalTimeLabel;
        private Label _farmingItem1Label;
        private Label _farmingItem2Label;
        private Label _farmingItem3Label;
        private Label _farmingItem4Label;
        private Label _farmingItem5Label;
        private Label _revenueTotalDropLabel;
        private Label _revenueNetProfitLabel;
        private Label _revenueActiveTimeLabel;
        private Label _tradingBuyLabel;
        private Label _tradingSellLabel;
        private Label _tradingNetLabel;


        //新增
        private Label _farmingRoundTimeLabel;
        private List<Label> _farmingItemLabels;
        private Page? _pendingPage = null;

        // —— 收益页所需标签 —— 
        private Label _revenueLine1Label, _revenueLine2Label, _revenueLine3Label, _revenueLine4Label;
        private Label _revenueMaxLabel, _revenueMaxItemLabel, _revenueMinLabel, _revenueMinItemLabel;





        private void BeginNativeDrag()
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }


        public NewStatsWindow()
        {
            InitializeComponent();
            SetupEvents();
            BuildPage(_currentPage, _contentCurrent); // 初始构建
            UpdateAccentBar();
        }

        private void InitializeComponent()
        {
            Text = "统计概览 (NewUI)";
            Size = new Size(360, 230);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(100, 100);
            FormBorderStyle = FormBorderStyle.None;
            BackColor = _cardBackground;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            // 圆角区域
            SetRoundedRegion();

            // 顶部导航
            _navBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = _cardBackground
            };
            Controls.Add(_navBar);

            _accentUnderline = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 2,                    // 线条粗细可自行调整
                BackColor = _accentFarming     // 初始与当前页一致（构造时当前为 Farming）
            };
            _navBar.Controls.Add(_accentUnderline);

            _tabRevenue = MakeTabButton("💰 收益", Page.Revenue);
            _tabFarming = MakeTabButton("⚔️ 成本", Page.Farming);
            _tabTrading = MakeTabButton("🛒 交易", Page.Trading);

            // 简单水平布局
            _tabRevenue.Location = new Point(25, 6);
            _tabFarming.Location = new Point(135, 6);
            _tabTrading.Location = new Point(245, 6);

            _navBar.Controls.AddRange(new Control[] { _tabRevenue, _tabFarming, _tabTrading });

            // 内容层（双层用于滑动动画）
            _contentCurrent = new Panel
            {
                Location = new Point(0, _navBar.Bottom),
                Size = new Size(Width, Height - _navBar.Height),
                BackColor = _cardBackground
            };
            _contentNext = new Panel
            {
                Location = new Point(Width, _navBar.Bottom), // 初始在右侧屏外
                Size = new Size(Width, Height - _navBar.Height),
                BackColor = _cardBackground
            };
            Controls.Add(_contentNext);
            Controls.Add(_contentCurrent);

            // 动画计时器
            _slideTimer = new System.Windows.Forms.Timer { Interval = 15 }; // ~60+ FPS
            _slideTimer.Tick += SlideTimer_Tick;
        }

        private Button MakeTabButton(string text, Page page)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = _textColor,
                BackColor = _cardBackground,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 32),
                Cursor = Cursors.Hand,
                Tag = page
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 40);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 50, 50);
            btn.Click += (_, __) =>
            {
                if (btn.Tag is Page p) SwitchTo(p);
            };
            return btn;
        }

        private void SetupEvents()
        {
            // 拖拽窗口
            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _mouseDownPoint = e.Location;
                    _isDragging = true;
                    Cursor = Cursors.SizeAll;
                }
            };
            MouseMove += (s, e) =>
            {
                if (_isDragging)
                {
                    var newLoc = Location;
                    newLoc.X += e.X - _mouseDownPoint.X;
                    newLoc.Y += e.Y - _mouseDownPoint.Y;
                    Location = newLoc;
                }
            };
            MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDragging = false;
                    Cursor = Cursors.Default;
                }
            };

            // 右键确认退出
            MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right) ShowExitConfirmation();
            };

            // 双击打开详情（由当前页面决定）
            DoubleClick += (s, e) => OpenDetailFor(_currentPage);

            // 将子控件也接入拖拽与快捷操作
            //foreach (Control c in Controls)
            //{
            //    c.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _mouseDownPoint = e.Location; _isDragging = true; Cursor = Cursors.SizeAll; } };
            //    c.MouseMove += (s, e) => { if (_isDragging) { var nl = Location; nl.X += e.X - _mouseDownPoint.X; nl.Y += e.Y - _mouseDownPoint.Y; Location = nl; } };
            //    c.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) { _isDragging = false; Cursor = Cursors.Default; } };
            //    c.MouseClick += (s, e) => { if (e.Button == MouseButtons.Right) Hide(); };
            //    c.DoubleClick += (s, e) => OpenDetailFor(_currentPage);
            //}
            ApplyInteractiveHandlers(this);
            ApplyInteractiveHandlers(_contentCurrent);
            ApplyInteractiveHandlers(_contentNext);
        }

        private void SwitchTo(Page target)
        {
            if (target == _currentPage) return;

            if (_slideTimer.Enabled)
            {
                // 动画没结束，记录想去的目标页，等会儿自动切
                _pendingPage = target;
                return;
            }

            _nextPage = target;
            BuildPage(_nextPage, _contentNext);

            // —— 用视觉顺序来决定方向，避免枚举值与显示顺序不一致 —— 
            int VisualIndex(Page p) => p == Page.Revenue ? 0 : p == Page.Farming ? 1 : 2; // 收益→成本→交易（左→右）
            bool toRight = VisualIndex(target) < VisualIndex(_currentPage);

            int w = Width;
            _contentNext.Top = _navBar.Bottom;
            _contentCurrent.Top = _navBar.Bottom;

            _contentNext.Left = toRight ? -w : w;
            _contentCurrent.Left = 0;

            _animationLeftStartCurrent = _contentCurrent.Left;
            _animationLeftStartNext = _contentNext.Left;
            _animationTargetLeftCurrent = toRight ? w : -w;
            _animationTargetLeftNext = 0;
            _animationDx = Math.Max(12, w / 18) * (toRight ? 1 : -1);

            _slideTimer.Start();
        }

        private void SlideTimer_Tick(object sender, EventArgs e)
        {
            // 逐帧平移
            _contentCurrent.Left += _animationDx;
            _contentNext.Left += _animationDx;

            bool currentDone = (_animationDx < 0) ? _contentCurrent.Left <= _animationTargetLeftCurrent
                                                  : _contentCurrent.Left >= _animationTargetLeftCurrent;
            bool nextDone = (_animationDx < 0) ? _contentNext.Left <= _animationTargetLeftNext
                                               : _contentNext.Left >= _animationTargetLeftNext;

            if (currentDone && nextDone)
            {
                // 动画结束，交换容器角色
                _slideTimer.Stop();
                var tmp = _contentCurrent;
                _contentCurrent = _contentNext;
                _contentNext = tmp;

                _contentNext.Left = Width; // 复位到屏外
                _contentNext.Controls.Clear();

                _currentPage = _nextPage;
                UpdateAccentBar();
                if (_pendingPage.HasValue && _pendingPage.Value != _currentPage)
                {
                    var target = _pendingPage.Value;
                    _pendingPage = null;
                    // 立刻开始下一次切换
                    SwitchTo(target);
                }

            }
        }

        private Color GetAccent(Page p) =>
            p == Page.Farming ? _accentFarming :
            p == Page.Revenue ? _accentRevenue : _accentTrading;

        private void UpdateAccentBar()
        {
            Invalidate(new Rectangle(0, 0, Width, 6)); // 触发顶部色条重绘
            if (_accentUnderline != null)
                _accentUnderline.BackColor = GetAccent(_currentPage);
            UpdateTabTextColors();
        }

        private void UpdateTabTextColors()
        {
            _tabFarming.ForeColor = (_currentPage == Page.Farming) ? _accentFarming : _textColor;
            _tabRevenue.ForeColor = (_currentPage == Page.Revenue) ? _accentRevenue : _textColor;
            _tabTrading.ForeColor = (_currentPage == Page.Trading) ? _accentTrading : _textColor;
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            // 背景卡片
            using (var brush = new SolidBrush(_cardBackground))
            {
                g.FillRoundedRectangle(brush, 0, 0, Width - 1, Height - 1, 12);
            }

            // 顶部装饰条（根据当前页切换颜色）
            Color accent = _accentFarming;
            if (_currentPage == Page.Revenue) accent = _accentRevenue;
            else if (_currentPage == Page.Trading) accent = _accentTrading;

            using (var brush = new SolidBrush(accent))
            {
                g.FillRoundedRectangle(brush, 0, 0, Width, 4, 12);
            }
        }

        private void SetRoundedRegion()
        {
            // 在窗口还未创建句柄或尺寸为 0 时不设置圆角，以免 GDI 抛异常
            if (Width <= 0 || Height <= 0 || !IsHandleCreated)
                return;

            int radius = 12;
            using var path = new GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddArc(Math.Max(1, Width - radius * 2), 0, radius * 2, radius * 2, 270, 90);
            path.AddArc(Math.Max(1, Width - radius * 2), Math.Max(1, Height - radius * 2), radius * 2, radius * 2, 0, 90);
            path.AddArc(0, Math.Max(1, Height - radius * 2), radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            Region = new Region(path);
        }

        private void BuildPage(Page page, Panel host)
        {
            host.Controls.Clear();

            // 统计容器
            var stats = new Panel
            {
                Location = new Point(20, 0),
                Size = new Size(Width - 40, 140),
                BackColor = _cardBackground
            };
            host.Controls.Add(stats);

            // 按页面定制 3 行标签
            if (page == Page.Farming)
            {
                //条件允许的情况下可以针对名称进行匹配颜色 蓝 紫 金 
                //回响可以卸载探针后方


                //成本标签页记录的应当为当前轮次的成本而不是总计成本
                //此标签页的显示应该为
                //在线时间: 0:00:00|总计轮次: 0|总计成本: 0.00 火
                //刷图时间: 0:00:00|当前轮次: 0|本轮成本: 0.00 火
                //钢铁练境的信标 X 1 | 10.00 火 | 10.00 火
                //异界回响 X 31 | 1.00 火 | 31.00 火
                //梦魇罗盘 X 1 | 40.00 火 | 40.00 火
                //罪孽之劫掠罗盘 X 1 | 40.00 火 | 40.00 火
                //破军之珍奇罗盘 X 1 | 40.00 火 | 40.00 火
                //饰品之武装罗盘 X 1 | 40.00 火 | 40.00 火
                //其中在线时间为从软件启动开始计算，刷图时间为本轮刷图用时与收益页面中的活跃时间相同
                //门票，回响，探针，罗盘X4



                //_farmingTotalTimeLabel = MakeRow("在线时间: 0:00:00|总计轮次: 0|总计成本: 0.00 火", 0);
                ////_farmingTotalTimeLabel = MakeRow("刷图时间: 0:00:00|当前轮次: 0|本轮成本: 0.00 火", 15);
                //stats.Controls.Add(_farmingTotalTimeLabel);

                //// 添加前5个消耗量最高的道具显示（每个物品一行）
                //_farmingItem1Label = MakeRow("消耗最高: 暂无数据", 30);
                //_farmingItem2Label = MakeRow("", 45);
                //_farmingItem3Label = MakeRow("", 60);
                ////75
                //_farmingItem4Label = MakeRow("", 90);
                ////105
                //_farmingItem5Label = MakeRow("", 120);
                //stats.Controls.Add(_farmingItem1Label);
                //stats.Controls.Add(_farmingItem2Label);
                //stats.Controls.Add(_farmingItem3Label);
                //stats.Controls.Add(_farmingItem4Label);
                //stats.Controls.Add(_farmingItem5Label);


                // 成本页标签
                _farmingTotalTimeLabel = MakeRow("在线时间: 0:00:00 | 总计轮次: 0 | 总计成本: 0.00 火", 0);
                _farmingRoundTimeLabel = MakeRow("刷图时间: 0:00:00 | 当前轮次: 0 | 本轮成本: 0.00 火", 15);
                stats.Controls.Add(_farmingTotalTimeLabel);
                stats.Controls.Add(_farmingRoundTimeLabel);

                // 9个物品行
                _farmingItemLabels = new List<Label>();
                for (int i = 0; i < 9; i++)
                {
                    var lbl = MakeRow("", 35 + i * 15);
                    _farmingItemLabels.Add(lbl);
                    stats.Controls.Add(lbl);
                }

            }
            else if (page == Page.Revenue)
            {
                ////新增一个掉落总价最高与一个掉落总价最低
                //_revenueTotalDropLabel = MakeRow("总掉落: 0 火", 0);
                //stats.Controls.Add(_revenueTotalDropLabel);

                //_revenueNetProfitLabel = MakeRow("净利润: 0 火", 25);
                //_revenueNetProfitLabel.Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold);
                //_revenueNetProfitLabel.ForeColor = _accentRevenue;
                //stats.Controls.Add(_revenueNetProfitLabel);

                //_revenueActiveTimeLabel = MakeRow("活跃时间: 0:00:00", 50);
                //stats.Controls.Add(_revenueActiveTimeLabel);
                // —— 收益页：三行汇总 + 最高/最低 —— 

                // —— 四行汇总（与成本页行高/字体一致）——
                _revenueLine1Label = MakeRowTight("在线时间: 0:00:00 | 刷图时间: 0:00:00", 0);
                _revenueLine2Label = MakeRowTight("本轮掉落: 0.00 火 | 本轮利润: 0.00 火", 15);
                _revenueLine3Label = MakeRowTight("累计收益: 0.00 火 | 累计时间: 0:00:00", 30);
                _revenueLine4Label = MakeRowTight("时均收益: 0.00 火 | 轮均收益: 0.00 火", 45);

                // —— 本轮最高收益（标题 + 条目各占一行）——
                _revenueMaxLabel = MakeRowTight("本轮最高收益", 60);
                _revenueMaxItemLabel = MakeRowTight("—", 75);

                // —— 本轮最低收益（标题 + 条目各占一行）——
                _revenueMinLabel = MakeRowTight("本轮最低收益", 90);
                _revenueMinItemLabel = MakeRowTight("—", 105);

                // 颜色与成本页一致（白色文本，不上绿色）
                _revenueLine1Label.ForeColor = _textColor;
                _revenueLine2Label.ForeColor = _textColor;
                _revenueLine3Label.ForeColor = _textColor;
                _revenueLine4Label.ForeColor = _textColor;
                _revenueMaxLabel.ForeColor = _textColor;
                _revenueMinLabel.ForeColor = _textColor;

                // 按顺序加入容器（与成本页一致）
                stats.Controls.Add(_revenueMinItemLabel);
                stats.Controls.Add(_revenueMinLabel);
                stats.Controls.Add(_revenueMaxItemLabel);
                stats.Controls.Add(_revenueMaxLabel);
                stats.Controls.Add(_revenueLine4Label);
                stats.Controls.Add(_revenueLine3Label);
                stats.Controls.Add(_revenueLine2Label);
                stats.Controls.Add(_revenueLine1Label);

            }
            else if (page == Page.Trading)
            {
                _tradingBuyLabel = MakeRow("购买商品: 0 火", 0);
                _tradingBuyLabel.ForeColor = Color.FromArgb(255, 100, 100);
                stats.Controls.Add(_tradingBuyLabel);

                _tradingSellLabel = MakeRow("出售商品: 0 火", 25);
                _tradingSellLabel.ForeColor = Color.FromArgb(100, 255, 100);
                stats.Controls.Add(_tradingSellLabel);

                _tradingNetLabel = MakeRow("净利润: 0 火", 50);
                _tradingNetLabel.Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold);
                stats.Controls.Add(_tradingNetLabel);
            }

            // 操作提示
            var hint = new Label
            {
                Text = "拖拽移动 | 双击查看详情 | 右键退出",
                Location = new Point(20, 150),
                Size = new Size(Width - 40, 20),
                Font = new Font("Microsoft YaHei", 8f),
                ForeColor = _secondaryTextColor,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = _cardBackground
            };
            host.Controls.Add(hint);
            ApplyInteractiveHandlers(host);
        }

        private Label MakeRow(string text, int y, Color? color = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(0, y),
                Size = new Size(Width - 40, 20),
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = color ?? _secondaryTextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = _cardBackground
            };
        }

        // 紧凑行：高度15px，用于收益页避免被裁切
        private Label MakeRowTight(string text, int y, Color? color = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(0, y),
                Size = new Size(Width - 40, 15),            // ★ 高度 15px
                Font = new Font("Microsoft YaHei", 8.5f),   // ★ 字体略小，避免行高撑大
                ForeColor = color ?? _secondaryTextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = _cardBackground
            };
        }

        private void OpenDetailFor(Page page)
        {
            try
            {
                // 如果已有详情窗，就激活它即可（避免重复创建）
                if (_detailForm != null && !_detailForm.IsDisposed)
                {
                    _detailForm.Activate();
                    return;
                }

                // 创建详情窗体
                _detailForm = new RoundedDetailForm
                {
                    Text = "详细统计",
                    StartPosition = FormStartPosition.CenterScreen,
                    Size = new Size(920, 600),
                    FormBorderStyle = FormBorderStyle.FixedSingle,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.FromArgb(30, 30, 30),
                    TopMost = true,  // 确保详情窗口也是置顶的
                    Owner = this // 指定主人窗体，便于激活切换
                };

                // 紧跟着创建 detailControl 的位置
                var host = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10),
                    BackColor = Color.FromArgb(30, 30, 30)
                };
                var detailControl = new NewStatsDetailControl { Dock = DockStyle.Fill };
                host.Controls.Add(detailControl);
                _detailForm.Controls.Add(host);


                // 选择打开时要展示的页签（公开方法）
                NewStatsDetailControl.DetailView dv =
                    page == Page.Revenue ? NewStatsDetailControl.DetailView.Revenue :
                    page == Page.Trading ? NewStatsDetailControl.DetailView.Trading :
                                           NewStatsDetailControl.DetailView.Farming;
                detailControl.SelectView(dv);  // 显式切到对应页签

                // 立即更新详情窗口的数据
                detailControl.UpdateAllStats();

                // —— 联动：打开详情时隐藏主窗体 ——
                Hide();

                // 详情窗关闭时，重新显示主窗体
                _detailForm.FormClosed += (s, e) =>
                {
                    _detailForm = null;          // 清理引用
                    Show();                 // 重新显示
                    BringToFront();         // 置前
                    Activate();             // 获取焦点
                    TopMost = true;         // 确保主窗口重新置顶
                };

                _detailForm.Show();
                _detailForm.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开详情窗口时出错: " + ex.Message);
            }
        }




        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // 可能在 InitializeComponent() 之前被触发（如设置 Size 时），此时成员仍为 null
            if (_navBar == null || _contentCurrent == null || _contentNext == null)
                return;

            SetRoundedRegion();

            // 自适应内容面板尺寸
            var contentTop = _navBar.Bottom;
            var contentHeight = Math.Max(0, Height - contentTop);
            _contentCurrent.Size = new Size(Width, contentHeight);
            _contentNext.Size = new Size(Width, contentHeight);

            // 在窗口尺寸变化时，确保 next 容器保持在屏外复位（避免因拉伸露出）
            if (!_slideTimer?.Enabled ?? true)
            {
                _contentCurrent.Left = 0;
                _contentCurrent.Top = contentTop;
                _contentNext.Left = Width; // 屏外右侧
                _contentNext.Top = contentTop;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // 确保窗口激活时保持置顶状态
            if (!TopMost)
            {
                TopMost = true;
            }
        }

        // 横线（_navBar 含下划线面板）下方才生效
        private bool IsInInteractiveArea(Point clientPt)
        {
            return clientPt.Y >= (_navBar?.Bottom ?? 0);
        }

        // 递归给 root 及其子控件绑定交互事件；并对后续新增的子控件也自动绑定
        // 递归给 root 及其子控件绑定交互事件；并对后续新增的子控件也自动绑定
        private void ApplyInteractiveHandlers(Control root)
        {
            if (root == null) return;

            void bindIfNeeded(Control c)
            {
                // —— 关键：跳过导航条本身、以及在导航条上的一切控件（按钮等）——
                if (c == _navBar || c.Parent == _navBar || c is Button)
                {
                    // 仍然递归，以免导航条里还有容器（保险）
                    foreach (Control child in c.Controls) bindIfNeeded(child);
                    // 不给它本身绑定拖拽/右键/双击，交给按钮自己的 Click 去处理
                    return;
                }

                if (_boundControls.Contains(c))
                {
                    foreach (Control child in c.Controls) bindIfNeeded(child);
                    return;
                }
                _boundControls.Add(c);

                c.MouseDown += Control_MouseDown;
                c.MouseMove += Control_MouseMove;
                c.MouseUp += Control_MouseUp;
                c.MouseClick += Control_MouseClick;
                c.DoubleClick += Control_DoubleClick;

                foreach (Control child in c.Controls)
                    bindIfNeeded(child);

                // 动态新增的子控件也自动绑定
                c.ControlAdded += (s, e) => bindIfNeeded(e.Control);
            }

            bindIfNeeded(root);
        }




        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var src = sender as Control ?? this;
            var client = PointToClient(src.PointToScreen(e.Location));
            if (!IsInInteractiveArea(client)) return;

            _pressPointScreen = Control.MousePosition; // 屏幕坐标
            _dragStarted = false;                      // 还没开始拖
            Cursor = Cursors.SizeAll;                  // 反馈一下
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if ((Control.MouseButtons & MouseButtons.Left) == 0) return; // 只在按住左键时
            if (_dragStarted) return;

            var cur = Control.MousePosition;
            int dx = Math.Abs(cur.X - _pressPointScreen.X);
            int dy = Math.Abs(cur.Y - _pressPointScreen.Y);
            if (dx >= DRAG_THRESHOLD || dy >= DRAG_THRESHOLD)
            {
                _dragStarted = true;
                BeginNativeDrag(); // 真正开始拖动（系统接管，顺滑）
            }
        }


        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Cursor = Cursors.Default;
                _dragStarted = false;
            }
        }



        private void Control_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var src = sender as Control ?? this;
            var client = PointToClient(src.PointToScreen(e.Location));
            if (IsInInteractiveArea(client)) ShowExitConfirmation();
        }


        private void Control_DoubleClick(object sender, EventArgs e)
        {
            var client = PointToClient(Control.MousePosition);
            if (!IsInInteractiveArea(client)) return;

            int now = Environment.TickCount;
            if (now - _lastOpenTick < 300) return; // 去抖
            _lastOpenTick = now;

            OpenDetailFor(_currentPage); // 只开一次
        }

        /// <summary>
        /// 显示退出确认对话框
        /// </summary>
        private void ShowExitConfirmation()
        {
            var result = MessageBox.Show(
                "确定要退出程序吗？",
                "确认退出",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                // 通知主程序退出
                //Program.RequestExit();

                Close();
            }
        }


        /// <summary>
        /// 更新所有统计数据
        /// </summary>
        public void UpdateAllStats()
        {
            UpdateFarmingStats();
            UpdateRevenueStats();
            UpdateTradingStats();
            UpdatePriceData();
        }

        /// <summary>
        /// 更新刷图统计
        /// </summary>
        //public void UpdateFarmingStats()
        //{
        //    try
        //    {
        //        if (_currentPage != Page.Farming || _farmingTotalTimeLabel == null) return;

        //        var farmingManager = ServiceLocator.Instance.Get<FarmingCostManager>();
        //        var currentDropManager = ServiceLocator.Instance.Get<CurrentDropManager>();

        //        var farmingSummary = farmingManager.GetFarmingSummary();
        //        var currentDropSummary = currentDropManager.GetCurrentDropSummary();

        //        // 计算总消耗
        //        double totalCost = farmingSummary.Sum(x => x.TotalValue);

        //        // 计算总轮次（这里简化处理，实际可能需要更复杂的逻辑）
        //        int totalRounds = farmingSummary.Count > 0 ? farmingSummary.Max(x => x.RunCount) : 0;

        //        // 格式化时间显示
        //        string timeStr = FormatTimeSpan(currentDropSummary.ActiveTime);

        //        _farmingTotalTimeLabel.Text = $"总时间: {timeStr}/总轮次: {totalRounds}/总消耗: {totalCost:F2} 火";

        //        // 更新前5个消耗量最高的道具显示（每个物品一行）
        //        var top5Items = farmingSummary
        //            .OrderByDescending(x => x.TotalValue)
        //            .Take(5)
        //            .ToList();

        //        var itemLabels = new[] { _farmingItem1Label, _farmingItem2Label, _farmingItem3Label, _farmingItem4Label, _farmingItem5Label };

        //        for (int i = 0; i < itemLabels.Length; i++)
        //        {
        //            if (itemLabels[i] != null)
        //            {
        //                if (i < top5Items.Count)
        //                {
        //                    var item = top5Items[i];
        //                    itemLabels[i].Text = $"消耗最高{i + 1}: {item.ItemName} ({item.TotalValue:F1}火)";
        //                }
        //                else
        //                {
        //                    itemLabels[i].Text = "";
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //ConsoleLogger.Instance.LogError($"更新刷图统计失败: {ex.Message}");
        //    }
        //}

        // 让每行成本明细对齐显示的格式化函数
        private static string FormatCostLine(string name, int qty, double unit, double total)
        {
            // name 左对齐宽 14，数量右对齐宽 3，单价/总价右对齐宽 7
            // 例如：探针           X  2 |   3.50 火 |    7.00 火
            return $"{name} X {qty} | {unit:0.00} 火 | {total:0.00} 火";
        }


        public void UpdateFarmingStats()
        {

            if (_farmingTotalTimeLabel == null || _farmingItemLabels == null)
            {
                // 在一个离屏 Panel 上构建成本页控件，避免打断当前显示内容
                var temp = new Panel { Size = _contentCurrent.Size, BackColor = _cardBackground };
                BuildPage(Page.Farming, temp);
                // 不把 temp 加到窗体上，仅为了初始化字段（_farmingTotalTimeLabel 等）
            }

            // 依赖的管理器
            var costMgr = ServiceLocator.Instance.Get<NewUI.Managers.FarmingCostManager>();
            var priceMgr = ServiceLocator.Instance.Get<NewUI.Managers.PriceManager>();

            // 在线时间 = 进程存活时间
            var online = DateTime.Now - Process.GetCurrentProcess().StartTime;

            // 当前轮次 = _rounds 最后一项
            var rounds = costMgr.GetAllFarmingRounds();
            var current = rounds.Count > 0 ? rounds[^1] : null;
            var active = current?.Duration ?? TimeSpan.Zero;
            var currentRoundNo = current?.RoundNumber ?? 0;

            // 总轮次 / 总成本
            int totalRounds = costMgr.GetTotalRounds();
            double totalCost = costMgr.GetTotalCost();

            // 当前轮次成本与逐项明细
            var usage = costMgr.GetCurrentRoundItems(); // <int itemId, int count>
            double currentRoundCost = 0.0;
            var lines = new List<string>();

            foreach (var kv in usage)
            {
                var id = kv.Key;
                var qty = kv.Value;
                var unit = priceMgr.GetItemUnitPriceWithoutTax(id);
                var name = priceMgr.GetItemName(id);
                var lineTotal = unit * qty;
                currentRoundCost += lineTotal;

                lines.Add(FormatCostLine(name, qty, unit, lineTotal));
            }

            // 更新两行头部
            _farmingTotalTimeLabel.Text =
                $"在线时间: {online:hh\\:mm\\:ss} | 总计轮次: {totalRounds} | 总计成本: {totalCost:F2} 火";
            _farmingRoundTimeLabel.Text =
                $"刷图时间: {active:hh\\:mm\\:ss} | 当前轮次: {currentRoundNo} | 本轮成本: {currentRoundCost:F2} 火";

            // 显示最多 9 行
            for (int i = 0; i < _farmingItemLabels.Count; i++)
            {
                _farmingItemLabels[i].Text = i < lines.Count ? lines[i] : string.Empty;
            }

        }


        /// <summary>
        /// 更新收益统计
        /// </summary>
        //public void UpdateRevenueStats()
        //{
        //收益标签页记录的应当为当前轮次的收益
        //此标签页的显示应该为


        //在线时间: 0:00:00|刷图时间: 0:00:00
        //本轮掉落: 0.00 火|本轮利润: 0.00 火
        //累计收益: 0.00 火|累计时间: 0:00:00
        //时均收益: 0.00 火|轮均收益: 0.00 火
        //本轮最高收益
        //罪孽之劫掠罗盘 X 1 | 40.00 火 | 40.00 火
        //本轮最低收益
        //异界回响 X 1 | 1.00 火 | 1.00 火


        //在线时间为软件启动时间，刷图时间为本轮刷图用时，累计时间为总刷图用时去除安全区
        //其中在线时间与刷图时间应与成本页面中的数据一致，累计时间应为在线时间-刷图时间
        //本轮掉落为本轮所有掉落的总计收益(未考虑后续增加交易行税率功能，实际应当根据用户是否选择开启交易行
        //税率功能动态调节本轮掉落 如果开启应该为(真实掉落-初火掉落)*税率)
        //累计收益为本次启动的所有收益 与本轮收益一样未考虑后续税率功能，轮均收益未累计收益/轮次 时均收益为
        //累计收益/累计时间(不满1小时按1小时计算)
        //    try
        //    {
        //        if (_currentPage != Page.Revenue || _revenueTotalDropLabel == null) return;

        //        var currentDropManager = ServiceLocator.Instance.Get<CurrentDropManager>();
        //        var currentDropSummary = currentDropManager.GetCurrentDropSummary();

        //        _revenueTotalDropLabel.Text = $"总掉落: {currentDropSummary.TotalValue:F2} 火";
        //        _revenueNetProfitLabel.Text = $"净利润: {currentDropSummary.NetProfit:F2} 火";
        //        _revenueActiveTimeLabel.Text = $"活跃时间: {FormatTimeSpan(currentDropSummary.ActiveTime)}";
        //    }
        //    catch (Exception ex)
        //    {
        //        //ConsoleLogger.Instance.LogError($"更新收益统计失败: {ex.Message}");
        //    }
        //}

        public void UpdateRevenueStats()
        {
            try
            {
                if (_currentPage != Page.Revenue || _revenueLine1Label == null) return;

                // —— 数据源 —— 
                var dropMgr = ServiceLocator.Instance.Get<CurrentDropManager>();
                var costMgr = ServiceLocator.Instance.Get<NewUI.Managers.FarmingCostManager>();
                var priceMgr = ServiceLocator.Instance.Get<NewUI.Managers.PriceManager>();

                var sum = dropMgr?.GetCurrentDropSummary();

                // —— 时间线（与成本页口径一致）——
                TimeSpan online = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime; // 在线时间
                TimeSpan active = sum?.ActiveTime ?? TimeSpan.Zero;                                        // 本轮刷图时间
                TimeSpan cumTime = online - active;                                                        // 累计时间
                if (cumTime < TimeSpan.Zero) cumTime = TimeSpan.Zero;

                // —— 本轮掉落/成本/利润 —— 
                double roundDrop = 0.0;
                int roundTotalCount = 0;
                if (sum?.DropItems != null)
                {
                    foreach (var it in sum.DropItems)
                    {
                        roundDrop += it.TotalValue;
                        roundTotalCount += it.Count;
                    }
                }

                double roundCost = 0.0;
                try
                {
                    var usage = costMgr?.GetCurrentRoundItems(); // Dictionary<string,int> 物品ID/名称 -> 数量
                    if (usage != null && priceMgr != null)
                    {
                        foreach (var kv in usage)
                        {
                            var unit = priceMgr.GetItemUnitPriceWithoutTax(kv.Key);
                            roundCost += unit * kv.Value;
                        }
                    }
                }
                catch { /* 忽略计价异常，保证界面可用 */ }

                double roundProfit = roundDrop - roundCost;

                // —— 累计/均值 —— 
                double totalIncome = sum?.TotalValue ?? 0.0;
                int totalRounds = 0;
                try { totalRounds = costMgr?.GetTotalRounds() ?? 0; } catch { }
                double avgPerRound = totalRounds > 0 ? totalIncome / totalRounds : 0.0;

                // 时均收益：累计收益 / 累计时间（不足1小时按1小时）
                double hours = Math.Max(1.0, cumTime.TotalHours <= 0 ? 0.0 : cumTime.TotalHours);
                double avgPerHour = hours > 0 ? totalIncome / hours : 0.0;

                // —— 本轮最高/最低收益 —— 
                CurrentDropInfo maxItem = null, minItem = null;
                double maxUnit = 0.0, minUnit = 0.0;
                bool hasItems = sum?.DropItems != null && sum.DropItems.Count > 0;
                if (hasItems)
                {
                    maxItem = sum.DropItems.OrderByDescending(x => x.TotalValue).First();
                    minItem = sum.DropItems.OrderBy(x => x.TotalValue).First();
                    maxUnit = maxItem.Count > 0 ? (maxItem.TotalValue / maxItem.Count) : maxItem.TotalValue;
                    minUnit = minItem.Count > 0 ? (minItem.TotalValue / minItem.Count) : minItem.TotalValue;
                }

                // ========== 测试数据开关 ==========
                const bool USE_TEST = true; // ← 想看示例就改为 true
                if (USE_TEST)
                {
                    online = TimeSpan.FromMinutes(90);  // 01:30:00
                    active = TimeSpan.FromMinutes(45);  // 00:45:00
                    cumTime = online - active;
                    roundDrop = 0.00;
                    roundCost = 20.00;
                    roundProfit = -1.00;
                    totalIncome = 30000.00;
                    totalRounds = 5;
                    avgPerRound = totalIncome / totalRounds;
                    avgPerHour = totalIncome / 100;        // 假设累计 2.5 小时

                    hasItems = true;
                    maxItem = new CurrentDropInfo { ItemName = "罪孽之劫掠罗盘", Count = 1, TotalValue = 40.00 };
                    minItem = new CurrentDropInfo { ItemName = "异界回响", Count = 1, TotalValue = 1.00 };
                    maxUnit = 40.00;
                    minUnit = 1.00;
                }
                // ========== 测试数据结束 ==========

                // —— 写 UI（与成本页同排版：每条固定行高一行）——
                _revenueLine1Label.Text = $"在线时间: {FormatTimeSpan(online)} | 刷图时间: {FormatTimeSpan(active)}";
                _revenueLine2Label.Text = $"本轮掉落: {roundDrop:F2} 火 | 本轮利润: {roundProfit:F2} 火";
                _revenueLine3Label.Text = $"累计收益: {totalIncome:F2} 火 | 累计时间: {FormatTimeSpan(cumTime)}";
                _revenueLine4Label.Text = $"时均收益: {avgPerHour:F2} 火 | 轮均收益: {avgPerRound:F2} 火";

                RenderRevenueLine2(roundDrop, roundProfit);
                RenderRevenueLine4(avgPerHour, avgPerRound);

                _revenueMaxLabel.Text = "本轮最高收益";
                _revenueMinLabel.Text = "本轮最低收益";
                _revenueMaxItemLabel.Text = hasItems
                    ? $"{maxItem.ItemName} X {maxItem.Count} | {maxUnit:F2} 火 | {maxItem.TotalValue:F2} 火"
                    : "—";
                _revenueMinItemLabel.Text = hasItems
                    ? $"{minItem.ItemName} X {minItem.Count} | {minUnit:F2} 火 | {minItem.TotalValue:F2} 火"
                    : "—";
            }
            catch
            {
                // 静默失败，避免干扰运行
            }
        }

        // —— 区间配色（严格按你的规则）——
        private Color ColorForProfit(double v)
        {
            if (v < 0) return Color.Red;                 // < 0 红
            if (v == 0) return Color.Gray;               // = 0 灰
            if (v > 0 && v < 1000) return Color.LimeGreen; // 0~1000 绿色（不含1000）
            if (v >= 1000 && v < 2000) return Color.Orange; // 1000~2000 橙色（含1000，不含2000）
            return Color.Gold;                           // > 2000 金色
        }

        // —— 只让“数字部分”变色的通用渲染器 ——
        //    首次会用一个 Panel 替换原 Label，后续刷新复用，不会越堆越多。
        private void RenderSegments(Label holder, (string text, Color? color)[] segments)
        {
            if (holder == null || holder.Parent == null) return;

            // 已替换过则复用
            if (holder.Tag is Panel existed && existed.Parent == holder.Parent)
            {
                if (existed.Controls.Count != segments.Length)
                {
                    existed.Controls.Clear();
                    BuildSegmentLabels(existed, segments, holder.Font, holder.BackColor);
                }
                else
                {
                    for (int i = 0; i < segments.Length; i++)
                    {
                        var segLbl = (Label)existed.Controls[i];
                        segLbl.Text = segments[i].text;
                        segLbl.ForeColor = segments[i].color ?? holder.ForeColor;
                    }
                }
                existed.Visible = true;
                existed.BringToFront();
                holder.Visible = false;
                return;
            }

            // 第一次：创建 Panel 替换 Label
            var panel = new Panel
            {
                Location = holder.Location,
                Size = holder.Size,
                BackColor = holder.BackColor,
                Anchor = holder.Anchor
            };

            BuildSegmentLabels(panel, segments, holder.Font, holder.BackColor);

            holder.Parent.Controls.Add(panel);
            panel.BringToFront();
            holder.Visible = false;
            holder.Tag = panel;
        }

        private void BuildSegmentLabels(Panel panel, (string text, Color? color)[] segments, Font font, Color backColor)
        {
            panel.SuspendLayout();
            panel.Controls.Clear();

            int x = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                var (txt, clr) = segments[i];
                var seg = new Label
                {
                    AutoSize = true,
                    Text = txt,
                    Font = font,
                    ForeColor = clr ?? Color.White,
                    BackColor = backColor,
                    Location = new Point(x, (panel.Height - font.Height) / 2)
                };
                panel.Controls.Add(seg);
                seg.PerformLayout();
                x = seg.Right;
            }
            panel.ResumeLayout(performLayout: true);
        }

        // —— 专门渲染收益两行（使用 roundDrop 变量名）——
        private void RenderRevenueLine2(double roundDrop, double netProfit)
        {
            // "本轮掉落: {roundDrop} 火 | 本轮利润: {netProfit} 火"
            RenderSegments(_revenueLine2Label, new (string, Color?)[]
            {
        ("本轮掉落: ", null),
        (roundDrop.ToString("0.00"), ColorForProfit(roundDrop)),
        (" 火 | 本轮利润: ", null),
        (netProfit.ToString("0.00"), ColorForProfit(netProfit)),
        (" 火", null),
            });
        }

        private void RenderRevenueLine4(double hourlyProfit, double roundProfit)
        {
            // "时均收益: {hourly} 火 | 轮均收益: {round} 火"
            RenderSegments(_revenueLine4Label, new (string, Color?)[]
            {
        ("时均收益: ", null),
        (hourlyProfit.ToString("0.00"), ColorForProfit(hourlyProfit)),
        (" 火 | 轮均收益: ", null),
        (roundProfit.ToString("0.00"), ColorForProfit(roundProfit)),
        (" 火", null),
            });
        }

        // 圆角详情窗体（无边框 + 阴影 + 自适应圆角）
        private sealed class RoundedDetailForm : Form
        {
            private const int CornerRadius = 18;

            public RoundedDetailForm()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                DoubleBuffered = true;
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.FromArgb(30, 30, 30);
                TopMost = true;
                Padding = new Padding(0);
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    // 轻微阴影
                    cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                    return cp;
                }
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                SetRoundedRegion();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                SetRoundedRegion();
                Invalidate();
            }

            private void SetRoundedRegion()
            {
                if (!IsHandleCreated || Width <= 0 || Height <= 0) return;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                int r = CornerRadius, d = r * 2;
                path.AddArc(0, 0, d, d, 180, 90);
                path.AddArc(Width - d - 1, 0, d, d, 270, 90);
                path.AddArc(Width - d - 1, Height - d - 1, d, d, 0, 90);
                path.AddArc(0, Height - d - 1, d, d, 90, 90);
                path.CloseFigure();

                Region?.Dispose();
                Region = new Region(path);
            }
        }




        /// <summary>
        /// 更新交易统计
        /// </summary>
        public void UpdateTradingStats()
        {
            try
            {
                if (_currentPage != Page.Trading || _tradingBuyLabel == null) return;

                var tradingManager = ServiceLocator.Instance.Get<TradingManager>();
                var tradingSummary = tradingManager.GetTradingSummary();

                _tradingBuyLabel.Text = $"购买商品: {tradingSummary.TotalBuyConsumeValue:F2} 火";
                _tradingSellLabel.Text = $"出售商品: {tradingSummary.TotalReceiveValue:F2} 火";
                _tradingNetLabel.Text = $"净利润: {tradingSummary.NetTradingProfit:F2} 火";
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新交易统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新物价数据
        /// </summary>
        public void UpdatePriceData()
        {
            try
            {
                // 物价数据更新逻辑可以在这里实现
                // 目前主要是为了保持接口一致性
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新物价数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 格式化时间跨度显示
        /// </summary>
        /// <param name="timeSpan">时间跨度</param>
        /// <returns>格式化后的时间字符串</returns>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            else
            {
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
        }

        /// <summary>
        /// 清理UI相关资源
        /// </summary>
        private void CleanupUIResources()
        {
            try
            {
                //ConsoleLogger.Instance.LogInfo("清理UI资源...");

                // 清理导航栏控件
                _tabFarming?.Dispose();
                _tabRevenue?.Dispose();
                _tabTrading?.Dispose();
                _accentUnderline?.Dispose();
                _navBar?.Dispose();

                // 清理内容面板
                _contentCurrent?.Controls.Clear();
                _contentCurrent?.Dispose();
                _contentNext?.Controls.Clear();
                _contentNext?.Dispose();

                // 清理已绑定控件的集合
                _boundControls?.Clear();

                //ConsoleLogger.Instance.LogInfo("UI资源清理完成");
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"UI资源清理失败: {ex.Message}");
            }
        }



    }

    // —— 图形扩展（圆角绘制） —— 与原窗体保持一致 ——
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float width, float height, float radius)
        {
            using var path = GetRoundedRectPath(x, y, width, height, radius);
            g.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, float x, float y, float width, float height, float radius)
        {
            using var path = GetRoundedRectPath(x, y, width, height, radius);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath GetRoundedRectPath(float x, float y, float width, float height, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;
            path.AddArc(x, y, diameter, diameter, 180, 90);
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

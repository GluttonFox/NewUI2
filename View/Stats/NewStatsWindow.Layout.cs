using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NewUI
{
    public partial class NewStatsWindow
    {
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
                Height = 2,
                BackColor = _accentFarming
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
                Location = new Point(Width, _navBar.Bottom),
                Size = new Size(Width, Height - _navBar.Height),
                BackColor = _cardBackground
            };
            Controls.Add(_contentNext);
            Controls.Add(_contentCurrent);

            // 动画计时器
            _slideTimer = new System.Windows.Forms.Timer { Interval = 15 };
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
                if (btn.Tag is Page target)
                {
                    SwitchTo(target);
                }
            };

            return btn;
        }

        private Color GetAccent(Page page) =>
            page switch
            {
                Page.Farming => _accentFarming,
                Page.Revenue => _accentRevenue,
                _ => _accentTrading
            };

        private void UpdateAccentBar()
        {
            Invalidate(new Rectangle(0, 0, Width, 6));
            if (_accentUnderline != null)
            {
                _accentUnderline.BackColor = GetAccent(_currentPage);
            }

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

            using (var brush = new SolidBrush(_cardBackground))
            {
                g.FillRoundedRectangle(brush, 0, 0, Width - 1, Height - 1, 12);
            }

            Color accent = GetAccent(_currentPage);
            using (var brush = new SolidBrush(accent))
            {
                g.FillRoundedRectangle(brush, 0, 0, Width, 4, 12);
            }
        }

        private void SetRoundedRegion()
        {
            if (Width <= 0 || Height <= 0 || !IsHandleCreated)
            {
                return;
            }

            int radius = 12;
            using var path = new GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddArc(Math.Max(1, Width - radius * 2), 0, radius * 2, radius * 2, 270, 90);
            path.AddArc(Math.Max(1, Width - radius * 2), Math.Max(1, Height - radius * 2), radius * 2, radius * 2, 0, 90);
            path.AddArc(0, Math.Max(1, Height - radius * 2), radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            Region = new Region(path);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (_navBar == null || _contentCurrent == null || _contentNext == null)
            {
                return;
            }

            SetRoundedRegion();

            var contentTop = _navBar.Bottom;
            var contentHeight = Math.Max(0, Height - contentTop);
            _contentCurrent.Size = new Size(Width, contentHeight);
            _contentNext.Size = new Size(Width, contentHeight);

            if (!_slideTimer?.Enabled ?? true)
            {
                _contentCurrent.Left = 0;
                _contentCurrent.Top = contentTop;
                _contentNext.Left = Width;
                _contentNext.Top = contentTop;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (!TopMost)
            {
                TopMost = true;
            }
        }
    }
}

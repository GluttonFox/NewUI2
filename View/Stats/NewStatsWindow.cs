using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NewUI
{
    /// <summary>
    /// 统一的统计窗口（将“刷图消耗”“收益统计”“交易统计”三窗体合并为单窗体）。
    /// 该部分类仅保留字段声明与基础构造逻辑，其余功能拆分至各自的局部类文件中。
    /// </summary>
    public partial class NewStatsWindow : Form
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

        // —— 内容容器（用于切换动画：当前/下一页两个容器滑动） ——
        private Panel _contentCurrent;
        private Panel _contentNext;

        // —— 动画 ——
        private System.Windows.Forms.Timer _slideTimer;
        private int _animationDx;
        private int _animationLeftStartCurrent;
        private int _animationLeftStartNext;
        private int _animationTargetLeftCurrent;
        private int _animationTargetLeftNext;

        private Page _currentPage = Page.Revenue;
        private Page _nextPage = Page.Revenue;
        private Page? _pendingPage;

        // —— 样式常量 ——
        private readonly Color _cardBackground = Color.FromArgb(30, 30, 30);
        private readonly Color _textColor = Color.FromArgb(255, 255, 255);
        private readonly Color _secondaryTextColor = Color.FromArgb(180, 180, 180);

        private readonly Color _accentFarming = Color.FromArgb(220, 53, 69);
        private readonly Color _accentRevenue = Color.FromArgb(40, 167, 69);
        private readonly Color _accentTrading = Color.FromArgb(255, 193, 7);

        private bool _isDragging;
        private Point _mouseDownPoint;

        private Point _pressPointScreen;
        private bool _dragStarted;
        private const int DRAG_THRESHOLD = 4;

        private int _lastOpenTick;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;

        // 跟踪已经绑定过交互事件的控件，避免重复绑定
        private readonly HashSet<Control> _boundControls = new HashSet<Control>();

        private Form _detailForm;

        // 数据标签引用（用于实时更新）
        private Label _farmingTotalTimeLabel;
        private Label _farmingRoundTimeLabel;
        private List<Label> _farmingItemLabels;

        private Label _revenueLine1Label;
        private Label _revenueLine2Label;
        private Label _revenueLine3Label;
        private Label _revenueLine4Label;
        private Label _revenueMaxLabel;
        private Label _revenueMaxItemLabel;
        private Label _revenueMinLabel;
        private Label _revenueMinItemLabel;

        private Label _tradingBuyLabel;
        private Label _tradingSellLabel;
        private Label _tradingNetLabel;

        public NewStatsWindow()
        {
            InitializeComponent();
            SetupEvents();
            BuildPage(_currentPage, _contentCurrent); // 初始构建
            UpdateAccentBar();
        }
    }
}

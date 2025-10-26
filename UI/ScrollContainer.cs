using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NewUI.UI;

namespace NewUI.UI
{
    public class ScrollContainer : UserControl
    {
        private readonly Panel _viewport;     // 显示窗口
        private readonly Panel _content;      // 实际内容（你往这里加控件）
        private readonly CustomScrollBar _bar;

        public Control ContentPanel => _content;

        public ScrollContainer()
        {
            DoubleBuffered = true;

            _viewport = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            _content = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(100, 100),
                BackColor = Color.Transparent
            };

            _bar = new CustomScrollBar
            {
                Dock = DockStyle.Right,
                Orientation = ScrollOrientationEx.Vertical,
                Width = 10,
                Margin = new Padding(0, 0, 0, 0),
                ThumbMinLength = 30,
                Thickness = 8,
            };

            _viewport.Controls.Add(_content);
            Controls.Add(_viewport);
            Controls.Add(_bar);

            _bar.ValueChanged += (_, __) => UpdateContentOffset();
            _viewport.Resize += (_, __) => RecalcScroll();
            _content.ControlAdded += (_, __) => HookChildEvents();
            _content.ControlRemoved += (_, __) => HookChildEvents();

            HookChildEvents();
            EnableMouseWheel(true);
        }

        private void HookChildEvents()
        {
            // 子控件尺寸变化引起内容高度变化时，重新计算
            foreach (Control c in _content.Controls)
                c.SizeChanged -= Child_SizeChanged;

            foreach (Control c in _content.Controls)
                c.SizeChanged += Child_SizeChanged;

            Child_SizeChanged(this, EventArgs.Empty);
        }

        private void Child_SizeChanged(object sender, EventArgs e)
        {
            // 以所有子控件的最大 Bottom 作为内容高度
            int contentH = 0, contentW = _viewport.Width - _bar.Width;
            foreach (Control c in _content.Controls)
                contentH = Math.Max(contentH, c.Bottom);

            _content.Size = new Size(contentW, Math.Max(contentH, _viewport.Height));
            RecalcScroll();
        }

        private void RecalcScroll()
        {
            int viewportH = _viewport.Height;
            int contentH = Math.Max(_content.Height, CalcContentHeight());
            _content.Height = contentH;

            int range = Math.Max(0, contentH - viewportH);

            _bar.Minimum = 0;
            _bar.Maximum = range <= 0 ? 0 : range;
            _bar.LargeChange = Math.Max(1, viewportH); // 可视窗口大小决定拇指长度
            _bar.Enabled = range > 0;
            if (_bar.Value > _bar.Maximum) _bar.Value = _bar.Maximum;

            UpdateContentOffset();
        }

        private int CalcContentHeight()
        {
            int h = 0;
            foreach (Control c in _content.Controls) h = Math.Max(h, c.Bottom);
            return h;
        }

        private void UpdateContentOffset()
        {
            _content.Top = -_bar.Value; // 通过移动内容实现滚动
        }

        public void EnableMouseWheel(bool enable)
        {
            if (enable)
                MouseWheel += ScrollContainer_MouseWheel;
            else
                MouseWheel -= ScrollContainer_MouseWheel;
        }

        private void ScrollContainer_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!_bar.Enabled) return;
            _bar.Value += e.Delta > 0 ? -_bar.SmallChange : _bar.SmallChange;
        }
    }
}

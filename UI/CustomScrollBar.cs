using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NewUI.UI
{
    public enum ScrollOrientationEx { Vertical, Horizontal }

    public class CustomScrollBar : Control
    {
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 0;
        private int _smallChange = 20;
        private int _largeChange = 100;
        private bool _dragging = false;
        private int _dragOffset = 0;

        [Browsable(true)]
        public ScrollOrientationEx Orientation { get; set; } = ScrollOrientationEx.Vertical;

        [Browsable(true)]
        public int Minimum
        {
            get => _minimum;
            set { _minimum = Math.Min(value, _maximum); _value = Math.Max(_value, _minimum); Invalidate(); }
        }

        [Browsable(true)]
        public int Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(value, _minimum); _value = Math.Min(_value, _maximum); Invalidate(); }
        }

        /// <summary>可视窗口大小（用于计算拇指长度），通常=ViewportSize。不能超过(Max-Min)</summary>
        [Browsable(true)]
        public int LargeChange
        {
            get => _largeChange;
            set { _largeChange = Math.Max(1, value); Invalidate(); }
        }

        [Browsable(true)]
        public int SmallChange
        {
            get => _smallChange;
            set { _smallChange = Math.Max(1, value); }
        }

        [Browsable(true)]
        public int Value
        {
            get => _value;
            set
            {
                int nv = Math.Max(_minimum, Math.Min(_maximum, value));
                if (nv != _value)
                {
                    _value = nv;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // 主题色
        [Browsable(true)]
        public Color TrackColor { get; set; } = Color.FromArgb(60, 60, 60);
        [Browsable(true)]
        public Color ThumbColor { get; set; } = Color.FromArgb(0, 150, 255);
        [Browsable(true)]
        public Color ThumbHoverColor { get; set; } = Color.FromArgb(30, 170, 255);
        [Browsable(true)]
        public int Thickness { get; set; } = 8;
        [Browsable(true)]
        public int ThumbMinLength { get; set; } = 32;
        [Browsable(true)]
        public int CornerRadius { get; set; } = 4;

        private bool _hoverThumb = false;

        public event EventHandler ValueChanged;

        public CustomScrollBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            TabStop = false;
            Cursor = Cursors.Hand;
            Width = 10;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int delta = e.Delta > 0 ? -SmallChange : SmallChange;
            Value += delta;
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled) return;

            var thumb = GetThumbRect();
            if (thumb.Contains(e.Location))
            {
                _dragging = true;
                _dragOffset = Orientation == ScrollOrientationEx.Vertical ? e.Y - thumb.Top : e.X - thumb.Left;
                Capture = true;
            }
            else
            {
                // 点击轨道：按页跳
                if (Orientation == ScrollOrientationEx.Vertical)
                    Value += e.Y < thumb.Top ? -LargeChange : LargeChange;
                else
                    Value += e.X < thumb.Left ? -LargeChange : LargeChange;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var thumb = GetThumbRect();
            bool hover = thumb.Contains(e.Location);
            if (hover != _hoverThumb)
            {
                _hoverThumb = hover;
                Invalidate();
            }

            if (_dragging)
            {
                int trackLength = Orientation == ScrollOrientationEx.Vertical ? Height : Width;
                int barLen = Math.Max(ThumbMinLength, GetThumbLength(trackLength));
                int trackUsable = trackLength - barLen;
                if (trackUsable <= 0) return;

                int pos = (Orientation == ScrollOrientationEx.Vertical ? e.Y : e.X) - _dragOffset;
                pos = Math.Max(0, Math.Min(trackUsable, pos));

                double ratio = (Maximum - Minimum) <= 0 ? 0.0 : (double)pos / trackUsable;
                Value = Minimum + (int)Math.Round(ratio * (Maximum - Minimum));
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var trackRect = Orientation == ScrollOrientationEx.Vertical
                ? new Rectangle((Width - Thickness) / 2, 0, Thickness, Height)
                : new Rectangle(0, (Height - Thickness) / 2, Width, Thickness);

            using (var trackBrush = new SolidBrush(Enabled ? TrackColor : Color.FromArgb(40, TrackColor)))
            using (var thumbBrush = new SolidBrush(Enabled ? (_hoverThumb ? ThumbHoverColor : ThumbColor) : Color.FromArgb(80, ThumbColor)))
            using (var trackPath = RoundedRect(trackRect, CornerRadius))
            {
                g.FillPath(trackBrush, trackPath);

                var thumbRect = GetThumbRect();
                using (var thumbPath = RoundedRect(thumbRect, CornerRadius))
                    g.FillPath(thumbBrush, thumbPath);
            }
        }

        private Rectangle GetThumbRect()
        {
            int trackLength = Orientation == ScrollOrientationEx.Vertical ? Height : Width;
            int barLen = Math.Max(ThumbMinLength, GetThumbLength(trackLength));
            int trackUsable = trackLength - barLen;

            int pos = 0;
            if (Maximum > Minimum && trackUsable > 0)
            {
                double ratio = (double)(Value - Minimum) / (Maximum - Minimum);
                pos = (int)Math.Round(ratio * trackUsable);
            }

            if (Orientation == ScrollOrientationEx.Vertical)
            {
                int x = (Width - Thickness) / 2;
                return new Rectangle(x, pos, Thickness, barLen);
            }
            else
            {
                int y = (Height - Thickness) / 2;
                return new Rectangle(pos, y, barLen, Thickness);
            }
        }

        private int GetThumbLength(int trackLength)
        {
            // 按可视区域比例确定拇指长度（LargeChange 越大拇指越长）
            int range = Math.Max(1, Maximum - Minimum);
            double visibleRatio = Math.Max(0.05, Math.Min(1.0, (double)LargeChange / (range + LargeChange)));
            return (int)Math.Round(trackLength * visibleRatio);
        }

        private System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            if (d <= 0) { path.AddRectangle(r); return path; }

            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

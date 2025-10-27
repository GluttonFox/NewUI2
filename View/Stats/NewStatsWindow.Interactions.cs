using System;
using System.Drawing;
using System.Windows.Forms;

namespace NewUI
{
    public partial class NewStatsWindow
    {
        private void SetupEvents()
        {
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

            MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    ShowExitConfirmation();
                }
            };

            DoubleClick += (s, e) => OpenDetailFor(_currentPage);

            ApplyInteractiveHandlers(this);
            ApplyInteractiveHandlers(_contentCurrent);
            ApplyInteractiveHandlers(_contentNext);
        }

        private void ApplyInteractiveHandlers(Control root)
        {
            if (root == null)
            {
                return;
            }

            void bindIfNeeded(Control control)
            {
                if (control == _navBar || control.Parent == _navBar || control is Button)
                {
                    foreach (Control child in control.Controls)
                    {
                        bindIfNeeded(child);
                    }

                    return;
                }

                if (_boundControls.Contains(control))
                {
                    foreach (Control child in control.Controls)
                    {
                        bindIfNeeded(child);
                    }

                    return;
                }

                _boundControls.Add(control);

                control.MouseDown += Control_MouseDown;
                control.MouseMove += Control_MouseMove;
                control.MouseUp += Control_MouseUp;
                control.MouseClick += Control_MouseClick;
                control.DoubleClick += Control_DoubleClick;

                foreach (Control child in control.Controls)
                {
                    bindIfNeeded(child);
                }

                control.ControlAdded += (s, e) => bindIfNeeded(e.Control);
            }

            bindIfNeeded(root);
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var src = sender as Control ?? this;
            var client = PointToClient(src.PointToScreen(e.Location));
            if (!IsInInteractiveArea(client))
            {
                return;
            }

            _pressPointScreen = Control.MousePosition;
            _dragStarted = false;
            Cursor = Cursors.SizeAll;
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if ((Control.MouseButtons & MouseButtons.Left) == 0)
            {
                return;
            }

            if (_dragStarted)
            {
                return;
            }

            var cur = Control.MousePosition;
            int dx = Math.Abs(cur.X - _pressPointScreen.X);
            int dy = Math.Abs(cur.Y - _pressPointScreen.Y);
            if (dx >= DRAG_THRESHOLD || dy >= DRAG_THRESHOLD)
            {
                _dragStarted = true;
                BeginNativeDrag();
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
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var src = sender as Control ?? this;
            var client = PointToClient(src.PointToScreen(e.Location));
            if (IsInInteractiveArea(client))
            {
                ShowExitConfirmation();
            }
        }

        private void Control_DoubleClick(object sender, EventArgs e)
        {
            var client = PointToClient(Control.MousePosition);
            if (!IsInInteractiveArea(client))
            {
                return;
            }

            int now = Environment.TickCount;
            if (now - _lastOpenTick < 300)
            {
                return;
            }

            _lastOpenTick = now;
            OpenDetailFor(_currentPage);
        }

        private void ShowExitConfirmation()
        {
            var result = MessageBox.Show(
                "确定要退出程序吗？",
                "确认退出",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Close();
            }
        }

        private bool IsInInteractiveArea(Point clientPt)
        {
            return clientPt.Y >= (_navBar?.Bottom ?? 0);
        }

        private void BeginNativeDrag()
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
    }
}

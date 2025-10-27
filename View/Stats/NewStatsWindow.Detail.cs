using System;
using System.Drawing;
using System.Windows.Forms;

namespace NewUI
{
    public partial class NewStatsWindow
    {
        private void OpenDetailFor(Page page)
        {
            try
            {
                if (_detailForm != null && !_detailForm.IsDisposed)
                {
                    _detailForm.Activate();
                    return;
                }

                _detailForm = new RoundedDetailForm
                {
                    Text = "详细统计",
                    StartPosition = FormStartPosition.CenterScreen,
                    Size = new Size(920, 600),
                    FormBorderStyle = FormBorderStyle.FixedSingle,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.FromArgb(30, 30, 30),
                    TopMost = true,
                    Owner = this
                };

                var host = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10),
                    BackColor = Color.FromArgb(30, 30, 30)
                };
                var detailControl = new NewStatsDetailControl { Dock = DockStyle.Fill };
                host.Controls.Add(detailControl);
                _detailForm.Controls.Add(host);

                NewStatsDetailControl.DetailView view =
                    page == Page.Revenue ? NewStatsDetailControl.DetailView.Revenue :
                    page == Page.Trading ? NewStatsDetailControl.DetailView.Trading :
                                           NewStatsDetailControl.DetailView.Farming;
                detailControl.SelectView(view);
                detailControl.UpdateAllStats();

                Hide();

                _detailForm.FormClosed += (s, e) =>
                {
                    _detailForm = null;
                    Show();
                    BringToFront();
                    Activate();
                    TopMost = true;
                };

                _detailForm.Show();
                _detailForm.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开详情窗口时出错: " + ex.Message);
            }
        }

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
                    cp.ClassStyle |= 0x00020000;
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
                if (!IsHandleCreated || Width <= 0 || Height <= 0)
                {
                    return;
                }

                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                int r = CornerRadius;
                int d = r * 2;
                path.AddArc(0, 0, d, d, 180, 90);
                path.AddArc(Width - d - 1, 0, d, d, 270, 90);
                path.AddArc(Width - d - 1, Height - d - 1, d, d, 0, 90);
                path.AddArc(0, Height - d - 1, d, d, 90, 90);
                path.CloseFigure();

                Region?.Dispose();
                Region = new Region(path);
            }
        }
    }
}

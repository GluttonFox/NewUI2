using System;
using System.Drawing;
using System.Windows.Forms;
using NewUI.DataSources;

namespace NewUI
{
    public partial class NewStatsWindow
    {
        private void RenderRevenueStats(RevenueStatsData data)
        {
            if (data == null || _revenueLine1Label == null)
            {
                return;
            }

            UpdateLabel(_revenueLine1Label,
                $"在线时间: {FormatTimeSpan(data.OnlineTime)} | 刷图时间: {FormatTimeSpan(data.ActiveTime)}");
            UpdateLabel(_revenueLine2Label,
                $"本轮掉落: {data.RoundDrop:F2} 火 | 本轮利润: {data.RoundProfit:F2} 火");
            UpdateLabel(_revenueLine3Label,
                $"累计收益: {data.TotalIncome:F2} 火 | 累计时间: {FormatTimeSpan(data.CumulativeTime)}");
            UpdateLabel(_revenueLine4Label,
                $"时均收益: {data.AveragePerHour:F2} 火 | 轮均收益: {data.AveragePerRound:F2} 火");

            RenderRevenueLine2(data.RoundDrop, data.RoundProfit);
            RenderRevenueLine4(data.AveragePerHour, data.AveragePerRound);

            UpdateLabel(_revenueMaxLabel, "本轮最高收益");
            UpdateLabel(_revenueMinLabel, "本轮最低收益");

            if (data.HasItemExtremes && data.MaxItem != null && data.MinItem != null)
            {
                UpdateLabel(_revenueMaxItemLabel,
                    $"{data.MaxItem.ItemName} X {data.MaxItem.Count} | {data.MaxItem.UnitValue:F2} 火 | {data.MaxItem.TotalValue:F2} 火");
                UpdateLabel(_revenueMinItemLabel,
                    $"{data.MinItem.ItemName} X {data.MinItem.Count} | {data.MinItem.UnitValue:F2} 火 | {data.MinItem.TotalValue:F2} 火");
            }
            else
            {
                UpdateLabel(_revenueMaxItemLabel, "—");
                UpdateLabel(_revenueMinItemLabel, "—");
            }
        }

        private void RenderRevenueLine2(double roundDrop, double netProfit)
        {
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
            RenderSegments(_revenueLine4Label, new (string, Color?)[]
            {
                ("时均收益: ", null),
                (hourlyProfit.ToString("0.00"), ColorForProfit(hourlyProfit)),
                (" 火 | 轮均收益: ", null),
                (roundProfit.ToString("0.00"), ColorForProfit(roundProfit)),
                (" 火", null),
            });
        }

        private Color ColorForProfit(double value)
        {
            if (value < 0) return Color.Red;
            if (value == 0) return Color.Gray;
            if (value > 0 && value < 1000) return Color.LimeGreen;
            if (value >= 1000 && value < 2000) return Color.Orange;
            return Color.Gold;
        }

        private void RenderSegments(Label holder, (string text, Color? color)[] segments)
        {
            if (holder == null || holder.Parent == null)
            {
                return;
            }

            if (holder.Tag is Panel panel && panel.Parent == holder.Parent)
            {
                RebuildOrUpdateSegmentLabels(panel, segments, holder.Font, holder.BackColor, holder.ForeColor);
                panel.Visible = true;
                panel.BringToFront();
                holder.Visible = false;
                return;
            }

            var container = new Panel
            {
                Location = holder.Location,
                Size = holder.Size,
                BackColor = holder.BackColor,
                Anchor = holder.Anchor
            };

            RebuildOrUpdateSegmentLabels(container, segments, holder.Font, holder.BackColor, holder.ForeColor);

            holder.Parent.Controls.Add(container);
            container.BringToFront();
            holder.Visible = false;
            holder.Tag = container;
        }

        private void RebuildOrUpdateSegmentLabels(Panel panel, (string text, Color? color)[] segments, Font font, Color backColor, Color defaultColor)
        {
            panel.SuspendLayout();
            panel.Controls.Clear();

            int x = 0;
            foreach (var (text, color) in segments)
            {
                var label = new Label
                {
                    AutoSize = true,
                    Text = text,
                    Font = font,
                    ForeColor = color ?? defaultColor,
                    BackColor = backColor,
                    Location = new Point(x, (panel.Height - font.Height) / 2)
                };

                panel.Controls.Add(label);
                label.PerformLayout();
                x = label.Right;
            }

            panel.ResumeLayout(true);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }

            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }
}

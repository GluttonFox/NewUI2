using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NewUI.DataSources;

namespace NewUI
{
    public partial class NewStatsWindow
    {
        /// <summary>
        /// 使用最新的数据快照刷新所有可见视图。
        /// </summary>
        /// <param name="snapshot">来自数据源的统计数据快照。</param>
        public void Render(StatsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            ExecuteSafely(() => RenderFarmingStats(snapshot.Farming));
            ExecuteSafely(() => RenderRevenueStats(snapshot.Revenue));
            ExecuteSafely(() => RenderTradingStats(snapshot.Trading));
        }

        private void ExecuteSafely(Action updateAction)
        {
            if (updateAction == null)
            {
                return;
            }

            try
            {
                updateAction();
            }
            catch
            {
                // 静默处理，避免阻断 UI 更新。
            }
        }

        private void UpdateLabel(Label label, string text)
        {
            if (label == null)
            {
                return;
            }

            label.Text = text ?? string.Empty;
        }

        private void UpdateLabelList(IReadOnlyList<Label> labels, IReadOnlyList<string> values)
        {
            if (labels == null)
            {
                return;
            }

            int count = values?.Count ?? 0;
            for (int i = 0; i < labels.Count; i++)
            {
                var value = i < count ? values[i] : string.Empty;
                UpdateLabel(labels[i], value);
            }
        }
    }
}

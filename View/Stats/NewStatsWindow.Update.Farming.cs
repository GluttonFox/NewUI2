using System;
using System.Windows.Forms;
using NewUI.DataSources;

namespace NewUI
{
    public partial class NewStatsWindow
    {
        private void RenderFarmingStats(FarmingStatsData data)
        {
            if (data == null)
            {
                return;
            }

            EnsureFarmingLabelsInitialized();

            UpdateLabel(_farmingTotalTimeLabel,
                $"在线时间: {FormatTimeSpan(data.OnlineTime)} | 总计轮次: {data.TotalRounds} | 总计成本: {data.TotalCost:F2} 火");
            UpdateLabel(_farmingRoundTimeLabel,
                $"刷图时间: {FormatTimeSpan(data.ActiveTime)} | 当前轮次: {data.CurrentRoundNumber} | 本轮成本: {data.CurrentRoundCost:F2} 火");
            UpdateLabelList(_farmingItemLabels, data.CostLines ?? Array.Empty<string>());
        }

        private void EnsureFarmingLabelsInitialized()
        {
            if (_farmingTotalTimeLabel != null && _farmingItemLabels != null)
            {
                return;
            }

            var temp = new Panel { Size = _contentCurrent.Size, BackColor = _cardBackground };
            BuildPage(Page.Farming, temp);
        }
    }
}

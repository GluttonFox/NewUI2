using NewUI.DataSources;

namespace NewUI
{
    public partial class NewStatsWindow
    {
        private void RenderTradingStats(TradingStatsData data)
        {
            if (_tradingBuyLabel == null || _tradingSellLabel == null || _tradingNetLabel == null)
            {
                return;
            }

            if (data == null || !data.HasData)
            {
                UpdateLabel(_tradingBuyLabel, "购买商品: —");
                UpdateLabel(_tradingSellLabel, "出售商品: —");
                UpdateLabel(_tradingNetLabel, "净利润: —");
                return;
            }

            UpdateLabel(_tradingBuyLabel, $"购买商品: {data.TotalBuyValue:F2} 火");
            UpdateLabel(_tradingSellLabel, $"出售商品: {data.TotalSellValue:F2} 火");
            UpdateLabel(_tradingNetLabel, $"净利润: {data.NetProfit:F2} 火");
        }
    }
}

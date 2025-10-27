using System;
using System.Collections.Generic;

namespace NewUI.DataSources
{
    /// <summary>
    /// 封装刷图、收益与交易的统计数据快照。
    /// </summary>
    public sealed class StatsSnapshot
    {
        public StatsSnapshot(FarmingStatsData? farming, RevenueStatsData? revenue, TradingStatsData? trading)
        {
            Farming = farming ?? new FarmingStatsData();
            Revenue = revenue ?? new RevenueStatsData();
            Trading = trading ?? new TradingStatsData();
        }

        public FarmingStatsData Farming { get; }

        public RevenueStatsData Revenue { get; }

        public TradingStatsData Trading { get; }
    }

    /// <summary>
    /// 刷图相关的统计信息。
    /// </summary>
    public sealed class FarmingStatsData
    {
        public TimeSpan OnlineTime { get; init; }

        public TimeSpan ActiveTime { get; init; }

        public int TotalRounds { get; init; }

        public int CurrentRoundNumber { get; init; }

        public double TotalCost { get; init; }

        public double CurrentRoundCost { get; init; }

        public IReadOnlyList<string> CostLines { get; init; } = Array.Empty<string>();

        public bool HasData { get; init; }
    }

    /// <summary>
    /// 收益相关的统计信息。
    /// </summary>
    public sealed class RevenueStatsData
    {
        public TimeSpan OnlineTime { get; init; }

        public TimeSpan ActiveTime { get; init; }

        public TimeSpan CumulativeTime { get; init; }

        public double RoundDrop { get; init; }

        public double RoundCost { get; init; }

        public double RoundProfit { get; init; }

        public double TotalIncome { get; init; }

        public double AveragePerHour { get; init; }

        public double AveragePerRound { get; init; }

        public int TotalRounds { get; init; }

        public RevenueItemDetail? MaxItem { get; init; }

        public RevenueItemDetail? MinItem { get; init; }

        public bool HasItemExtremes { get; init; }
    }

    /// <summary>
    /// 表示单个收益物品的详情。
    /// </summary>
    public sealed class RevenueItemDetail
    {
        public string ItemName { get; init; } = string.Empty;

        public int Count { get; init; }

        public double TotalValue { get; init; }

        public double UnitValue { get; init; }
    }

    /// <summary>
    /// 交易统计信息。
    /// </summary>
    public sealed class TradingStatsData
    {
        public double TotalBuyValue { get; init; }

        public double TotalSellValue { get; init; }

        public double NetProfit { get; init; }

        public bool HasData { get; init; }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NewUI.Managers;

namespace NewUI.DataSources
{
    /// <summary>
    /// 使用 <see cref="ServiceLocator"/> 提供的管理器生成统计数据快照。
    /// </summary>
    public sealed class ServiceLocatorStatsDataSource : IStatsDataSource
    {
        private readonly ServiceLocator _serviceLocator;

        public ServiceLocatorStatsDataSource(ServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
        }

        public StatsSnapshot GetSnapshot()
        {
            var farming = BuildFarmingStats();
            var revenue = BuildRevenueStats();
            var trading = BuildTradingStats();
            return new StatsSnapshot(farming, revenue, trading);
        }

        private FarmingStatsData BuildFarmingStats()
        {
            try
            {
                if (!_serviceLocator.TryGet(out FarmingCostManager? costManager))
                {
                    return new FarmingStatsData();
                }

                _serviceLocator.TryGet(out PriceManager? priceManager);

                var online = SafeGetOnlineTime();
                var rounds = costManager.GetAllFarmingRounds();
                var currentRound = rounds.Count > 0 ? rounds[^1] : null;
                var usage = costManager.GetCurrentRoundItems();
                var costLines = BuildCostLines(priceManager, usage, out var currentRoundCost);

                return new FarmingStatsData
                {
                    OnlineTime = online,
                    ActiveTime = currentRound?.Duration ?? TimeSpan.Zero,
                    TotalRounds = costManager.GetTotalRounds(),
                    CurrentRoundNumber = currentRound?.RoundNumber ?? 0,
                    TotalCost = costManager.GetTotalCost(),
                    CurrentRoundCost = currentRoundCost,
                    CostLines = costLines,
                    HasData = rounds.Count > 0
                };
            }
            catch
            {
                return new FarmingStatsData();
            }
        }

        private RevenueStatsData BuildRevenueStats()
        {
            try
            {
                _serviceLocator.TryGet(out CurrentDropManager? dropManager);
                _serviceLocator.TryGet(out FarmingCostManager? costManager);
                _serviceLocator.TryGet(out PriceManager? priceManager);

                var summary = dropManager?.GetCurrentDropSummary();
                var online = SafeGetOnlineTime();
                var active = summary?.ActiveTime ?? TimeSpan.Zero;
                var cumulative = online - active;
                if (cumulative < TimeSpan.Zero)
                {
                    cumulative = TimeSpan.Zero;
                }

                double roundDrop = summary?.DropItems?.Sum(item => item.TotalValue) ?? 0.0;
                double roundCost = CalculateCurrentRoundCost(costManager, priceManager);
                double roundProfit = roundDrop - roundCost;

                double totalIncome = summary?.TotalValue ?? 0.0;
                int totalRounds = costManager?.GetTotalRounds() ?? 0;
                double avgPerRound = totalRounds > 0 ? totalIncome / totalRounds : 0.0;

                double hours = cumulative.TotalHours;
                hours = hours <= 0 ? 0.0 : hours;
                hours = Math.Max(1.0, hours);
                double avgPerHour = hours > 0 ? totalIncome / hours : 0.0;

                var extremes = EvaluateRoundExtremes(summary);

                return new RevenueStatsData
                {
                    OnlineTime = online,
                    ActiveTime = active,
                    CumulativeTime = cumulative,
                    RoundDrop = roundDrop,
                    RoundCost = roundCost,
                    RoundProfit = roundProfit,
                    TotalIncome = totalIncome,
                    AveragePerHour = avgPerHour,
                    AveragePerRound = avgPerRound,
                    TotalRounds = totalRounds,
                    MaxItem = extremes.maxItem,
                    MinItem = extremes.minItem,
                    HasItemExtremes = extremes.hasItems
                };
            }
            catch
            {
                return new RevenueStatsData();
            }
        }

        private TradingStatsData BuildTradingStats()
        {
            try
            {
                if (!_serviceLocator.TryGet(out TradingManager? tradingManager))
                {
                    return new TradingStatsData();
                }

                var summary = tradingManager.GetTradingSummary();
                if (summary == null)
                {
                    return new TradingStatsData();
                }

                return new TradingStatsData
                {
                    TotalBuyValue = summary.TotalBuyConsumeValue,
                    TotalSellValue = summary.TotalReceiveValue,
                    NetProfit = summary.NetTradingProfit,
                    HasData = true
                };
            }
            catch
            {
                return new TradingStatsData();
            }
        }

        private static IReadOnlyList<string> BuildCostLines(PriceManager? priceManager, IDictionary<int, int>? usage, out double totalCost)
        {
            var lines = new List<string>();
            totalCost = PopulateCostDetails(priceManager, usage, lines);
            return lines;
        }

        private static double CalculateCurrentRoundCost(FarmingCostManager? costManager, PriceManager? priceManager)
        {
            var usage = costManager?.GetCurrentRoundItems();
            return PopulateCostDetails(priceManager, usage, null);
        }

        private static double PopulateCostDetails(PriceManager? priceManager, IDictionary<int, int>? usage, List<string>? lines)
        {
            double total = 0.0;
            if (usage == null || priceManager == null)
            {
                return total;
            }

            foreach (var (itemId, count) in usage)
            {
                var unitPrice = priceManager.GetItemUnitPriceWithoutTax(itemId);
                total += unitPrice * count;

                if (lines != null)
                {
                    var itemName = priceManager.GetItemName(itemId);
                    var lineTotal = unitPrice * count;
                    lines.Add(FormatCostLine(itemName, count, unitPrice, lineTotal));
                }
            }

            return total;
        }

        private static string FormatCostLine(string name, int quantity, double unit, double total)
        {
            return $"{name} X {quantity} | {unit:0.00} 火 | {total:0.00} 火";
        }

        private static (RevenueItemDetail? maxItem, RevenueItemDetail? minItem, bool hasItems) EvaluateRoundExtremes(CurrentDropSummary? summary)
        {
            if (summary?.DropItems == null || summary.DropItems.Count == 0)
            {
                return (null, null, false);
            }

            var maxItem = summary.DropItems.OrderByDescending(x => x.TotalValue).First();
            var minItem = summary.DropItems.OrderBy(x => x.TotalValue).First();

            return (
                new RevenueItemDetail
                {
                    ItemName = maxItem.ItemName,
                    Count = maxItem.Count,
                    TotalValue = maxItem.TotalValue,
                    UnitValue = maxItem.Count > 0 ? maxItem.TotalValue / maxItem.Count : maxItem.TotalValue
                },
                new RevenueItemDetail
                {
                    ItemName = minItem.ItemName,
                    Count = minItem.Count,
                    TotalValue = minItem.TotalValue,
                    UnitValue = minItem.Count > 0 ? minItem.TotalValue / minItem.Count : minItem.TotalValue
                },
                true);
        }

        private static TimeSpan SafeGetOnlineTime()
        {
            try
            {
                return DateTime.Now - Process.GetCurrentProcess().StartTime;
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }
    }
}

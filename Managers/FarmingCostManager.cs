using System;
using System.Collections.Generic;
using System.Linq;

namespace NewUI.Managers
{
    public class FarmingRound
    {
        public int RoundNumber { get; set; }
        public string RunType { get; set; } = "普通";
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(3);
        public Dictionary<int, int> ItemUsage { get; set; } = new();
    }

    public class FarmingSummaryItem
    {
        public string ItemName { get; set; } = "";
        public double TotalValue { get; set; }   // 这里先用“数量”占位
        public int RunCount { get; set; }
    }

    public class FarmingCostManager
    {
        private readonly List<FarmingRound> _rounds = new();

        public FarmingCostManager()
        {
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 1,
                RunType = "普通",
                Duration = TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(30)),
                ItemUsage = new() { { 1001, 2 }, { 1002, 1 }, { 1003, 1 } } // + 罗盘2
            });
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 2,
                RunType = "加成",
                Duration = TimeSpan.FromMinutes(3),
                ItemUsage = new() { { 1001, 3 }, { 1002, 1 }, { 1004, 2 } } // + 罗盘3
            });
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 3,
                RunType = "加成",
                Duration = TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(45)),
                ItemUsage = new() { { 1001, 2 }, { 1005, 1 } } // + 罗盘4
            });
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 4,
                RunType = "加成",
                Duration = TimeSpan.FromMinutes(3),
                ItemUsage = new() { { 1001, 3 }, { 1002, 1 }, { 1004, 2 } } // + 罗盘3
            });
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 5,
                RunType = "加成",
                Duration = TimeSpan.FromMinutes(3),
                ItemUsage = new() { { 1001, 3 }, { 1002, 1 }, { 1004, 2 } } // + 罗盘3
            });
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 6,
                RunType = "加成",
                Duration = TimeSpan.FromMinutes(3),
                ItemUsage = new() { { 1001, 3 }, { 1002, 1 }, { 1004, 2 } } // + 罗盘3
            });
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 7,
                RunType = "加成",
                Duration = TimeSpan.FromMinutes(3),
                ItemUsage = new() { { 1001, 3 }, { 1002, 1 }, { 1004, 2 } } // + 罗盘3
            });
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 8,
                RunType = "加成",
                Duration = TimeSpan.FromMinutes(3),
                ItemUsage = new() { { 1001, 3 }, { 1002, 1 }, { 1004, 2 } } // + 罗盘3
            });
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 9,
                RunType = "加成",
                Duration = TimeSpan.FromMinutes(3),
                ItemUsage = new() { { 1001, 3 }, { 1002, 1 }, { 1004, 2 } } // + 罗盘3
            });
            _rounds.Add(new FarmingRound
            {
                RoundNumber = 10,
                RunType = "加成",
                Duration = TimeSpan.FromMinutes(3),
                ItemUsage = new() { { 1001, 3 }, { 1002, 1 }, { 1004, 2 } } // + 罗盘3
            });

        }

        public List<FarmingRound> GetAllFarmingRounds() => new List<FarmingRound>(_rounds);

        public List<FarmingSummaryItem> GetFarmingSummary()
        {
            var price = ServiceLocator.Instance.Get<PriceManager>();
            var dict = new Dictionary<string, double>();

            foreach (var r in _rounds)
            {
                foreach (var kv in r.ItemUsage)
                {
                    var name = price.GetItemName(kv.Key);   // 例如：罗盘2/罗盘3/罗盘4
                    if (!dict.ContainsKey(name)) dict[name] = 0;
                    dict[name] += kv.Value;                 // 这里 TotalValue 仍用“数量”占位
                }
            }

            int runCount = _rounds.Count;
            return dict.Select(kv => new FarmingSummaryItem
            {
                ItemName = kv.Key,
                TotalValue = kv.Value,
                RunCount = runCount
            })
            .OrderByDescending(x => x.TotalValue)
            .ToList();
        }




        public Dictionary<int, int> GetCurrentRoundItems()
        {
            return _rounds.LastOrDefault()?.ItemUsage ?? new Dictionary<int, int>();
        }

        public int GetTotalRounds() => _rounds.Count;

        public double GetTotalCost()
        {
            var price = ServiceLocator.Instance.Get<NewUI.Managers.PriceManager>();
            double total = 0;
            foreach (var r in _rounds)
                foreach (var kv in r.ItemUsage)
                    total += price.GetItemUnitPriceWithoutTax(kv.Key) * kv.Value; // 用现有API
            return total;
        }


    }
}

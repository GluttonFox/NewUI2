using System.Collections.Generic;
using System.Linq;

namespace NewUI.Managers
{
    public class PriceInfo
    {
        public int ItemBaseId { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "材料";
        public double Price { get; set; }
        public long LastTime { get; set; }  // Unix 秒
    }

    public class PriceDataSummary
    {
        public int TotalItems { get; set; }
        public double AveragePrice { get; set; }
        public double MaxPrice { get; set; }
        public double MinPrice { get; set; }
        public long LastUpdateTime { get; set; }
    }

    public class PriceManager
    {
        private readonly Dictionary<int, PriceInfo> _prices = new();

        public PriceManager()
        {
            var now = System.DateTimeOffset.Now.ToUnixTimeSeconds();
            var seed = new[]
            {
                new PriceInfo{ ItemBaseId=1001, Name="探针", Type="消耗品", Price=3.500, LastTime=now },
                new PriceInfo{ ItemBaseId=1002, Name="罗盘", Type="消耗品", Price=6.750, LastTime=now },
                new PriceInfo{ ItemBaseId=2001, Name="蓝色回响", Type="回响", Price=12.300, LastTime=now },
                new PriceInfo{ ItemBaseId=2002, Name="紫色回响", Type="回响", Price=28.900, LastTime=now },
                new PriceInfo{ ItemBaseId=2003, Name="金色回响", Type="回响", Price=68.000, LastTime=now },
                new PriceInfo{ ItemBaseId=1003, Name="罗盘2", Type="消耗品", Price=3.000, LastTime=now },
new PriceInfo{ ItemBaseId=1004, Name="罗盘3", Type="消耗品", Price=4.000, LastTime=now },
new PriceInfo{ ItemBaseId=1005, Name="饰品之武装罗盘", Type="消耗品", Price=5.000, LastTime=now },
new PriceInfo{ ItemBaseId=1006, Name="罗盘8", Type="消耗品", Price=3.000, LastTime=now },
new PriceInfo{ ItemBaseId=1007, Name="罗盘7", Type="消耗品", Price=4.000, LastTime=now },
new PriceInfo{ ItemBaseId=1008, Name="罗盘6", Type="消耗品", Price=5.000, LastTime=now },

            };
            foreach (var p in seed) _prices[p.ItemBaseId] = p;
        }

        public PriceDataSummary GetPriceDataSummary()
        {
            var list = _prices.Values.ToList();
            return new PriceDataSummary
            {
                TotalItems = list.Count,
                AveragePrice = list.Count == 0 ? 0 : list.Average(x => x.Price),
                MaxPrice = list.Count == 0 ? 0 : list.Max(x => x.Price),
                MinPrice = list.Count == 0 ? 0 : list.Min(x => x.Price),
                LastUpdateTime = list.Count == 0 ? 0 : list.Max(x => x.LastTime)
            };
        }

        public List<PriceInfo> GetAllPriceData() => _prices.Values.OrderByDescending(x => x.Price).ToList();

        public PriceInfo? GetItemPriceInfo(int itemBaseId) =>
            _prices.TryGetValue(itemBaseId, out var info) ? info : null;

        public string GetItemName(int itemBaseId) =>
            GetItemPriceInfo(itemBaseId)?.Name ?? $"物品#{itemBaseId}";

        public double GetItemUnitPriceWithoutTax(int itemBaseId) =>
            GetItemPriceInfo(itemBaseId)?.Price ?? 0.0;
    }
}

using System;
using System.Collections.Generic;

namespace NewUI.Managers
{
    /// <summary>
    /// 当前掉落摘要模型（DTO）
    /// </summary>
    public class CurrentDropSummary
    {
        public string SceneName { get; set; }
        public IReadOnlyList<CurrentDropInfo> DropItems { get; set; } = new List<CurrentDropInfo>();
        public IReadOnlyList<CurrentCostInfo> CostItems { get; set; } = new List<CurrentCostInfo>();
        public double TotalValue { get; set; }
        public double TotalCost { get; set; }
        public double NetProfit { get; set; }
        public int TotalItems { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public TimeSpan TotalTime { get; set; }
        public TimeSpan ActiveTime { get; set; }
        public TimeSpan SafeZoneTime { get; set; }
        public bool IsInSafeZone { get; set; }
    }

    public class CurrentDropInfo
    {
        public string ItemName { get; set; } = "";
        public int Count { get; set; }
        public double TotalValue { get; set; }
    }

    public class CurrentCostInfo
    {
        public string ItemName { get; set; } = "";
        public int Count { get; set; }
        public double TotalValue { get; set; }
    }
    /// <summary>
    /// 掉落管理器，用于提供和维护当前掉落数据
    /// </summary>
    public class CurrentDropManager
    {
        private readonly object _lock = new object();

        // 实际存放当前掉落统计信息的内部字段
        private CurrentDropSummary _currentDropSummary = new CurrentDropSummary();

        /// <summary>
        /// 获取当前掉落摘要（返回快照）
        /// </summary>
        public CurrentDropSummary GetCurrentDropSummary()
        {
            lock (_lock)
            {
                return new CurrentDropSummary
                {
                    SceneName = _currentDropSummary.SceneName,
                    DropItems = new List<CurrentDropInfo>(_currentDropSummary.DropItems),
                    CostItems = new List<CurrentCostInfo>(_currentDropSummary.CostItems),
                    TotalValue = _currentDropSummary.TotalValue,
                    TotalCost = _currentDropSummary.TotalCost,
                    NetProfit = _currentDropSummary.NetProfit,
                    TotalItems = _currentDropSummary.TotalItems,
                    SessionStartTime = _currentDropSummary.SessionStartTime,
                    LastUpdateTime = _currentDropSummary.LastUpdateTime,
                    TotalTime = _currentDropSummary.TotalTime,
                    ActiveTime = _currentDropSummary.ActiveTime,
                    SafeZoneTime = _currentDropSummary.SafeZoneTime,
                    IsInSafeZone = _currentDropSummary.IsInSafeZone
                };
            }
        }

        // 放在 CurrentDropManager 类里（_rounds 定义的后面）
        public List<DropRound> GetAllDropRounds()
        {
            return new List<DropRound>(_rounds);
        }

        // 1) 轮次明细模型
        public class DropRound
        {
            public int RoundNumber { get; set; }
            public string SceneName { get; set; } = "荒原-1";
            public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(2);
            public List<DropItem> DropItems { get; set; } = new();
        }
        public class DropItem
        {
            public int ItemBaseId { get; set; }
            public string ItemName { get; set; } = "";
            public int Quantity { get; set; }
            public double TotalValue { get; set; }
        }

        // 2) 在 CurrentDropManager 类里加上测试数据与新方法：
        private readonly List<DropRound> _rounds = new List<DropRound>
{
    new DropRound{
        RoundNumber=1, SceneName="荒原-1", Duration=TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(10)),
        DropItems = { new DropItem{ ItemBaseId=2001, ItemName="蓝色回响", Quantity=1, TotalValue=0 } }
    },
    new DropRound{
        RoundNumber=2, SceneName="荒原-2", Duration=TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(40)),
        DropItems = { new DropItem{ ItemBaseId=2002, ItemName="紫色回响", Quantity=1, TotalValue=1 } }
    },
    new DropRound{
        RoundNumber=3, SceneName="荒原-3", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=-1 } }
    },
    new DropRound{
        RoundNumber=4, SceneName="荒原-4", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=99 } }
    },
    new DropRound{
        RoundNumber=5, SceneName="荒原-5", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=101 } }
    },
    new DropRound{
        RoundNumber=6, SceneName="荒原-5", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=499 } }
    },
    new DropRound{
        RoundNumber=7, SceneName="荒原-5", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=501 } }
    },
    new DropRound{
        RoundNumber=8, SceneName="荒原-5", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=999 } }
    },
    new DropRound{
        RoundNumber=9, SceneName="荒原-5", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=101 } }
    },
    new DropRound{
        RoundNumber=10, SceneName="荒原-5", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=4999 } }
    },
    new DropRound{
        RoundNumber=11, SceneName="荒原-5", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=5001 } }
    },
    new DropRound{
        RoundNumber=12, SceneName="荒原-5", Duration=TimeSpan.FromMinutes(3),
        DropItems = { new DropItem{ ItemBaseId=2003, ItemName="金色回响", Quantity=1, TotalValue=1999 } }
    },
    new DropRound{
        RoundNumber=13, SceneName="荒原-5", Duration=TimeSpan.FromMinutes(3),
        DropItems =
    {
        new DropItem { ItemBaseId = 2001, ItemName = "赤焰晶核", Quantity = 1, TotalValue = 105 },
        new DropItem { ItemBaseId = 2002, ItemName = "黑曜碎片", Quantity = 2, TotalValue = 210 },
        new DropItem { ItemBaseId = 2003, ItemName = "金色回响", Quantity = 3, TotalValue = 180 },
        new DropItem { ItemBaseId = 2004, ItemName = "湛蓝结晶", Quantity = 4, TotalValue = 270 },
        new DropItem { ItemBaseId = 2005, ItemName = "秘银齿轮", Quantity = 5, TotalValue = 90 },
        new DropItem { ItemBaseId = 2006, ItemName = "炽炎碎片", Quantity = 6, TotalValue = 210 },
        new DropItem { ItemBaseId = 2007, ItemName = "虚空残响", Quantity = 7, TotalValue = 130 },
        new DropItem { ItemBaseId = 2008, ItemName = "古代符文石", Quantity = 8, TotalValue = 115 },
        new DropItem { ItemBaseId = 2009, ItemName = "星辉尘晶", Quantity = 9, TotalValue = 240 },
        new DropItem { ItemBaseId = 2010, ItemName = "梦魇之核", Quantity = 10, TotalValue = 155 },
        new DropItem { ItemBaseId = 2011, ItemName = "灵息碎片", Quantity = 11, TotalValue = 220 },
        new DropItem { ItemBaseId = 2012, ItemName = "深红罗盘", Quantity = 12, TotalValue = 195 },
        new DropItem { ItemBaseId = 2013, ItemName = "湮灭晶石", Quantity = 13, TotalValue = 160 },
        new DropItem { ItemBaseId = 2014, ItemName = "虚光碎晶", Quantity = 14, TotalValue = 210 },
        new DropItem { ItemBaseId = 2015, ItemName = "时空碎片", Quantity = 15, TotalValue = 100 },
        new DropItem { ItemBaseId = 2016, ItemName = "烈焰结晶", Quantity = 16, TotalValue = 150 },
        new DropItem { ItemBaseId = 2017, ItemName = "极光粉末", Quantity = 17, TotalValue = 270 },
        new DropItem { ItemBaseId = 2018, ItemName = "梦核碎尘", Quantity = 18, TotalValue = 200 },
        new DropItem { ItemBaseId = 2019, ItemName = "苍穹之泪", Quantity = 19, TotalValue = 190 },
        new DropItem { ItemBaseId = 2020, ItemName = "冰晶碎片", Quantity = 20, TotalValue = 210 },
        new DropItem { ItemBaseId = 2021, ItemName = "灵火结晶", Quantity = 21, TotalValue = 115 },
        new DropItem { ItemBaseId = 2022, ItemName = "空灵石英", Quantity = 22, TotalValue = 205 },
        new DropItem { ItemBaseId = 2023, ItemName = "湛蓝罗盘", Quantity = 23, TotalValue = 165 },
        new DropItem { ItemBaseId = 2024, ItemName = "赤红罗盘", Quantity = 24, TotalValue = 145 },
        new DropItem { ItemBaseId = 2025, ItemName = "秘银残片", Quantity = 25, TotalValue = 255 },
        new DropItem { ItemBaseId = 2026, ItemName = "古代残页", Quantity = 26, TotalValue = 120 },
        new DropItem { ItemBaseId = 2027, ItemName = "暗影尘屑", Quantity = 27, TotalValue = 190 },
        new DropItem { ItemBaseId = 2028, ItemName = "辉耀碎晶", Quantity = 28, TotalValue = 170 },
        new DropItem { ItemBaseId = 2029, ItemName = "余烬结晶", Quantity = 29, TotalValue = 200 },
        new DropItem { ItemBaseId = 2030, ItemName = "晨星碎片", Quantity = 30, TotalValue = 175 }
    }

    }
};
    }
}

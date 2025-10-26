using System;
using System.Collections.Generic;
using System.Linq;

namespace NewUI.Managers
{
    public class TradingRecordLine
    {
        public int ItemBaseId { get; set; }
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
    }

    public class TradingRecord
    {
        public string SaleId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public List<TradingRecordLine> BuyRecords { get; set; } = new();
        public List<TradingRecordLine> ReceiveRecords { get; set; } = new();
    }

    public class TradingSummary
    {
        public double TotalBuyConsumeValue { get; set; }
        public double TotalReceiveValue { get; set; }
        public double NetTradingProfit { get; set; }
        public List<TradingRecord> TradingRecords { get; set; } = new();
    }

    public class TradingManager
    {
        private readonly List<TradingRecord> _records = new();

        public TradingManager()
        {
            _records.Add(new TradingRecord
            {
                CreateTime = DateTime.Now.AddMinutes(-15),
                BuyRecords = { new TradingRecordLine { ItemBaseId = 1001, ItemName = "探针", Quantity = 2 } },
                ReceiveRecords = { new TradingRecordLine { ItemBaseId = 2002, ItemName = "紫色回响", Quantity = 1 } }
            });
            _records.Add(new TradingRecord
            {
                CreateTime = DateTime.Now.AddMinutes(-5),
                BuyRecords = { new TradingRecordLine { ItemBaseId = 1002, ItemName = "罗盘", Quantity = 1 } },
                ReceiveRecords = { new TradingRecordLine { ItemBaseId = 2003, ItemName = "金色回响", Quantity = 1 } }
            });
        }

        public TradingSummary GetTradingSummary()
        {
            var price = ServiceLocator.Instance.Get<PriceManager>();

            double buy = 0, income = 0;
            foreach (var r in _records)
            {
                foreach (var b in r.BuyRecords)
                    buy += (price.GetItemPriceInfo(b.ItemBaseId)?.Price ?? 0) * Math.Abs(b.Quantity);

                foreach (var g in r.ReceiveRecords)
                    income += (price.GetItemPriceInfo(g.ItemBaseId)?.Price ?? 0) * g.Quantity;
            }

            return new TradingSummary
            {
                TotalBuyConsumeValue = buy,
                TotalReceiveValue = income,
                NetTradingProfit = income - buy,
                TradingRecords = _records.OrderByDescending(x => x.CreateTime).ToList()
            };
        }
    }
}

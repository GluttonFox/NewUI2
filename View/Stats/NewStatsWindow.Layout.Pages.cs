using System.Drawing;
using System.Windows.Forms;

namespace NewUI
{
    public partial class NewStatsWindow
    {
        private void BuildPage(Page page, Panel host)
        {
            if (host == null)
            {
                return;
            }

            host.Controls.Clear();

            var statsContainer = CreateStatsContainer();
            host.Controls.Add(statsContainer);

            switch (page)
            {
                case Page.Farming:
                    BuildFarmingPage(statsContainer);
                    break;
                case Page.Revenue:
                    BuildRevenuePage(statsContainer);
                    break;
                case Page.Trading:
                    BuildTradingPage(statsContainer);
                    break;
            }

            host.Controls.Add(CreateHintLabel());
            ApplyInteractiveHandlers(host);
        }

        private Panel CreateStatsContainer()
        {
            return new Panel
            {
                Location = new Point(20, 0),
                Size = new Size(Width - 40, 140),
                BackColor = _cardBackground
            };
        }

        private Label CreateHintLabel()
        {
            return new Label
            {
                Text = "拖拽移动 | 双击查看详情 | 右键退出",
                Location = new Point(20, 150),
                Size = new Size(Width - 40, 20),
                Font = new Font("Microsoft YaHei", 8f),
                ForeColor = _secondaryTextColor,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = _cardBackground
            };
        }

        private void BuildFarmingPage(Panel container)
        {
            _farmingTotalTimeLabel = CreateRow(container,
                "在线时间: 0:00:00 | 总计轮次: 0 | 总计成本: 0.00 火",
                y: 0);

            _farmingRoundTimeLabel = CreateRow(container,
                "刷图时间: 0:00:00 | 当前轮次: 0 | 本轮成本: 0.00 火",
                y: 15);

            _farmingItemLabels = new System.Collections.Generic.List<Label>();
            for (int i = 0; i < 9; i++)
            {
                var itemLabel = CreateRow(container, string.Empty, y: 35 + i * 15, height: 15, fontSize: 8.5f);
                _farmingItemLabels.Add(itemLabel);
            }
        }

        private void BuildRevenuePage(Panel container)
        {
            _revenueLine1Label = CreateRow(container,
                "在线时间: 0:00:00 | 刷图时间: 0:00:00",
                y: 0,
                height: 15,
                fontSize: 8.5f,
                color: _textColor);

            _revenueLine2Label = CreateRow(container,
                "本轮掉落: 0.00 火 | 本轮利润: 0.00 火",
                y: 15,
                height: 15,
                fontSize: 8.5f,
                color: _textColor);

            _revenueLine3Label = CreateRow(container,
                "累计收益: 0.00 火 | 累计时间: 0:00:00",
                y: 30,
                height: 15,
                fontSize: 8.5f,
                color: _textColor);

            _revenueLine4Label = CreateRow(container,
                "时均收益: 0.00 火 | 轮均收益: 0.00 火",
                y: 45,
                height: 15,
                fontSize: 8.5f,
                color: _textColor);

            _revenueMaxLabel = CreateRow(container, "本轮最高收益", y: 60, height: 15, fontSize: 8.5f, color: _textColor);
            _revenueMaxItemLabel = CreateRow(container, "—", y: 75, height: 15, fontSize: 8.5f);

            _revenueMinLabel = CreateRow(container, "本轮最低收益", y: 90, height: 15, fontSize: 8.5f, color: _textColor);
            _revenueMinItemLabel = CreateRow(container, "—", y: 105, height: 15, fontSize: 8.5f);
        }

        private void BuildTradingPage(Panel container)
        {
            _tradingBuyLabel = CreateRow(container,
                "购买商品: 0 火",
                y: 0,
                color: Color.FromArgb(255, 100, 100));

            _tradingSellLabel = CreateRow(container,
                "出售商品: 0 火",
                y: 25,
                color: Color.FromArgb(100, 255, 100));

            _tradingNetLabel = CreateRow(container,
                "净利润: 0 火",
                y: 50,
                fontSize: 11f,
                fontStyle: FontStyle.Bold,
                color: _secondaryTextColor);
        }

        private Label CreateRow(
            Panel container,
            string text,
            int y,
            int height = 20,
            float fontSize = 9f,
            FontStyle fontStyle = FontStyle.Regular,
            Color? color = null)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(0, y),
                Size = new Size(Width - 40, height),
                Font = new Font("Microsoft YaHei", fontSize, fontStyle),
                ForeColor = color ?? _secondaryTextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = _cardBackground
            };

            container.Controls.Add(label);
            return label;
        }
    }
}

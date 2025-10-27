using System;
using System.Windows.Forms;
using NewUI.DataSources;

namespace NewUI
{
    /// <summary>
    /// 将数据源与主统计窗口连接起来，负责定时刷新 UI。
    /// </summary>
    public sealed class StatsUIController : IDisposable
    {
        private readonly NewStatsWindow _window;
        private readonly IStatsDataSource _dataSource;
        private readonly Timer _refreshTimer;

        public StatsUIController(NewStatsWindow window, IStatsDataSource dataSource)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

            _refreshTimer = new Timer
            {
                Interval = 1000
            };
            _refreshTimer.Tick += (_, _) => Refresh();
        }

        /// <summary>
        /// 启动定时刷新，并立即刷新一次。
        /// </summary>
        public void Start()
        {
            Refresh();
            _refreshTimer.Start();
        }

        /// <summary>
        /// 立即从数据源获取最新快照并刷新窗口。
        /// </summary>
        public void Refresh()
        {
            try
            {
                var snapshot = _dataSource.GetSnapshot();
                _window.Render(snapshot);
            }
            catch
            {
                // 捕获异常以避免刷新循环终止。
            }
        }

        public void Dispose()
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
        }
    }
}

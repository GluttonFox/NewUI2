using System;
using System.Threading;
using System.Windows.Forms;
using NewUI.DataSources;

namespace NewUI
{
    /// <summary>
    /// UI启动器，负责托管统计窗口与定时刷新逻辑。
    /// </summary>
    public class UIStarter : IDisposable
    {
        private Thread _uiThread;
        private static UIStarter _instance;
        private NewStatsWindow _mainWindow;
        private StatsUIController _statsController;
        private readonly IStatsDataSource _dataSource;

        public static UIStarter Instance => _instance;

        public UIStarter()
            : this(new ServiceLocatorStatsDataSource(ServiceLocator.Instance))
        {
        }

        public UIStarter(IStatsDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        /// <summary>
        /// 启动UI。
        /// </summary>
        public void StartUI()
        {
            _instance = this;

            _uiThread = new Thread(() =>
            {
                try
                {
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.EnableVisualStyles();

                    _mainWindow = new NewStatsWindow();
                    _statsController = new StatsUIController(_mainWindow, _dataSource);
                    _statsController.Start();

                    Application.Run(_mainWindow);
                }
                catch (Exception)
                {
                    //ConsoleLogger.Instance.LogError($"UI启动失败: {ex.Message}");
                }
                finally
                {
                    _statsController?.Dispose();
                    _statsController = null;
                }
            })
            {
                IsBackground = false
            };

            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();

            Thread.Sleep(100);
        }

        /// <summary>
        /// 主动请求刷新所有统计数据。
        /// </summary>
        public void RefreshAllData() => ExecuteOnUiThread(() => _statsController?.Refresh());

        public void UpdateFarmingRounds() => RefreshAllData();

        public void UpdateRevenueStats() => RefreshAllData();

        public void UpdateTradingStats() => RefreshAllData();

        public void UpdatePriceData() => RefreshAllData();

        private void ExecuteOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            try
            {
                if (_mainWindow?.IsDisposed == false)
                {
                    _mainWindow.Invoke(new Action(action));
                }
            }
            catch (Exception)
            {
                //ConsoleLogger.Instance.LogError($"执行UI更新失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                ExecuteOnUiThread(() =>
                {
                    _statsController?.Dispose();
                    _statsController = null;

                    _mainWindow?.Close();
                });
            }
            catch (Exception)
            {
                //ConsoleLogger.Instance.LogError($"UI资源清理失败: {ex.Message}");
            }
            finally
            {
                _mainWindow = null;
                _statsController?.Dispose();
                _statsController = null;
            }
        }
    }
}

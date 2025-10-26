using System;
using System.Threading;
using System.Windows.Forms;
using GameLogMonitor;

namespace NewUI
{
    /// <summary>
    /// UI启动器
    /// </summary>
    public class UIStarter
    {
        private Thread _uiThread;
        private static UIStarter _instance;
        private NewStatsWindow _mainWindow;
        private System.Windows.Forms.Timer _refreshTimer;

        public static UIStarter Instance => _instance;

        /// <summary>
        /// 启动UI
        /// </summary>
        public void StartUI()
        {
            _instance = this;

            // 在STA线程中创建UI
            _uiThread = new Thread(() =>
            {
                try
                {
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.EnableVisualStyles();

                    _mainWindow = new NewStatsWindow();
                    
                    // 初始化定时刷新器
                    InitializeRefreshTimer();
                    
                    Application.Run(_mainWindow);
                }
                catch (Exception ex)
                {
                    //ConsoleLogger.Instance.LogError($"UI启动失败: {ex.Message}");
                }
            });

            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.IsBackground = false;
            _uiThread.Start();

            // 等待UI线程启动
            Thread.Sleep(100);
        }

        /// <summary>
        /// 初始化定时刷新器
        /// </summary>
        private void InitializeRefreshTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1秒刷新一次
            };
            _refreshTimer.Tick += (s, e) => RefreshAllData();
            _refreshTimer.Start();
        }

        /// <summary>
        /// 刷新所有数据
        /// </summary>
        public void RefreshAllData()
        {
            try
            {
                if (_mainWindow?.IsDisposed == false)
                {
                    _mainWindow.Invoke(() =>
                    {
                        _mainWindow.UpdateAllStats();
                    });
                }
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"刷新UI数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新刷图轮次统计
        /// </summary>
        public void UpdateFarmingRounds()
        {
            try
            {
                if (_mainWindow?.IsDisposed == false)
                {
                    _mainWindow.Invoke(() =>
                    {
                        _mainWindow.UpdateFarmingStats();
                    });
                }
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新刷图统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新收益统计
        /// </summary>
        public void UpdateRevenueStats()
        {
            try
            {
                if (_mainWindow?.IsDisposed == false)
                {
                    _mainWindow.Invoke(() =>
                    {
                        _mainWindow.UpdateRevenueStats();
                    });
                }
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新收益统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新交易统计
        /// </summary>
        public void UpdateTradingStats()
        {
            try
            {
                if (_mainWindow?.IsDisposed == false)
                {
                    _mainWindow.Invoke(() =>
                    {
                        _mainWindow.UpdateTradingStats();
                    });
                }
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新交易统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新物价数据
        /// </summary>
        public void UpdatePriceData()
        {
            try
            {
                if (_mainWindow?.IsDisposed == false)
                {
                    _mainWindow.Invoke(() =>
                    {
                        _mainWindow.UpdatePriceData();
                    });
                }
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"更新物价数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _refreshTimer = null;
                
                _mainWindow?.Close();
                _mainWindow = null;
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogError($"UI资源清理失败: {ex.Message}");
            }
        }

    }
}
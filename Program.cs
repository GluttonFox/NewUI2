using System;
using System.Diagnostics;

namespace GameLogMonitor
{
    /// <summary>
    /// 主程序入口点
    /// </summary>
    class Program
    {
        // 用于控制程序退出的信号
        //private static readonly ManualResetEvent _exitEvent = new(false);

        static void Main(string[] args)
        {


            //using var serviceLocatorWrapper = ServiceLocator.CreateWrapper();
            //using var eventHandlerWrapper = EventHandler.CreateWrapper();
            
            try
            {
                //ConsoleLogger.Instance.LogInfo("初始化服务定位器...");

                // 初始化服务定位器
                //serviceLocatorWrapper.Value.InitializeServices();

                // 初始化事件处理器
                //ConsoleLogger.Instance.LogInfo("初始化事件处理器...");
                //eventHandlerWrapper.Value.Initialize();

                // 启动新的UI界面
                //ConsoleLogger.Instance.LogInfo("启动UI界面...");
                var uiStarter = new NewUI.UIStarter();
                uiStarter.StartUI();
            }
            catch (Exception ex)
            {
                //ConsoleLogger.Instance.LogCritical("程序启动失败", ex);
                //ConsoleLogger.Instance.LogInfo("程序正在运行，关闭UI窗口将退出程序。");
                
                //_exitEvent.WaitOne();
                return;
            }

            // 动态查找游戏日志路径
            //string logPath = FindGameLogPath();

            //if (string.IsNullOrEmpty(logPath))
            //{
                //ConsoleLogger.Instance.LogError("未找到游戏进程或日志文件");
                //ConsoleLogger.Instance.LogWarning("请确保游戏正在运行");
                //ConsoleLogger.Instance.LogWarning("请确保管理员模式启动");
                //ConsoleLogger.Instance.LogInfo("程序正在运行，关闭UI窗口将退出程序。");
                
                //_exitEvent.WaitOne();
            //    return;
            //}

            //using var parser = new LogParser(logPath);
            //parser.Start();
            //ConsoleLogger.Instance.LogInfo("日志监控已启动...");
            //ConsoleLogger.Instance.LogInfo("等待日志输入...");
            //ConsoleLogger.Instance.LogInfo("程序正在运行，关闭UI窗口将退出程序。");

            // 等待退出信号
            //_exitEvent.WaitOne();
            
            //ConsoleLogger.Instance.LogInfo("程序正在退出...");
            
            // 确保所有资源都被正确释放
            //parser?.Dispose();

        }
        
        /// <summary>
        /// 通知程序退出
        /// </summary>
        //public static void RequestExit()
        //{
        //    _exitEvent.Set();
        //}

        /// <summary>
        /// 查找游戏日志路径
        /// </summary>
        /// <returns>游戏日志文件路径，如果未找到则返回null</returns>
        //private static string FindGameLogPath()
        //{
        //    return ExceptionHandler.SafeExecute(() =>
        //    {
        //        // 查找游戏进程
        //        Process[] processes = Process.GetProcessesByName(Constants.Game.ProcessName);
        //        string exePath = null;

        //        try
        //        {
        //            if (processes.Length == 0)
        //            {
        //                throw new GameProcessException("未找到游戏进程");
        //            }

        //            Process gameProcess = processes[0];
        //            ConsoleLogger.Instance.LogInfo($"找到游戏进程: {Constants.Game.ProcessName} (PID: {gameProcess.Id})");

        //            // 获取进程exe路径
        //            exePath = gameProcess.MainModule?.FileName;
        //            if (string.IsNullOrEmpty(exePath))
        //            {
        //                throw new GameProcessException("无法获取进程exe路径");
        //            }
        //        }
        //        finally
        //        {
        //            // 释放所有Process对象
        //            foreach (var process in processes)
        //            {
        //                process?.Dispose();
        //            }
        //        }

        //        // 向上返回两次目录
        //        DirectoryInfo exeDir = Directory.GetParent(exePath) ?? throw new DirectoryNotFoundException("无法获取exe目录");
        //        DirectoryInfo parentDir = exeDir.Parent ?? throw new DirectoryNotFoundException("无法获取父目录");
        //        DirectoryInfo grandParentDir = parentDir.Parent ?? throw new DirectoryNotFoundException("无法获取祖父目录");

        //        // 构建日志文件路径
        //        string logPath = Path.Combine(grandParentDir.FullName, Constants.Game.LogFileRelativePath);


        //        // 检查日志文件是否存在
        //        if (File.Exists(logPath))
        //        {
        //            return logPath;
        //        }
        //        else
        //        {
        //            throw new FileNotFoundException("日志文件不存在", logPath);
        //        }
        //    }, "查找游戏日志路径");
        //}

    }
}
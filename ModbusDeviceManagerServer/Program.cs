using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using SpaceCG.Generic;
using SpaceCG.ModbusExtension;

namespace ModbusDeviceManagerServer
{
    class Program
    {
        public delegate bool ControlCtrlDelegate(int CtrlType);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);
        private static ControlCtrlDelegate CancelHandler = new ControlCtrlDelegate(CancelHandlerRoutine);
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger("MDMS_Module");

        private static bool Running = true;
        private static ModbusDeviceManager ModbusDeviceManager;

        static void Main(string[] args)
        {
#if DEBUG
            ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.Level = log4net.Core.Level.Debug;
#endif
            SetConsoleCtrlHandler(CancelHandler, true);
            Console.Title = "Modbus Device Manager Server v2.0.230508";
            SystemEvents.SessionEnded += SystemEvents_SessionEnded;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Process.GetCurrentProcess().EnableRaisingEvents = true;

            ModbusDeviceManager = new ModbusDeviceManager();
            ModbusDeviceManager.LoadDeviceConfig("ModbusDevices.Config");

            while (Running)
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                ConsoleKeyHandler(info);
                Thread.Sleep(10);
            }
        }
        public static void ConsoleKeyHandler(ConsoleKeyInfo info)
        {
            switch (info.Key)
            {
                case ConsoleKey.R:  //Ctrl+R
                    if (info.Modifiers == ConsoleModifiers.Control)
                    {
                        Console.Clear();
                        Log.Info("重新加载配置文件 ModbusDevices.Config");
                        ModbusDeviceManager.LoadDeviceConfig("ModbusDevices.Config");
                    }
                    break;

                case ConsoleKey.T:  //Ctrl+T
                    if (info.Modifiers == ConsoleModifiers.Control)
                    {
                        Console.Clear();
                        Log.Info("Console Clears ...... ");
                    }
                    break;

                case ConsoleKey.D:  //Ctrl+D
                    if (info.Key == ConsoleKey.D && info.Modifiers == ConsoleModifiers.Control)
                    {
                        log4net.Repository.Hierarchy.Logger root = ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root;
                        root.Level = (root.Level == log4net.Core.Level.Info) ? log4net.Core.Level.Debug : log4net.Core.Level.Info;
                        Log.Warn($"Root Logger Current Level: {root.Level}");
                    }
                    break;
            }
        }
        /// <summary>
        /// 程序退出处理
        /// </summary>
        private static void ExitDispose()
        {
            Running = false;
            ModbusDeviceManager.Dispose();

            SystemEvents.SessionEnded -= SystemEvents_SessionEnded;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }
        private static bool CancelHandlerRoutine(int CtrlType)
        {
            Log.Info($"正在退出程序 ...... ExitCode: {CtrlType}");

            switch (CtrlType)
            {
                case 0:
                    //Console.WriteLine("Ctrl+C关闭"); //Ctrl+C关闭  
                    break;
                case 2:
                    //Console.WriteLine("Exit关闭");//按控制台关闭按钮关闭  
                    break;
            }

            ExitDispose();
            Thread.Sleep(100);
            Environment.Exit(0);

            return true;
        }
        private static void SystemEvents_SessionEnded(object sender, SessionEndedEventArgs e)
        {
            Log.Info($"正在注销或关闭系统......SessionEnded({e.Reason})");

            ExitDispose();
            Thread.Sleep(100);
            Environment.Exit(0);
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error($"未处理的异常信息：{(Exception)e.ExceptionObject}");
            Log.Info($"公共语言运行时是否即将终止: {e.IsTerminating}");
        }


    }
}

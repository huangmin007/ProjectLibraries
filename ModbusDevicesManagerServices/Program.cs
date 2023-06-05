using System;
using System.IO.Ports;
using System.Threading;
using SpaceCG.Extensions.Modbus;
using SpaceCG.Generic;

namespace ModbusDevicesManagerServices
{
    class Program
    {
        static readonly LoggerTrace Logger = new LoggerTrace("ProgramMain");

        private static bool Running = true;
        private static ModbusDeviceManager ModbusDeviceManager;
        private static string defaultConfigFile = "ModbusDevices.Config";


        static void Main(string[] args)
        {
            string title = "Modbus Device Manager Server v2.1.230602";
            Console.Title = "Modbus Device Manager Server v2.1.230602";
            Console.WriteLine($"Echo: {title}");

            string[] names = SerialPort.GetPortNames();
            if(names.Length > 0 ) 
            {
                foreach (string name in names) { Console.WriteLine(name); }
            }

            Console.CancelKeyPress += Console_CancelKeyPress;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ModbusDeviceManager = new ModbusDeviceManager();
            ModbusDeviceManager.LoadDeviceConfig(defaultConfigFile);
            
            while (Running)
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                ConsoleKeyHandler(info);
                Thread.Sleep(10);
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Logger.Info($"Console Cancel Key .");
            ExitDispose();
        }

        public static void ConsoleKeyHandler(ConsoleKeyInfo info)
        {
            switch (info.Key)
            {
                case ConsoleKey.R:  //Ctrl+R
                    if (info.Modifiers == ConsoleModifiers.Control)
                    {
                        Console.Clear();
                        Logger.Info($"重新加载配置文件 {defaultConfigFile}");
                        ModbusDeviceManager.LoadDeviceConfig(defaultConfigFile);
                    }
                    break;

                case ConsoleKey.T:  //Ctrl+T
                    if (info.Modifiers == ConsoleModifiers.Control)
                    {
                        Console.Clear();
                        Logger.Info("Console Clears ...... ");
                    }
                    break;
            }
        }

        private static void ExitDispose()
        {
            Running = false;
            ModbusDeviceManager?.Dispose();

            Environment.Exit(0);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error($"未处理的异常信息：{(Exception)e.ExceptionObject}");
            Logger.Info($"公共语言运行时是否即将终止: {e.IsTerminating}");
        }
    }
}

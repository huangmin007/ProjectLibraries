using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using SpaceCG.Extensions.Modbus;
using SpaceCG.Generic;

namespace ModbusDevicesManagerServices
{
    class Program
    {
        static readonly LoggerTrace Logger = new LoggerTrace("ProgramMain");

        private static bool Running = true;
        private static ControlInterface ControlInterface;
        private static ConnectionManager ConnectionManager;
        private static ModbusDeviceManager ModbusDeviceManager;
        
        private static string DefaultConfigFile = "ModbusDevices.Config";
        private static string Title = "Modbus Device Manager Server v2.1.230602";

        static void Main(string[] args)
        {
            Console.Title = Title;
            Console.WriteLine($"{Title}");

            string[] names = SerialPort.GetPortNames();
            if(names.Length > 0 ) 
            {
                foreach (string name in names) { Console.WriteLine(name); }
            }

            Console.CancelKeyPress += Console_CancelKeyPress;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            LoadDeviceConfig(DefaultConfigFile);
            
            while (Running)
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                ConsoleKeyHandler(info);
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// 加载设备配置文件，配置文件参考 ModbusDevices.Config
        /// </summary>
        /// <param name="configFile"></param>
        public static void LoadDeviceConfig(String configFile)
        {
            if (!File.Exists(configFile))
            {
                Logger.Error($"指定的配置文件不存在 {configFile}");
                return;
            }

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreWhitespace = true;
            XmlReader reader = XmlReader.Create(configFile, settings);
            XElement Configuration = XElement.Load(reader, LoadOptions.None);

            string modbusMName = Configuration.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(modbusMName))
            {
                Logger.Error($"需要配置 {nameof(ConnectionManager)} 名称");
                return;
            }
            XElement Connections = Configuration.Element("Connectins");
            if (Connections == null)
            {
                Logger.Error($"连接配置不存在");
                return;
            }
            IEnumerable<XElement> ModbusElements = Configuration.Descendants("Modbus");
            if (Connections == null)
            {
                Logger.Error($"Modbus 设备配置不存在");
                return;
            }

            ushort localPort = ushort.TryParse(Configuration.Attribute("LocalPort")?.Value, out ushort port) && port >= 1024 ? port : (ushort)2023;
            if(ControlInterface == null)
                ControlInterface = new ControlInterface(localPort);

            if (ConnectionManager == null)
                ConnectionManager = new ConnectionManager(ControlInterface, Connections.Attribute("Name")?.Value);
            ConnectionManager.TryParseConnectionElements(Connections.Descendants(ConnectionManager.XConnection));

            if(ModbusDeviceManager == null)
                ModbusDeviceManager = new ModbusDeviceManager(ControlInterface, modbusMName);
            ModbusDeviceManager.TryParseModbusElements(ModbusElements);
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
                        Logger.Info($"重新加载配置文件 {DefaultConfigFile}");
                        LoadDeviceConfig(DefaultConfigFile);
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

            ModbusDeviceManager?.RemoveAll();
            ConnectionManager?.RemoveAll();
            ControlInterface?.Dispose();

            Environment.Exit(0);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error($"未处理的异常信息：{(Exception)e.ExceptionObject}");
            Logger.Info($"公共语言运行时是否即将终止: {e.IsTerminating}");
        }
    }
}

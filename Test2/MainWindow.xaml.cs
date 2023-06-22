using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Modbus.Device;
using SpaceCG.Extensions.Modbus;
using SpaceCG.Generic;

namespace Test2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        ControlInterface controlInterface;

        public MainWindow()
        {
            InitializeComponent();
            //LoggerExtensions.Configuration();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            controlInterface?.Dispose();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            //LoggerExtensions.Info($"Closed.");

            logger1?.Dispose();
            modbusTransport?.Dispose();
        }

        private static readonly LoggerTrace logger1 = new LoggerTrace();
        private static readonly LoggerTrace logger2 = new LoggerTrace("test");

        IModbusMaster master;
        ModbusTransport modbusTransport;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoggerTrace.FileTraceEventType = SourceLevels.Verbose;

            FTextWriterTraceListener trace = logger1.TraceSource.Listeners[0] as FTextWriterTraceListener;
            if (trace != null) trace.WriteEvent += (s, we) =>
            {
                TextBox_Trace?.Dispatcher.InvokeAsync(() => TextBox_Trace.AppendText(we.Message));
            };
            
            logger1.Info("中文测试2");
            logger1.Warn("Eng");
            logger1.Info("{0},{1}", "中文测试1,,,1", "中文测试2,,,,2");


            logger1.Info("String fileName = curLogFile.Name.Substring(0, curLogFile.Name.Length - curLogFile.Extension.Length + 1);");

            logger2.Info("中文测试2 Chinese test...");

            String str = "test.Dispose";
            bool result = Regex.IsMatch(str, @"\*.Dispose", RegexOptions.Singleline);
            Console.WriteLine($"Result::{result}");

            controlInterface = new ControlInterface(2023);
            controlInterface.AccessObjects.Add("window", this);

#if false
            //System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort("COM3", 9600);
            //serialPort.Open();
            //master = ModbusSerialMaster.CreateRtu(serialPort);

            master = SpaceCG.Extensions.NModbus4Extensions.CreateNModbus4Master("SERIAL", "COM3", 9600);
            modbusTransport = new ModbusTransport(master, "test bus");
            ModbusIODevice device = new ModbusIODevice(0x01, "LH-IO204");
            for(ushort i = 0; i < 2; i ++)
                device.Registers.Add(new Register(i, RegisterType.CoilsStatus));
            for (ushort i = 0; i < 4; i++)
                device.Registers.Add(new Register(i, RegisterType.DiscreteInput));

            device.Registers.Add(new Register(0x02, RegisterType.DiscreteInput, 2));

            modbusTransport.ModbusDevices.Add(device);
            modbusTransport.InputChangeEvent += ModbusTransport_InputChangeEvent;
            modbusTransport.OutputChangeEvent += ModbusTransport_OutputChangeEvent;
            modbusTransport.StartTransport();

            int t = 3;
            Console.WriteLine(  t.ToString("2"));
#endif
            //TestLoad();
            Console.WriteLine($"main:{Thread.CurrentThread.ManagedThreadId}");

            Thread thread = new Thread(TTest);
            thread.Start();
        }

        private void TTest(object obj)
        {
            Console.WriteLine($"thread:{Thread.CurrentThread.ManagedThreadId}");
            logger1.Info("THread...");
        }

        private void ModbusTransport_OutputChangeEvent(ModbusTransport transport, ModbusIODevice device, Register register)
        {
            if(register.Count == 1)
                Console.WriteLine($"Output: {transport} DeviceAddress:{device.Address} RegisterAddress:{register} RegisterValue:{register.Value}");
            else
                Console.WriteLine($"Output: {transport} DeviceAddress:{device.Address} RegisterAddress:{register} RegisterValue:{Convert.ToString((int)register.Value, 2)}");
        }

        private void ModbusTransport_InputChangeEvent(ModbusTransport transport, ModbusIODevice device, Register register)
        {
            if (register.Count == 1)
                Console.WriteLine($"Input: {transport} DeviceAddress:{device.Address} RegisterAddress:{register} RegisterValue:{register.Value}");
            else
                Console.WriteLine($"Input: {transport} DeviceAddress:{device.Address} RegisterAddress:{register} RegisterValue:{Convert.ToString((int)register.Value, 2)}");
        }

        private void Button_btn_Click(object sender, RoutedEventArgs e)
        {
            //logger2.Info("String fileName = curLogFile.Name.Substring(0, curLogFile.Name.Length - curLogFile.Extension.Length + 1);");
            //int r = Add2(5, 9);
            //logger1.Info($"Result::{r}");

            //modbusTransport.TurnSingleCoil(0x01, 0x00);
            Console.WriteLine($"click:{Thread.CurrentThread.ManagedThreadId}");
            logger1.Debug("Test ..........");
            logger1.Debug("{0},{1}", "Test,,,1", "Test,,,,2");
            Task.Run(()=>
            {
                Console.WriteLine($"task:{Thread.CurrentThread.ManagedThreadId}");

                logger1.Info("测试进程");
            });
        }

        public int Add(int a, int b) 
        {
            return Task.Run<int>(() =>
            {
                Thread.Sleep(5000);
                return a + b;
            }).Result;
        }

        public int Add2(int a, int b)
        {
            return a + b;
        }

        private void TestLoad()
        {
            Console.WriteLine();
            MethodInfo method = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(int) } );
            Console.WriteLine(  method?.Name);
            method.Invoke(null, new object[] {"15+16" });
        }

    }
}

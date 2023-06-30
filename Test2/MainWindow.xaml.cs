using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Extensions;
using SpaceCG.Extensions.Modbus;
using SpaceCG.Generic;
using SpaceCG.Net;

namespace Test2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            logger1?.Dispose();
            client?.Dispose();
        }

        private static LoggerTrace logger1 = new LoggerTrace();

        IAsyncClient client;
        IAsyncServer server;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoggerTrace.ConsoleTraceEvent += LoggerTrace_TraceSourceEvent;            
            InstanceExtensions.ConvertChangeTypeExtension = ConvertChangeTypeExtension2;

            //client = new AsyncTcpClient();
            //client = new AsyncUdpClient();
            //client.Connect("127.0.0.1", 5334);

            logger1.Info(client);

            //server = new AsyncTcpServer(3000);
            //server = new AsyncUdpServer(3000);
            //server.ClientDataReceived += Server_ClientDataReceived;

            Console.WriteLine($"test::{WindowState.Normal}");
        }

        private void Server_ClientDataReceived(object sender, AsyncDataEventArgs e)
        {
            Console.WriteLine(e.EndPoint);
        }

        private static bool ConvertChangeTypeExtension2(object value, Type conversionType, out object conversionValue)
        {
            conversionValue = null;
            if (value.GetType() == typeof(string) && conversionType == typeof(Brush))
            {
                string sValue = value.ToString();

                if (sValue.IndexOf("#") == 0)// && uint.TryParse(sValue.Substring(1), NumberStyles.HexNumber, null, out uint uValue))
                {
                    //BrushConverter
                    //var b= Brushes.Aqua;
                    Color color = (Color)ColorConverter.ConvertFromString(sValue);
                    Console.WriteLine(color);
                    //Colors
                    SolidColorBrush colorBrush = new SolidColorBrush(color);
                    conversionValue = colorBrush;
                    
                    return true;
                }

                
            }
            
            return false;
        }

        int count = 0;
        private void LoggerTrace_TraceSourceEvent(object sender, TraceEventArgs e)
        {
            TextBox_Trace?.Dispatcher.InvokeAsync(
                    () =>
                    {
                        TextBox_Trace.AppendText($"{count++}");
                        TextBox_Trace.AppendText(e.FormatMessage);
                    });
        }

        private void TTest(object obj)
        {
            using (LoggerTrace logger4 = new LoggerTrace())
            {
                logger4.Debug($"logger4...");
            }

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
            Button button = (Button)sender;
            if (button == Button_Test)
            {
                //logger1.Info(client);
                logger1.Info(server);
            }
            else if(button == Button_Close)
            {
                //client.Close();
                server.Stop();
            }
            else if(button == Button_Connect)
            {
                //client.Connect();
                server.Start();
            }
            else if(button == Button_Send)
            {
                //client.SendMessage("hello");
                server.SendMessage("hello");
            }
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

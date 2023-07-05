using System;
using System.Collections.Generic;
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
        ReflectionController controller;

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

            Console.WriteLine($"SynchronizationContext.Current: {SynchronizationContext.Current}");
            Console.WriteLine($"GetCurrentProcess.Id: {Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine($"TaskScheduler.FromCurrentSynchronizationContextt: {TaskScheduler.FromCurrentSynchronizationContext().Id}");
            Console.WriteLine($"TaskScheduler.Current: {TaskScheduler.Current.Id}");
            Console.WriteLine($"TaskScheduler.Default: {TaskScheduler.Default.Id}");

            int tf = 23;
            var task1 = Task.Factory.StartNew<object>(() =>
            {
                Thread.Sleep(3000);
                Console.WriteLine("test..");
                return 12 + tf;
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);// TaskScheduler.FromCurrentSynchronizationContext());
            //Console.WriteLine(  task1.Result);
            if(task1.IsCompleted)
            {
                Console.WriteLine(task1.Result);
            }

            MethodInfo method = this.GetType().GetMethod("Add");
            Console.WriteLine(method.ReturnType);

            XElement element = XElement.Parse("<Action Target=\"obj\" Method=\"Me\" Params=\"12\" />");
            Console.WriteLine(  element);
            element.Add(XElement.Parse("<Return Result=\"True\" Value=\"aaa\" />"));
            Console.WriteLine(element);

            Console.WriteLine($"async:{Exec(true, "async")}");
            //Console.WriteLine($"sync::{Exec(false, "sync")}");

            controller = new ReflectionController(2025);
            controller.AccessObjects.Add("Window", this);
            //this.Dispatcher.Invoke
        }

        public bool Exec(bool async, string msg)
        {
            int tf = 34;
            Console.WriteLine($"start...{msg}");
            var task1 = Task.Factory.StartNew<object>(() =>
            {
                Console.WriteLine($"GetCurrentProcess.Id: {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(3000);
                Console.WriteLine($"test..{msg}");
                return 12 + tf;
            }, CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskScheduler.FromCurrentSynchronizationContext());// TaskScheduler.FromCurrentSynchronizationContext());
            task1.Wait();
            return task1.IsCompleted;
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

        private void Button_btn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button == Button_Test)
            {
                //logger1.Info(client);
                //logger1.Info(server);

                XElement action = XElement.Parse("<Action Target=\"Window\" Method=\"Add2\" Params=\"200,230\" />");

                controller.TryParseControlMessage(action);
            }
            else if(button == Button_Close)
            {
                //client.Close();
                //server.Stop();

                XElement action = XElement.Parse("<Action Target=\"Window\" Method=\"Add2\" Params=\"100,230\" Sync=\"False\" />");

                controller.TryParseControlMessage(action);
            }
            else if(button == Button_Connect)
            {
                //client.Connect();
                //server.Start();
            }
            else if(button == Button_Send)
            {
                //client.SendMessage("hello");
                //server.SendMessage("hello");
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
            Thread.Sleep(5000);
            this.Width = a + b;
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

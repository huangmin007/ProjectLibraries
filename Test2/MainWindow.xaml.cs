using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using SpaceCG.Extensions;
using SpaceCG.Generic;
using SpaceCG.Net;

namespace Test2
{
    public enum TestEnum:ushort
    {
        A = 0x00,
        B = 0x10,
        C = 0x20,
        D = 0x40,
    }

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

            rpcClient?.Dispose();
            rpcServer?.Dispose();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            controller?.Dispose();

        }

        private static LoggerTrace logger1 = new LoggerTrace();

        ReflectionController controller;

        InputHook inputHook;

        RPCServer rpcServer;
        RPCClient rpcClient;

        public int Add33(int a, int b) => a + b;

        public float Add(float a, float b) => a + b;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Type convertType = typeof(Thickness);
            Thickness thick = new Thickness(12);
            TypeConverter converter = TypeDescriptor.GetConverter(convertType);
            Console.WriteLine($"CanConvertTo::{converter.CanConvertTo(convertType)}");
            Console.WriteLine($"ToString::{converter.ConvertToString(thick)}"); 

            string message = "1234";

            rpcServer = new RPCServer(2025);
            rpcServer.AccessObjects.Add("Window", this);
            rpcServer.AccessObjects.Add("string", message);
            rpcServer.Start();

            //rpcServer.AccessObjects.Add("Thread", typeof(Thread).GetMethod("Sleep", new Type[] { typeof(int) }));
           
            rpcClient = new RPCClient("127.0.0.1", 2025);
            rpcClient.ConnectAsync();
            rpcClient.Timeout = 10000;

            string xmlString = "<data> <string><![CDATA[这是一段未解析字符数据<a>test</a>,[0x12,0x13,0xAA] ]]></string> </data>";
            XElement XML = XElement.Parse(xmlString);
            Console.WriteLine(XML);
            Console.WriteLine(XML.Element("string").Value);

            Type[] src = new Type[] { typeof(int), typeof(string) };
            List<Type> dst = new List<Type>() { typeof(int), typeof(string)};
            bool booo = TypeExtensions.Equals(src, dst);
            Console.WriteLine($"Equals::{booo}");

            Type type = Type.GetType("System.Threading.Thread", false, true);            
            Console.WriteLine($"Type::{type}");
            int t = 3;
            
            bool r = TypeExtensions.ConvertFrom("TestEnum.C", typeof(TestEnum), out object convert);
            Console.WriteLine($"{r}");
            Console.WriteLine($"{convert.GetType()}::{convert}");
            //Console.WriteLine(typeof(TestEnum).Name);

            string str = "2023-09-10.IFLYTEK.story.log";
            Console.WriteLine(str.Substring(0, str.IndexOf('.')));
            Console.WriteLine(str.Substring(str.IndexOf('.')+1, str.LastIndexOf('.') - str.IndexOf('.') - 1) );
            

            if (TypeExtensions.ConvertFrom("#ffaa00FF", typeof(Color), out object color))
            {
                this.Background = new SolidColorBrush((Color)color);
            }

            if (TypeExtensions.ConvertFrom(new string[] {"12", "0x12", "0B1101_1111" }, typeof(byte[]), out object result))
            {
                Console.WriteLine(result);
                Array array = (Array)result;
                for(int i = 0; i < array.Length; i ++)
                {
                    Console.WriteLine($"{i}:{array.GetValue(i)} {array.GetValue(i).GetType()}");
                }
            }

            if (TypeExtensions.ConvertFrom("80", out Thickness margin))
            {
                Image_Test.Margin = margin;
            }

            object boo = TypeDescriptor.GetConverter(typeof(bool)).ConvertFrom("True");
            Console.WriteLine(boo);

            StringExtensions.ToNumber("1280", out double number);
            Console.WriteLine(Console.Out);
            Console.WriteLine(Console.Out is TextWriter);

            inputHook = new InputHook();
            inputHook.KeydbEvent += InputHook_KeydbEvent;
        }

        private void InputHook_KeydbEvent(KEYBDDATA e)
        {
            Console.WriteLine($"KeyboardEvent::{e.dwFlags}:{e.wVk}");
        }

        private async void Button_btn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            logger1.Info($"Click {button.Name}");
            if (button == Button_Test)
            {
                var result = await rpcClient.TryCallMethodAsync("Window", "Add", new object[] { "120", "160" });
                logger1.Info($"Result::{result}");

                //var result = await rpcClient.CallMethodAsync("Window", "Add", new object[] { 12, 16 });
                //logger1.Info($"Result::{result}");
#if false
                await Task.Run(() =>
                {
                    var result = rpcClient?.CallMethod("Window", "Add", new object[] { 12, 16 });
                    logger1.Info($"Result::{result}");
                });
#endif
            }
            else if(button == Button_Close)
            {
                rpcServer.TryCallMethod("string", "ToNumber2", new object[] { 12 }, out object returnValue);
                Console.WriteLine($"RPC Server CallMethod::{returnValue}");
            }
            else if(button == Button_Connect)
            {
                logger1.Info("<Action Target=\"Window\" Method=\"SetWidth\" Params=\"400\" Sync=\"False\" />");
            }
            else if(button == Button_Send)
            {
                
            }
        }

        public void SetWidth(int a) 
        {
            Console.WriteLine($"SetWidth ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}");
            Thread.Sleep(3000);

            this.Width = (int)a;

            Console.WriteLine($"aaa>>{a}");
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

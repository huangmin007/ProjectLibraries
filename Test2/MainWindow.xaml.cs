﻿using System;
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
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using SpaceCG.Extensions;
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
            controller?.Dispose();
        }

        private static LoggerTrace logger1 = new LoggerTrace();

        ReflectionController controller;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //controller = new ReflectionController(2023);
            //controller.SynchronizationContext = new ReflectionSynchronizationContext();
            //controller.AccessObjects.Add("Window", this);

            if(TypeExtensions.ConvertFrom("#ffaa00FF", typeof(Color), out object color))
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

            if(TypeExtensions.ConvertFrom("D:\\Desktop\\big\\IMG_ (8).jpg", typeof(ImageSource), out object source))
            {
                Image_Test.Source = (ImageSource)source;
            }

            if (TypeExtensions.ConvertFrom("80", out Thickness margin))
            {
                Image_Test.Margin = margin;
            }

            object boo = TypeDescriptor.GetConverter(typeof(bool)).ConvertFrom("True");
            Console.WriteLine(boo);

            StringExtensions.ToNumber<Double>("1280", out double number);
        }

        private void Button_btn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button == Button_Test)
            {
                controller.TryParseControlMessage("<Action Target=\"Window\" Method=\"SetWidth\" Params=\"300\" Sync=\"True\" />");
            }
            else if(button == Button_Close)
            {
                controller.TryParseControlMessage("<Action Target=\"Window\" Method=\"SetWidth\" Params=\"400\" Sync=\"False\" />");
            }
            else if(button == Button_Connect)
            {
            }
            else if(button == Button_Send)
            {
                this.Dispatcher.InvokeAsync(() =>
                {
                    Console.WriteLine($"InvokeAsync 1  {Thread.CurrentThread.ManagedThreadId}");
                    Thread.Sleep(3000);
                    Console.WriteLine($"InvokeAsync 2");
                });
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

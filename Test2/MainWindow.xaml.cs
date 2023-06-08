using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpaceCG.Generic;
using SpaceCG.Net;

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

            //Logger?.Dispose();
            
        }

        private LoggerTrace logger = new LoggerTrace();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logger.Info("中文测试2");
            logger.Warn("Eng");

            logger.Info("String fileName = curLogFile.Name.Substring(0, curLogFile.Name.Length - curLogFile.Extension.Length + 1);");

            String str = "test.Dispose";
            bool result = Regex.IsMatch(str, @"\*.Dispose", RegexOptions.Singleline);
            Console.WriteLine($"Result::{result}");

            controlInterface = new ControlInterface(2023);            
            controlInterface.AccessObjects.Add("window", "hello");

        }

        private void Button_btn_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("String fileName = curLogFile.Name.Substring(0, curLogFile.Name.Length - curLogFile.Extension.Length + 1);");
            
        }

    }
}

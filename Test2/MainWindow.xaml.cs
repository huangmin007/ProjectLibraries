using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

namespace Test2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //ControllerInterface controllerInterface;

        public MainWindow()
        {
            InitializeComponent();
            //LoggerExtensions.Configuration();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            //controllerInterface?.Dispose();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            //LoggerExtensions.Info($"Closed.");

            //Logger?.Dispose();


        }

        private static LoggerTrace Logger = new LoggerTrace(nameof(MainWindow));
        private static LoggerTrace Logger2 = new LoggerTrace("aadafd", SourceLevels.Information);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Debug("Logger Debug");
            Logger2.Debug("Logger Debug");
            Logger.Info("Logger Info");
            Logger2.Info("Logger Info");
            Logger.Warn("Logger Warn");
            Logger2.Warn("Logger Warn");
            Logger.Fatal("Logger Fatal");
            Logger2.Fatal("Logger Fatal");

            Console.WriteLine("Window.Loaded");

            //throw new Exception("aaaaaa");
        }
    }
}

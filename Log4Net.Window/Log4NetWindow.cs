using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using log4net.Core;
using Win32API.User32;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 独立的日志窗体对象
    /// <para>使用 Ctrl+L 显示激活窗体/隐藏窗体 </para>
    /// </summary>
    public partial class Log4NetWindow : Window
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(nameof(Log4NetWindow));

        private IntPtr Handle;
        private HwndSource HwndSource;

        /// <summary> TextBox </summary>
        protected TextBox TextBox;
        /// <summary> ListView </summary>
        protected ListView ListView;
        /// <summary> ListBoxAppender </summary>
        private ListBoxAppender ListBoxAppender;

        /// <summary>
        /// 最大显示行数
        /// </summary>
        protected int MaxLines = 512;

        /// <summary>
        /// 关闭窗体时并销毁窗体
        /// </summary>
        private bool CloseDispose = false;

        /// <summary>
        /// Logger Window
        /// <para>使用 Ctrl+L 显示激活窗体/隐藏窗体 </para>
        /// </summary>
        /// <param name="maxLines"></param>
        public Log4NetWindow(int maxLines = 512)
        {
            this.MaxLines = maxLines;
            OnInitializeControls();
        }

        /// <summary>
        /// 强制关闭窗体，并销毁
        /// </summary>
        /// <param name="force"></param>
        public void Close(bool force)
        {
            CloseDispose = force;
            this.Close();
        }

        /// <inheritdoc/>
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            switch (e.Key)
            {
                case Key.T:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    {
                        this.Topmost = !this.Topmost;
                    }
                    break;
            }
        }
        /// <inheritdoc/>
        protected override void OnClosing(CancelEventArgs e)
        {
            if(!CloseDispose) this.Hide();

            e.Cancel = !CloseDispose;
			base.OnClosing(e);
        }
        /// <inheritdoc/>
        protected override void OnClosed(EventArgs e)
        {
            if (HwndSource != null)
            {
                HwndSource.Dispose();
                HwndSource = null;
            }

            if (Handle != null)
            {
                bool result = User32.UnregisterHotKey(Handle, 0);
                //result = result || User32.UnregisterHotKey(Handle, 1);

                Handle = IntPtr.Zero;
            }

            ListView.SelectionChanged -= ListView_SelectionChanged;
        }

        /// <summary>
        /// 初使化 UI 控件
        /// </summary>
        protected void OnInitializeControls()
        {
            //Grid
            Grid grid = new Grid();
            //grid.ShowGridLines = true;
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0.85, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0.15, GridUnitType.Star) });

            //ListView
            this.ListView = new ListView();
            this.ListView.SelectionChanged += ListView_SelectionChanged;
            this.ListBoxAppender = new ListBoxAppender(ListView, MaxLines);

            //GridSplitter
            GridSplitter splitter = new GridSplitter()
            {
                Height = 4.0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            //TextBox
            this.TextBox = new TextBox()
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.WrapWithOverflow,
            };
            TextBox.MouseDoubleClick += (s, e) => { ClearTextBox(); };
            TextBox.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

            grid.Children.Add(ListView);
            grid.Children.Add(splitter);
            grid.Children.Add(TextBox);
            Grid.SetRow(ListView, 0);
            Grid.SetRow(splitter, 1);
            Grid.SetRow(TextBox, 2);

            this.Title = "日志信息";
            this.Width = 1280;
            this.Height = 720;
            this.Content = grid;
            this.Loaded += LoggerWindow_Loaded;

            this.WindowState = WindowState.Minimized;
            this.Show();
        }

        /// <summary>
        /// 添加日志事件对象
        /// </summary>
        /// <param name="loggingEvent"></param>
        public void AppendLoggingEvent(LoggingEvent loggingEvent)
        {
            this.ListBoxAppender.AppendLoggingEvent(loggingEvent);
        }
        /// <summary>
        /// 清除日志列表内容
        /// </summary>
        public void ClearLogger()
        {
            this.ListView.Items.Clear();
        }

        /// <summary>
        /// 清除 TextBox 内容
        /// </summary>
        public void ClearTextBox()
        {
            this.TextBox.Text = "";
            this.TextBox.Foreground = Brushes.Black;
            this.TextBox.FontWeight = FontWeights.Normal;
        }

        /// <summary>
        /// ListView Select Changed Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListViewItem item = (ListViewItem)this.ListView.SelectedItem;
            
            if(item != null)
            {
                this.TextBox.Text = item.ToolTip.ToString();

                LoggingEvent logger = (LoggingEvent)item.Content;
                this.TextBox.Foreground = logger.Level >= Level.Error ? Brushes.Red : Brushes.Black;
                this.TextBox.FontWeight = logger.Level >= Level.Warn ? FontWeights.Black : FontWeights.Normal;
            }
            else
            {
                ClearTextBox();
            }
        }

        /// <summary>
        /// Window Loaded Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoggerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Handle = new WindowInteropHelper(this).Handle;

            bool result = User32.RegisterHotKey(Handle, 0, RhkModifier.CONTROL | RhkModifier.SHIFT, VirtualKeyCode.VK_W);
            Log.Info($"注册系统全局热键 CTRL+SHIFT+W 日志窗体显示/隐藏 ... {(result ? "OK" : "Failed")}");
            //result = result || User32.RegisterHotKey(Handle, 1, RhkModifier.CONTROL | RhkModifier.SHIFT, VirtualKeyCode.VK_D);
            //Log.Info($"注册系统全局热键 CTRL+SHIFT+D 日志级别切换 ... {(result ? "OK" : "Failed")}");

            if (result)
            {
                HwndSource = HwndSource.FromHwnd(Handle);
                HwndSource.AddHook(WindowProcHandler);
            }

            this.Topmost = true;
            this.Hide();
        }

        /// <summary>
        /// 更改日志级别
        /// </summary>
        public static void ChangeLoggerLevel()
        {
            log4net.Repository.Hierarchy.Logger root = ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root;
            root.Level = (root.Level == log4net.Core.Level.Info) ? log4net.Core.Level.Debug : log4net.Core.Level.Info;
            Log.Warn($"Root Logger Current Level: {root.Level}");
#if false
            if (((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.Level == log4net.Core.Level.Info)
                ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.Level = log4net.Core.Level.Debug;
            else
                ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.Level = log4net.Core.Level.Info;

            log4net.Core.Level level = ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.Level;
            Log.Info($"Current Logger Level: {level}");
#endif
        }

        /// <summary>
        /// Window Process Handler
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        protected IntPtr WindowProcHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            MessageType msgType = (MessageType)msg;
            if(msgType == MessageType.WM_HOTKEY)
            {
                RhkModifier rhk = (RhkModifier)(lParam.ToInt32() & 0xFFFF);     //低双字节
                VirtualKeyCode key = (VirtualKeyCode)(lParam.ToInt32() >> 16);  //高双字节 key

                if(rhk == (RhkModifier.CONTROL | RhkModifier.SHIFT))
                {
                    if (key == VirtualKeyCode.VK_W)
                    {
                        if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden)
                        {
                            this.Show();
                            this.Activate();
                            this.WindowState = WindowState.Normal;
                        }
                        else
                        {
                            this.WindowState = WindowState.Minimized;
                            this.Hide();
                        }
                    }
                    else if(key == VirtualKeyCode.VK_D)
                    {
                        ChangeLoggerLevel();
                    }

                    handled = true;
                }
            }

            return IntPtr.Zero;
        }
    }
}

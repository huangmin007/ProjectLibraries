#pragma warning disable CS1591
using log4net.Appender;
using System;
using log4net.Core;
using System.Windows.Controls;
using log4net.Layout;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 多字符值转换
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    internal class MultiStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || parameter == null) return "";
            return string.Format(parameter.ToString(), values);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    /// <summary>
    /// ba la ba la
    /// </summary>
    [ValueConversion(sourceType:typeof(double), targetType: typeof(double), ParameterType = typeof(double))]
    internal class WidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Console.WriteLine("{0} * {1} = {2}", value, parameter, (double)value * (double)parameter);
            return (double)value * (double)parameter;   //ActualWidth * Star;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(sourceType: typeof(ListViewItem), targetType: typeof(string))]
    internal class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            //ListViewItem item = (ListViewItem)value;
            //return item.Name.Replace("ID_", "");
            
            //ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
            //int index = listView.ItemContainerGenerator.IndexFromContainer(item) + 1;
            //int index = listView.Items.IndexOf(item);
            //return index;
            
            return value.ToString().Replace("ID_", "");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Log4Net WPF ListBox/ListView Appender
    /// </summary>
    internal class ListBoxAppender : AppenderSkeleton
    {
        /// <summary>
        /// 获取或设置最大可见行数
        /// </summary>
        protected int MaxLines = 512;
        /// <summary> ListBox </summary>
        protected ListBox ListBox = null;
        /// <summary> ListView </summary>
        protected ListView ListView = null;
        /// <summary> TextBox.AppendText Delegate Function </summary>
        protected Action<LoggingEvent> AppendHandlerDelegate;

        

        private int ID = 0;     //Logger ID
        private bool changeBgColor = true;  //切换背景标志变量

        /// <summary>
        /// 表格视图列的容器
        /// </summary>
        private GridView view;
        /// <summary>
        /// 所有视图列的容器
        /// </summary>
        private List<GridViewColumn> ViewColumns;


        /// <summary>
        /// Log4Net Appender for WPF ListBox and ListView
        /// </summary>
        /// <param name="listBox"></param>
        public ListBoxAppender(ListBox listBox)
        {
            if (listBox == null) throw new ArgumentNullException("参数不能为空");

            this.AppendHandlerDelegate = AppendHandler;
            this.Layout = new PatternLayout("[%date{HH:mm:ss.fff}] [%thread] [%5level] [%method(%line)] %logger - %message (%r) %newline");

            DefaultStyle(listBox);
            log4net.Config.BasicConfigurator.Configure(this);
        }

        /// <summary>
        /// 设置控件默认样式
        /// </summary>
        /// <param name="listBox"></param>
        protected void DefaultStyle(ListBox listBox)
        {
            // 必须使用 typeof 运算符
            if (listBox.GetType() == typeof(ListBox))
            {
                this.ListBox = listBox;                
                this.ListBox.SelectionMode = SelectionMode.Single;
                this.ListBox.VerticalContentAlignment = VerticalAlignment.Center;
                this.ListBox.MouseDoubleClick += ListBox_MouseDoubleClick;

                //WeakEventManager<ListBox, MouseButtonEventArgs>.AddHandler(ListBox, "MouseDoubleClick", (s, e) =>
                //{               });
            }
            else if (listBox.GetType() == typeof(ListView))
            {
                this.ListView = listBox as ListView;
                this.ListView.Name = "ListView";
                this.ListView.SelectionMode = SelectionMode.Single;
                this.ListView.MouseDoubleClick += ListBox_MouseDoubleClick;

                GridViewColumn IDColumn = new GridViewColumn()
                {
                    Width = 25,
                    Header = "ID",
                    //DisplayMemberBinding = new Binding("ID"),                    
                    DisplayMemberBinding = new Binding("Name")
                    {
                        Converter = new IndexConverter(),
                        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListViewItem), 1),
                    },
                };                
                GridViewColumn DateColumn = new GridViewColumn()
                {
                    Width = 90,
                    Header = "Date",
                    DisplayMemberBinding = new Binding("TimeStamp") { StringFormat = "yyyy-MM-dd" },
                };
                GridViewColumn TimeColumn = new GridViewColumn()
                {
                    Width = 90,                    
                    Header = "Time",
                    DisplayMemberBinding = new Binding("TimeStamp") { StringFormat = "HH:mm:ss.fff" },
                };
                //Test
                Binding bind_width = new Binding("ActualWidth")
                {
                    Converter = new WidthConverter(), ConverterParameter = 0.1, IsAsync = true,
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListView), 1),
                };
                BindingOperations.SetBinding(TimeColumn, GridViewColumn.WidthProperty, bind_width);
                
                GridViewColumn DomainColumn = CreateColumn("Domain", "Domain");
                GridViewColumn UserNameColumn = CreateColumn("UserName", "UserName");
                GridViewColumn IdentityColumn = CreateColumn("Identity", "Identity");
                GridViewColumn ThreadColumn = CreateColumn("Thread", "ThreadName", 50);
                GridViewColumn LevelColumn = CreateColumn("Level", "Level", 60);
                GridViewColumn LocationColumn = new GridViewColumn()
                {
                    Width = 150,
                    Header = "Location",
                    DisplayMemberBinding = new MultiBinding()
                    {
                        Bindings = {
                            //new Binding("LoggingEvent.LocationInformation.ClassName"),
                            new Binding("LocationInformation.MethodName"),
                            new Binding("LocationInformation.LineNumber"),
                        },
                        Converter = new MultiStringConverter(),
                        ConverterParameter = "{0}({1})",
                    }
                };
                GridViewColumn LoggerColumn = CreateColumn("Logger", "LoggerName", 120);
                GridViewColumn MessageColumn = CreateColumn("Message", "RenderedMessage", 580);
                GridViewColumn ExceptionColumn = CreateColumn("Exception", "ExceptionObject", 150);

                ViewColumns = new List<GridViewColumn>();
                ViewColumns.Add(IDColumn);
                ViewColumns.Add(DateColumn);
                ViewColumns.Add(TimeColumn);
                ViewColumns.Add(DomainColumn);
                ViewColumns.Add(UserNameColumn);
                ViewColumns.Add(IdentityColumn);
                ViewColumns.Add(ThreadColumn);
                ViewColumns.Add(LevelColumn);
                ViewColumns.Add(LocationColumn);
                ViewColumns.Add(LoggerColumn);
                ViewColumns.Add(MessageColumn);
                ViewColumns.Add(ExceptionColumn);

                view = new GridView();
                view.Columns.Add(IDColumn);
                //view.Columns.Add(DateColumn);
                view.Columns.Add(TimeColumn);
                //view.Columns.Add(DomainColumn);
                //view.Columns.Add(UserNameColumn);
                //view.Columns.Add(IdentityColumn);
                view.Columns.Add(ThreadColumn);
                view.Columns.Add(LevelColumn);
                view.Columns.Add(LocationColumn);
                view.Columns.Add(LoggerColumn);
                view.Columns.Add(MessageColumn);
                view.Columns.Add(ExceptionColumn);

                //设置 ColumnHeaderContainer 样式
                view.ColumnHeaderContainerStyle = new Style();
                view.ColumnHeaderContainerStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));
                view.ColumnHeaderContainerStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
                view.ColumnHeaderContainerStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));

                //设置 ItemContainer 样式
                //使用 Foreground 属性设置前景色，与使用 Style 样式不一样
                //使用 Foreground 属性设置，XAML 样式可覆盖 Foreground 属性
                //使用 Style Codes 设置，XAML 样式不可覆盖 Style Codes 设置的值                
                ListView.ItemContainerStyle = new Style();
                ListView.ItemContainerStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));
                //ListView.Foreground = Brushes.Red; //无效

                this.ListView.View = view;
                this.ListView.ContextMenu = CreateListViewContextMenu();
            }
            else
            {
                // ...
            }
        }
        /// <summary>
        /// Mouse Double Cliek Copy to Clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item =  ListBox != null ? (ListBoxItem)ListBox.SelectedItem : 
                                ListView != null ? (ListViewItem)ListView.SelectedItem : 
                                null;

            if (item != null) Clipboard.SetText(item.ToolTip.ToString());
        }

        /// <summary>
        /// Log4Net Appender for WPF ListBox and ListView
        /// </summary>
        /// <param name="listBox"></param>
        /// <param name="maxLines">最大行数为 1024 行，默认为 512 行</param>
        public ListBoxAppender(ListBox listBox, int maxLines) : this(listBox)
        {
            this.MaxLines = maxLines > 1024 ? 1024 : maxLines;
        }

        /// <summary>
        /// 添加日志事件对象
        /// </summary>
        /// <param name="loggingEvent"></param>
        public void AppendLoggingEvent(LoggingEvent loggingEvent)
        {
            this.Append(loggingEvent);
        }

        /// <summary>
        /// @override
        /// </summary>
        /// <param name="loggingEvent"></param>
        protected override void Append(LoggingEvent loggingEvent)
        {
            this.ListBox?.Dispatcher.BeginInvoke(AppendHandlerDelegate, loggingEvent);
            this.ListView?.Dispatcher.BeginInvoke(AppendHandlerDelegate, loggingEvent);
        }

        /// <summary>
        /// Addend Logging Event
        /// </summary>
        /// <param name="loggingEvent"></param>
        protected void AppendHandler(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null) return;

            //LoggingEvent
            String text = string.Empty;
            PatternLayout patternLayout = this.Layout as PatternLayout;
            if (patternLayout != null)
            {
                text = patternLayout.Format(loggingEvent);
                if (loggingEvent.ExceptionObject != null)
                    text += loggingEvent.ExceptionObject.ToString() + Environment.NewLine;
            }
            else
            {
                text = loggingEvent.LoggerName + "-" + loggingEvent.RenderedMessage + Environment.NewLine;
            }

            text = string.Format("[{0}] {1}", ID ++, text.TrimEnd());            
            ListBoxItem item = null;

            //ListView
            if (this.ListView != null)
            {
                item = new ListViewItem();
                item.Height = 24;
                item.ToolTip = text;
                item.Content = loggingEvent;
                item.Name = string.Format("ID_{0}", ID);
                //item.Content = new {ID = ListView.Items.Count + 1, LoggingEvent = loggingEvent };
                item.Background = TextBoxAppender.GetBgColorBrush(loggingEvent.Level, changeBgColor = !changeBgColor);
                
                this.ListView.Items.Add(item);
                this.ListView.ScrollIntoView(item);

                int count = this.ListView.Items.Count - MaxLines;
                while(count -- > 0) this.ListView.Items.RemoveAt(0);
                if (ListView.Items.NeedsRefresh) this.ListView.Items.Refresh();
            }

            //ListBox
            if (this.ListBox != null)
            {
                item = new ListBoxItem();
                item.Height = 24;
                item.ToolTip = text;
                item.Content = text;
                item.Name = string.Format("ID_{0}", ID);
                item.Background = TextBoxAppender.GetBgColorBrush(loggingEvent.Level, changeBgColor = !changeBgColor);

                this.ListBox.Items.Add(item);
                this.ListBox.ScrollIntoView(item);
                if (ListBox.Items.Count > MaxLines) this.ListBox.Items.RemoveAt(0);
            }            
        }
        
        /// <summary>
        /// MenuItem Click Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_ClickHandler(object sender, RoutedEventArgs e)
        {
            MenuItem mItem = (MenuItem)sender;
            if (mItem.Name == "Clear")
            {
                ListView.Items.Clear();
                return;
            }

            if (mItem.Parent == null) return;
            MenuItem pItem = (MenuItem)mItem.Parent;

            if (pItem.Name == "Levels")
            {
                this.ListView.Items.Filter = (o) =>
                {
                    LoggingEvent logger = (LoggingEvent)((ListViewItem)o).Content;

                    if (logger == null) return true;
                    return mItem.Name.ToUpper() == logger.Level.Name ? mItem.IsChecked : true;
                };

                int count = this.ListView.Items.Count - MaxLines;
                while (count-- > 0) this.ListView.Items.RemoveAt(0);
                if (ListView.Items.NeedsRefresh) this.ListView.Items.Refresh();

                return;
            }

            if(pItem.Name == "Columns")
            {
                if (mItem.IsChecked)
                {
                    //查找数据在 ViewColumns 集合的索引位置
                    int itemIndex = ViewColumns
                        .Select((item, index) => new { Header = item.Header.ToString(), Index = index })
                        .Where((o) => o.Header == mItem.Name)
                        .First().Index;
                    if (itemIndex == -1) return;
                    
                    //查找合适(IsChecked)的插入的索引位置，以防乱序，后在添加在列的最后面
                    var itemGroup = from Control item in pItem.Items where item is MenuItem && ((MenuItem)item).IsChecked select item;
                    int insertIndex = itemGroup
                        .Select((item, index) => new { Name = item.Name.ToString(), Index = index })
                        .Where(o => o.Name == mItem.Name)
                        .First().Index;

                    //插入列
                    if (insertIndex != -1) ((GridView)ListView.View).Columns.Insert(insertIndex, ViewColumns[itemIndex]);
                }
                else
                {
                    //查找数据在 ListView.View 集合的索引位置
                    int removeIndex = ((GridView)ListView.View).Columns
                        .Select((item, index) => new { Header = item.Header.ToString(), Index = index })
                        .Where(o => o.Header == mItem.Name)
                        .First().Index;

                    //移除列
                    if (removeIndex != -1) ((GridView)ListView.View).Columns.RemoveAt(removeIndex);
                }

                return;
            }

        }

        /// <summary>
        /// Create ListView ContextMenu
        /// </summary>
        /// <returns></returns>
        protected ContextMenu CreateListViewContextMenu()
        {
            Func<string, bool, MenuItem> CreateCheckBoxMenuItem = (label, isChecked) =>
            {
                MenuItem item = new MenuItem()
                {
                    Name = label,
                    Header = label,
                    IsChecked = isChecked,
                    IsCheckable = true,
                };
                item.Click += MenuItem_ClickHandler;
                
                return item;
            };

            //Level
            MenuItem Levels = new MenuItem() { Header = "Levels", Name = "Levels" };
            Levels.Items.Add(CreateCheckBoxMenuItem("Trace", true));
            Levels.Items.Add(CreateCheckBoxMenuItem("Debug", true));
            Levels.Items.Add(new Separator());
            Levels.Items.Add(CreateCheckBoxMenuItem("Info", true));
            Levels.Items.Add(CreateCheckBoxMenuItem("Warn", true));
            Levels.Items.Add(new Separator());
            Levels.Items.Add(CreateCheckBoxMenuItem("Error", true));
            Levels.Items.Add(CreateCheckBoxMenuItem("Fatal", true));

            //Columns (两数据集合的交集，就是中否选中)
            MenuItem Columns = new MenuItem() { Header = "Columns" , Name = "Columns" };
            var itemCollection = (from GridViewColumn item in ViewColumns
                                 let boo = view.Columns.IndexOf(item) != -1
                                  //let boo = ((GridView)ListView.View).Columns.IndexOf(item) != -1
                                  select CreateCheckBoxMenuItem(item.Header.ToString(), boo)).ToList();
            //foreach (var item in itemCollection) Columns.Items.Add(item);
            for (int i = 0; i < itemCollection.Count; i ++)
            {
                if (i == 1 || i == 6) Columns.Items.Add(new Separator());
                Columns.Items.Add(itemCollection.ElementAt(i));
            }
            
            //Clear
            MenuItem Clear = new MenuItem() { Header = "Clear All", Name = "Clear" };
            Clear.Click += MenuItem_ClickHandler;

            //ContextMenu
            ContextMenu menu = new ContextMenu();
            menu.Items.Add(Levels);
            menu.Items.Add(new Separator());
            menu.Items.Add(Columns);
            menu.Items.Add(new Separator());
            menu.Items.Add(Clear);

            return menu;
        }

        /// <summary>
        /// 创建简单的 GridViewColumn 对象
        /// </summary>
        /// <param name="header"></param>
        /// <param name="path"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static GridViewColumn CreateColumn(string header, string path, int width = -1)
        {
            GridViewColumn column = new GridViewColumn()
            {
                Header = header,
                DisplayMemberBinding = new Binding(path),
            };
            if (width > 0) column.Width = width;

            return column;
        }


        /// <summary>
        /// 创建简单的 GridViewColumn 对象
        /// </summary>
        /// <param name="header"></param>
        /// <param name="paths"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static GridViewColumn CreateColumn(string header, string[] paths, string parameter)
        {
            MultiBinding Binds = new MultiBinding();
            Binds.ConverterParameter = parameter;
            Binds.Converter = new MultiStringConverter();
            for (int i = 0; i < paths.Length; i++)
                Binds.Bindings.Add(new Binding(paths[i]));

            GridViewColumn column = new GridViewColumn()
            {
                Header = header,
                DisplayMemberBinding = Binds,
            };

            return column;
        }
    }
}



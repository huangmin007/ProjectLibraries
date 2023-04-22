using System;
using log4net.Core;
using log4net.Layout;
using log4net.Appender;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace SpaceCG.Generic
{
    /// <summary>
    /// Log4Net WPF TextBoxBase/TextBox/RichTextBox Appender
    /// </summary>
    internal class TextBoxAppender : AppenderSkeleton
    {
        /// <summary> Backgroud Color 1 </summary>
        public static readonly SolidColorBrush BgColor1 = new SolidColorBrush(Color.FromArgb(0x00, 0xC8, 0xC8, 0xC8));
        /// <summary> Backgroud Color 2 </summary>
        public static readonly SolidColorBrush BgColor2 = new SolidColorBrush(Color.FromArgb(0x60, 0xC8, 0xC8, 0xC8));
        /// <summary> Default Text Color 3 </summary>
        public static readonly SolidColorBrush TextColor = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));

        /// <summary> Info Color </summary>
        public static readonly SolidColorBrush InfoColor = new SolidColorBrush(Color.FromArgb(0x7F, 0xFF, 0xFF, 0xFF));
        /// <summary> Warn Color </summary>
        public static readonly SolidColorBrush WarnColor = new SolidColorBrush(Color.FromArgb(0x7F, 0xFF, 0xFF, 0x00));
        /// <summary> Error Color </summary>
        public static readonly SolidColorBrush ErrorColor = new SolidColorBrush(Color.FromArgb(0x7F, 0xFF, 0x00, 0x00));
        /// <summary> Fatal Color </summary>
        public static readonly SolidColorBrush FatalColor = new SolidColorBrush(Color.FromArgb(0xBF, 0xFF, 0x00, 0x00));

        /// <summary>
        /// 获取或设置最大可见行数
        /// </summary>
        protected uint MaxLines = 512;
        /// <summary> TextBoxBase </summary>
        protected TextBoxBase TextBoxBase;
        /// <summary> TextBox.AppendText Delegate Function </summary>
        protected Action<LoggingEvent> AppendLoggingEventDelegate;

        private TextBox tb;
        private RichTextBox rtb;
        private bool changeBgColor = true;  //切换背景标志变量


        /// <summary>
        /// Log4Net Appender for WPF TextBoxBase(TextBox and RichTextBox)
        /// </summary>
        /// <param name="textBox"></param>
        public TextBoxAppender(TextBoxBase textBox)
        {
            if (textBox == null) throw new ArgumentNullException("参数不能为空");

            this.TextBoxBase = textBox;
            this.TextBoxBase.IsReadOnly = true;
            this.AppendLoggingEventDelegate = AppendLoggingEvent;
            this.Layout = new PatternLayout("[%date{HH:mm:ss}] [%thread] [%5level] [%method(%line)] %logger - %message (%r) %newline");

            DefaultStyle(textBox);
            log4net.Config.BasicConfigurator.Configure(this);
        }

        /// <summary>
        /// 设置控件默认样式
        /// </summary>
        /// <param name="textBox"></param>
        protected void DefaultStyle(TextBoxBase textBox)
        {
            // 属 TextBoxBase 子级，可以使用 is 运算符
            if (textBox is TextBox)
            {
                tb = (TextBox)this.TextBoxBase;
                tb.IsReadOnly = true;
                tb.Foreground = Brushes.Black;
                tb.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            }
            else if (textBox is RichTextBox)
            {
                rtb = (RichTextBox)this.TextBoxBase;
                rtb.IsReadOnly = true;
                rtb.AcceptsReturn = true;
                rtb.Document.LineHeight = 2;
                rtb.Foreground = Brushes.Black;
                rtb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                // ...                
            }
        }

        /// <summary>
        /// Log4Net Appender for WPF TextBoxBase 
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="maxLines">最大行数为 1024 行，默认为 512 行</param>
        public TextBoxAppender(TextBoxBase textBox, uint maxLines):this(textBox)
        {
            this.MaxLines = maxLines > 1024 ? 1024 : maxLines;
        }

        /// <summary>
        /// @override
        /// </summary>
        /// <param name="loggingEvent"></param>
        protected override void Append(LoggingEvent loggingEvent)
        {
            this.TextBoxBase?.Dispatcher.BeginInvoke(this.AppendLoggingEventDelegate, loggingEvent);
        }

        /// <summary>
        /// TextBox Append LoggingEvent
        /// </summary>
        /// <param name="loggingEvent"></param>
        protected void AppendLoggingEvent(LoggingEvent loggingEvent)
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

            //TextBox
            if (tb != null)
            {
                tb.AppendText(text);
                tb.ScrollToEnd();

                if (tb.LineCount > MaxLines)
                    tb.Text = tb.Text.Remove(0, tb.GetCharacterIndexFromLineIndex(1));

                return;
            }
            //RichTextBox
            if (rtb != null)
            {
                Paragraph paragraph = new Paragraph(new Run(text.TrimEnd()));
                paragraph.Background = GetBgColorBrush(loggingEvent.Level, changeBgColor = !changeBgColor);
                
                rtb.Document.Blocks.Add(paragraph);
                rtb.ScrollToEnd();

                if (rtb.Document.Blocks.Count > MaxLines)
                    rtb.Document.Blocks.Remove(rtb.Document.Blocks.FirstBlock);

                return;
            }
        }
        
        /// <summary>
        /// 跟据 Level 获取颜色
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static SolidColorBrush GetColorBrush(Level level)
        {
            return level == Level.Fatal ? FatalColor : level == Level.Error ? ErrorColor : level == Level.Warn ? WarnColor : InfoColor;
        }
        /// <summary>
        /// 跟据 Level and Line 获取背景颜色
        /// </summary>
        /// <param name="level"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static SolidColorBrush GetBgColorBrush(Level level, int line)
        {
            if (level == Level.Fatal) return FatalColor;
            else if (level == Level.Error) return ErrorColor;
            else if (level == Level.Warn) return WarnColor;
            else return line % 2 == 0 ? BgColor1 : BgColor2;
        }
        /// <summary>
        /// 跟据 Level and Change 获取背景颜色
        /// </summary>
        /// <param name="level"></param>
        /// <param name="change"></param>
        /// <returns></returns>
        public static SolidColorBrush GetBgColorBrush(Level level, bool change)
        {
            if (level == Level.Fatal) return FatalColor;
            else if (level == Level.Error) return ErrorColor;
            else if (level == Level.Warn) return WarnColor;
            else return change ? BgColor1 : BgColor2;
        }
        
    }
}

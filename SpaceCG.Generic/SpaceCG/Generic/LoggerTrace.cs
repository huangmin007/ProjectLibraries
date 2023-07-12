using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using SpaceCG.Extensions;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 轻量日志跟踪记录对象，支持跨平台
    /// </summary>
    public sealed class LoggerTrace : IDisposable
    {
        private static readonly string MainModuleName = "trace";
        private static readonly TextFileStreamTraceListener ConsoleListener;
        private static readonly TextFileStreamTraceListener TextFileListener;

        /// <summary>
        /// 默认文本文件记录源开关和事件类型筛选器筛选的跟踪消息的级别
        /// <para>文本文件记录(日志)的跟踪消息的级别</para>
        /// </summary>
        public static SourceLevels FileTraceLevels
        {
            get { return ((EventTypeFilter)TextFileListener.Filter).EventType; }
            set { ((EventTypeFilter)TextFileListener.Filter).EventType = value; }
        }
        /// <summary>
        /// 默认文本文件记录源跟踪事件
        /// </summary>
        public static event EventHandler<TraceEventArgs> FileTraceEvent;

        /// <summary>
        /// 默认控制台源开关和事件类型筛选器筛选的跟踪消息的级别
        /// <para>控制台记录的跟踪消息的级别</para>
        /// </summary>
        public static SourceLevels ConsoleTraceLevels
        {
            get { return ((EventTypeFilter)ConsoleListener.Filter).EventType; }
            set { ((EventTypeFilter)ConsoleListener.Filter).EventType = value; }
        }
        /// <summary>
        /// 默认控制台消息源跟踪事件
        /// </summary>
        public static event EventHandler<TraceEventArgs> ConsoleTraceEvent;

        /// <summary>
        /// <see cref="LoggerTrace"/> 静态构造函数
        /// </summary>
        static LoggerTrace()
        {
            ProcessModule processModule = Process.GetCurrentProcess().MainModule;
            string moduleFileName = processModule?.FileName;
            if (!String.IsNullOrEmpty(moduleFileName))
            {
                FileInfo info = new FileInfo(moduleFileName);
                MainModuleName = info.Name.Replace(info.Extension, "");
            }

            string path = "logs";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            String defaultFileName = $"{path}/{MainModuleName}.{DateTime.Today.ToString("yyyy-MM-dd")}.log";

            Trace.AutoFlush = true;
            TextFileListener = new TextFileStreamTraceListener(defaultFileName, "FileTrace");
            TextFileListener.Filter = new EventTypeFilter(SourceLevels.Information);
            TextFileListener.TraceSourceEvent += (s, e) => FileTraceEvent?.Invoke(s, e);
            TextFileListener.WriteLine(Environment.NewLine);

            ConsoleListener = new TextFileStreamTraceListener(Console.Out, "ConsoleTrace");
            ConsoleListener.Filter = new EventTypeFilter(SourceLevels.All);
            ConsoleListener.TraceSourceEvent += (s, e) => ConsoleTraceEvent?.Invoke(s, e);

            OperatingSystem os = Environment.OSVersion;
            TraceEventCache eventCache = new TraceEventCache();

            ConsoleListener.WriteLine($"[Header] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
            TextFileListener.WriteLine($"[Header] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

            String systemInfo = $"[{os} ({os.Platform})]({(Environment.Is64BitOperatingSystem ? "64" : "32")} 位操作系统 / 逻辑处理器: {Environment.ProcessorCount})";
            String moduleInfo = $"[{moduleFileName}]({(Environment.Is64BitProcess ? "64" : "32")} 位进程 / 进程 ID: {Process.GetCurrentProcess().Id})";

            ConsoleListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, TraceEventType.Information, 0, systemInfo);
            ConsoleListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, TraceEventType.Information, 0, moduleInfo);

            TextFileListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, TraceEventType.Information, 0, systemInfo);
            TextFileListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, TraceEventType.Information, 0, moduleInfo);

            if (processModule != null)
            {
                ConsoleListener.Write(processModule.FileVersionInfo.ToString());
                TextFileListener.Write(processModule.FileVersionInfo.ToString());
            }
            
            ConsoleListener.Flush();
            TextFileListener.Flush();
            FileExtensions.ReserveFileDays(30, path, $"{MainModuleName}.*.log");

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;

            ConsoleListener.WriteLine($"[Footer] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
            TextFileListener.WriteLine($"[Footer] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

            ConsoleListener.Flush();
            TextFileListener.Flush();
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            TraceEventCache eventCache = new TraceEventCache();
            TraceEventType eventType = e.IsTerminating ? TraceEventType.Critical : TraceEventType.Error;

            TextFileListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, eventType, 0, $"公共语言运行时是否即将终止: {e.IsTerminating}");
            TextFileListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, eventType, 0, $"未处理的异常对象: {e.ExceptionObject}");
            TextFileListener.Flush();

            ConsoleListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, eventType, 0, $"公共语言运行时是否即将终止: {e.IsTerminating}");
            ConsoleListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, eventType, 0, $"未处理的异常对象: {e.ExceptionObject}");
            ConsoleListener.Flush();

            if (e.IsTerminating)
            {
                AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;
                Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException((Exception)e.ExceptionObject));
            }
        }
        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            TraceEventCache eventCache = new TraceEventCache();
            ConsoleListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, TraceEventType.Warning, 0, $"CurrentDomain_FirstChanceException: {e.Exception}");
            TextFileListener.TraceEvent(eventCache, AppDomain.CurrentDomain.FriendlyName, TraceEventType.Warning, 0, $"CurrentDomain_FirstChanceException: {e.Exception}");

            ConsoleListener.Flush();
            TextFileListener.Flush();
        }
        

        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 跟踪代码的执行并将跟踪消息的源对象
        /// </summary>
        private TraceSource TraceSource;
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源开关和事件类型筛选器筛选的跟踪消息的级别
        /// </summary>
        public SourceLevels SourceLevels
        {
            get { return TraceSource.Switch.Level; }
            set { TraceSource.Switch.Level = value; }
        }

        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是否大于 <see cref="SourceLevels.Verbose"/> 级别
        /// </summary>
        public bool IsDebugEnabled => SourceLevels != SourceLevels.Off && SourceLevels <= SourceLevels.Verbose;
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是否大于 <see cref="SourceLevels.Information"/> 级别
        /// </summary>
        public bool IsInfoEnabled => SourceLevels != SourceLevels.Off && SourceLevels <= SourceLevels.Information;
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是否大于 <see cref="SourceLevels.Warning"/> 级别
        /// </summary>
        public bool IsWarnEnabled => SourceLevels != SourceLevels.Off && SourceLevels <= SourceLevels.Warning;
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是否大于 <see cref="SourceLevels.Error"/> 级别
        /// </summary>
        public bool IsErrorEnabled => SourceLevels != SourceLevels.Off && SourceLevels <= SourceLevels.Error;
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是否大于 <see cref="SourceLevels.Critical"/> 级别
        /// </summary>
        public bool IsFatalEnabled => SourceLevels != SourceLevels.Off && SourceLevels <= SourceLevels.Critical;

        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <exception cref="Exception"></exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public LoggerTrace()
        {
            Initialize(null, SourceLevels.All);
        }
        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="name"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public LoggerTrace(String name)
        {
            Initialize(name, SourceLevels.All);
        }
        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="defaultLevel"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public LoggerTrace(SourceLevels defaultLevel)
        {
            Initialize(null, defaultLevel);
        }
        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultLevel"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public LoggerTrace(String name, SourceLevels defaultLevel)
        {
            Initialize(name, defaultLevel);
        }

        /// <summary>
        /// Initialize Trace Source
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultLevel"></param>
        /// <exception cref="Exception"></exception>
        private void Initialize(String name, SourceLevels defaultLevel)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                try
                {
                    StackFrame frame = new StackFrame(2, false);
                    MethodBase method = frame.GetMethod();
                    if (method != null)
                    {
                        Type declaringType = method.DeclaringType;
                        if (declaringType != null) name = declaringType.Name;
                    }
                }
                catch (Exception)
                {
                    name = Process.GetCurrentProcess().MainModule?.FileName;
                }
            }

            TraceSource = new TraceSource(name, defaultLevel);
            TraceSource.Switch = new SourceSwitch($"{name}_Switch", defaultLevel.ToString());
            TraceSource.Switch.Level = defaultLevel;

            if (ConsoleListener != null) TraceSource.Listeners.Add(ConsoleListener);
            if (TextFileListener != null) TraceSource.Listeners.Add(TextFileListener);
            if (ConsoleListener != null && TextFileListener != null) TraceSource.Listeners.Remove("Default");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (TraceSource != null)
            {
                TraceSource.Flush();
                TraceSource.Listeners.Clear();
                TraceSource = null;
            }
        }

        /// <summary>
        /// 使用指定的事件类型、事件标识符和消息，将跟踪事件消息写入 <see cref="TraceSource.Listeners"/> 集合中的跟踪侦听器中。
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="data"></param>
        public void TraceEvent(TraceEventType eventType, object data) => TraceSource?.TraceEvent(eventType, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
        /// <summary>
        /// 使用指定的事件类型、事件标识符和消息，将跟踪事件消息写入 <see cref="TraceSource.Listeners"/> 集合中的跟踪侦听器中。
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void TraceEvent(TraceEventType eventType, string format, params object[] args) => TraceSource?.TraceEvent(eventType, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(args.Length == 0 ? format : string.Format(format, args)));

        /// <summary>
        /// 使用指定的事件类型、事件标识符和跟踪数据，将跟踪数据写入 <see cref="TraceSource.Listeners"/> 集合中的跟踪侦听器中。
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="data"></param>
        public void TraceData(TraceEventType eventType, object data) => TraceSource?.TraceData(eventType, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
        /// <summary>
        /// 使用指定的事件类型、事件标识符和跟踪数据，将跟踪数据写入 <see cref="TraceSource.Listeners"/> 集合中的跟踪侦听器中。
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void TraceData(TraceEventType eventType, string format, params object[] args) => TraceSource?.TraceData(eventType, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(args.Length == 0 ? format : string.Format(format, args)));

        /// <summary>
        /// 跟踪调试信息
        /// </summary>
        /// <param name="data"></param>
        public void Debug(object data) => TraceSource?.TraceEvent(TraceEventType.Verbose, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
        /// <summary>
        /// 跟踪调试性消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Debug(string format, params object[] args) => TraceSource?.TraceEvent(TraceEventType.Verbose, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(args.Length == 0 ? format : string.Format(format, args)));
        /// <summary>
        /// 跟踪信息性消息
        /// </summary>
        /// <param name="data"></param>
        public void Info(object data) => TraceSource?.TraceEvent(TraceEventType.Information, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
        /// <summary>
        /// 跟踪信息性消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Info(string format, params object[] args) => TraceSource?.TraceEvent(TraceEventType.Information, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(args.Length == 0 ? format : string.Format(format, args)));
        /// <summary>
        /// 跟踪非严重问题消息
        /// </summary>
        /// <param name="data"></param>
        public void Warn(object data) => TraceSource?.TraceEvent(TraceEventType.Warning, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
        /// <summary>
        /// 跟踪非严重问题消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Warn(string format, params object[] args) => TraceSource?.TraceEvent(TraceEventType.Warning, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(args.Length == 0 ? format : string.Format(format, args)));
        /// <summary>
        /// 跟踪可恢复的错误消息
        /// </summary>
        /// <param name="data"></param>
        public void Error(object data) => TraceSource?.TraceEvent(TraceEventType.Error, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
        /// <summary>
        /// 跟踪可恢复的错误消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Error(string format, params object[] args) => TraceSource?.TraceEvent(TraceEventType.Error, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(args.Length == 0 ? format : string.Format(format, args)));
        /// <summary>
        /// 跟踪致命错误或应用程序崩溃消息
        /// </summary>
        /// <param name="data"></param>
        public void Fatal(object data) => TraceSource?.TraceEvent(TraceEventType.Critical, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
        /// <summary>
        /// 跟踪致命错误或应用程序崩溃消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Fatal(string format, params object[] args) => TraceSource?.TraceEvent(TraceEventType.Critical, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(args.Length == 0 ? format : string.Format(format, args)));
        
        /// <summary>
        /// 刷新跟踪侦听器集合中的所有跟踪侦听器
        /// </summary>
        public void Flush() => TraceSource?.Flush();

        /// <summary>
        /// format stack frame message
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string FormatStackMessage(object data)
        {
            try
            {
                StackFrame frame = new StackFrame(2, false);
                MethodBase method = frame.GetMethod();

                return $"({(method != null ? method.Name : "UnknownMethod")}) {data}";
            }
            catch (Exception)
            {
                return $"(ExceptionMethod) {data}";
            }
        }
    }

    /// <summary>
    /// 文本/文件/流跟踪或调试定向输出  <br/><br/>
    /// <inheritdoc/>
    /// </summary>
    public class TextFileStreamTraceListener : System.Diagnostics.TextWriterTraceListener
    {
        /// <summary>
        /// 单个文件的最大大小
        /// </summary>
        protected const long FILE_MAX_SIZE = 1024 * 1024 * 2;
        /// <summary>
        /// 跟踪事件 <see cref="TRACE_TARGET_COUNT"/> 次数后检测一次文件，减少频繁的检查文件大小
        /// </summary>
        protected const int TRACE_TARGET_COUNT = 16;

        /// <summary>
        /// 当前跟踪次数
        /// </summary>
        private int CurrentTreceCount = 0;
        /// <summary>
        /// 跟踪事件格式化消息容器 <see cref="EventFormatMessage"/> <see cref="TraceEventArgs.FormatMessage"/>
        /// </summary>
        protected StringBuilder EventFormatMessage { get; private set; } = new StringBuilder(1024);

        /// <summary>
        /// 跟踪源事件
        /// </summary>
        public event EventHandler<TraceEventArgs> TraceSourceEvent;

        #region Constructors
        /// <inheritdoc/>
        public TextFileStreamTraceListener(Stream stream) : base(stream)
        {
        }
        /// <inheritdoc/>
        public TextFileStreamTraceListener(Stream stream, string name) : base(stream, name)
        {
        }
        /// <inheritdoc/>
        public TextFileStreamTraceListener(TextWriter writer) : base(writer)
        {
        }
        /// <inheritdoc/>
        public TextFileStreamTraceListener(TextWriter writer, string name) : base(writer, name)
        {
        }
        /// <inheritdoc/>
        public TextFileStreamTraceListener(string fileName) : base(fileName)
        {
        }
        /// <inheritdoc/>
        public TextFileStreamTraceListener(string fileName, string name) : base(fileName, name)
        {
        }
        #endregion

        /// <summary>
        /// 跟据事件类型返回 控制台颜色
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ConsoleColor GetConsoleColor(TraceEventType eventType)
        {
            return eventType == TraceEventType.Verbose ? ConsoleColor.Green :
                eventType == TraceEventType.Information ? ConsoleColor.Gray :
                eventType == TraceEventType.Warning ? ConsoleColor.Yellow :
                eventType == TraceEventType.Error ? ConsoleColor.Red :
                eventType == TraceEventType.Critical ? ConsoleColor.DarkRed :
                eventType == TraceEventType.Start ? ConsoleColor.Cyan :
                eventType == TraceEventType.Stop ? ConsoleColor.Cyan :
                eventType == TraceEventType.Suspend ? ConsoleColor.Magenta :
                eventType == TraceEventType.Resume ? ConsoleColor.Magenta :
                eventType == TraceEventType.Transfer ? ConsoleColor.DarkYellow : 
                ConsoleColor.Gray;
        }
        /// <summary>
        /// 获取事件类型字符
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetEventTypeChars(TraceEventType eventType)
        {
            return eventType == TraceEventType.Verbose ? "DEBUG" :
                eventType == TraceEventType.Information ? "INFO" :
                eventType == TraceEventType.Warning ? "WARN" :
                eventType == TraceEventType.Error ? "ERROR" :
                eventType == TraceEventType.Critical ? "FATAL" :
                eventType == TraceEventType.Start ? "START" :
                eventType == TraceEventType.Stop ? "STOP" :
                eventType == TraceEventType.Suspend ? "SUSPEND" :
                eventType == TraceEventType.Resume ? "RESUME" :
                eventType == TraceEventType.Transfer ? "TRANSFER" :
                "UNKNOW";
        }

        /// <inheritdoc/>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                Console.ForegroundColor = GetConsoleColor(eventType);

                WriteHeader(source, eventType, id);
                WriteMessage(message);
                WriteFooter(eventCache);

                Console.ResetColor();
                TraceSourceEventInvoke(new TraceEventArgs(eventType, source, id, message, null));
            }
        }
        /// <inheritdoc/>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                Console.ForegroundColor = GetConsoleColor(eventType);
                string message = args.Length <= 0 ? format : string.Format(format, args);

                WriteHeader(source, eventType, id);
                WriteMessage(message);
                WriteFooter(eventCache);

                Console.ResetColor();
                TraceSourceEventInvoke(new TraceEventArgs(eventType, source, id, message, null));
            }
        }

        /// <inheritdoc/>
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
            {
                Console.ForegroundColor = GetConsoleColor(eventType);
                string message = data != null ? data.ToString() : string.Empty;

                WriteHeader(source, eventType, id);
                WriteMessage(message);
                WriteFooter(eventCache);

                Console.ResetColor();
                TraceSourceEventInvoke(new TraceEventArgs(eventType, source, id, message, new object[] { data }));
            }
        }
        /// <inheritdoc/>
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data)) return;

            Console.ForegroundColor = GetConsoleColor(eventType);
            StringBuilder messageBuilder = new StringBuilder();
            if (data.Length > 0)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (i != 0) messageBuilder.Append(", ");
                    if (data[i] != null) messageBuilder.Append(data[i].ToString());
                }
            }

            WriteHeader(source, eventType, id);
            WriteMessage(messageBuilder.ToString());
            WriteFooter(eventCache);

            Console.ResetColor();
            TraceSourceEventInvoke(new TraceEventArgs(eventType, source, id, messageBuilder.ToString(), data));
        }

        #region Protected Write Header/Message/Footer
        /// <summary>
        /// Write Header <see cref="TextWriterTraceListener.Write(string)"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventType"></param>
        /// <param name="id"></param>
        protected void WriteHeader(string source, TraceEventType eventType, int id)
        {
            string eventName = GetEventTypeChars(eventType); //eventType.ToString().ToUpper();
            eventName = eventName.Length >= 5 ? eventName.Substring(0, 5) : eventName.PadLeft(5, ' ');
            string formatMessage = string.Format("[{0}] [{1}] [{2}] [{3}]: ", DateTime.Now.ToString("HH:mm:ss.fff"), eventName, id.ToString().PadLeft(2, ' '), source);

            Write(formatMessage);
            EventFormatMessage.Append(formatMessage);
        }
        /// <summary>
        /// Write Message <see cref="TextWriterTraceListener.WriteLine(string)"/>
        /// </summary>
        /// <param name="message"></param>
        protected void WriteMessage(string message)
        {
            WriteLine(message);
            EventFormatMessage.AppendLine(message);
        }
        /// <summary>
        /// Write Footer
        /// </summary>
        /// <param name="eventCache"></param>
        protected void WriteFooter(TraceEventCache eventCache)
        {
            if (eventCache == null) return;

            string message = "";
            string indent = "\t";

            if ((TraceOptions.ThreadId & TraceOutputOptions) != 0)
            {
                message = $"{indent}ThreadId={eventCache.ThreadId}";
                WriteLine(message);
                EventFormatMessage.AppendLine(message);
            }

            if ((TraceOptions.ProcessId & TraceOutputOptions) != 0)
            {
                message = $"{indent}ProcessId={eventCache.ProcessId}";
                WriteLine(message);
                EventFormatMessage.AppendLine(message);
            }

            if ((TraceOptions.DateTime & TraceOutputOptions) != 0)
            {
                message = $"{indent}DateTime={eventCache.DateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff")}";
                WriteLine(message);
                EventFormatMessage.AppendLine(message);
            }

            if ((TraceOptions.Timestamp & TraceOutputOptions) != 0)
            {
                message = $"{indent}Timestamp={eventCache.Timestamp}";
                WriteLine(message);
                EventFormatMessage.AppendLine(message);
            }

            if ((TraceOptions.Callstack & TraceOutputOptions) != 0)
            {
                message = $"{indent}Callstack={eventCache.Callstack}";
                WriteLine(message);
                EventFormatMessage.AppendLine(message);
            }

            if ((TraceOptions.LogicalOperationStack & TraceOutputOptions) != 0)
            {
                bool flag = true;
                StringBuilder messageBuilder = new StringBuilder();
                messageBuilder.Append($"{indent}LogicalOperationStack=");
                Stack logicalOperationStack = eventCache.LogicalOperationStack;

                foreach (object item in logicalOperationStack)
                {
                    if (!flag)
                    {
                        messageBuilder.Append(", ");
                    }
                    else
                    {
                        flag = false;
                    }
                    messageBuilder.Append(item.ToString());
                }

                WriteLine(messageBuilder.ToString());
                EventFormatMessage.AppendLine(messageBuilder.ToString());
            }
        }
        #endregion

        /// <summary>
        /// 触发 <see cref="TraceSourceEvent"/> 事件，以及检查文件大小
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void TraceSourceEventInvoke(TraceEventArgs eventArgs)
        {
            eventArgs.FormatMessage = EventFormatMessage.ToString();
            TraceSourceEvent?.Invoke(this, eventArgs);

            CurrentTreceCount++;
            EventFormatMessage.Clear();

            if (CurrentTreceCount == TRACE_TARGET_COUNT)
            {
                CheckFileSize();
                CurrentTreceCount = 0;
            }
        }

        /// <summary>
        /// 检查文件大小
        /// </summary>
        protected void CheckFileSize()
        {
            if (Writer == null) return;

            StreamWriter writer = Writer as StreamWriter;
            if (writer?.BaseStream == null || writer.BaseStream.Length < FILE_MAX_SIZE) return;

            FileStream fileStream = writer.BaseStream as FileStream;
            if (fileStream == null) return;

            try
            {
                FileInfo curLogFile = new FileInfo(fileStream.Name);
                string headName = curLogFile.Name.Substring(0, curLogFile.Name.IndexOf('.'));
                string fileName = curLogFile.Name.Substring(0, curLogFile.Name.Length - curLogFile.Extension.Length + 1); //fileName.(0).log
                int count = (int)(curLogFile.Directory.GetFiles($"{fileName}*", SearchOption.TopDirectoryOnly)?.Length) - 1;

                //Console.WriteLine(curLogFile.Name);                             //Test2.2023-06-24.log
                //Console.WriteLine($"HeadName:{headName}  FileName:{fileName}"); //HeadName:Test2  FileName:Test2.2023-06-24.

                String sourceFileName = curLogFile.FullName;
                string destFileName = $"{curLogFile.Directory.FullName}\\{fileName}({count}){curLogFile.Extension}";

                Writer.Flush();
                Writer.Close();
                Writer.Dispose();
                Writer = null;

                File.Move(sourceFileName, destFileName);
                FileExtensions.ReserveFileDays(30, curLogFile.DirectoryName, $"{headName}*{curLogFile.Extension}");

#if false
                //解决过 24点 后文件名上的日期问题，但无法彻底解决，因为父类的属性 fileName 是私有的, 得继承 TraceListener 重写才可行
                Encoding encoding = new UTF8Encoding(false);
                encoding.EncoderFallback = EncoderFallback.ReplacementFallback;
                encoding.DecoderFallback = DecoderFallback.ReplacementFallback;
                string path = $"{curLogFile.Directory.FullName}\\{headName}.{DateTime.Today.ToString("yyyy-MM-dd")}{curLogFile.Extension}";
                Writer = new StreamWriter(path, true, encoding, 4096);
#endif
            }
            catch (Exception ex)
            {
                WriteLine($"备份日志文件失败：{fileStream.Name}");
                WriteLine(ex.ToString());
                Flush();
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            TraceSourceEvent = null;
            EventFormatMessage?.Clear();

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 跟踪事件的参数
    /// </summary>
    public class TraceEventArgs : EventArgs
    {
        /// <summary>
        /// 跟踪事件的类型
        /// </summary>
        public TraceEventType EventType { get; internal set; }

        /// <summary>
        /// 事件的数值标识符
        /// </summary>
        public int ID { get; internal set; }

        /// <summary>
        /// 跟踪源
        /// </summary>
        public string Source { get; internal set; }

        /// <summary>
        /// 跟踪事件的消息
        /// </summary>
        public string Message { get; internal set; }

        /// <summary>
        /// 跟踪事件的数据
        /// </summary>
        public object[] Data { get; internal set; }

        /// <summary>
        /// 跟踪事件的时间
        /// </summary>
        public DateTime DateTime { get; internal set; }

        /// <summary>
        /// 跟踪事件的格式化消息
        /// </summary>
        public string FormatMessage { get; internal set; }

        /// <summary>
        /// 跟踪事件的参数
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="source"></param>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <param name="formatMessage"></param>
        public TraceEventArgs(TraceEventType eventType, string source, int id, string message, object[] data = null, string formatMessage = null)
        {
            this.ID = id;
            this.Data = data;
            this.Source = source;
            this.Message = message;
            this.EventType = eventType;
            this.DateTime = DateTime.Now;
            this.FormatMessage = formatMessage;
        }
    }

}

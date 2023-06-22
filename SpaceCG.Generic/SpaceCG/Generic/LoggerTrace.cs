using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using SpaceCG.Extensions;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 轻量的日志跟踪记录对象，支持跨平台
    /// </summary>
    public sealed class LoggerTrace : IDisposable
    {
        private static readonly string mainModuleName = "trace";
        private static readonly TextWriterTraceListener consoleListener;
        private static readonly TextWriterTraceListener textFileListener;

        /// <summary>
        /// 全局 <see cref="FTextWriterTraceListener"/> 源开关和事件类型筛选器筛选的跟踪消息的级别
        /// <para>文本文件(日志)记录的跟踪消息的级别</para>
        /// </summary>
        public static SourceLevels FileTraceEventType
        {
            get { return ((EventTypeFilter)textFileListener.Filter).EventType; }
            set { ((EventTypeFilter)textFileListener.Filter).EventType = value; }
        }

        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 跟踪代码的执行并将跟踪消息的源对象
        /// </summary>
        public TraceSource TraceSource { get; private set; }
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源开关和事件类型筛选器筛选的跟踪消息的级别
        /// </summary>
        public SourceLevels EventType
        {
            get { return TraceSource.Switch.Level; }
            set { TraceSource.Switch.Level = value; }
        }

        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是大于 <see cref="SourceLevels.Verbose"/> 级别
        /// </summary>
        public bool IsDebugEnabled => EventType != SourceLevels.Off && EventType <= SourceLevels.Verbose;
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是大于 <see cref="SourceLevels.Information"/> 级别
        /// </summary>
        public bool IsInfoEnabled => EventType != SourceLevels.Off && EventType <= SourceLevels.Information;
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是大于 <see cref="SourceLevels.Warning"/> 级别
        /// </summary>
        public bool IsWarnEnabled => EventType != SourceLevels.Off && EventType <= SourceLevels.Warning;
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是大于 <see cref="SourceLevels.Error"/> 级别
        /// </summary>
        public bool IsErrorEnabled => EventType != SourceLevels.Off && EventType <= SourceLevels.Error;
        /// <summary>
        /// 当前 <see cref="LoggerTrace"/> 源筛选的跟踪消息是大于 <see cref="SourceLevels.Critical"/> 级别
        /// </summary>
        public bool IsFatalEnabled => EventType != SourceLevels.Off && EventType <= SourceLevels.Critical;


        static LoggerTrace()
        {
            ProcessModule processModule = Process.GetCurrentProcess().MainModule;
            string moduleFileName = processModule?.FileName;
            if (!String.IsNullOrEmpty(moduleFileName))
            {
                FileInfo info = new FileInfo(moduleFileName);
                mainModuleName = info.Name.Replace(info.Extension, "");
            }

            string path = "logs";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            String defaultFileName = $"{path}/{mainModuleName}.{DateTime.Today.ToString("yyyy-MM-dd")}.log";

            textFileListener = new FTextWriterTraceListener(defaultFileName, "FileTrace");
            textFileListener.Filter = new EventTypeFilter(SourceLevels.Information);

            consoleListener = new FTextWriterTraceListener(Console.Out, "ConsoleTrace");
            consoleListener.Filter = new EventTypeFilter(SourceLevels.All);

            OperatingSystem os = Environment.OSVersion;
            String systemInfo = $"{Environment.NewLine}[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] [{os} ({os.Platform})]({(Environment.Is64BitOperatingSystem ? "64" : "32")} 位操作系统 / 逻辑处理器: {Environment.ProcessorCount})";
            String moduleInfo = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] [{moduleFileName}]({(Environment.Is64BitProcess ? "64" : "32")} 位进程 / 进程 ID: {Process.GetCurrentProcess().Id})";
            
            consoleListener.WriteLine(systemInfo);
            textFileListener.WriteLine(systemInfo);

            consoleListener.WriteLine(moduleInfo);
            textFileListener.WriteLine(moduleInfo);

            if(processModule != null)
            {
                consoleListener.Write(processModule.FileVersionInfo.ToString());
                textFileListener.Write(processModule.FileVersionInfo.ToString());
            }

            consoleListener.Flush();
            textFileListener.Flush();
            FileExtensions.ReserveFileDays(30, path, $"{mainModuleName}.*.log");
        }

        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <exception cref="Exception"></exception>
        public LoggerTrace()
        {
            Initialize(null, SourceLevels.All);
        }
        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="name"></param>
        public LoggerTrace(String name)
        {
            Initialize(name, SourceLevels.All);
        }
        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="defaultLevel"></param>
        public LoggerTrace(SourceLevels defaultLevel)
        {
            Initialize(null, defaultLevel);
        }
        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultLevel"></param>
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
            if(String.IsNullOrWhiteSpace(name))
            {
                StackFrame frame = new StackFrame(2, false);
                MethodBase method = frame.GetMethod();
                if (method == null) throw new Exception("获取在其中执行帧的方法失败");

                Type declaringType = method.DeclaringType;
                if (declaringType == null) throw new Exception("获取声明该成员的类失败");

                name = declaringType.Name;
            }

            TraceSource = new TraceSource(name, defaultLevel);
            TraceSource.Switch = new SourceSwitch($"{name}_Switch", defaultLevel.ToString());
            TraceSource.Switch.Level = defaultLevel;

            if (consoleListener != null) TraceSource.Listeners.Add(consoleListener);
            if (textFileListener != null) TraceSource.Listeners.Add(textFileListener);

            if (consoleListener != null && textFileListener != null) TraceSource.Listeners.Remove("Default");
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
        public void TraceEvent(TraceEventType eventType, object data)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(eventType, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
            TraceSource.Flush();
        }
        /// <summary>
        /// 使用指定的事件类型、事件标识符和消息，将跟踪事件消息写入 <see cref="TraceSource.Listeners"/> 集合中的跟踪侦听器中。
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void TraceEvent(TraceEventType eventType, string format, params object[] args)// => TraceEvent(eventType, string.Format(format, args));
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(eventType, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(string.Format(format, args)));
            TraceSource.Flush();
        }

        /// <summary>
        /// 使用指定的事件类型、事件标识符和跟踪数据，将跟踪数据写入 <see cref="TraceSource.Listeners"/> 集合中的跟踪侦听器中。
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="data"></param>
        public void TraceData(TraceEventType eventType, object data)
        {
            if (TraceSource == null) return;

            TraceSource.TraceData(eventType, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
            TraceSource.Flush();
        }
        /// <summary>
        /// 使用指定的事件类型、事件标识符和跟踪数据，将跟踪数据写入 <see cref="TraceSource.Listeners"/> 集合中的跟踪侦听器中。
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void TraceData(TraceEventType eventType, string format, params object[] args)
        {
            if (TraceSource == null) return;

            TraceSource.TraceData(eventType, Thread.CurrentThread.ManagedThreadId, FormatStackMessage((string.Format(format, args))));
            TraceSource.Flush();
        }

        /// <summary>
        /// 跟踪调试信息
        /// </summary>
        /// <param name="data"></param>
        public void Debug(object data)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Verbose, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
            TraceSource.Flush();
        }
        /// <summary>
        /// 跟踪调试性消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Debug(string format, params object[] args)// => Debug(string.Format(format, args));
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Verbose, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(string.Format(format, args)));
            TraceSource.Flush();
        }
        /// <summary>
        /// 跟踪信息性消息
        /// </summary>
        /// <param name="data"></param>
        public void Info(object data)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Information, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
            TraceSource.Flush();
        }
        /// <summary>
        /// 跟踪信息性消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Info(string format, params object[] args) // => Info(string.Format(format, args));
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Information, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(string.Format(format, args)));
            TraceSource.Flush();
        }
        /// <summary>
        /// 跟踪非严重问题消息
        /// </summary>
        /// <param name="data"></param>
        public void Warn(object data)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Warning, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
            TraceSource.Flush();
        }
        /// <summary>
        /// 跟踪非严重问题消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Warn(string format, params object[] args)// => Warn(string.Format(format, args));
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Warning, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(string.Format(format, args)));
            TraceSource.Flush();
        }
        /// <summary>
        /// 跟踪可恢复的错误消息
        /// </summary>
        /// <param name="data"></param>
        public void Error(object data)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Error, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
            TraceSource.Flush();
        }
        /// <summary>
        /// 跟踪可恢复的错误消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Error(string format, params object[] args)// => Error(string.Format(format, args));
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Error, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(string.Format(format, args)));
            TraceSource.Flush();
        }
        /// <summary>
        /// 跟踪致命错误或应用程序崩溃消息
        /// </summary>
        /// <param name="data"></param>
        public void Fatal(object data)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Critical, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(data));
            TraceSource.Flush();
        }
        /// <summary>
        /// 跟踪致命错误或应用程序崩溃消息
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Fatal(string format, params object[] args)// => Fatal(string.Format(format, args));
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Critical, Thread.CurrentThread.ManagedThreadId, FormatStackMessage(string.Format(format, args)));
            TraceSource.Flush();
        }

        /// <summary>
        /// format stack frame message
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string FormatStackMessage(object data)
        {
            StackFrame frame = new StackFrame(2, false);
            MethodBase method = frame.GetMethod();

            return method != null ? $"({method.Name}) {data}" : $"{data}";
        }
    }

    /// <inheritdoc/>
    public class FTextWriterTraceListener : System.Diagnostics.TextWriterTraceListener
    {
        /// <summary>
        /// Write 事件
        /// </summary>
        public event EventHandler<WriteEventArgs> WriteEvent;

        /// <inheritdoc/>
        public FTextWriterTraceListener(Stream stream) : base(stream)
        {
        }
        /// <inheritdoc/>
        public FTextWriterTraceListener(Stream stream, string name) : base(stream, name)
        {
        }
        /// <inheritdoc/>
        public FTextWriterTraceListener(TextWriter writer) : base(writer)
        {
        }
        /// <inheritdoc/>
        public FTextWriterTraceListener(TextWriter writer, string name) : base(writer, name)
        {
        }
        /// <inheritdoc/>
        public FTextWriterTraceListener(string fileName) : base(fileName)
        {
        }
        /// <inheritdoc/>
        public FTextWriterTraceListener(string fileName, string name) : base(fileName, name)
        {
        }

        /// <inheritdoc/>
        public override void Write(string message)
        {
            base.Write(message);
            WriteEvent?.Invoke(this, new WriteEventArgs(message));

            CheckFileSize();
        }
        /// <inheritdoc/>
        public override void WriteLine(string message)
        {
            base.WriteLine(message);
            WriteEvent?.Invoke(this, new WriteEventArgs(message + Environment.NewLine));

            CheckFileSize();
        }

        /// <inheritdoc/>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                WriteHeader(source, eventType, id);
                WriteLine(message);
                WriteFooter(eventCache);
            }
        }
        /// <inheritdoc/>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                WriteHeader(source, eventType, id);
                if (args != null)
                {
                    WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
                }
                else
                {
                    WriteLine(format);
                }

                WriteFooter(eventCache);
            }
        }
        /// <inheritdoc/>
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
            {
                return;
            }

            WriteHeader(source, eventType, id);
            StringBuilder stringBuilder = new StringBuilder();
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (i != 0)
                    {
                        stringBuilder.Append(", ");
                    }

                    if (data[i] != null)
                    {
                        stringBuilder.Append(data[i].ToString());
                    }
                }
            }

            WriteLine(stringBuilder.ToString());
            WriteFooter(eventCache);
        }
        /// <inheritdoc/>
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            if (Filter == null || Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
            {
                WriteHeader(source, eventType, id);
                string message = string.Empty;
                if (data != null)
                {
                    message = data.ToString();
                }

                WriteLine(message);
                WriteFooter(eventCache);
            }
        }
        /// <summary>
        /// Write Header
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventType"></param>
        /// <param name="id"></param>
        protected void WriteHeader(string source, TraceEventType eventType, int id)
        {
            Write(string.Format("[{0}] [{1}] [{2}] ({3}) : ", DateTime.Now.ToString("HH:mm:ss.fff"),
                  eventType.ToString().Substring(0, 5).ToUpper(), source, id.ToString()));
        }
        /// <summary>
        /// Write Footer
        /// </summary>
        /// <param name="eventCache"></param>
        protected void WriteFooter(TraceEventCache eventCache)
        {
            if (eventCache == null) return;

            String indent = "\t";

            if ((TraceOptions.ThreadId & TraceOutputOptions) != 0)
            {
                WriteLine($"{indent}ThreadId={eventCache.ThreadId}");
            }

            if ((TraceOptions.ProcessId & TraceOutputOptions) != 0)
            {
                WriteLine($"{indent}ProcessId={eventCache.ProcessId}");
            }

            if ((TraceOptions.DateTime & TraceOutputOptions) != 0)
            {
                WriteLine($"{indent}DateTime={eventCache.DateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff")}");
            }

            if ((TraceOptions.Timestamp & TraceOutputOptions) != 0)
            {
                WriteLine($"{indent}Timestamp={eventCache.Timestamp}");
            }

            if ((TraceOptions.Callstack & TraceOutputOptions) != 0)
            {
                WriteLine($"{indent}Callstack={eventCache.Callstack}");
            }

            if ((TraceOptions.LogicalOperationStack & TraceOutputOptions) != 0)
            {
                Write($"{indent}LogicalOperationStack=");
                Stack logicalOperationStack = eventCache.LogicalOperationStack;
                bool flag = true;
                foreach (object item in logicalOperationStack)
                {
                    if (!flag)
                    {
                        Write(", ");
                    }
                    else
                    {
                        flag = false;
                    }

                    Write(item.ToString());
                }

                WriteLine(string.Empty);
            }
        }
        private void CheckFileSize()
        {
            if (Writer == null) return;

            StreamWriter writer = Writer as StreamWriter;
            if (writer?.BaseStream == null) return;

            const long MaxSize = 1024 * 1024 * 2;
            if (writer.BaseStream.Length < MaxSize) return;

            FileStream fileStream = writer.BaseStream as FileStream;
            if (fileStream == null) return;

            try
            {
                FileInfo curLogFile = new FileInfo(fileStream.Name);
                String fileName = curLogFile.Name.Substring(0, curLogFile.Name.Length - curLogFile.Extension.Length + 1);
                int count = (int)(curLogFile.Directory.GetFiles($"{fileName}*", SearchOption.TopDirectoryOnly)?.Length) - 1;

                String sourceFileName = curLogFile.FullName;
                string destFileName = $"{curLogFile.Directory.FullName}\\{fileName}{count}{curLogFile.Extension}";

                Writer.Flush();
                Writer.Close();
                Writer.Dispose();
                Writer = null;

                File.Move(sourceFileName, destFileName);
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
            WriteEvent = null;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// WriteEventArgs
    /// </summary>
    public class WriteEventArgs : EventArgs
    {
        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; internal set; }

        /// <summary>
        /// WriteEventArgs
        /// </summary>
        /// <param name="message"></param>
        public WriteEventArgs(string message)
        {
            this.Message = message;
        }
    }
}

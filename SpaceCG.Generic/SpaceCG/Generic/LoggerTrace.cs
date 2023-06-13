using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using SpaceCG.Extensions;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 轻量的日志跟踪记录对象，支持跨平台
    /// </summary>
    public sealed class LoggerTrace : IDisposable
    {
        /// <summary>
        /// 跟踪代码的执行并将跟踪消息的源
        /// </summary>
        public TraceSource TraceSource { get; private set; }

        private static int id = 0;
        private static readonly TextWriterTraceListener consoleListener;
        private static readonly TextWriterTraceListener textFileListener;
        /// <summary>
        /// IsDebugEnabled
        /// </summary>
        public bool IsDebugEnabled => TraceSource?.Switch.Level != SourceLevels.Off && TraceSource?.Switch.Level <= SourceLevels.Verbose;
        /// <summary>
        /// IsInfoEnabled
        /// </summary>
        public bool IsInfoEnabled => TraceSource?.Switch.Level != SourceLevels.Off && TraceSource?.Switch.Level <= SourceLevels.Information;

        static LoggerTrace()
        {
            string path = "logs";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            String defaultFileName = $"{path}/trace.{DateTime.Today.ToString("yyyy-MM-dd")}.log";

            textFileListener = new ETextWriterTraceListener(defaultFileName, "FileTrace");
            textFileListener.Filter = new EventTypeFilter(SourceLevels.Information);

            consoleListener = new ETextWriterTraceListener(Console.Out, "ConsoleTrace");
            consoleListener.Filter = new EventTypeFilter(SourceLevels.All);

            OperatingSystem os = Environment.OSVersion;
            String message = $"{Environment.NewLine}[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] (OS: {os.Platform} {os.Version})";
            consoleListener.WriteLine(message);
            textFileListener.WriteLine(message);

            consoleListener.Flush();
            textFileListener.Flush();

            FileExtensions.ReserveFileDays(30, path, "trace.*.log");
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
                StackFrame frame = new StackFrame(2, true);
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
        /// Debug
        /// </summary>
        /// <param name="message"></param>
        public void Debug(object message)
        {
            if (TraceSource == null) return;
            TraceSource.TraceEvent(TraceEventType.Verbose, id++, FormatMessage(message));
            TraceSource.Flush();
        }
        /// <summary>
        /// Info
        /// </summary>
        /// <param name="message"></param>
        public void Info(object message)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Information, id++, FormatMessage(message));
            TraceSource.Flush();
        }
        /// <summary>
        /// Warn
        /// </summary>
        /// <param name="message"></param>
        public void Warn(object message)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Warning, id++, FormatMessage(message));
            TraceSource.Flush();
        }
        /// <summary>
        /// Error
        /// </summary>
        /// <param name="message"></param>
        public void Error(object message)
        {
            TraceSource.TraceEvent(TraceEventType.Error, id++, FormatMessage(message));
            TraceSource.Flush();
        }
        /// <summary>
        /// Fatal
        /// </summary>
        /// <param name="message"></param>
        public void Fatal(object message)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Critical, id++, FormatMessage(message));
            TraceSource.Flush();
        }

        private static string FormatMessage(object message)
        {
            StackFrame frame = new StackFrame(2, true);
            MethodBase method = frame.GetMethod();

            return method != null ? $"({method.Name}) {message}" : $"{message}";
        }
    }

    /// <inheritdoc/>
    public sealed class ETextWriterTraceListener : System.Diagnostics.TextWriterTraceListener
    {
        /// <summary>
        /// Write 事件
        /// </summary>
        public event EventHandler<WriteEventArgs> WriteEvent;

        /// <inheritdoc/>
        public ETextWriterTraceListener(Stream stream) : base(stream)
        {
        }
        /// <inheritdoc/>
        public ETextWriterTraceListener(Stream stream, string name) : base(stream, name)
        {
        }
        /// <inheritdoc/>
        public ETextWriterTraceListener(TextWriter writer) : base(writer)
        {
        }
        /// <inheritdoc/>
        public ETextWriterTraceListener(TextWriter writer, string name) : base(writer, name)
        {
        }
        /// <inheritdoc/>
        public ETextWriterTraceListener(string fileName) : base(fileName)
        {
        }
        /// <inheritdoc/>
        public ETextWriterTraceListener(string fileName, string name) : base(fileName, name)
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

        private void WriteHeader(string source, TraceEventType eventType, int id)
        {
            Write(string.Format(CultureInfo.InvariantCulture, "[{0}] [{1}] [{2}] ({3}) : ",
                DateTime.Now.ToString("HH:mm:ss.fff"),
                eventType.ToString().Substring(0, 5).ToUpper(),
                source,
                id.ToString(CultureInfo.InvariantCulture)
            ));
        }
        private void WriteFooter(TraceEventCache eventCache)
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

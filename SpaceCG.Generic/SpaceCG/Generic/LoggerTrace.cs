using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

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

        public bool IsDebugEnabled => TraceSource?.Switch.Level != SourceLevels.Off && TraceSource?.Switch.Level <= SourceLevels.Verbose;
        public bool IsInfoEnabled => TraceSource?.Switch.Level != SourceLevels.Off && TraceSource?.Switch.Level <= SourceLevels.Information;

        static LoggerTrace()
        {
            string path = "logs";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            String defaultFileName = $"{path}/Trace.{DateTime.Today.ToString("yyyy-MM-dd")}.log";

            textFileListener = new ETextWriterTraceListener(defaultFileName, "File");
            textFileListener.Filter = new EventTypeFilter(SourceLevels.Information);

            consoleListener = new ETextWriterTraceListener(Console.Out, "Console");
            consoleListener.Filter = new EventTypeFilter(SourceLevels.All);

            OperatingSystem os = Environment.OSVersion;
            String message = $"{Environment.NewLine}[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] (OS: {os.Platform} {os.Version})";
            consoleListener.WriteLine(message);
            textFileListener.WriteLine(message);

            consoleListener.Flush();
            textFileListener.Flush();

            ReserveFileDays(30, path, "Trace.*.log");
        }

        /// <summary>
        /// 保留目录中的文件数量
        /// <para>跟据文件创建日期排序，保留 count 个最新文件，超出 count 数量的文件删除</para>
        /// <para>注意：该函数是比较文件的创建日期</para>
        /// </summary>
        /// <param name="count">要保留的数量</param>
        /// <param name="path">文件目录，当前目录 "/" 表示，不可为空</param>
        /// <param name="searchPattern">只在目录中(不包括子目录)，查找匹配的文件；例如："*.jpg" 或 "temp_*.png"</param>
        public static void ReserveFileCount(int count, string path, string searchPattern = null)
        {
            if (count < 0 || String.IsNullOrWhiteSpace(path)) throw new ArgumentException("参数错误");

            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = searchPattern == null ? dir.GetFiles() : dir.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);

            if (files.Length <= count) return;

            //按文件的创建时间，升序排序(最新创建的排在最前面)
            Array.Sort(files, (f1, f2) =>
            {
                return f2.CreationTime.CompareTo(f1.CreationTime);
            });

            for (int i = count; i < files.Length; i++)
            {
                files[i].Delete();
            }
        }

        /// <summary>
        /// 保留目录中的文件天数
        /// <para>跟据文件上次修时间起计算，保留 days 天的文件，超出 days 天的文件删除</para>
        /// <para>注意：该函数是比较文件的上次修改日期</para>
        /// </summary>
        /// <param name="days">保留天数</param>
        /// <param name="path">文件夹目录</param>
        /// <param name="searchPattern">文件匹配类型, 只在目录中(不包括子目录)，查找匹配的文件；例如："*.jpg" 或 "temp_*.png"</param>
        public static void ReserveFileDays(int days, string path, string searchPattern = null)
        {
            if (days < 0 || String.IsNullOrWhiteSpace(path)) return;

            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = searchPattern == null ? dir.GetFiles() : dir.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
            if (files.Length == 0) return;

            IEnumerable<FileInfo> removes =
                  from file in files
                  where file.LastWriteTime < DateTime.Today.AddDays(-days)
                  select file;

            foreach (var file in removes)
            {
                file.Delete();
            }
        }

        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <exception cref="Exception"></exception>
        public LoggerTrace()
        {
            StackFrame frame = new StackFrame(1, true);
            MethodBase method = frame.GetMethod();
            if (method == null) throw new Exception("获取在其中执行帧的方法失败");

            Type declaringType = method.DeclaringType;
            if (declaringType == null) throw new Exception("获取声明该成员的类失败");
            //Console.WriteLine($"Logger Name: {declaringType.Name}");

            Initialize(declaringType.Name, SourceLevels.All);
        }
        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultLevel"></param>
        public LoggerTrace(String name, SourceLevels defaultLevel)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name), "参数不能为空");
            Initialize(name, defaultLevel);
        }
        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="name"></param>
        public LoggerTrace(String name) : this(name, SourceLevels.All)
        {
        }

        private void Initialize(String name, SourceLevels defaultLevel)
        {
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

        public void Debug(object message)
        {
            if (TraceSource == null) return;
            TraceSource.TraceEvent(TraceEventType.Verbose, id++, FormatMessage(message));
            TraceSource.Flush();
        }

        public void Info(object message)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Information, id++, FormatMessage(message));
            TraceSource.Flush();
        }

        public void Warn(object message)
        {
            if (TraceSource == null) return;

            TraceSource.TraceEvent(TraceEventType.Warning, id++, FormatMessage(message));
            TraceSource.Flush();
        }

        public void Error(object message)
        {
            TraceSource.TraceEvent(TraceEventType.Error, id++, FormatMessage(message));
            TraceSource.Flush();
        }

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

            CheckWriter();
        }
        /// <inheritdoc/>
        public override void WriteLine(string message)
        {
            base.WriteLine(message);
            WriteEvent?.Invoke(this, new WriteEventArgs(message + Environment.NewLine));

            CheckWriter();
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
        private void CheckWriter()
        {
            if (Writer == null) return;

            StreamWriter writer = Writer as StreamWriter;
            if (writer?.BaseStream == null) return;

            const long MaxSize = 1024 * 1024 * 2;
            if (writer.BaseStream.Length >= MaxSize)
            {
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
        }
    }

    public class WriteEventArgs:EventArgs
    {
        public string Message { get; internal set; }

        public WriteEventArgs(string message)
        {
            this.Message = message;
        }
    }
}

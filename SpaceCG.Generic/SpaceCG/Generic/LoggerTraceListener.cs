using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 日志跟踪侦听器
    /// </summary>
    public class LoggerTraceListener : System.Diagnostics.TraceListener
    {
        /// <summary>
        /// 单个文件的最大大小
        /// </summary>
        protected readonly int FILE_MAX_SIZE = 1024 * 1024 * 2;
        /// <summary>
        /// Stream.Write 多少次后刷新缓冲区
        /// </summary>
        protected readonly int FlushInterval = 4;

        private bool _isDisposed = false;

        private int _flushCount = 0;

        private string _fileName;
        private TextWriter _writer;

        private readonly CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
        private readonly ConcurrentQueue<TraceEntry> TraceQueue = new ConcurrentQueue<TraceEntry>();

        /// <summary>
        /// 跟踪事件类型名称
        /// </summary>
        public static readonly IReadOnlyDictionary<TraceEventType, string> EventTypeNames = new Dictionary<TraceEventType, string>
        {
            { TraceEventType.Transfer,"Transfer" },
            { TraceEventType.Resume, "Resume" },
            { TraceEventType.Suspend, "Suspend" },
            { TraceEventType.Stop, "Stop" },
            { TraceEventType.Start, "Start" },
            { TraceEventType.Verbose, "Debug" },
            { TraceEventType.Information, "Info" },
            { TraceEventType.Warning, "Warn" },
            { TraceEventType.Error, "Error" },
            { TraceEventType.Critical, "Critical" }
        };
        /// <summary>
        /// 跟踪事件对应的 Console 类型颜色
        /// </summary>
        public static readonly IReadOnlyDictionary<TraceEventType, ConsoleColor> EventTypeConsoleColors = new Dictionary<TraceEventType, ConsoleColor>
        {
            { TraceEventType.Transfer, ConsoleColor.DarkYellow },
            { TraceEventType.Resume, ConsoleColor.Magenta },
            { TraceEventType.Suspend, ConsoleColor.Magenta },
            { TraceEventType.Stop, ConsoleColor.Cyan },
            { TraceEventType.Start, ConsoleColor.Cyan },
            { TraceEventType.Verbose, ConsoleColor.Green },
            { TraceEventType.Information, ConsoleColor.Gray },
            { TraceEventType.Warning, ConsoleColor.Yellow },
            { TraceEventType.Error, ConsoleColor.Red },
            { TraceEventType.Critical, ConsoleColor.DarkRed }
        };

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name"></param>
        private LoggerTraceListener(string name):base(name)
        {
            Task.Factory.StartNew(ProcessTraceQueue, TaskCreationOptions.LongRunning);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Dispose();
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public LoggerTraceListener() : this(nameof(LoggerTraceListener))
        {            
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="name"></param>
        /// <exception cref="ArgumentException"></exception>
        public LoggerTraceListener(string fileName, string name):this(name)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException(nameof(fileName));
            _fileName = fileName;
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="name"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public LoggerTraceListener(Stream stream, string? name):this(name)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            _writer = new StreamWriter(stream);
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="stream"></param>
        public LoggerTraceListener(Stream stream): this(stream, string.Empty)
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="name"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public LoggerTraceListener(TextWriter writer, string? name): this(name)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            _writer = writer;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="writer"></param>
        public LoggerTraceListener(TextWriter writer): this(writer, string.Empty)
        {
        }

        private void ProcessTraceQueue()
        {
            if (_writer == null) EnsureWriter();
            _writer.WriteLine($"\r\n[Header] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

            OperatingSystem os = Environment.OSVersion;
            string systemInfo = $"[{os} ({os.Platform})]({(Environment.Is64BitOperatingSystem ? "64" : "32")} 位操作系统 / 逻辑处理器: {Environment.ProcessorCount})";
            string moduleInfo = $"[{Process.GetCurrentProcess().MainModule.ModuleName}]({(Environment.Is64BitProcess ? "64" : "32")} 位进程 / 进程 ID: {Process.GetCurrentProcess().Id})";
            _writer.WriteLine(systemInfo);
            _writer.WriteLine(moduleInfo);
            _writer.Flush();

            DateTime lastWriteTime = DateTime.Now;
            TimeSpan WriteInterval = TimeSpan.FromSeconds(3);

            bool hasFileName = !string.IsNullOrWhiteSpace(_fileName);

            while (!CancelTokenSource.IsCancellationRequested)
            {
                if (_writer == null) EnsureWriter();

                if (TraceQueue.IsEmpty)
                {                   
                    if (DateTime.Now - lastWriteTime > WriteInterval)
                    {
                        _flushCount = 0;
                        _writer?.Flush();
                        CheckFileStreamSize();

                        lastWriteTime = DateTime.MaxValue;
                    }

                    Thread.Sleep(10);
                    continue;
                }

                if (TraceQueue.TryDequeue(out TraceEntry entry))
                {
                    lastWriteTime = DateTime.Now;

                    if (hasFileName)
                    {
                        _writer.WriteLine(entry.ToFromatString());
                    }
                    else
                    {
                        Console.ForegroundColor = EventTypeConsoleColors[entry.EventType];
                        _writer.WriteLine(entry.ToFromatString());
                        Console.ResetColor();
                    }

                    _flushCount++;
                    if (_flushCount >= FlushInterval)
                    {
                        _flushCount = 0;
                        _writer.Flush();
                    }
                }
            }
        }

        /// <inheritdoc/> 
        public override void Write(string message)
        {
            EnqueueTraceEvent(TraceEventType.Verbose, message, null);
        }
        /// <inheritdoc/> 
        public override void WriteLine(string message)
        {
            EnqueueTraceEvent(TraceEventType.Verbose, message, null);
        }
        /// <inheritdoc/> 
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null)) return;

            EnqueueTraceEvent(eventType, message, source);
        }
        /// <inheritdoc/> 
        public override void Flush()
        {
            if (_writer != null)
            {
                _flushCount = 0;
                _writer.Flush();
            }
            else
            {
                _flushCount = FlushInterval;
            }
            base.Flush();
        }

        /// <inheritdoc/> 
        public override void Close()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer.Dispose();
                _writer = null;
            }
            base.Close();
        }
        /// <inheritdoc/> 
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            CancelTokenSource.Cancel();
            CancelTokenSource.Dispose();

            while (!TraceQueue.IsEmpty)
            {
                if (_writer == null) EnsureWriter();
                if (TraceQueue.TryDequeue(out TraceEntry entry))
                {
                    _writer?.WriteLine(entry.ToFromatString());
                    _writer?.Flush();
                }
            }

            _writer?.WriteLine($"[Footer] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\r\n");
            _writer?.Flush();

            Flush();
            Close();

            base.Dispose(disposing);
        }
        
        /// <summary>
        /// 写入跟踪队列
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="message"></param>
        /// <param name="source"></param>
        private void EnqueueTraceEvent(TraceEventType eventType, string message, string source)
        {
            if (_isDisposed || string.IsNullOrEmpty(message)) return;

            var frame = new StackFrame(3, true);
            //var stackTrace = new StackTrace(3, true);
            //var frame = FindCallerFrame(stackTrace);

            var entry = new TraceEntry
            {
                EventType = eventType,
                Message = message,
                Source = source,
                TypeName = frame?.GetMethod()?.DeclaringType?.Name ?? string.Empty,
                MethodName = frame?.GetMethod()?.Name ?? string.Empty,
                LineNumber = frame?.GetFileLineNumber() ?? 0
            };
            
            TraceQueue.Enqueue(entry);
        }

        /// <summary>
        /// 查找调用者的栈帧
        /// </summary>
        /// <param name="stackTrace"></param>
        /// <returns></returns>
        private StackFrame FindCallerFrame(StackTrace stackTrace)
        {
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame?.GetMethod();
                if (method == null) continue;

                var declaringType = method.DeclaringType;
                if (declaringType == null) continue;

                if (declaringType.Namespace != null && (declaringType.Namespace.StartsWith("System.Diagnostics") || declaringType == typeof(LoggerTraceListener)))
                {
                    continue;
                }

                return frame;
            }
            return null;
        }

        /// <summary>
        /// 获取指定编码的编码器，并设置"?"替换回退
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private static Encoding GetEncodingWithFallback(Encoding encoding)
        {
            Encoding fallbackEncoding = (Encoding)encoding.Clone();
            fallbackEncoding.EncoderFallback = EncoderFallback.ReplacementFallback;
            fallbackEncoding.DecoderFallback = DecoderFallback.ReplacementFallback;

            return fallbackEncoding;
        }

        /// <summary>
        /// 确保日志文件存在，以及日志文件大小限制
        /// </summary>
        private void EnsureWriter()
        {           
            if (_writer == null)
            {
                if (string.IsNullOrWhiteSpace(_fileName))
                {
                    var moduleFile = new FileInfo(GetModuleFileName());
                    _fileName = Path.Combine(moduleFile.DirectoryName, "logs", $"{DateTime.Now:yyyy-MM-dd}.{moduleFile.Name.Replace(moduleFile.Extension, ".log")}");
                }

                bool success = false;

                string fullPath = Path.GetFullPath(_fileName);
                string dirPath = Path.GetDirectoryName(fullPath)!;
                string fileNameOnly = Path.GetFileName(fullPath);

                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

                Encoding noBOMwithFallback = GetEncodingWithFallback(new System.Text.UTF8Encoding(false));

                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        _writer = new StreamWriter(fullPath, true, noBOMwithFallback, 4096);
                        success = true;
                        break;
                    }
                    catch (IOException)
                    {
                        var extensions = Path.GetExtension(fileNameOnly);
                        fileNameOnly = fileNameOnly.Replace(extensions, $".({Guid.NewGuid()}){extensions}");
                        fullPath = Path.Combine(dirPath, fileNameOnly);
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }

                if (!success)
                {
                    _fileName = null;
                }
            }
        }
        /// <summary>
        /// 检查日志文件大小
        /// </summary>
        private void CheckFileStreamSize()
        {
            if (!string.IsNullOrWhiteSpace(_fileName) && _writer != null && _writer is StreamWriter streamWriter)
            {
                if (streamWriter.BaseStream is FileStream fileStream && fileStream.Length >= FILE_MAX_SIZE)
                {
                    string fullPath = Path.GetFullPath(_fileName);
                    FileInfo fileInfo = new FileInfo(fullPath);

                    string fnHeader = fileInfo.Name.Substring(0, 12);
                    int count = (from file in Directory.GetFiles(fileInfo.DirectoryName, $"{fnHeader}*", SearchOption.TopDirectoryOnly)
                                 select file).Count();

                    var destFileName = Path.Combine(fileInfo.DirectoryName, $"{fileInfo.Name.Replace(fileInfo.Extension, $"({count}){fileInfo.Extension}")}");

                    _writer.Flush();
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;

                    File.Move(fullPath, destFileName);
                    //File.Move(fullPath, destFileName, true);

                    EnsureWriter();
                }
            }
        }

        /// <summary>
        /// 获取当前程序模块的文件名
        /// </summary>
        /// <returns>返回当前程序模块的完整路径文件名</returns>
        private string GetModuleFileName()
        {
            ProcessModule? processModule = Process.GetCurrentProcess().MainModule;
            if (!string.IsNullOrWhiteSpace(processModule?.FileName)) 
                return processModule.FileName;

            string? moduleName = processModule?.ModuleName;
            if (!string.IsNullOrWhiteSpace(moduleName) && moduleName.StartsWith("donet", StringComparison.OrdinalIgnoreCase))
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 0) return args[0];
            }

            return $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}{DateTime.Now:yyyy-MM-dd}.Unknown.{Guid.NewGuid()}.log";
        }

        /// <summary>
        /// 跟踪对象
        /// </summary>
        public class TraceEntry
        {
            /// <summary>
            /// 跟踪事件的时间戳
            /// </summary>
            public DateTime Timestamp { get; set; } = DateTime.Now;
            /// <summary>
            /// 跟踪事件的类型
            /// </summary>
            public TraceEventType EventType { get; set; } = TraceEventType.Verbose;
            /// <summary>
            /// 跟踪事件的消息
            /// </summary>
            public string Message { get; set; } = string.Empty;
            /// <summary>
            /// 跟踪事件的源
            /// </summary>
            public string Source { get; set; } = string.Empty;

            /// <summary>
            /// 跟踪事件的类型名
            /// </summary>
            public string TypeName { get; set; } = string.Empty;
            /// <summary>
            /// 跟踪事件的方法名
            /// </summary>
            public string MethodName { get; set; } = string.Empty;
            /// <summary>
            /// 跟踪事件的具体行号
            /// </summary>
            public int LineNumber { get; set; } = 0;

            /// <summary>
            /// 获取当前线程的ID
            /// </summary>
            public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;

            /// <summary>
            /// 构造函数
            /// </summary>
            public TraceEntry()
            {
            }

            /// <summary>
            /// 转换为格式化字符串
            /// </summary>
            /// <returns></returns>
            public string ToFromatString()
            {
                return $"[{Timestamp:HH:mm:ss.fff}] [{EventTypeNames[EventType].PadLeft(5)}] [{ThreadId.ToString().PadLeft(2)}] [{Source}.{TypeName}.{MethodName}]({LineNumber}) - {Message}";
            }
        }
    }

    

}

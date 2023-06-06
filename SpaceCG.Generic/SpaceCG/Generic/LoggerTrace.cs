using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 简单的日志跟踪记录对象
    /// </summary>
    public sealed class LoggerTrace : IDisposable
    {
        private TraceSource traceSource;

        private static int id = 0;
        private static readonly TextWriterTraceListener consoleListener;
        private static readonly TextWriterTraceListener textFileListener;

        public bool IsDebugEnabled => traceSource?.Switch.Level != SourceLevels.Off && traceSource?.Switch.Level <= SourceLevels.Verbose;
        public bool IsInfoEnabled => traceSource?.Switch.Level != SourceLevels.Off && traceSource?.Switch.Level <= SourceLevels.Information;

        static LoggerTrace()
        {
            string path = "logs";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            String defaultFileName = $"{path}/Trace.{DateTime.Today.ToString("yyyy-MM-dd")}.log";

            textFileListener = new TextWriterTraceListener(defaultFileName, "FileTrace");
            textFileListener.Filter = new EventTypeFilter(SourceLevels.Information);

            consoleListener = new TextWriterTraceListener(Console.Out, "ConsoleTrace");
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
        /// <param name="name"></param>
        /// <param name="defaultLevel"></param>
        public LoggerTrace(String name, SourceLevels defaultLevel)
        {
            traceSource = new TraceSource(name, defaultLevel);
            traceSource.Switch = new SourceSwitch($"{name}_Switch", defaultLevel.ToString());
            traceSource.Switch.Level = defaultLevel;

            if (consoleListener != null) traceSource.Listeners.Add(consoleListener);
            if (textFileListener != null) traceSource.Listeners.Add(textFileListener);

            if (consoleListener != null && textFileListener != null) traceSource.Listeners.Remove("Default");
        }

        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="name"></param>
        public LoggerTrace(String name) : this(name, SourceLevels.All)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (traceSource != null)
            {
                traceSource.Flush();
                traceSource.Listeners.Clear();
                traceSource = null;
            }
        }


        public void Debug(object message)
        {
            if (traceSource == null) return;
            traceSource.TraceEvent(TraceEventType.Verbose, id++, FormatMessage(message));
            traceSource.Flush();
        }

        public void Info(object message)
        {
            if (traceSource == null) return;

            traceSource.TraceEvent(TraceEventType.Information, id++, FormatMessage(message));
            traceSource.Flush();
        }

        public void Warn(object message)
        {
            if (traceSource == null) return;

            traceSource.TraceEvent(TraceEventType.Warning, id++, FormatMessage(message));
            traceSource.Flush();
        }

        public void Error(object message)
        {
            traceSource.TraceEvent(TraceEventType.Error, id++, FormatMessage(message));
            traceSource.Flush();
        }

        public void Fatal(object message)
        {
            if (traceSource == null) return;

            traceSource.TraceEvent(TraceEventType.Critical, id++, FormatMessage(message));
            traceSource.Flush();
        }

        private static string FormatMessage(object message)
        {
            return $"[{DateTime.Now.ToString("HH:mm:ss.fff")}] {message}";
        }
    }
}

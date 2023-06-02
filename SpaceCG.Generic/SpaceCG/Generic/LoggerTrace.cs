using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public bool IsDebugEnabled => traceSource?.Switch.Level <= SourceLevels.Verbose;
        public bool IsInfoEnabled => traceSource?.Switch.Level <= SourceLevels.Information;
        

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

            if(consoleListener != null) traceSource.Listeners.Add(consoleListener);
            if(textFileListener != null) traceSource.Listeners.Add(textFileListener);

            if(consoleListener != null && textFileListener != null) traceSource.Listeners.Remove("Default");
        }

        /// <summary>
        /// 日志跟踪对象
        /// </summary>
        /// <param name="name"></param>
        public LoggerTrace(String name):this(name, SourceLevels.All)
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

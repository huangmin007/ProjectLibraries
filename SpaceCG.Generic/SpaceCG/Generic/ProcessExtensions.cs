using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceCG.Generic
{
    public static class ProcessExtensions
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(ProcessExtensions));

        /// <summary>
        /// 启动配置文件中的应用程序或相关文档
        /// <para name="fileNameCfgKey">配置的键值格式：(命名空间.Process属性)，属性 FileName 不可为空。</para>
        /// <para>例如：Process.FileName, 多个进程操作，可以在命名空间上处理：Process1.FileName ... </para>
        /// </summary>
        /// <param name="fileNameCfgKey">配置的键值格式：(命名空间.Process属性)，例如：Process.FileName, 多个进程操作，可以在命名空间上处理：Process1.FileName ... </param>
        /// <returns></returns>
        public static Process CreateProcessModule(string fileNameCfgKey)
        {
            if (String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[fileNameCfgKey])) return null;

            String fileName = ConfigurationManager.AppSettings[fileNameCfgKey].Trim();
            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                Logger.Warn($"应用程序或文档 \"{fileName}\" 不存在");
                return null;
            }

            String nameSpace = fileNameCfgKey.Replace("FileName", "");
            ProcessStartInfo startInfo = new ProcessStartInfo(fileInfo.FullName);
            startInfo.WorkingDirectory = fileInfo.DirectoryName;

            InstanceExtensions.SetInstancePropertyValues(startInfo, nameSpace);

            //重复一次，转为使用绝对路径
            startInfo.FileName = fileInfo.FullName;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => Logger.Warn($"应用程序或文档 \"{fileName}\" 发生退出事件(ExitCode:{process.ExitCode})");

            Task.Run(() =>
            {
                try
                {
                    if (process.Start())
                        Logger.Info($"已启动的应用程序或文档 \"{fileName}\"");
                    else
                        Logger.Warn($"应用程序或文档 \"{fileName}\" 启动失败");
                }
                catch (Exception ex)
                {
                    Logger.Error($"应用程序或文档 \"{fileName}\" 启动时发生错误：{ex}");
                }
            });

            return process;
        }

        /// <summary>
        /// 退出并释放进程对象资源
        /// </summary>
        /// <param name="process"></param>
        public static void DisposeProcessModule(ref Process process)
        {
            if (process == null) return;

            int code = 0;
            String name = "";

            try
            {
                if (!process.HasExited)
                {
                    name = process.ProcessName;

                    process.Kill();
                    code = process.ExitCode;
                }

                process.Dispose();
                Logger.Info($"退出并释放进程模块 {name}(ExitCode:{code}) 完成");
            }
            catch (Exception ex)
            {
                Logger.Error($"退出并释放 进程模块资源 对象错误: {ex}");
            }
            finally
            {
                process = null;
            }
        }
    }
}

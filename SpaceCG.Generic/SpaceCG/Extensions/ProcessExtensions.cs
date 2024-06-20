using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    public static class ProcessExtensions
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ProcessExtensions));

        private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// 获取进程是否已设置开机启动
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static bool IsStartWithStartUp(this Process process)
        {
            return false;
        }

    }
}

using System;
using System.IO.Ports;
using System.Text;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// SerialPort Extensions
    /// </summary>
    public static class SerialPortExtensions
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(SerialPortExtensions));

        /// <summary>
        /// 向 <see cref="SerialPort"/> 发送字节数据
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool SendBytes(this SerialPort serialPort, byte[] bytes)
        {
            if (serialPort != null && serialPort.IsOpen) return false;
            try
            {
                serialPort.Write(bytes, 0, bytes.Length);
                return true;
            }
            catch(Exception ex) 
            {
                Logger.Warn(ex.Message);
                return false;
            }
        }
        /// <summary>
        /// 向 <see cref="SerialPort"/> 发送消息内容
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool SendMessage(this SerialPort serialPort, string message) => SendBytes(serialPort, Encoding.UTF8.GetBytes(message));
        
    }
}

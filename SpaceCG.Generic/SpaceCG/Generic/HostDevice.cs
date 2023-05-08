using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpaceCG.Generic
{
    public class HostDevice
    {
        public static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(HostDevice));

        /// <summary>
        /// 关机或重启主机函数
        /// <para>关机：-s -t {timeout}</para>
        /// <para>重启：-r -t {timeout}</para>
        /// </summary>
        /// <param name="arguments"></param>
        public static void Shutdown(String arguments)
        {
            string fileName = Path.Combine(Environment.SystemDirectory, "Shutdown.exe");
            if (!File.Exists(fileName)) return;

            if (String.IsNullOrWhiteSpace(arguments)) arguments = "-s -t 3";

            try
            {
                Process.Start(fileName, arguments);
            }
            catch(Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// 远程网络唤醒，Wake-on-LAN
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="macAddress"></param>
        public static bool RemoteWakeUp(String ipAddress, String macAddress)
        {
            if (String.IsNullOrWhiteSpace(ipAddress)) return false;
            if (String.IsNullOrWhiteSpace(macAddress)) return false;

            Regex macRegx = new Regex(@"^([0-9a-fA-F]{2})(([/\s:-][0-9a-fA-F]{2}){5})$");
            if (!macRegx.IsMatch(macAddress)) return false;

            byte[] headBytes = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] macBytes = new byte[6];
            String[] macString = macAddress.Split(macAddress[2]);
            for(int i = 0; i < 6; i ++)
            {
                if(byte.TryParse(macString[i], System.Globalization.NumberStyles.HexNumber, null, out byte result))
                {
                    macBytes[i] = result;
                }
                else
                {
                    return false;
                }
            }

            byte[] packet = new byte[102];
            Array.Copy(headBytes, packet, 6);
            for(int i = 1; i <= 16; i ++)
            {
                Array.Copy(macBytes, 0, packet, i * 6, 6);
            }

            try
            {
                UdpClient client = new UdpClient();
                client.Connect(System.Net.IPAddress.Broadcast, 9);
                client.Send(packet, packet.Length);
            }
            catch(Exception ex)
            {
                Logger.Error(ex);
                return false;
            }

            return true;
        }
    }

    
}

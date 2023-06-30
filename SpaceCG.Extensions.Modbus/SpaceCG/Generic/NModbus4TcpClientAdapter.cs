using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Modbus.IO;

namespace SpaceCG.Generic
{
    /// <summary>
    /// NModbus4 TcpClient Reconnection Adapter
    /// <para>示例：</para>
    /// <code> ModbusIpMaster.CreateIp(new NModbus4TcpClientAdapter("127.0.0.1", 8899)); <br/> ModbusSerialMaster.CreateRtu(new NModbus4TcpClientAdapter("127.0.0.1", 8899));</code>
    /// </summary>
    public class NModbus4TcpClientAdapter : IStreamResource, IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(NModbus4TcpClientAdapter));

        /// <summary> <see cref="IPEndPoint"/> </summary>
        protected IPEndPoint remoteEP;
        /// <summary> <see cref="TcpClient"/> </summary>
        protected TcpClient tcpClient;

        private Thread thread;
        private bool flags = true;

        private int readTimeout = -1;
        private int writeTimeout = -1;

        /// <summary>
        /// NModbus4 TcpClient reconnect adapter
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public NModbus4TcpClientAdapter(TcpClient tcpClient)
        {
            if (tcpClient == null)
                throw new ArgumentNullException(nameof(tcpClient), "参数不能为空");

            this.tcpClient = tcpClient;
            remoteEP = tcpClient.Client?.RemoteEndPoint as IPEndPoint;

            thread = new Thread(CheckConnectStatus);
            thread.IsBackground = true;
            thread.Start(this);
        }

        /// <summary>
        /// NModbus4 TcpClient reconnect adapter
        /// </summary>
        /// <param name="remoteEP"></param>
        public NModbus4TcpClientAdapter(IPEndPoint remoteEP)
        {
            if (remoteEP == null)
                throw new ArgumentNullException(nameof(remoteEP), "参数不能为空");

            this.remoteEP = remoteEP;
            tcpClient = new TcpClient(remoteEP);

            thread = new Thread(CheckConnectStatus);
            thread.IsBackground = true;
            thread.Start(this);
        }

        /// <summary>
        /// NModbus4 TcpClient reconnect adapter
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public NModbus4TcpClientAdapter(string hostname, int port) : this(new IPEndPoint(IPAddress.Parse(hostname), port))
        {
        }

        /// <summary>
        /// NModbus4 TcpClient reconnect adapter
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public NModbus4TcpClientAdapter(IPAddress address, int port) : this(new IPEndPoint(address, port))
        {
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <param name="adapter"></param>
        protected void CheckConnectStatus(object adapter)
        {
            while (flags)
            {
                if (tcpClient == null) return;
                if (remoteEP == null) remoteEP = tcpClient.Client?.RemoteEndPoint as IPEndPoint;

                if (!IsOnline(tcpClient) && remoteEP != null)
                {
                    Logger.Warn($"NModbus4 TcpClient Adapter Disconnect! {tcpClient.Client?.LocalEndPoint} x {remoteEP}");
                    //Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] NModbus4 TcpClient Adapter Disconnect! {tcpClient.Client?.LocalEndPoint} x {remoteEP}");

                    try
                    {
                        tcpClient?.Dispose();

                        tcpClient = new TcpClient();
                        tcpClient.Connect(remoteEP.Address, remoteEP.Port);//同步连接

                        if (IsOnline(tcpClient))
                        {
                            ReadTimeout = readTimeout;
                            WriteTimeout = writeTimeout;
                            Logger.Info($"NModbus4 TcpClient Adapter Reconnect Success! {tcpClient.Client.LocalEndPoint} => {remoteEP}");
                            //Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] NModbus4 TcpClient Adapter Reconnect Success! {tcpClient.Client.LocalEndPoint} => {remoteEP}");
                        }
                    }
                    catch (Exception) { }
                }

                Thread.Sleep(IsOnline(tcpClient) ? 3000 : 1000);
            }
        }

        /// <inheritdoc/>
        public int InfiniteTimeout => -1;

        /// <inheritdoc/>
        public int ReadTimeout
        {
            get
            {
                try { return tcpClient.GetStream().ReadTimeout; }
                catch { return -1; }
            }
            set
            {
                readTimeout = value;
                try { tcpClient.GetStream().ReadTimeout = value; }
                catch { }
            }
        }

        /// <inheritdoc/>
        public int WriteTimeout
        {
            get
            {
                try { return tcpClient.GetStream().WriteTimeout; }
                catch { return -1; }
            }
            set
            {
                writeTimeout = value;
                try { tcpClient.GetStream().WriteTimeout = value; }
                catch { }
            }
        }

        /// <inheritdoc/>
        public void Write(byte[] buffer, int offset, int size)
        {
            try { tcpClient.GetStream().Write(buffer, offset, size); }
            catch (Exception) { }
        }

        /// <inheritdoc/>
        public int Read(byte[] buffer, int offset, int size)
        {
            try { return tcpClient.GetStream().Read(buffer, offset, size); }
            catch (Exception) { return size; }
        }

        /// <inheritdoc/>
        public void DiscardInBuffer()
        {
            try { tcpClient.GetStream().Flush(); }
            catch (Exception) { }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                flags = false;
                remoteEP = null;

                thread?.Abort();
                thread = null;

                tcpClient?.Dispose();
                tcpClient = null;
            }
        }

        /// <summary>
        /// <see cref="TcpClient"/> 连接状态
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        public static bool IsOnline(TcpClient tcpClient)
        {
            if (tcpClient == null || tcpClient.Client == null) return false;
            return !(tcpClient.Client.Poll(1000, SelectMode.SelectRead) && tcpClient.Client.Available == 0 || !tcpClient.Client.Connected);
        }

    }
}

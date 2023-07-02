using System;
using System.IO.Ports;
using System.Threading;
using Modbus.IO;

namespace SpaceCG.Generic
{
    /// <summary>
    /// NModbus4 SerialPort Reconnection Adapter
    /// <para>只能解决串口 USB 接口断开重连的情况，不能解决总线断开重连，总线断开只是读写超时，串口 USB 连接还在</para>
    /// <para>示例：</para>
    /// <code> ModbusSerialMaster.CreateRtu(new NModbus4SerialPortAdapter("COM3", 9600));</code>
    /// </summary>
    public class NModbus4SerialPortAdapter : IStreamResource, IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(NModbus4SerialPortAdapter));

        /// <summary> <see cref="SerialPort"/> </summary>
        protected SerialPort serialPort;

        private bool running = true;

        /// <summary>
        /// NModbus4 SerialPort reconnect adapter
        /// </summary>
        /// <param name="serialPort"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public NModbus4SerialPortAdapter(SerialPort serialPort)
        {
            if (serialPort == null) throw new ArgumentNullException(nameof(serialPort), "参数不能为空");

            this.serialPort = serialPort;
            if (!this.serialPort.IsOpen) this.serialPort.Open();

            ThreadPool.QueueUserWorkItem(CheckConnectStatus);
        }

        /// <summary>
        /// NModbus4 SerialPort reconnect adapter
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="args"></param>
        public NModbus4SerialPortAdapter(string portName, int baudRate, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(portName) || baudRate < 4800)
                throw new ArgumentException("参数错误");

            serialPort = new SerialPort(portName, baudRate);

            if (args.Length >= 1)
            {
                Parity parity = args[0].GetType() == typeof(Parity) ? (Parity)args[0] : (Parity)Enum.Parse(typeof(Parity), args[0].ToString(), true);
                serialPort.Parity = parity;
            }
            if (args.Length >= 2)
            {
                int dataBits = args[1].GetType() == typeof(int) ? (int)args[1] : int.Parse(args[1].ToString());
                serialPort.DataBits = dataBits;
            }
            if (args.Length >= 3)
            {
                StopBits stopBits = args[2].GetType() == typeof(StopBits) ? (StopBits)args[2] : (StopBits)Enum.Parse(typeof(StopBits), args[2].ToString(), true);
                serialPort.StopBits = stopBits;
            }

            serialPort.Open();
            ThreadPool.QueueUserWorkItem(CheckConnectStatus);
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <param name="adapter"></param>
        protected void CheckConnectStatus(object adapter)
        {
            while (running)
            {
                if (serialPort == null) return;

                bool IsOpen = false;
                try { IsOpen = serialPort.IsOpen; }
                catch { }

                if (!IsOpen)
                {
                    string[] portNames = SerialPort.GetPortNames();
                    if (portNames?.Length <= 0 || Array.IndexOf(portNames, serialPort.PortName.ToUpper()) == -1)
                    {
                        Logger.Warn($"NModbus4 SerialPort Adapter Device Disconnect!");
                        //Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] NModbus4 SerialPort Adapter Device Disconnect!");
                        Thread.Sleep(2000);
                        continue;
                    }

                    Logger.Warn($"NModbus4 SerialPort Adapter Disconnect! {serialPort.PortName},{serialPort.BaudRate}");
                    //Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] NModbus4 SerialPort Adapter Disconnect! {serialPort.PortName},{serialPort.BaudRate}");

                    try
                    {
                        serialPort.Open();
                        IsOpen = serialPort.IsOpen;

                        if (IsOpen)
                        {
                            serialPort.DiscardInBuffer();
                            serialPort.DiscardOutBuffer();
                            Logger.Info($"NModbus4 SerialPort Adapter Reconnect Success! {serialPort.PortName},{serialPort.BaudRate}");
                            //Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] NModbus4 SerialPort Adapter Reconnect Success! {serialPort.PortName},{serialPort.BaudRate}");
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex); }
                }

                Thread.Sleep(2000);
            }
        }

        /// <inheritdoc/>
        public int InfiniteTimeout => -1;

        /// <inheritdoc/>
        public int ReadTimeout
        {
            get
            {
                try { return serialPort.ReadTimeout; }
                catch { return -1; }
            }
            set
            {
                try { serialPort.ReadTimeout = value; }
                catch { }
            }
        }

        /// <inheritdoc/>
        public int WriteTimeout
        {
            get
            {
                try { return serialPort.WriteTimeout; }
                catch { return -1; }
            }
            set
            {
                try { serialPort.WriteTimeout = value; }
                catch { }
            }
        }

        /// <inheritdoc/>
        public void Write(byte[] buffer, int offset, int size)
        {
            try { if (serialPort.IsOpen) serialPort.Write(buffer, offset, size); }
            catch (Exception) { }
        }

        /// <inheritdoc/>
        public int Read(byte[] buffer, int offset, int size)
        {
            try { return serialPort.IsOpen ? serialPort.Read(buffer, offset, size) : size; }
            catch (Exception ex) { Console.WriteLine(ex); return size; }
        }

        /// <inheritdoc/>
        public void DiscardInBuffer()
        {
            try { serialPort.DiscardInBuffer(); }
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
                running = false;

                serialPort?.Dispose();
                serialPort = null;
            }
        }

    }
}

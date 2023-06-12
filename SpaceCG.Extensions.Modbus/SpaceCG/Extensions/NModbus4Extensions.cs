#define NModbus4

using System;
using System.Net.Sockets;
using SpaceCG.Generic;
using Modbus.Device;
using System.Linq;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// NModbus4Extensions
    /// </summary>
    public static partial class NModbus4Extensions
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(NModbus4Extensions));

#if NModbus4

        #region NModbus4 翻转线圈扩展函数
        /// <summary>
        /// 翻转单线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        public static async void TurnSingleCoilAsync(this IModbusMaster master, byte slaveAddress, ushort startAddress)
        {
            if (master?.Transport == null) return;

            bool[] values = await master.ReadCoilsAsync(slaveAddress, startAddress, 1);
            if (values?.Length != 1) return;

            await master.WriteSingleCoilAsync(slaveAddress, startAddress, !values[0]);
        }
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        public static async void TurnMultipleCoilsAsync(this IModbusMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (master?.Transport == null) return;

            bool[] values = await master.ReadCoilsAsync(slaveAddress, startAddress, numberOfPoints);
            if (values?.Length <= 0) return;

            for (int i = 0; i < values.Length; i++)
                values[i] = !values[i];

            await master.WriteMultipleCoilsAsync(slaveAddress, startAddress, values);
        }
        /// <summary>
        /// 翻转多个线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="coilsAddresses"></param>
        public static async void TurnMultipleCoilsAsync(this IModbusMaster master, byte slaveAddress, ushort[] coilsAddresses)
        {
            if (master?.Transport == null) return;

            ushort startAddress = coilsAddresses.Min();
            ushort maxAddress = coilsAddresses.Max();
            ushort numberOfPoints = (ushort)(maxAddress - startAddress + 1);

            bool[] values = await master.ReadCoilsAsync(slaveAddress, startAddress, numberOfPoints);
            if (values?.Length <= 0) return;

            int i = 0;
            for (ushort address = startAddress; address <= maxAddress; address++, i++)
            {
                values[i] = Array.IndexOf(coilsAddresses, address) >= 0 ? !values[i] : values[i];
            }

            await master.WriteMultipleCoilsAsync(slaveAddress, startAddress, values);
        }
        /// <summary>
        /// 翻转单线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        public static void TurnSingleCoil(this IModbusMaster master, byte slaveAddress, ushort startAddress)
        {
            if (master?.Transport == null) return;

            bool[] values = master.ReadCoils(slaveAddress, startAddress, 1);
            if (values?.Length != 1) return;

            master.WriteSingleCoil(slaveAddress, startAddress, !values[0]);
        }
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        public static void TurnMultipleCoils(this IModbusMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (master?.Transport == null) return;

            bool[] values = master.ReadCoils(slaveAddress, startAddress, numberOfPoints);
            if (values?.Length <= 0) return;

            for (int i = 0; i < values.Length; i++)
                values[i] = !values[i];

            master.WriteMultipleCoils(slaveAddress, startAddress, values);
        }
        /// <summary>
        /// 翻转多个线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="coilsAddresses"></param>
        public static void TurnMultipleCoils(this IModbusMaster master, byte slaveAddress, ushort[] coilsAddresses)
        {
            if (master?.Transport == null) return;

            ushort startAddress = coilsAddresses.Min();
            ushort maxAddress = coilsAddresses.Max();
            ushort numberOfPoints = (ushort)(maxAddress - startAddress + 1);

            bool[] values = master.ReadCoils(slaveAddress, startAddress, numberOfPoints);
            if (values?.Length <= 0) return;

            int i = 0;
            for (ushort address = startAddress; address <= maxAddress; address++, i ++)
            {
                values[i] = Array.IndexOf(coilsAddresses, address) >= 0 ? !values[i] : values[i];
            }

            master.WriteMultipleCoils(slaveAddress, startAddress, values);
        }
        
        /// <summary>
        /// 翻转单线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        public static void TurnSingleCoilAsync(this ModbusSerialMaster master, byte slaveAddress, ushort startAddress) => TurnSingleCoilAsync((IModbusMaster)master, slaveAddress, startAddress);
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        public static void TurnMultipleCoilsAsync(this ModbusSerialMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints) => TurnMultipleCoilsAsync((IModbusMaster)master, slaveAddress, startAddress, numberOfPoints);
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="coilsAddresses"></param>
        public static void TurnMultipleCoilsAsync(this ModbusSerialMaster master, byte slaveAddress, ushort[] coilsAddresses) => TurnMultipleCoilsAsync((IModbusMaster)master, slaveAddress, coilsAddresses);
        /// <summary>
        /// 翻转单线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        public static void TurnSingleCoil(this ModbusSerialMaster master, byte slaveAddress, ushort startAddress) => TurnSingleCoil((IModbusMaster)master, slaveAddress, startAddress);
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        public static void TurnMultipleCoils(this ModbusSerialMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints) => TurnMultipleCoils((IModbusMaster)master, slaveAddress, startAddress, numberOfPoints);
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="coilsAddresses"></param>
        public static void TurnMultipleCoils(this ModbusSerialMaster master, byte slaveAddress, ushort[] coilsAddresses) => TurnMultipleCoils((IModbusMaster)master, slaveAddress, coilsAddresses);

        /// <summary>
        /// 翻转单线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        public static void TurnSingleCoilAsync(this ModbusIpMaster master, byte slaveAddress, ushort startAddress) => TurnSingleCoilAsync((IModbusMaster)master, slaveAddress, startAddress);
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        public static void TurnMultipleCoilsAsync(this ModbusIpMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints) => TurnMultipleCoilsAsync((IModbusMaster)master, slaveAddress, startAddress, numberOfPoints);
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="coilsAddresses"></param>
        public static void TurnMultipleCoilsAsync(this ModbusIpMaster master, byte slaveAddress, ushort[] coilsAddresses) => TurnMultipleCoilsAsync((IModbusMaster)master, slaveAddress, coilsAddresses);
        /// <summary>
        /// 翻转单线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        public static void TurnSingleCoil(this ModbusIpMaster master, byte slaveAddress, ushort startAddress) => TurnSingleCoil((IModbusMaster)master, slaveAddress, startAddress);
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        public static void TurnMultipleCoils(this ModbusIpMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints) => TurnMultipleCoils((IModbusMaster)master, slaveAddress, startAddress, numberOfPoints);
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="coilsAddresses"></param>
        public static void TurnMultipleCoils(this ModbusIpMaster master, byte slaveAddress, ushort[] coilsAddresses) => TurnMultipleCoils((IModbusMaster)master, slaveAddress, coilsAddresses);
        #endregion

        /// <summary>
        /// 创建 NModbus4 主机对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="hostORcom"></param>
        /// <param name="portORbaudRate"></param>
        /// <returns></returns>
        public static IModbusMaster CreateNModbus4Master(string type, string hostORcom, int portORbaudRate)
        {
            if (portORbaudRate <= 0) return null;
            if (String.IsNullOrWhiteSpace(type) || String.IsNullOrWhiteSpace(hostORcom))
                throw new ArgumentNullException("参数不能为空");

            IModbusMaster master;
            type = type.ToUpper().Trim();

            try
            {
                if (type.IndexOf("TCP") >= 0)
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(hostORcom, portORbaudRate);

                    if (type.IndexOf("RTU") == -1)
                        master = ModbusIpMaster.CreateIp(tcpClient);
                    else
                        master = ModbusSerialMaster.CreateRtu(tcpClient);
                }
                else if (type.IndexOf("UDP") >= 0)
                {
                    UdpClient udpClient = new UdpClient();
                    udpClient.Connect(hostORcom, portORbaudRate);

                    if (type.IndexOf("RTU") == -1)
                        master = ModbusIpMaster.CreateIp(udpClient);
                    else
                        master = ModbusSerialMaster.CreateRtu(udpClient);
                }
                else if (type.IndexOf("SERIAL") >= 0)
                {
                    System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort(hostORcom, portORbaudRate);
                    serialPort.Open();

                    master = ModbusSerialMaster.CreateRtu(serialPort);
                }
                else
                {
                    throw new ArgumentException($"不支持创建的类型：({type},{hostORcom},{portORbaudRate}) ", nameof(type));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"创建 ModbusMaster 对象 ({type},{hostORcom},{portORbaudRate}) 错误：{ex}");
                return null;
            }

            Logger.Info($"创建 ModbusMaster 对象 ({type},{hostORcom},{portORbaudRate}) 完成");

            //Dispose.
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                Logger.Info($"exit modbus ...");
                DisposeNModbus4Master(ref master);
            };

            return master;
        }
        
        /// <summary>
        /// 关闭并清理 NModbus4Master 对象
        /// </summary>
        /// <param name="master"></param>
        public static void DisposeNModbus4Master(ref IModbusMaster master)
        {
            if (master == null) return;

            try
            {
                Logger.Debug($"Dispose ModbusMaster: {master.Transport?.ToString()} {master.ToString()}");
                master.Transport?.Dispose();
                master.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error($"ModbusMaster Dispose Exception: {ex}");
            }
            finally
            {
                master = null;
            }
        }
#endif
    }
}

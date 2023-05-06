#define NModbus4

using System;
using System.Configuration;
using System.IO.Ports;
using System.Net.Sockets;

namespace SpaceCG.Generic
{
    public static partial class InstanceExtension
    {
#if NModbus4
        public static void Sleep(this Modbus.Device.IModbusMaster master, int millisecondsTimeout)
        {
            if (millisecondsTimeout > 0) System.Threading.Thread.Sleep(millisecondsTimeout);
        }

        /// <summary>
        /// 翻转单线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        public static async void TurnSingleCoilAsync(this Modbus.Device.IModbusMaster master, byte slaveAddress, ushort startAddress)
        {
            if (master?.Transport == null) return;

            bool[] value = await master.ReadCoilsAsync(slaveAddress, startAddress, 1);
            if (value?.Length != 1) return;

            await master.WriteSingleCoilAsync(slaveAddress, startAddress, !value[0]);
        }
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        public static async void TurnMultipleCoilisAsync(this Modbus.Device.IModbusMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (master?.Transport == null) return;

            bool[] value = await master.ReadCoilsAsync(slaveAddress, startAddress, numberOfPoints);
            if (value?.Length <= 0) return;

            for (int i = 0; i < value.Length; i++)
                value[i] = !value[i];

            await master.WriteMultipleCoilsAsync(slaveAddress, startAddress, value);
        }
        /// <summary>
        /// 翻转单线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        public static void TurnSingleCoil(this Modbus.Device.IModbusMaster master, byte slaveAddress, ushort startAddress)
        {
            if (master?.Transport == null) return;

            bool[] value = master.ReadCoils(slaveAddress, startAddress, 1);
            if (value?.Length != 1) return;

            master.WriteSingleCoil(slaveAddress, startAddress, !value[0]);
        }
        /// <summary>
        /// 翻转多线圈
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        public static void TurnMultipleCoilis(this Modbus.Device.IModbusMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (master?.Transport == null) return;

            bool[] value = master.ReadCoils(slaveAddress, startAddress, numberOfPoints);
            if (value?.Length <= 0) return;

            for (int i = 0; i < value.Length; i++)
                value[i] = !value[i];

            master.WriteMultipleCoils(slaveAddress, startAddress, value);
        }

        public static void TurnSingleCoilAsync(this Modbus.Device.ModbusSerialMaster master, byte slaveAddress, ushort startAddress) => TurnSingleCoilAsync((Modbus.Device.IModbusMaster)master, slaveAddress, startAddress);
        public static void TurnMultipleCoilisAsync(this Modbus.Device.ModbusSerialMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints) => TurnMultipleCoilisAsync((Modbus.Device.IModbusMaster)master, slaveAddress, startAddress, numberOfPoints);
        public static void TurnSingleCoilAsync(this Modbus.Device.ModbusIpMaster master, byte slaveAddress, ushort startAddress) => TurnSingleCoilAsync((Modbus.Device.IModbusMaster)master, slaveAddress, startAddress);
        public static void TurnMultipleCoilisAsync(this Modbus.Device.ModbusIpMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints) => TurnMultipleCoilisAsync((Modbus.Device.IModbusMaster)master, slaveAddress, startAddress, numberOfPoints);
        
        public static void TurnSingleCoil(this Modbus.Device.ModbusSerialMaster master, byte slaveAddress, ushort startAddress) => TurnSingleCoil((Modbus.Device.IModbusMaster)master, slaveAddress, startAddress);
        public static void TurnMultipleCoilis(this Modbus.Device.ModbusSerialMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints) => TurnMultipleCoilis((Modbus.Device.IModbusMaster)master, slaveAddress, startAddress, numberOfPoints);
        public static void TurnSingleCoil(this Modbus.Device.ModbusIpMaster master, byte slaveAddress, ushort startAddress) => TurnSingleCoil((Modbus.Device.IModbusMaster)master, slaveAddress, startAddress);
        public static void TurnMultipleCoilis(this Modbus.Device.ModbusIpMaster master, byte slaveAddress, ushort startAddress, ushort numberOfPoints) => TurnMultipleCoilis((Modbus.Device.IModbusMaster)master, slaveAddress, startAddress, numberOfPoints);

        /// <summary>
        /// 创建 NModbus4 主机对象
        /// <para>配置的键值格式：Type,RemoteHostORcomm,RemotePortORbaudRate</para>
        /// <para>Type支持：TCP/TCP-RTU/UDP/UDP-RTU/SERIAL </para>
        /// </summary>
        /// <param name="cfgKey">Config Key Format:(type,hostORcom,portORbaudRate)</param>
        /// <returns></returns>
        public static Modbus.Device.IModbusMaster CreateNModbus4Master(string cfgKey)
        {
            if (String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[cfgKey])) return null;

            String[] args = ConfigurationManager.AppSettings[cfgKey].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length != 3) throw new ArgumentException("参数格式不正确");

            if (!int.TryParse(args[2], out int portOrbaudRate)) return null;

            return CreateNModbus4Master(args[0], args[1], portOrbaudRate);
        }
        /// <summary>
        /// 创建 NModbus4 主机对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="hostORcom"></param>
        /// <param name="portORbaudRate"></param>
        /// <returns></returns>
        public static Modbus.Device.IModbusMaster CreateNModbus4Master(string type, string hostORcom, int portORbaudRate)
        {
            if (portORbaudRate <= 0) return null;
            if (String.IsNullOrWhiteSpace(type) || String.IsNullOrWhiteSpace(hostORcom))
                throw new ArgumentNullException("参数不能为空");

            Modbus.Device.IModbusMaster master;
            type = type.ToUpper().Trim();

            try
            {
                if (type.IndexOf("TCP") >= 0)
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(hostORcom, portORbaudRate);

                    if (type.IndexOf("RTU") == -1)
                        master = Modbus.Device.ModbusIpMaster.CreateIp(tcpClient);
                    else
                        master = Modbus.Device.ModbusSerialMaster.CreateRtu(tcpClient);
                }
                else if (type.IndexOf("UDP") >= 0)
                {
                    UdpClient udpClient = new UdpClient();
                    udpClient.Connect(hostORcom, portORbaudRate);

                    if (type.IndexOf("RTU") == -1)
                        master = Modbus.Device.ModbusIpMaster.CreateIp(udpClient);
                    else
                        master = Modbus.Device.ModbusSerialMaster.CreateRtu(udpClient);
                }
                else if (type.IndexOf("SERIAL") >= 0)
                {
                    SerialPort serialPort = new SerialPort(hostORcom, portORbaudRate);
                    serialPort.Open();

                    master = Modbus.Device.ModbusSerialMaster.CreateRtu(serialPort);
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
        public static void DisposeNModbus4Master(ref Modbus.Device.IModbusMaster master)
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

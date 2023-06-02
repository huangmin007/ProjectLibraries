using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using SpaceCG.Extensions;
using SpaceCG.Generic;
using SpaceCG.Module.Modbus;
using SpaceCG.Net;

namespace Test
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window,IDisposable
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(SerialPortExtensions));

        Modbus.Device.IModbusMaster master;
        ModbusTransportDevice transport;
        ModbusIODevice device;

        ModbusDeviceManager deviceManager;
        ControllerInterface ControllerInterface = new ControllerInterface(2023);

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.Level = log4net.Core.Level.Debug;
#endif
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            deviceManager?.Dispose();

            Client?.Dispose();
            Server?.Dispose();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if(e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //ControllerInterface.InstallKeyboardService(true);
            ControllerInterface.AddControlObject("Window", this);
#if false
            ushort[] addresses = {1,2,5,6,8,9,10,20,21,22,23 };
            Dictionary<ushort, ushort>  result = ModbusDevice.SpliteAddresses(addresses);

            foreach(var kv in result)
                Console.WriteLine($"startAddress:{kv.Key} numof:{kv.Value}");
#endif
#if false
            Dictionary<ushort, ushort> registers = new Dictionary<ushort, ushort>();
            registers.Add(0x01, 0x1122);
            registers.Add(0x02, 0x3344);
            registers.Add(0x03, 0x5566);
            registers.Add(0x04, 0x7788);

            Register description = Register.Create(0x0001, RegisterType.HoldingRegister, 4, true);

            var value = ModbusIODevice.GetRegisterValue(description);

            Console.WriteLine(">>> {0:X16}", value);
#endif
#if false
            Console.WriteLine("Loading..");
            master = NModbus4Extensions.CreateNModbus4Master("SERIAL", "COM3", 9600);

            if (master == null) throw new Exception("Master Error.");

            device = new ModbusIODevice(0x01, "LH-IO222");
            device.AddRegister(0, RegisterType.CoilsStatus, 2);
            device.AddRegister(0, RegisterType.DiscreteInput, 2);
            device.AddRegister(0, RegisterType.InputRegister);
            device.AddRegister(1, RegisterType.InputRegister);

            transport = new ModbusTransportDevice(master, "传输总线x");
            transport.AddIODevice(device);
            transport.StartTransport();

            transport.InputChangeEvent += Transport_InputChangeEvent;
            transport.OutputChangeEvent += Transport_OutputChangeEvent;
#endif
            //String str = "0xAA";
            //bool result = byte.TryParse(str, NumberStyles.AllowHexSpecifier | NumberStyles.HexNumber, null, out byte value);
            //Console.WriteLine($"resul: {result} , {value}");

            //String registerAddress = $"{RegisterType.CoilsStatus}Address";
            //Console.WriteLine(registerAddress);

            //Console.WriteLine(typeof(NumberStyles));

            //deviceManager = new ModbusDeviceManager();
            //deviceManager.LoadDeviceConfig("ModbusDevices.Config");

#if false
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            //object[] arr2 = StringExtension.ConvertParameters2("0x01,3,[True,True,False]");
            //object[] arr2 = StringExtensions.SplitParameters("0x03");
            //object[] arr2 = StringExtensions.SplitParameters("'hello,world','ni,hao'");
            object[] arr2 = StringExtensions.SplitParameters("'hello,world',0x01,3,'ni?,hao,[aa,bb]', [True,True,False],['aaa,bb,c','ni,hao'],15,\"aa,aaa\",15");

            Console.WriteLine(arr2.Length);
            foreach(var o in arr2)
            {
                if (o.GetType().IsArray)
                {
                    foreach(var so in (Array)o)
                    {
                        Console.Write($"{so},,,");
                    }
                    Console.WriteLine("");
                }
                else
                {
                    Console.WriteLine(o);
                }
            }


            //String s = "\'aaaa,bbbb\'";
            //object[] objs = StringExtensions.SplitParameters(s);
            //foreach (var o in objs)

            //Console.WriteLine($"=>{o}");


            //bool result = StringExtensions.TryParse("45", out UInt32 v);
            //Console.WriteLine($"{result},,{v}");

            //var value = StringExtensions.ConvertParamsToValueType(typeof(Byte), 0x45);
            //var array = StringExtensions.ConvertParamsToArrayType(typeof(Byte[]), new Object[] { "0x45", "0x46", 0x47 });

            //WindowStyle.None
            //InstanceExtensions.SetInstancePropertyValue(this, "WindowStyle", 0);

            //Console.WriteLine(value);
#endif

            //ControllerInterface ControlInterface = new ControllerInterface(2000);
            //ControlInterface.AccessObjects.TryAdd("a", this);

            //int v = 0B1101;
            //Byte value = 0x00;
            //StringExtensions.TryParse("B1101", out value);

            //ushort.TryParse("1101", NumberStyles.Number, null, out ushort result);
            //Console.WriteLine(result);

            //StringExtensions.TryParse("0B1101_1101", out byte result);
            //Console.WriteLine($"Result:::{result}");

            //StringExtensions.TryParse("4545", out short result2);
            //Console.WriteLine($"Result2:::{result2}");

            //short[] result3 = new short[] { 0x01, 0x02 };
            //StringExtensions.TryParse<short>("0x4545,0B1101", ref result3);

            //Console.WriteLine($"Result3:::{result3}");

            //Console.WriteLine(Convert.ToByte("46", 16));
            //Console.WriteLine(Convert.ToByte("1101", 2));

            //Console.WriteLine(bool.TryParse("fAlse", out bool rv));
            //Console.WriteLine(rv);

            //serialPort = new SerialPort("Com3", 115200);
            //AutoReconnection2(serialPort, this);
            //SerialPortExtensions.AutoReconnection(serialPort);
            //serialPort.Open();

#if false
            //Server = new AsyncUdpServer(2201);
            Server = new AsyncTcpServer(2200);
            Server.ClientConnected += Server_ClientConnected;
            Server.ClientDisconnected += Server_ClientDisconnected;
            Server.ClientDataReceived += Server_ClientDataReceived;
            //Server.Start();

            Client = new AsyncTcpClient();
            //Client = new AsyncUdpClient();
            Client.Connected += Client_Connected;
            Client.Disconnected += Client_Disconnected;
            Client.DataReceived += Client_DataReceived;
            Client.Exception += Client_Exception;
            //Client.Connect("192.168.40.212", 2204);
#endif
            test();
        }

        private void test()
        {
            //String parameters = "15";
            String parameters = "'hello,world',0x01,3,'ni?,hao,[aa,bb]', [True,True,False],['aaa,bb,c','ni,hao'],15,\"aa,aaa\",15";
            Console.WriteLine(parameters);

            MatchCollection matchs = StringExtensions.RegexStringArguments.Matches(parameters);
            foreach(Match match in matchs)
            {
                Console.WriteLine($"({match}) {match.Name} {match.Success},{match.Value} ");
                if (!match.Success) continue;

                foreach(Group group in match.Groups)
                {
                    Console.WriteLine($"\t-{group.Name} {group.Success}");
                    if(match.Name != group.Name && group.Success)
                    {
                        Console.WriteLine($"\t\t{group.Value}");
                    }
                }
            }

        }

        private void Client_Exception(object sender, AsyncExceptionEventArgs e)
        {
            Console.WriteLine($"Exception::{e.EndPoint},{e.Exception.GetType()}");
            //SocketException ex = (SocketException)e.Exception;
            //Console.WriteLine($"{ex.ErrorCode},,{ex.SocketErrorCode},,{ex.Message}");
        }

        private void Client_DataReceived(object sender, AsyncDataEventArgs e)
        {
            Console.WriteLine($"Data::{e.EndPoint}");
            String msg = Encoding.UTF8.GetString(e.Bytes);
            Console.WriteLine($"Message::{msg}");
        }

        private void Client_Disconnected(object sender, AsyncEventArgs e)
        {
            Console.WriteLine($"Disconnected::{e.EndPoint}");
        }

        private void Client_Connected(object sender, AsyncEventArgs e)
        {
            Console.WriteLine($"Connected::{e.EndPoint}");
        }

        IAsyncClient Client;
        IAsyncServer Server;
        SerialPort serialPort;

        private void Server_ClientDataReceived(object sender, AsyncDataEventArgs e)
        {
            Console.WriteLine($"Data::{e.EndPoint} {Encoding.UTF8.GetString(e.Bytes)}");

            IPEndPoint endPoint = (IPEndPoint)e.EndPoint;
            ((IAsyncServer)sender).SendBytes(Encoding.UTF8.GetBytes("Hellowwww"), endPoint);
            
            //Console.WriteLine($"{endPoint.Address},,{endPoint.Port}");
            //((IAsyncServer)sender).Send(endPoint.Address.ToString(), endPoint.Port, Encoding.UTF8.GetBytes("Hellowwww"));
        }

        private void Server_ClientDisconnected(object sender, AsyncEventArgs e)
        {
            Console.WriteLine($"Disconnected::{e.EndPoint}");
        }

        private void Server_ClientConnected(object sender, AsyncEventArgs e)
        {
            Console.WriteLine($"Connected::{e.EndPoint}");
        }

        private void UdpServer_DataReceived(AsyncUdpServer arg1, EndPoint arg2, byte[] arg3)
        {
            String msg = Encoding.UTF8.GetString(arg3);
            Console.WriteLine($"{arg2} say: {msg}");
        }

        private void Transport_OutputChangeEvent(ModbusTransportDevice transportDevice, ModbusIODevice slaveDevice, Register register)
        {
            Console.WriteLine(register);
            if(register.Type == RegisterType.CoilsStatus || register.Type == RegisterType.DiscreteInput)
                Console.WriteLine($"OutputChange {transportDevice} slaveAddress:{slaveDevice.Address}, registerAddress:{register.Address}, newValue:{Convert.ToString((int)register.Value, 2)}, oldValue:{register.LastValue}");
        }

        private void Transport_InputChangeEvent(ModbusTransportDevice transportDevice, ModbusIODevice slaveDevice, Register register)
        {
            Console.WriteLine(register);
            Console.WriteLine($"InputChange {transportDevice} slaveAddress:{slaveDevice.Address}, registerAddress:{register.Address}, newValue:{Convert.ToString((int)register.Value, 2)}, oldValue:{register.LastValue}");
        }

#if false
        private void Device_InputChangeHandler(byte slaveAddress, ushort registerAddress, ulong newValue, ulong oldValue)
        {
            //Thread.Sleep(4000);
            Task.Run(() =>
            {
                Console.WriteLine($"ElapsedMilliseconds: {transport.ElapsedMilliseconds} ms");
                Console.WriteLine($"InputChange slaveAddress:{slaveAddress}, registerAddress:{registerAddress}, newValue:{newValue}, oldValue:{oldValue}");
            });
        }

        private void Device_OutputChangeHandler(byte slaveAddress, ushort registerAddress, ulong newValue, ulong oldValue)
        {
            Task.Run(() =>
            {
                Console.WriteLine($"OutputChange slaveAddress:{slaveAddress}, registerAddress:{registerAddress}, newValue:{newValue}, oldValue:{oldValue}");
            });
        }
#endif

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if(button == Button_Test1)
            {
                //deviceManager.LoadDeviceConfig("ModbusDevices.Config");
                //transport.TurnSingleCoil(device.Address, 0);
                //transport.WriteSingleCoil(device.Address, 0, true);

                //ICollection<EndPoint> clients = Server.Clients;
                //Console.WriteLine(clients.IsReadOnly);

                //ICollection<EndPoint> clients2 = Server.Clients;
                //Console.WriteLine(clients2.IsReadOnly);


            }
            else if(button == Button_Test2)
            {
                Byte[] data = Encoding.UTF8.GetBytes("Hello World");
                //udpServer.SendBytes("127.0.0.1", 10000, data);
                //transport.TurnSingleCoil(device.Address, 1);

                Client.SendBytes(data);
                //((AsyncUdpClient)Client).SendTo(data, "192.168.40.212", 10000);
                //((AsyncTcpClient)Client).TestSendTo("192.168.40.212", 38952, data);
            }
            else if(button == Button_Start)
            {
                //server.Start();

                //transport.StartTransport();
                Client.Connect("127.0.0.1", 2205);
            }
            else if(button == Button_Stop)
            {
                //server.Stop();

                //transport.StopTransport();
                Client.Close();
            }
        }

        public int Add(int a, int b = 100)
        {
            Console.WriteLine($"AAA {a}, {b}");
            return a + b;
        }

        public void echo(String msg, String a)
        {
            Console.WriteLine($"ECHO::{msg} say::{a}");
            //<Action Target="Window" Method="echo" Params="\'hellowol\'" />
        }

        public void Dispose()
        {
            
        }
    }
}

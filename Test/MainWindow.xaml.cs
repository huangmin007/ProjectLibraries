﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpaceCG.Extensions;
using SpaceCG.Module.Modbus;
using SpaceCG.Module.Reflection;

namespace Test
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window,IDisposable
    {
        Modbus.Device.IModbusMaster master;
        ModbusTransportDevice transport;
        ModbusIODevice device;

        ModbusDeviceManager deviceManager;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            deviceManager?.Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

            deviceManager = new ModbusDeviceManager();
            deviceManager.LoadDeviceConfig("ModbusDevices.Config");

#if false
            //object[] arr1 = StringExtension.ConvertParameters2("0x01,3,[True,True,False]");
            object[] arr2 = StringExtensions.SplitParameters("0x01,3,[True,True,False]");
            Console.WriteLine(arr2.Length);

            bool result = StringExtensions.TryParse("45", out UInt32 v);
            Console.WriteLine($"{result},,{v}");

            var value = StringExtensions.ConvertParamsToValueType(typeof(Byte), 0x45);
            var array = StringExtensions.ConvertParamsToArrayType(typeof(Byte[]), new Object[] { "0x45", "0x46", 0x47 });
            
            //WindowStyle.None
            InstanceExtensions.SetInstancePropertyValue(this, "WindowStyle", 0);

            Console.WriteLine(value);
#endif

            //ControllerInterface ControlInterface = new ControllerInterface(2000);
            //ControlInterface.AccessObjects.TryAdd("a", this);

            int v = 0B1101;
            Byte value = 0x00;
            //StringExtensions.TryParse("B1101", out value);

            //ushort.TryParse("1101", NumberStyles.Number, null, out ushort result);
            //Console.WriteLine(result);

            StringExtensions.TryParse("0B1101_1101", out byte result);
            Console.WriteLine($"Result:::{result}");

            StringExtensions.TryParse("4545", out short result2);
            Console.WriteLine($"Result2:::{result2}");

            short[] result3 = new short[] { 0x01, 0x02 };
            StringExtensions.TryParse<short>("0x4545,0B1101", ref result3);

            Console.WriteLine($"Result3:::{result3}");

            //Console.WriteLine(Convert.ToByte("46", 16));
            //Console.WriteLine(Convert.ToByte("1101", 2));

            //Console.WriteLine(bool.TryParse("fAlse", out bool rv));
            //Console.WriteLine(rv);
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
                deviceManager.LoadDeviceConfig("ModbusDevices.Config");
                //transport.TurnSingleCoil(device.Address, 0);
                //transport.WriteSingleCoil(device.Address, 0, true);
            }
            else if(button == Button_Test2)
            {
                
                //transport.TurnSingleCoil(device.Address, 1);
            }
            else if(button == Button_Start)
            {
                //transport.StartTransport();
            }
            else if(button == Button_Stop)
            {
                //transport.StopTransport();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using SpaceCG.ModbusExtension;

namespace Test
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Modbus.Device.IModbusMaster master;

        public MainWindow()
        {
            InitializeComponent();
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

            RegisterDescription description = RegisterDescription.Create(0x0001, 4, RegisterType.HoldingRegister, true);

            var value = ModbusDevice.GetRegistersValue(description);
            Console.WriteLine(">>> {0:X16}", value);
#endif

            

            ModbusIODevice device = new ModbusIODevice(0x01, "控制IO");
            device.AddRegisters(0, RegisterType.CoilsStatus, 4);
            device.OutputChangeHandler += Device_OutputChangeHandler;
            device.InitializeDevice();

            ModbusTransportDevice transport = new ModbusTransportDevice(master, "传输总线x");
            transport.AddDevice(device);
            transport.StartTransport();

            transport.WriteSingleCoil(device.Address, 2, true);
        }

        private void Device_OutputChangeHandler(byte slaveAddress, ushort registerAddress, ulong newValue, ulong oldValue)
        {
            
        }
    }
}

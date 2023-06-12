
### READE.md

- ### SpaceCG.Extensions.Modbus
    - 跨平台，支持 Window, Linux, Android
    - 依赖项，.Net Standard >= 2.0
    - 依赖项，NModbus4.Core >= 1.0.2
    - 依赖项，System.IO.Ports >= 7.0.0
    - 依赖项，SpaceCG.Generic

- ### 代码
    - ModbusDeviceManager: 设备管理对象，参考 ModbusDevices.Config 协议配置
    - ModbusTransportDevice: Modbus传输设备，支持添加多个设备，设备数据同步，寄存器数据事件等
    - ModbusIODevice: Modbus IO 设备，支持寄存定义，线圈的翻转操作等
    - 等
    - 

```
NuGet install-package: SpaceCG.Extensions.Modbus
```

- ### 应用示例
```C#
//第一种使用方式：配置数据, 参考 ModbusDevices.Config 文件
ModbusDeviceManager modbusDeviceManager = new ModbusDeviceManager();
private void Initialize()
{
    modbusDeviceManager.LoadDeviceConfig("ModbusDevices.Config");
    //modbusDeviceManager?.Dispose();
    //modbusDeviceManager = null;
}
```

```C#
//第二种使用方式：创建对象
IModbusMaster master;
ModbusTransport modbusTransport;

private void Initialize()
{
    System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort("COM3", 9600);
    serialPort.Open();
    master = ModbusSerialMaster.CreateRtu(serialPort);

    modbusTransport = new ModbusTransport(master, "test bus");
    ModbusIODevice device = new ModbusIODevice(0x01, "LH-IO204");
    for(ushort i = 0; i < 2; i ++)
        device.Registers.Add(new Register(i, RegisterType.CoilsStatus));
    for (ushort i = 0; i < 4; i++)
        device.Registers.Add(new Register(i, RegisterType.DiscreteInput));
    device.Registers.Add(new Register(0x02, RegisterType.DiscreteInput, 2));

    modbusTransport.ModbusDevices.Add(device);
    modbusTransport.InputChangeEvent += ModbusTransport_InputChangeEvent;
    modbusTransport.OutputChangeEvent += ModbusTransport_OutputChangeEvent;
    modbusTransport.StartTransport();

    //modbusTransport?.StopTransport();
    //modbusTransport?.Dispose();
    //modbusTransport = null;
}

private void ModbusTransport_OutputChangeEvent(ModbusTransport transport, ModbusIODevice device, Register register)
{
    if(register.Count == 1)
        Console.WriteLine($"Output: {transport} DeviceAddress:{device.Address} RegisterAddress:{register} RegisterValue:{register.Value}");
    else
        Console.WriteLine($"Output: {transport} DeviceAddress:{device.Address} RegisterAddress:{register} RegisterValue:{Convert.ToString((int)register.Value, 2)}");
}

private void ModbusTransport_InputChangeEvent(ModbusTransport transport, ModbusIODevice device, Register register)
{
    if (register.Count == 1)
        Console.WriteLine($"Input: {transport} DeviceAddress:{device.Address} RegisterAddress:{register} RegisterValue:{register.Value}");
    else
        Console.WriteLine($"Input: {transport} DeviceAddress:{device.Address} RegisterAddress:{register} RegisterValue:{Convert.ToString((int)register.Value, 2)}");
}
```

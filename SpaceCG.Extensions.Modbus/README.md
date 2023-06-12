
### READE.md

- ### SpaceCG.Extensions.Modbus
    - ��ƽ̨��֧�� Window, Linux, Android
    - �����.Net Standard >= 2.0
    - �����NModbus4.Core >= 1.0.2
    - �����System.IO.Ports >= 7.0.0
    - �����SpaceCG.Generic

- ### ����
    - ModbusDeviceManager: �豸������󣬲ο� ModbusDevices.Config Э������
    - ModbusTransportDevice: Modbus�����豸��֧����Ӷ���豸���豸����ͬ�����Ĵ��������¼���
    - ModbusIODevice: Modbus IO �豸��֧�ּĴ涨�壬��Ȧ�ķ�ת������
    - ��
    - 

```
NuGet install-package: SpaceCG.Extensions.Modbus
```

- ### Ӧ��ʾ��
```C#
//��һ��ʹ�÷�ʽ����������, �ο� ModbusDevices.Config �ļ�
ModbusDeviceManager modbusDeviceManager = new ModbusDeviceManager();
private void Initialize()
{
    modbusDeviceManager.LoadDeviceConfig("ModbusDevices.Config");
    //modbusDeviceManager?.Dispose();
    //modbusDeviceManager = null;
}
```

```C#
//�ڶ���ʹ�÷�ʽ����������
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


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


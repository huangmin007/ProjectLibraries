
### READE.md

- ### SpaceCG.Generic
    - 跨平台，支持 Window, Linux, Android
    - 依赖项，.Net Standard >= 2.0

- ### 库
    - 轻量的日志跟踪 SpaceCG.Generic.LoggerTrace 
    - 控制接口对象 SpaceCG.Generic.ControlInterface
    - UDP,TCP 通信包 SpaceCG.Net
    - 反射函数、文本操作扩展函数
    - 等
    - 

```
NuGet install-package: SpaceCG.Generic
```
- ### 示例代码 LoggerTrace
```C#
//默认输出到日志文件、控制台，具体参考源码 LoggerTrace.cs
private static readonly LoggerTrace Logger = new LoggerTracce();
private void Initialize()
{
    Logger.Info("中文测试");
    Logger.Info("Hello");
    
    //输出事件，写入文本框
    ETextWriterTraceListener textWriterTrace = Logger.TraceSource.Listeners[0] as ETextWriterTraceListener;
    if (textWriterTrace != null) textWriterTrace.WriteEvent += (s, we) =>
    {
        TextBox_Trace?.Dispatcher.InvokeAsync(() => TextBox_Trace.AppendText(we.Message));
    };
}
```

- ### 示例代码 ControlInterface
```C#
ControlInterface controlInterface;
private void Initialize()
{
    controlInterface = new ControlInterface(2023);
    controlInterface.AccessObjects.Add("name", object);
    controlInterface.AccessObjects.Add("name2", object2);
    controlInterface.MethodFilters.Add("*.Dispose");

    //controlInterface?.Dispose();
    //controlInterface = null;
}
```

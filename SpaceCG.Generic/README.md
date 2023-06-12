
### READE.md

- ### SpaceCG.Generic
    - ��ƽ̨��֧�� Window, Linux, Android
    - �����.Net Standard >= 2.0

- ### ��
    - ��������־���� SpaceCG.Generic.LoggerTrace 
    - ���ƽӿڶ��� SpaceCG.Generic.ControlInterface
    - UDP,TCP ͨ�Ű� SpaceCG.Net
    - ���亯�����ı�������չ����
    - ��
    - 

```
NuGet install-package: SpaceCG.Generic
```
- ### ʾ������ LoggerTrace
```C#
//Ĭ���������־�ļ�������̨������ο�Դ�� LoggerTrace.cs
private static readonly LoggerTrace Logger = new LoggerTracce();
private void Initialize()
{
    Logger.Info("���Ĳ���");
    Logger.Info("Hello");
    
    //����¼���д���ı���
    ETextWriterTraceListener textWriterTrace = Logger.TraceSource.Listeners[0] as ETextWriterTraceListener;
    if(textWriterTrace != null) textWriterTrace.WriteEvent += (s, e) => TextBox_Trace.AppendText(e.Message);
}
```

- ### ʾ������ ControlInterface
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

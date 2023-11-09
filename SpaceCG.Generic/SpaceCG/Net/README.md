## RPC 消息协议应用示例
***
> RPC协议：远程过程调用(Remote Procedure Call) 或 反射程序控制(Reflection Program Control)


### RPCServer 服务端或被调用端
``` C#
RPCServer server = new RPCServer(21025);
server.Start();					//启动服务
server.AccessObjects.Add("demo", this);		//添加可调用的对象实例
server.AccessObjects.Add(nameof(Thread), typeof(Thread));
//在本地测试或调用
var invokeResult = server.TryCallMethod("demo", "OpenPage", new object[]{ 1 });
var invokeResult = await server.TryCallMethodAsync("Thread", "Sleep", new object[]{ 300 });
//添加实例对象
int number = 12;
server.AccessObjects.Add(nameof(number), number);
var invokeResult = server.TryCallMethodAsync("number", "ToString");
//禁止访问实例的方法集合
server.MethodFilters.Add("*.Displse");	//禁止访问所有实例的 Dispose 方法，'*' 为能配符
server.MethodFilters.Add("demo.Close");	//禁止访问实例名为 demo 的 Close 方法
```

### RPCClient 客户端或远程调用端
```C#
RPCClient client = new RPCClient("hostname", 21025);
client.ConnectAsync();
//调用远程方法或函数
InvokeResult invokeResult = client.TryCallMethod("demo", "OpenPage", new object[]{ 1 });	//调用远程实例 demo 的方法
InvokeResult invokeResult = await server.TryCallMethodAsync("Thread", "Sleep", new object[]{ 300 });  //调用远程 Thread.Sleep 的静态方法

InvokeResult invokeResult = await server.TryCallMethodAsync("deom", "Close");	//返回结果为 Failed，ExceptionMessage 为调用失败原因
InvokeResult invoekResult = await server.TryCallMethodAsync("number", "ToString");	//返回结果为 SuccessAndReturn
```

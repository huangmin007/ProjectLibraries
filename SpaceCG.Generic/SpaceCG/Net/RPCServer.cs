using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using SpaceCG.Extensions;

namespace SpaceCG.Net
{
    
    /// <summary>
    /// XML-RPC 服务端
    /// </summary>
    public class RPCServer : IDisposable
    {
        /// <summary>
        /// Buffer Size
        /// </summary>
        internal const int BUFFER_SIZE = 1024 * 10;

        private int _localPort;
        private string _serverName;
        private IPAddress _localAddress;

        private bool _isDisposed;
        private TcpListener _tcpLstener;
        private CancellationTokenSource _cts;

        private UdpClient _udpDiscoveryServer;
        private SynchronizationContext _syncContext;

        /// <summary>  已连接客户端集合  </summary>
        public IReadOnlyList<TcpClient> Clients => _clients;
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        /// <summary>
        /// 可控制、访问的对象的方法过滤集合，指定对象的方法不在访问范围内；
        /// <para>字符格式为：{ObjectName}.{MethodName}, ObjectName 支持通配符 '*'</para>
        /// <para>例如："*.Dispose" 禁止反射访问所有对象的 Dispose 方法，默认已添加</para>
        /// </summary>
        public readonly List<string> MethodFilters = new List<string>(16) { "*.Dispose", "*.Close" };
        /// <summary>
        /// 注册的对象实例集合
        /// </summary>
        protected readonly ConcurrentDictionary<string, object> RegisteredObjects = new ConcurrentDictionary<string, object>();
        /// <summary>
        /// 历史调用过的唯一方法，无歧义的方法
        /// </summary>
        protected readonly ConcurrentDictionary<string, MethodInfo> HistoryMethodInfos = new ConcurrentDictionary<string, MethodInfo>();

        /// <summary>
        /// 服务名称、标识或 Demo 名称、标识
        /// </summary>
        public string Name => _serverName;
        /// <summary>
        /// 获取服务是否正在运行
        /// </summary>
        public bool IsRunning => _tcpLstener != null;

        /// <summary>
        /// 客户端连接事件
        /// </summary>
        public event EventHandler<EndPoint> ClientConnected;
        /// <summary>
        /// 客户端断开连接事件
        /// </summary>
        public event EventHandler<EndPoint> ClientDisconnected;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="localPort"></param>
        /// <param name="serverName"></param>
        public RPCServer(IPAddress ipAddress, int localPort, string serverName)
        {
            if (localPort < 1 || localPort > 65535)
                throw new ArgumentException("端口号必须在 1-65535 之间");

            if (string.IsNullOrWhiteSpace(serverName))
                throw new ArgumentException("服务名称不能为空");

            _localPort = localPort;
            _localAddress = ipAddress;

            _serverName = serverName;
            _syncContext = SynchronizationContext.Current;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="localPort"></param>
        /// <param name="serverName"></param>
        public RPCServer(int localPort, string serverName) : this(IPAddress.Any, localPort, serverName)
        {
        }        

        /// <summary>
        /// 注册可远程调用的对象
        /// </summary>
        /// <param name="objectName">对象名称</param>
        /// <param name="objectInstance">对象实例</param>
        public void RegisterObject(string objectName, object objectInstance)
        {
            if (string.IsNullOrWhiteSpace(objectName) || InvokeMessage.NamedRegex.IsMatch(objectName) == false)
                throw new ArgumentNullException(nameof(objectName), "对象名称不能为空或命名格式不正确");

            if (objectInstance == null)
                throw new ArgumentNullException(nameof(objectInstance), "对象实例不能为空");

            var objectType = objectInstance.GetType();
            if (objectType.IsValueType || objectType == typeof(RPCClient))
                throw new ArgumentException($"不能注册的对象实例类型 {objectType}");

            if (RegisteredObjects.ContainsKey(objectName))
            {
                RegisteredObjects[objectName] = objectInstance;
            }
            else
            {
                RegisteredObjects.TryAdd(objectName, objectInstance);
            }
        }

        /// <summary>
        /// 启动服务端
        /// </summary>
        public async Task StartAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(RPCServer));

            if (IsRunning) return;
            _cts = new CancellationTokenSource();
            _tcpLstener = new TcpListener(_localAddress, _localPort);

            try
            {
                _tcpLstener.Start();
                Trace.WriteLine($"XML-RPC 服务端已启动，监听 {_tcpLstener.LocalEndpoint}");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"启动 XML-RPC 服务端失败: ({ex.GetType().Name}){ex.Message}");
                await StopAsync();
                throw ex;
            }

            StartDiscoveryService(_localPort);

            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var client = await _tcpLstener.AcceptTcpClientAsync().ConfigureAwait(false);
                    _ = HandleClientReadAsync(client, _cts.Token);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                // 正常退出
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.OperationAborted) return; // 正常退出
                Trace.TraceInformation($"XML-RPC 服务端异常退出: ({ex.GetType().Name} SocketErrorCode:{ex.SocketErrorCode}){ex.Message}");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"XML-RPC 服务端等待客户端连接时异常: ({ex.GetType().Name}){ex.Message}");
            }
        }
        /// <summary>
        /// 停止服务端
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task StopAsync()
        {
            if (!IsRunning) return;

            _cts.Cancel();
            await Task.Delay(10);

            try
            {
                _udpDiscoveryServer.Dispose();
            }
            catch
            {
                // 忽略
            }

            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    try
                    {
                        client.Dispose();
                    }
                    catch
                    {
                        // 忽略
                    }
                }
                _clients.Clear();
            }
            
            try
            {
                _tcpLstener.Stop();
                _tcpLstener.Server.Dispose();
            }
            catch
            {
                // 忽略
            }

            _cts.Dispose();

            _cts = null;
            _tcpLstener = null;
            Trace.TraceInformation($"XML-RPC 服务端已停止");
        }

        /// <summary>
        /// 启动 UDP 发现服务
        /// </summary>
        protected void StartDiscoveryService(int udpPort = 12345)
        {
            if (udpPort < 1 || udpPort > 65535 || _udpDiscoveryServer != null) return;

            try
            {
                _udpDiscoveryServer = new UdpClient(_localPort);
                _udpDiscoveryServer.EnableBroadcast = true;

                // 开始监听UDP探测请求
                _ = Task.Run(() => ListenDiscoveryRequest(_cts.Token));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"启动 UDP 探测服务失败: ({ex.GetType().Name}){ex.Message}");
            }
        }
        /// <summary>
        /// 监听 UDP 发现探测请求
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task ListenDiscoveryRequest(CancellationToken cancelToken)
        {
            Trace.TraceInformation($"XML-RPC({Name}) 发现探测服务已启动");

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpDiscoveryServer.ReceiveAsync();
                    if (cancelToken.IsCancellationRequested) break;

                    var remoteEndPoint = result.RemoteEndPoint;
                    var requestMessage = Encoding.UTF8.GetString(result.Buffer);
                    Trace.TraceInformation($"收到客户端 {remoteEndPoint} 发现探测请求: {requestMessage}");

                    // 解析请求消息（格式："DISCOVER:<ServerName>"）
                    if (requestMessage.StartsWith("DISCOVER:") && requestMessage.Substring(9) == Name)
                    {
                        // 构建响应消息（格式："SERVER_INFO:<ServerName>,<IP>,<Port>"）
                        var responseMessage = $"SERVER_INFO:{Name},{_localPort}";
                        var responseBytes = Encoding.UTF8.GetBytes(responseMessage);

                        // 发送响应
                        await _udpDiscoveryServer.SendAsync(responseBytes, responseBytes.Length, remoteEndPoint);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常退出
                    break;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.OperationAborted) return; // 正常退出
                    Trace.TraceError($"XML-RPC({Name}) 发现探测服务异常: ({ex.GetType().Name} SocketErrorCode:{ex.SocketErrorCode}){ex.Message}");
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"XML-RPC({Name}) 发现探测服务异常: ({ex.GetType().Name}){ex.Message}");
                    await Task.Delay(1000, cancelToken);    // 错误后延迟1秒再重试
                }
            }
            Trace.TraceInformation($"XML-RPC({Name}) 发现探测服务已停止");
        }

        /// <summary>
        /// 处理 Tcp 客户端连接
        /// </summary>
        private async Task HandleClientReadAsync(TcpClient client, CancellationToken cancelToken)
        {
            lock (_clients)
            {
                _clients.Add(client);
            }
            var remoteEndPoint = client.Client.RemoteEndPoint;
            ClientConnected?.Invoke(this, remoteEndPoint);

            var buffer = new byte[BUFFER_SIZE];
            Trace.TraceInformation($"客户端已连接: {remoteEndPoint}");

            try
            {
                while (!cancelToken.IsCancellationRequested && client.IsOnline())
                {
                    var stream = client.GetStream();
                    var readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancelToken);

                    // 客户端已断开了连接
                    if (readCount == 0) break;

                    var message = Encoding.UTF8.GetString(buffer, 0, readCount);
                    Debug.WriteLine($"收到来自 {remoteEndPoint} 的消息: {message}");

                    // 解析和处理消息
                    var callResult = TryCallMethod(message, out var invokeResult);                    
                    var responseBytes = Encoding.UTF8.GetBytes(invokeResult.ToXMLString());
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancelToken);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                // 正常退出
            }
            catch (Exception ex)
            {
                Trace.TraceError($"处理客户端 {remoteEndPoint} 数据时异常: ({ex.GetType().Name}){ex.Message}");
            }
            finally
            {
                lock (_clients)
                {
                    _clients.Remove(client);
                }

                client.Dispose();
                ClientDisconnected?.Invoke(this, remoteEndPoint);
                Trace.TraceInformation($"客户端已断开: {remoteEndPoint}");
            }
        }


        #region TryCallMethod
        /// <summary>
        /// 试图调用指定实例对象的方法
        /// </summary>
        /// <param name="message"></param>
        /// <param name="invokeResult"></param>
        /// <returns></returns>
        protected bool TryCallMethod(string message, out InvokeResult invokeResult)
        {
            invokeResult = null;
            if (string.IsNullOrEmpty(message))
            {
                invokeResult = new InvokeResult(InvokeStatusCode.Failed);
                invokeResult.ExceptionMessage = "Invoke Message is null or empty";
                return false;
            }

            XElement invokeMessage;
            try
            {
                invokeMessage = XElement.Parse(message);
            }
            catch (Exception ex)
            {
                invokeResult = new InvokeResult(InvokeStatusCode.Failed);
                invokeResult.ExceptionMessage = $"Invoke Message format error: {ex.Message}";
                return false;
            }

            return TryCallMethod(invokeMessage, out invokeResult);
        }
        /// <summary>
        /// 试图调用指定实例对象的方法
        /// </summary>
        /// <param name="message"></param>
        /// <param name="invokeResult"></param>
        /// <returns></returns>
        public bool TryCallMethod(XElement message, out InvokeResult invokeResult)
        {
            invokeResult = null;
            if (message == null)
            {
                invokeResult = new InvokeResult(InvokeStatusCode.Failed);
                invokeResult.ExceptionMessage = "Invoke Message is null";
                return false;
            }

            if (!InvokeMessage.TryParse(message, out var invokeMessage, out var exceptionMessage))
            {
                invokeResult = new InvokeResult(InvokeStatusCode.Failed);
                invokeResult.ExceptionMessage = exceptionMessage;
                return false;
            }

            return TryCallMethod(invokeMessage, out invokeResult);
        }
        /// <summary>
        /// 试图调用指定实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="invokeResult"></param>
        /// <returns></returns>
        public bool TryCallMethod(InvokeMessage invokeMessage, out InvokeResult invokeResult)
        {
            invokeResult = new InvokeResult(InvokeStatusCode.Failed);
            if (invokeMessage == null)
            {
                invokeResult.ExceptionMessage = "Invoke Message is null";
                return false;
            }

            if (!RegisteredObjects.TryGetValue(invokeMessage.ObjectName, out var objectInstance))
            {
                invokeResult.ExceptionMessage = $"Object '{invokeMessage.ObjectName}' not found";
                return false;
            }
            if (MethodFilters.IndexOf($"*.{invokeMessage.MethodName}") != -1 || MethodFilters.IndexOf($"{invokeMessage.ObjectName}.{invokeMessage.MethodName}") != -1)
            {
                invokeResult.ExceptionMessage = $"Object method '{invokeMessage.ObjectName}.{invokeMessage.MethodName}' is not allowed to be invoked";
                return false;
            }

            //获取对象方法
            int paramsLength = invokeMessage.Parameters?.Length ?? 0;
            Type instanceType = objectInstance is Type ? objectInstance as Type : objectInstance.GetType();
            IEnumerable<MethodInfo> methodInfos = GetMethods(instanceType, invokeMessage);
            if (methodInfos.Count() != 1)
            {
                invokeResult.ExceptionMessage = $"Object instance '{invokeMessage.ObjectName}'('{instanceType.FullName}') has {methodInfos.Count()} same methods named '{invokeMessage.MethodName}'";
                return false;
            }

            //参数解析转换
            int offsetIndex = 0;
            object[] convertParameters = null;
            MethodInfo methodInfo = methodInfos.First();
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            bool isExtensionMethod = methodInfo.IsDefined(typeof(ExtensionAttribute), false); //是否是实例类型的扩展方法
            if (isExtensionMethod)
            {
                offsetIndex = 1;
                convertParameters = new object[paramsLength + 1];
                convertParameters[0] = objectInstance;
            }
            else
            {
                offsetIndex = 0;
                convertParameters = invokeMessage.Parameters == null ? null : new object[paramsLength];
            }
            for (int i = 0; i < paramsLength; i++)
            {
                Type destinationType = parameterInfos[i + offsetIndex].ParameterType;
                if (!TypeExtensions.ConvertFrom(invokeMessage.Parameters[i], destinationType, out object convertValue))
                {
                    invokeResult.ExceptionMessage = $"Object instance '{invokeMessage.ObjectName}'('{instanceType.FullName}'), method named '{invokeMessage.MethodName}' parameters value '{invokeMessage.Parameters[i]}'('{invokeMessage.Parameters[i].GetType().FullName}') convert to type '{destinationType}' failed.";
                    Trace.TraceWarning(invokeResult.ExceptionMessage);
                    return false;
                }
                convertParameters[i + offsetIndex] = convertValue;
            }

            //消息分派到指定的上下文
            Action<SendOrPostCallback, object> dispatcher;
            if (invokeMessage.Asynchronous)
                dispatcher = _syncContext.Post;
            else dispatcher = _syncContext.Send;

            try
            {
                Type voidType = typeof(void);
                invokeResult.ExceptionMessage = "";
                invokeResult.ReturnType = methodInfo.ReturnType;
                invokeResult.StatusCode = invokeResult.ReturnType == voidType ? InvokeStatusCode.Success : InvokeStatusCode.SuccessAndReturn;

                dispatcher.Invoke((state) =>
                {
                    object value = methodInfo.Invoke(objectInstance, convertParameters);
                    if (state != null)
                    {
                        InvokeResult result = (InvokeResult)state;
                        result.ReturnValue = value;
                    }
                },
                methodInfo.ReturnType == voidType ? null : invokeResult);
                Trace.TraceInformation($"实例对象 {invokeMessage.ObjectName}({instanceType.FullName}) 的方法 {invokeMessage.MethodName}({paramsLength}) 调用成功");
            }
            catch (Exception ex)
            {
                invokeResult.StatusCode = InvokeStatusCode.Failed;
                invokeResult.ExceptionMessage = $"Object instance '{invokeMessage.ObjectName}'('{instanceType.FullName}'), method named '{invokeMessage.MethodName}' invoke failed: {ex.Message}";

                Trace.TraceWarning($"实例对象 {invokeMessage.ObjectName}({instanceType.FullName}) 的方法 {methodInfo.Name}({paramsLength}) 调用异常: {ex.Message}");
                Trace.TraceError($"XML-RPC Server MethodBase.Invoke Exception: {ex}");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 试图调用指定实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="invokeResult"></param>
        /// <returns></returns>
        public bool TryCallMethod(string objectName, string methodName, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName, null), out invokeResult);
        /// <summary>
        /// 试图调用指定实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="invokeResult"></param>
        /// <returns></returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters), out invokeResult);
        /// <summary>
        /// 试图调用指定实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="asynchronous"></param>
        /// <param name="parameters"></param>
        /// <param name="invokeResult"></param>
        /// <returns></returns>
        public bool TryCallMethod(string objectName, string methodName, bool asynchronous, object[] parameters, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName, asynchronous, parameters), out invokeResult);
        #endregion


        #region CallMethod
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult CallMethod(XElement invokeMessage) => TryCallMethod(invokeMessage, out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult CallMethod(InvokeMessage invokeMessage) => TryCallMethod(invokeMessage, out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult CallMethod(string objectName, string methodName) => TryCallMethod(new InvokeMessage(objectName, methodName), out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult CallMethod(string objectName, string methodName, params object[] parameters) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters), out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="asynchronous"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult CallMethod(string objectName, string methodName, bool asynchronous, params object[] parameters) => TryCallMethod(new InvokeMessage(objectName, methodName, asynchronous, parameters), out InvokeResult invokeResult) ? invokeResult : null;
        #endregion


        /// <summary>
        /// 获取对象或实例的方法
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="invokeMessage"></param>
        /// <returns></returns>
        protected IEnumerable<MethodInfo> GetMethods(Type instanceType, InvokeMessage invokeMessage)
        {
            if (instanceType == null || invokeMessage == null) return Array.Empty<MethodInfo>();

            Type extensionType = typeof(ExtensionAttribute);
            int paramsLength = invokeMessage.Parameters?.Length ?? 0;
            string objectMethod = $"{invokeMessage.ObjectName}.{invokeMessage.MethodName}.{paramsLength}";

            if (HistoryMethodInfos.ContainsKey(objectMethod))
                return new MethodInfo[] { HistoryMethodInfos[objectMethod] };

            // Get Instance Methods
            IEnumerable<MethodInfo> methodInfos = from method in instanceType.GetMethods()
                                                  where method.Name == invokeMessage.MethodName && method.GetParameters().Length == paramsLength
                                                  select method;

            int methodCount = methodInfos.Count();
            if (methodCount == 0)
            {
                //Get Extension Methods
                methodInfos = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                              where !assembly.GlobalAssemblyCache
                              from type in assembly.GetExportedTypes()
                              where type.IsSealed && !type.IsGenericType && !type.IsNested && type.IsAbstract
                              from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                              where method.Name == invokeMessage.MethodName && method.IsDefined(extensionType, false)
                              let methodParams = method.GetParameters()
                              where methodParams?.Length == (paramsLength + 1) && methodParams[0].ParameterType == instanceType
                              select method;
                Debug.WriteLine($"Get Extension Methods Count: {methodInfos?.Count()}");
            }

            methodCount = methodInfos.Count();
            if (methodCount <= 0) return Array.Empty<MethodInfo>();
            if (methodCount == 1)  //只有一个方法，不存在歧义，记录，下次不用重复查询
            {
                HistoryMethodInfos.TryAdd(objectMethod, methodInfos.First());
                return methodInfos;
            }

            //有多个方法，存在歧义，对比参数类型
            Type stringType = typeof(string);
            List<MethodInfo> methods = methodInfos.ToList();
            for (int i = 0; i < methods.Count; i++)
            {
                ParameterInfo[] paramsInfo = methods[i].GetParameters();
                int offset = methods[i].IsDefined(extensionType, false) ? 1 : 0;

                for (int k = 0; k < paramsLength; k++)
                {
                    Type inputParamType = invokeMessage.Parameters[k].GetType();
                    Type methodParamType = paramsInfo[k + offset].ParameterType;

                    if ((inputParamType == methodParamType) ||
                        (methodParamType.IsArray && inputParamType.IsArray) ||
                        (methodParamType.IsValueType && inputParamType == stringType))
                    {
                        continue;
                    }

                    methods.RemoveAt(i--);
                    break;
                }
            }
            return methods;
        }

        /// <inheritdoc/> 
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            StopAsync().Wait();
            RegisteredObjects.Clear();
        }
    }
}
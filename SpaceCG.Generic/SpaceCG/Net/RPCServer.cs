using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using SpaceCG.Generic;

namespace SpaceCG.Net
{
    /// <summary>
    /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
    /// <para>支持实例对象的公共方法，实例类型的扩展方法和类型的静态方法的查询调用</para>
    /// <para>注意：支持参数为值类型(<see cref="ValueType"/>)和数组类型(元素为值类型) 和 返回为值类型(<see cref="ValueType"/>)和<see cref="Void"/>类型的方法，或者是方法的参数和返回类型，支持类型转换器<see cref="TypeConverter"/>的</para>
    /// <param><see cref="RPCServer"/> 不会抛出异常，因为要支持本地/远程要访问，远程访问时不管访问过程或结果如何，调用过程是不能中断的，都是要返回客户端一个调用的过程和状态信息</param>
    /// </summary>
    public class RPCServer : IDisposable
    {
        internal static readonly LoggerTrace Logger = new LoggerTrace(nameof(RPCServer));

        /// <summary>
        /// Buffer Size
        /// </summary>
        internal const int BUFFER_SIZE = 1024;

        /// <summary>
        /// 连接的客户端集合
        /// </summary>
        public IReadOnlyCollection<TcpClient> Clients => ClientObjects.Keys;
        /// <summary>
        /// 客户端对象集合
        /// </summary>
        protected Dictionary<TcpClient, byte[]> ClientObjects { get; } = new Dictionary<TcpClient, byte[]>(16);

        /// <summary>
        /// 可控制、访问的对象的方法过滤集合，指定对象的方法不在访问范围内；
        /// <para>字符格式为：{ObjectName}.{MethodName}, ObjectName 支持通配符 '*'</para>
        /// <para>例如："*.Dispose" 禁止反射访问所有对象的 Dispose 方法，默认已添加</para>
        /// </summary>
        public List<string> MethodFilters { get; } = new List<string>(16) { "*.Dispose" };
        /// <summary>
        /// 可控制、访问的对象集合，可以通过反射技术访问的对象集合
        /// <para>值键对 &lt;name, object&gt; name 表示唯一的对象名称</para>
        /// </summary>
        public Dictionary<string, object> AccessObjects { get; } = new Dictionary<string, object>(16);

        private SynchronizationContext syncContext = SynchronizationContext.Current;
        /// <summary>
        /// 当前 <see cref="RPCServer"/> 的同步上下文, 默认为 <see cref="SynchronizationContext.Current"/>
        /// <para>see <see cref="System.Threading.SynchronizationContext"/>, DispatcherSynchronizationContext, <see cref="RPCSynchronizationContext"/> ... </para>
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public SynchronizationContext SynchronizationContext
        {
            get { return syncContext; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(SynchronizationContext), "参数不能设置为空");
                syncContext = value;
            }
        }

        private ushort localPort;
        private TcpListener listener;
        /// <summary>
        /// 历史调用过的唯一方法，无歧义的方法
        /// </summary>
        private Dictionary<string, MethodInfo> historyMethodInfos;

        /// <summary>
        /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
        /// <para>支持实例对象的公共方法，实例类型的扩展方法和类型的静态方的查询调用</para>
        /// <para>注意：支持参数为值类型(<see cref="ValueType"/>)和数组类型(元素为值类型) 和 返回为值类型(<see cref="ValueType"/>)和<see cref="Void"/>类型的方法，或者是方法的参数和返回类型，支持类型转换器<see cref="TypeConverter"/>的</para>
        /// </summary>
        public RPCServer()
        {
            this.syncContext = SynchronizationContext.Current;
            this.historyMethodInfos = new Dictionary<string, MethodInfo>(32);
            if (syncContext == null)
            {
                Logger.Warn($"当前线程的同步上下文为空，重新创建 SynchronizationContext");
                syncContext = new RPCSynchronizationContext();
            }
        }
        /// <summary>
        /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
        /// </summary>
        /// <param name="localPort"></param>
        public RPCServer(ushort localPort) : this()
        {
            this.localPort = localPort;
        }

        /// <summary>
        /// 启动 RPC(Remote Procedure Call)  服务
        /// </summary>
        public void Start()
        {
            if (localPort > 1024 && localPort < 65535)
            {
                Stop();
                listener = new TcpListener(IPAddress.Any, localPort);

                try { listener.AllowNatTraversal(true); }
                catch (Exception) { }

                try
                {
                    listener.Start(16);
                }
                catch (Exception ex)
                {
                    listener?.Stop();
                    listener = null;
                    Logger.Error($"RPC Server Start (Port:{localPort}) Excepiton: {ex}");
                    return;
                }
                Logger.Info($"RPC Server Start Success: {listener.LocalEndpoint}");

                AcceptTcpClient();
            }
        }
        /// <summary>
        /// 启动 RPC(Remote Procedure Call)  服务
        /// </summary>
        /// <param name="localPort"></param>
        public void Start(ushort localPort)
        {
            this.localPort = localPort;
            Start();
        }
        /// <summary>
        /// 停止 RPC(Remote Procedure Call)  服务
        /// </summary>
        public void Stop()
        {
            foreach (var clientObject in ClientObjects)
            {
                TcpClient client = clientObject.Key;
                try
                {
                    client?.Dispose();
                }
                catch (Exception) { }
            }
            ClientObjects.Clear();

            try
            {
                listener?.Stop();
            }
            catch { }
            finally
            {
                listener = null;
            }
        }
        /// <inheritdoc />
        public void Dispose()
        {
            Stop();

            localPort = 0;
            AccessObjects.Clear();
            historyMethodInfos.Clear();
        }

        /// <summary>
        /// 接受 <see cref="TcpClient"/> 的连接
        /// </summary>
        private async void AcceptTcpClient()
        {
            if (localPort <= 1024 || listener == null) return;

            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync();
                ClientObjects.Add(client, new byte[BUFFER_SIZE]);
                Logger.Info($"RPC Server Accept Remote Client {client.Client.RemoteEndPoint}, Online:{ClientObjects.Count}");
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Server Accept Remote Client Exception: {ex.Message}");
                return;
            }

            ReadClientMessage(client);
            AcceptTcpClient();
        }
        /// <summary>
        /// 读取 <see cref="TcpClient"/> 的网络流数据
        /// </summary>
        /// <param name="client"></param>
        private async void ReadClientMessage(TcpClient client)
        {
            if (localPort <= 1024 || listener == null) return;
            if (client == null || !client.Connected || !ClientObjects.ContainsKey(client))
            {
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                client.Dispose();
                return;
            }

            IPEndPoint remoteEndPoint = null;
            NetworkStream networkStream = null;

            try
            {
                networkStream = client.GetStream();
                remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            }
            catch (Exception ex)
            {
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                client?.Dispose();
                Logger.Warn($"RPC Client {remoteEndPoint} GetStream Exception: {ex}");
                return;
            }

            try
            {
                byte[] buffer = ClientObjects[client];
                int count = await networkStream.ReadAsync(buffer, 0, buffer.Length);

                if (count <= 0)
                {
                    if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                    client?.Dispose();
                    Logger.Info($"RPC Client {remoteEndPoint} Disconnection, Online:{ClientObjects.Count}");
                    return;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, count);
                Logger.Debug($"Receive RPC Client {remoteEndPoint} Invoke Message {count} Bytes \r\n{message}");

                _ = Task.Run(() => ParseClientMessage(client, message));
            }
            catch (ObjectDisposedException)
            {
                networkStream?.Dispose();
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                return;
            }
            catch (Exception ex)
            {
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                client?.Dispose();

                Logger.Warn($"RPC Client {remoteEndPoint} Invoke Message Exception: {ex}");
                return;
            }

            ReadClientMessage(client);
        }
        /// <summary>
        /// 解析客户端消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected void ParseClientMessage(TcpClient client, string message)
        {
            message = message.Trim();
            char first = message[0];
            char last = message[message.Length - 1];

            MessageFormatType formatType = MessageFormatType.Code;
            InvokeResult invokeResult = new InvokeResult(InvokeStatusCode.Failed, "", $"调用消息不符合协议要求");

            //正则表达式测试中，待下次更新 o_o!!
            if (first == '<' && last == '>')
            {
                XElement element = null;
                formatType = MessageFormatType.XML;

                try
                {
                    element = XElement.Parse(message);
                    if (!InvokeMessage.IsValid(element)) throw new FormatException("调用消息不符合协议要求");
                }
                catch (Exception ex)
                {
                    invokeResult.ExceptionMessage = $"数据解析异常: {ex.Message}";
                    Logger.Warn($"RPC Client {client.Client?.RemoteEndPoint} Invoke Message Format Exception: {invokeResult.ExceptionMessage}");
                    SendInvokeResult(ref client, invokeResult, formatType);
                    return;
                }

                if (element.Name.LocalName == nameof(InvokeMessage))
                {
                    bool result = TryCallMethod(element, out invokeResult);
                    SendInvokeResult(ref client, invokeResult, formatType);
                    return;
                }
                else if (element.Name.LocalName == $"{nameof(InvokeMessage)}s")
                {
                    var invokeResults = TryCallMethods(element);
                    SendInvokeResult(ref client, invokeResults, formatType);
                    return;
                }
                else
                {
                    invokeResult.ExceptionMessage = $"XML 调用消息不符合协议要求，不支持的节点名称 {element.Name.LocalName}，或是拼写错误。";
                    Logger.Warn($"RPC Client {client.Client?.RemoteEndPoint} Invoke Message Format Not Support: {invokeResult.ExceptionMessage}");
                    SendInvokeResult(ref client, invokeResult, formatType);
                    return;
                }
            }
            else if (first == '{' && last == '}')
            {
                formatType = MessageFormatType.JSON;
#if false
                // ... 
#endif
            }
            else
            {
                formatType = first == '<' || last == '>' ? MessageFormatType.XML : first == '{' || last == '}' ? MessageFormatType.JSON : MessageFormatType.Code;
                Logger.Warn($"RPC Client Invoke Message: {message}");
            }

            Logger.Warn($"RPC Client {client.Client?.RemoteEndPoint} Invoke Message Format Not Support: {invokeResult.ExceptionMessage}");
            SendInvokeResult(ref client, invokeResult, formatType);
        }

        /// <summary>
        /// 向客户端发送返回调用结果信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="invokeResult"></param>
        /// <param name="formatType"></param>
        protected void SendInvokeResult(ref TcpClient client, InvokeResult invokeResult, MessageFormatType formatType)
        {
            if (client == null || invokeResult == null) return;

            try
            {
                if (client.Connected)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes($"{invokeResult.ToFormatString(formatType)}");
                    client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
        /// <summary>
        /// 向客户端发送返回调用结果信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="invokeResults"></param>
        /// <param name="formatType"></param>
        protected void SendInvokeResult(ref TcpClient client, IEnumerable<InvokeResult> invokeResults, MessageFormatType formatType)
        {
            if (client == null || invokeResults == null) return;

            try
            {
                if (client.Connected)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes($"{InvokeResult.ToFormatString(invokeResults, formatType)}");
                    client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取对象或实例的方法
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="invokeMessage"></param>
        /// <returns></returns>
        protected IEnumerable<MethodInfo> GetMethods(Type instanceType, InvokeMessage invokeMessage)
        {
            if (instanceType == null || invokeMessage == null) return Enumerable.Empty<MethodInfo>();

            Type extensionType = typeof(ExtensionAttribute);
            int paramsLength = invokeMessage.Parameters?.Length ?? 0;
            string objectMethod = $"{invokeMessage.ObjectName}.{invokeMessage.MethodName}.{paramsLength}";

            if (historyMethodInfos.ContainsKey(objectMethod))
                return new MethodInfo[] { historyMethodInfos[objectMethod] };

            //Get Instance Methods
            IEnumerable<MethodInfo> methodInfos = from method in instanceType.GetMethods()
                                                  where method.Name == invokeMessage.MethodName && method.GetParameters().Length == paramsLength
                                                  select method;

            int methodCount = methodInfos?.Count() ?? 0;
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
                Logger.Debug($"Get Extension Method Count: {methodInfos?.Count()}");
            }

            methodCount = methodInfos?.Count() ?? 0;
            if (methodCount <= 0) return Enumerable.Empty<MethodInfo>();
            if (methodCount == 1)  //只有一个方法，不存在歧义，记录，下次不用重复查询
            {
                historyMethodInfos.Add(objectMethod, methodInfos.First());
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


        /// <summary>
        /// 调用实例对象的方法
        /// <para>支持实例对象的公共方法，实例类型的扩展方法和类型的静态方法的查询调用</para>
        /// <para>方法调用过程不管是失败或是成功, 输出参数 <see cref="InvokeResult" /> 调用过程或结果的消息对象 </para>
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        public bool TryCallMethod(InvokeMessage invokeMessage, out InvokeResult invokeResult)
        {
            invokeResult = new InvokeResult(InvokeStatusCode.Failed, invokeMessage?.ObjectMethod, $"{nameof(InvokeMessage)} 调用消息不符合协议要求");
            if (invokeMessage?.IsValid() == false)
            {
                Logger.Error(invokeResult.ExceptionMessage);
                return false;
            }
            if (!AccessObjects.TryGetValue(invokeMessage.ObjectName, out object objectInstance) || objectInstance == null)
            {
                invokeResult.ExceptionMessage = $"实例对象 {invokeMessage.ObjectName} 不存在，或为 null 对象";
                Logger.Warn($"{invokeResult.ExceptionMessage}");
                return false;
            }
            if (MethodFilters.IndexOf($"*.{invokeMessage.MethodName}") != -1 || MethodFilters.IndexOf($"{invokeMessage.ObjectName}.{invokeMessage.MethodName}") != -1)
            {
                invokeResult.ExceptionMessage = $"实例对象名名称 {invokeMessage.ObjectName}({objectInstance.GetType().FullName}) 的方法 {invokeMessage.MethodName} 被禁止访问";
                Logger.Warn($"{invokeResult.ExceptionMessage}");
                return false;
            }

            //获取对象方法
            int paramsLength = invokeMessage.Parameters?.Length ?? 0;
            Type instanceType = objectInstance is Type ? objectInstance as Type : objectInstance.GetType();
            IEnumerable<MethodInfo> methodInfos = GetMethods(instanceType, invokeMessage);
            if (methodInfos.Count() != 1)
            {
                invokeResult.ExceptionMessage = $"在实例对象 {invokeMessage.ObjectName}({instanceType.FullName}) 中, 匹配的方法 {invokeMessage.MethodName}(输入参数长度 {paramsLength}), 有 {methodInfos.Count()} 个";
                Logger.Warn($"{invokeResult.ExceptionMessage}");
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
                    invokeResult.ExceptionMessage = $"实例对象 {invokeMessage.ObjectName}({instanceType.FullName}), 方法 {invokeMessage.MethodName} 的参数值 {invokeMessage.Parameters[i]}({invokeMessage.Parameters[i].GetType().FullName}) 转换类型 {destinationType} 失败";
                    Logger.Warn($"{invokeResult.ExceptionMessage}");
                    return false;
                }
                convertParameters[i + offsetIndex] = convertValue;
            }

            //消息分派到指定的上下文
            Action<SendOrPostCallback, object> dispatcher;
            if (invokeMessage.Asynchronous) 
                dispatcher = syncContext.Post;
            else dispatcher = syncContext.Send;

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
                Logger.Info($"实例对象 {invokeMessage.ObjectName}({instanceType.FullName}) 的方法 {invokeMessage.MethodName} 调用成功");
            }
            catch (Exception ex)
            {
                invokeResult.StatusCode = InvokeStatusCode.Failed;
                invokeResult.ExceptionMessage = $"实例对象 {invokeMessage.ObjectName}({instanceType.FullName}) 的方法 {methodInfo.Name} 调用异常: {ex.Message}";

                Logger.Warn($"{invokeResult.ExceptionMessage}");
                Logger.Error($"RPC Server MethodBase.Invoke Exception: {ex}");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        public bool TryCallMethod(XElement invokeMessage, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(invokeMessage), out invokeResult);
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        public bool TryCallMethod(string objectName, string methodName, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName), out invokeResult);
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters), out invokeResult);
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="asynchronous">方法或函数是否异步执行/param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, bool asynchronous, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters, asynchronous, null), out invokeResult);


        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult TryCallMethod(XElement invokeMessage) => TryCallMethod(invokeMessage, out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult TryCallMethod(InvokeMessage invokeMessage) => TryCallMethod(invokeMessage, out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult TryCallMethod(string objectName, string methodName) => TryCallMethod(new InvokeMessage(objectName, methodName), out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult TryCallMethod(string objectName, string methodName, object[] parameters) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters), out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="asynchronous"></param>
        /// <returns>实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult TryCallMethod(string objectName, string methodName, object[] parameters, bool asynchronous) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters, asynchronous, null), out InvokeResult invokeResult) ? invokeResult : null;


        /// <summary>
        /// 调用多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns>调用成功时，返回的集合参数长度大于 0 </returns>
        public IEnumerable<InvokeResult> TryCallMethods(XElement invokeMessages)
        {
            if (!InvokeMessage.IsValid(invokeMessages))
            {
                Logger.Warn($"{nameof(invokeMessages)} 调用消息不符合协议要求");
                return Enumerable.Empty<InvokeResult>();
            }

            int IntervalDelay = 0;
            if (invokeMessages.Attribute(nameof(IntervalDelay)) != null)
            {
                IntervalDelay = int.TryParse(invokeMessages.Attribute(nameof(IntervalDelay)).Value, out int millisecondsDelay) ? millisecondsDelay : 0;
            }

            var messages = invokeMessages.Elements(nameof(InvokeMessage));
            InvokeResult[] invokeResults = new InvokeResult[messages.Count()];

            for (int i = 0; i < messages.Count(); i++)
            {
                invokeResults[i] = new InvokeResult(InvokeStatusCode.Unknow);
                bool success = TryCallMethod(messages.ElementAt(i), out invokeResults[i]);

                if (success && IntervalDelay > 0) Thread.Sleep(IntervalDelay);
            }

            return invokeResults;
        }
        /// <summary>
        /// 调用多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns>调用成功时，返回的集合参数长度大于 0 </returns>
        public IEnumerable<InvokeResult> TryCallMethods(IEnumerable<InvokeMessage> invokeMessages)
        {
            if (invokeMessages?.Count() == 0)
            {
                Logger.Warn($"{nameof(invokeMessages)} 集合参数为空");
                return Enumerable.Empty<InvokeResult>();
            }

            InvokeResult[] invokeResults = new InvokeResult[invokeMessages.Count()];

            for (int i = 0; i < invokeMessages.Count(); i++)
            {
                invokeResults[i] = new InvokeResult(InvokeStatusCode.Unknow);
                TryCallMethod(invokeMessages.ElementAt(i), out invokeResults[i]);
            }

            return invokeResults;
        }
        /// <summary>
        /// 调用多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns>用成功时，返回的集合参数长度大于 0 </returns>
        public Task<IEnumerable<InvokeResult>> TryCallMethodsAsync(XElement invokeMessages) => Task.Run(() => TryCallMethods(invokeMessages));
        /// <summary>
        /// 调用多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns>用成功时，返回的集合参数长度大于 0 </returns>
        public Task<IEnumerable<InvokeResult>> TryCallMethodsAsync(IEnumerable<InvokeMessage> invokeMessages) => Task.Run(() => TryCallMethods(invokeMessages));

#if false
        public bool TryCallMethod(System.Text.Json.JsonDocument invokeMessage, out InvokeResult invokeResult) { }
        public InvokeResult TryCallMethod(System.Text.Json.JsonDocument invokeMessage) { }

        public IEnumerable<InvokeResult> TryCallMethods(System.Text.Json.JsonDocument invokeMessages) { }
        public Task<IEnumerable<InvokeResult>> TryCallMethodsAsync(System.Text.Json.JsonDocument invokeMessages) { }
#endif
    }

    /// <inheritdoc/>
    public class RPCSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// 当前线程的同步上下文
        /// </summary>
        private SynchronizationContext CurrentContext = SynchronizationContext.Current;

        /// <summary>
        /// 创建 <see cref="RPCSynchronizationContext"/> 类的新实例
        /// </summary>
        public RPCSynchronizationContext()
        {
            CurrentContext = SynchronizationContext.Current;
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object state)
        {
            if (CurrentContext != null) CurrentContext.Send(d, state);
            else base.Send(d, state);
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object state)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(d.Invoke), state);
        }
    }
}

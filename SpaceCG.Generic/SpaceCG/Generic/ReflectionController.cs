﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using SpaceCG.Extensions;
using SpaceCG.Net;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 网络消息事件参数
    /// </summary>
    public class MessageEventArgs : HandledEventArgs
    {
        /// <summary> Action Element 默认为 null, 当为 null 时则使用默认 <see cref="XElement.Parse(string)"/> 解析 </summary>
        public XElement Action { get; set; } = null;

        /// <summary> 接收的原字节数据  </summary>
        public byte[] RawBytes { get; internal set; } = null;

        /// <summary> Sender </summary>
        internal object Sender { get; set; }

        /// <summary> Remote EndPoint </summary>
        public EndPoint EndPoint { get; internal set; }

        /// <summary>
        /// 网络消息事件参数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="endPoint"></param>
        public MessageEventArgs(XElement message, EndPoint endPoint)
        {
            this.Handled = true;
            this.Action = message;
            this.EndPoint = endPoint;
        }
        /// <summary>
        /// 网络消息事件参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endPoint"></param>
        /// <param name="rawBytes"></param>
        internal MessageEventArgs(object sender, EndPoint endPoint, byte[] rawBytes)
        {
            this.Sender = sender;
            this.RawBytes = rawBytes;
            this.EndPoint = endPoint;
        }
    }

    /// <summary>
    /// (RPC协议的实现)应用程序反射控制接口, 用于网络、键盘、代码、配置、或是其它方式的反射控制访问对象的方法或属性
    /// <code>//消息协议 (XML) 示例：
    /// &lt;Action Target="ObjectName" Method="MethodName" Params="MethodParams" /&gt;              //跟据调用的 Method 决定 Params 可选属性值
    /// &lt;Action Target="ObjectName" Method="MethodName" Params="MethodParams" Sync="True" /&gt;  //可选属性 Sync 是指同步执行还是异步执行
    /// //网络消息返回格式
    /// &lt;Return Result="True/False" Value="value" /&gt;
    /// </code>
    /// </summary>
    public class ReflectionController : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ReflectionController));

        /// <summary> <see cref="XEvent"/> Name </summary>
        public const string XEvent = "Event";
        /// <summary> <see cref="XType"/> Name </summary>
        public const string XType = "Type";
        /// <summary> <see cref="XName"/> Name </summary>
        public const string XName = "Name";

        /// <summary> <see cref="XAction"/> Name </summary>
        public const string XAction = "Action";
        /// <summary> <see cref="XTarget"/> Name </summary>
        public const string XTarget = "Target";

        /// <summary> <see cref="XMethod"/> Name </summary>
        public const string XMethod = "Method";
        /// <summary> <see cref="XParams"/> Name </summary>
        public const string XParams = "Params";

        /// <summary> <see cref="XProperty"/> Name </summary>
        public const string XProperty = "Property";
        /// <summary> <see cref="XValue"/> Name </summary>
        public const string XValue = "Value";

        /// <summary> <see cref="XSync"/> Name </summary>
        public const string XSync = "Sync";
        /// <summary> <see cref="XReturn"/> Name </summary>
        public const string XReturn = "Return";

        /// <summary>
        /// Network Message Event Handler
        /// </summary>
        public event EventHandler<MessageEventArgs> NetworkMessage;

        /// <summary>
        /// 网络访问接口服务对象
        /// </summary>
        private Dictionary<string, IConnection> NetworkServices = new Dictionary<string, IConnection>(8);

        /// <summary>
        /// 组合访问消息的集合，调用方式 <see cref="CallMessageGroup(int)"/>
        /// <para>主要用于键盘控制，或其它形式的组合操作</para>
        /// <para>key 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息</para>
        /// </summary>
        public Dictionary<int, IEnumerable<XElement>> MessageGroups { get; private set; } = new Dictionary<int, IEnumerable<XElement>>(16);

        /// <summary>
        /// 可控制、访问的对象集合，就是可以通过反射技术访问的对象集合
        /// <para>值键对 &lt;name, object&gt; name 表示唯一的对象名称</para>
        /// </summary>
        public Dictionary<string, object> AccessObjects { get; private set; } = new Dictionary<string, object>(16);

        /// <summary>
        /// 可控制、访问的对象的方法过滤集合，指定对象的方法不在访问范围内；字符格式为：objectName.methodName, objectName 支持通配符 '*'
        /// <para>例如："*.Dispose" 禁止反射访问所有对象的 Dispose 方法，默认已添加</para>
        /// </summary>        
        public List<string> MethodFilters { get; } = new List<string>(16) { "*.Dispose" };
        
        /// <summary>
        /// 默认情况下, 使用同步上下文执行控制或是访问对象, 默认为 true
        /// <para>可以跟据消息协议属性 Sync 动态调整当前消息是使用同步执行还是异步执行</para>
        /// </summary>
        public bool DefaultSyncContext { get; private set; } = true;
        /// <summary>
        /// 网络控制情况下，是否返回执行结果，默认为 false
        /// <para>可以跟据消息协议属性 Return 动态调整当前消息是否需要返回信息，只返回成功后的结果</para>
        /// </summary>
        public bool DefaultReturnResult { get; set; } = true;

        private SynchronizationContext syncContext = SynchronizationContext.Current;
        /// <summary>
        /// 当前反射的同步上下文, 默认为 <see cref="SynchronizationContext.Current"/>
        /// <code>//示例：
        /// ReflectionController.SynchronizationContext = new SynchronizationContext();
        /// ReflectionController.SynchronizationContext = new DispatcherSynchronizationContext();
        /// ReflectionController.SynchronizationContext = new ReflectionSynchronizationContext();
        /// </code>
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

        /// <summary>
        /// 应用程序反射控制接口, 用于网络、键盘、代码、配置、或是其它方式的反射控制访问对象的方法或属性
        /// <code>//消息协议 (XML) 示例：
        /// &lt;Action Target="ObjectName" Method="MethodName" Params="MethodParams" /&gt;              //跟据调用的 Method 决定 Params 可选属性值
        /// &lt;Action Target="ObjectName" Method="MethodName" Params="MethodParams" Sync="True" /&gt;  //可选属性 Sync 是指同步执行还是异步执行
        /// //网络消息返回格式
        /// &lt;Return Result="True/False" Value="value" /&gt;
        /// </code>
        /// </summary>
        public ReflectionController()
        {
            syncContext = SynchronizationContext.Current;
            if (syncContext == null)
            {
                Logger.Warn($"当前线程的同步上下文为空，重新创建 SynchronizationContext");
                syncContext = new SynchronizationContext();
            }
        }

        /// <summary>
        /// 应用程序反射控制接口, 用于网络、键盘、代码、配置、或是其它方式的反射控制访问对象的方法或属性
        /// <code>//消息协议 (XML) 示例：
        /// &lt;Action Target="ObjectName" Method="MethodName" Params="MethodParams" /&gt;              //跟据调用的 Method 决定 Params 可选属性值
        /// &lt;Action Target="ObjectName" Method="MethodName" Params="MethodParams" Sync="True" /&gt;  //可选属性 Sync 是指同步执行还是异步执行
        /// //网络消息返回格式
        /// &lt;Return Result="True/False" Value="value" /&gt;
        /// </code>
        /// <param name="localPort">是否启用网络服务访问，小于 1024 则不启动服务接口</param>
        /// </summary>
        public ReflectionController(ushort localPort):this()
        {           
            InstallNetworkService(localPort);
        }

        /// <summary>
        /// 安装网络 (TCP Server/Client) 控制接口服务，可以安装多个不同类型(TCP Server/Client)网络服务接口
        /// <para>地址为 "0.0.0.0"、null、或解析失败时, 则创建 Tcp 服务端，反之创建 Tcp 客户端</para>
        /// </summary>
        /// <param name="port">端口不得小于 1024 否则返回 false</param>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool InstallNetworkService(ushort port, string address = null)
        {
            if (port <= 1024) return false;

            try
            {
                if (!string.IsNullOrWhiteSpace(address) && IPAddress.TryParse(address, out IPAddress ipAddress) && ipAddress.ToString() != "0.0.0.0")
                {
                    IAsyncClient Client = new AsyncTcpClient();
                    if (Client.Connect(ipAddress, port) && !NetworkServices.ContainsKey($"{ipAddress}:{port}"))
                    {
                        Client.Disconnected += Client_Disconnected;
                        Client.DataReceived += Network_DataReceived;
                        NetworkServices.Add($"{ipAddress}:{port}", Client);
                        return true;
                    }
                    Client?.Dispose();
                }
                else
                {
                    IAsyncServer Server = new AsyncTcpServer(port);
                    if (Server.Start() && !NetworkServices.ContainsKey($"0.0.0.0:{port}"))
                    {
                        NetworkServices.Add($"0.0.0.0:{port}", Server);
                        Server.ClientDataReceived += Network_DataReceived;
                        return true;
                    }
                    Server?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            return false;
        }
        /// <summary>
        /// 卸载所有网络服务接口
        /// </summary>
        public void UninstallNetworkServices()
        {
            if (NetworkServices?.Count() == 0) return;

            Type disposeableType = typeof(IDisposable);
            foreach (var obj in NetworkServices)
            {
                if (disposeableType.IsAssignableFrom(obj.Value.GetType()))
                {
                    IDisposable connection = obj.Value;
                    connection?.Dispose();
                }
            }

            NetworkServices?.Clear();
        }

        /// <summary>
        /// 向所有连接对象发送字节数据
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public bool SendBytes(byte[] bytes)
        {
            if (bytes?.Length <= 0) return false;
            foreach(IConnection connection in NetworkServices.Values)
            {
                if(connection.IsConnected) connection.SendBytes(bytes);
            }
            return true;
        }
        /// <summary>
        /// 向所有连接对象发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool SendMessage(string message) => SendBytes(Encoding.UTF8.GetBytes(message));

        /// <summary>
        /// On Client Disconnected Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_Disconnected(object sender, AsyncEventArgs e)
        {
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                IAsyncClient client = (IAsyncClient)sender;
                client.Connect((IPEndPoint)e.EndPoint);

                if (Logger.IsDebugEnabled) Logger.Debug($"客户 {client} 端准备重新连接 {e.EndPoint}");
            });
        }
        /// <summary>
        /// On Client Receive Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Network_DataReceived(object sender, AsyncDataEventArgs e)
        {
            Logger.Debug(Encoding.UTF8.GetString(e.Bytes));
            MessageEventArgs eventArgs = new MessageEventArgs(sender, e.EndPoint, e.Bytes);

            if(NetworkMessage != null)
            {
                NetworkMessage.Invoke(this, eventArgs);
                if (eventArgs.Handled) return;
            }

            if (eventArgs.Action == null)
            {
                XElement element = null;
                string message = Encoding.UTF8.GetString(e.Bytes);

                try
                {
                    element = XElement.Parse(message);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"控制消息 {message} 应为 XML 格式数据, 试图解析错误：{ex}");
                    return;
                }
                eventArgs.Action = element;
            }

            this.TryParseControlMessage(eventArgs.Action, eventArgs);
        }

        /// <summary>
        /// 执行/调用组合消息 <see cref="MessageGroups"/>
        /// </summary>
        /// <param name="keyValue">keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息</param>
        /// <returns></returns>
        public void CallMessageGroup(int keyValue)
        {
            if (MessageGroups.Count() <= 0) return;
            if (!MessageGroups.ContainsKey(keyValue)) return;

            if (MessageGroups.TryGetValue(keyValue, out IEnumerable<XElement> messages))
            {
                this.TryParseControlMessages(messages);
            }
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 <see cref="AccessObjects"/> 字典中查找实例对象，并调用实例对象的方法或属性
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="internalEventArgs"></param>
        protected void TryParseControlMessage(XElement actionElement, MessageEventArgs internalEventArgs = null)
        {
            if (actionElement == null || !actionElement.HasAttributes || AccessObjects.Count() <= 0) return;

            if (actionElement.Name.LocalName == XAction)
            {
                if (actionElement.Attribute(XTarget) != null && actionElement.Attribute(XMethod) != null)
                {
                    TryParseCallMethod(actionElement, internalEventArgs);
                }
                else
                {
                    Logger.Error($"XML 格式数据错误 {actionElement} 不支持的格式");
                }
            }
#if false
            else
            {
                TryParseChangeValues(actionElement);
            }
#endif
        }

        /// <summary>
        /// 调用远程方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void CallRemoteMethod(string objectName, string methodName, params string[] args)
        {
            if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException($"{nameof(objectName)},{nameof(methodName)}", "调用参数不能为空");

            string arguments = "";
            for(int i = 0; i < args.Length; i ++)
                arguments += $"{args[i]}{(i != args.Length - 1 ? "," : "")}";

            string message = $"<Action Target=\"{objectName}\" Method=\"{methodName}\" Params=\"{arguments}\" />";
            SendMessage(message);
        }
        /// <summary>
        /// 调本本地方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void CallLocalMethod(string objectName, string methodName, params string[] args)
        {
            if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException($"{nameof(objectName)},{nameof(methodName)}", "调用参数不能为空");

            string arguments = "";
            for (int i = 0; i < args.Length; i++)
                arguments += $"{args[i]}{(i != args.Length - 1 ? "," : "")}";

            string message = $"<Action Target=\"{objectName}\" Method=\"{methodName}\" Params=\"{arguments}\" />";

            XElement element = null;
            try
            {
                element = XElement.Parse(message);
            }
            catch (Exception ex)
            {
                Logger.Warn($"控制消息 XML 格式数据 {message} 解析错误：{ex}");
                return;
            }

            TryParseControlMessage(element, null);
        }

        /// <summary>
        /// 试图解析 xml 格式消息，在 <see cref="AccessObjects"/> 字典中查找实例对象，并调用实例对象的方法或属性
        /// </summary>
        /// <param name="xmlMessage"></param>
        [Obsolete("弃用", true)]
        public void TryParseControlMessage(string xmlMessage)
        {
            XElement element = null;

            try
            {
                element = XElement.Parse(xmlMessage);
            }
            catch (Exception ex)
            {
                Logger.Warn($"控制消息 XML 格式数据 {xmlMessage} 解析错误：{ex}");
                return;
            }

            TryParseControlMessage(element, null);
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 <see cref="AccessObjects"/> 字典中查找实例对象，并调用实例对象的方法或属性
        /// </summary>
        /// <param name="actionElement"></param>
        public void TryParseControlMessage(XElement actionElement) => TryParseControlMessage(actionElement, null);
        /// <summary>
        /// 试图解析多个 xml 格式消息的集合, 在 <see cref="AccessObjects"/> 字典中查找实例对象，并调用实例对象的方法或属性
        /// </summary>
        /// <param name="actionElements"></param>
        public void TryParseControlMessages(IEnumerable<XElement> actionElements)
        {
            if (actionElements == null || actionElements.Count() <= 0) return;

            foreach (var actionElement in actionElements) TryParseControlMessage(actionElement);
        }
        
        /// <summary>
        /// 试图解析 xml 格式消息，在 <see cref="AccessObjects"/> 字典找实例对象，并调用实例对象的方法
        /// <para>XML 格式：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; </para>
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="internalEventArgs"></param>
        protected void TryParseCallMethod(XElement actionElement, MessageEventArgs internalEventArgs = null)
        {
            if (actionElement == null || actionElement.Name.LocalName != XAction) return;
            if (MethodFilters.IndexOf("*.*") != -1) return;

            string objectName = actionElement.Attribute(XTarget)?.Value;
            string methodName = actionElement.Attribute(XMethod)?.Value;
            if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(methodName))
            {
                Logger.Warn($"控制消息 XML 格式数数据错误，节点名称应为 {XAction}, 且属性 {XTarget}, {XMethod} 值不能为空");
                return;
            }
            if (!AccessObjects.TryGetValue(objectName, out object targetObject) || targetObject == null)
            {
                Logger.Warn($"未找到目标实例对象 {objectName}, 或目标对象为 null 没创建实例");
                return;
            }

            if (targetObject.GetType() == typeof(ReflectionController) ||
                MethodFilters.IndexOf($"*.{methodName}") != -1 || MethodFilters.IndexOf($"{objectName}.{methodName}") != -1)
            {
                Logger.Warn($"禁止访问的对象方法 {objectName}.{methodName} ");
                return;
            }

            bool isSyncContext = bool.TryParse(actionElement.Attribute(XSync)?.Value, out bool booSync) ? booSync : DefaultSyncContext;
            bool isReturnResult = bool.TryParse(actionElement.Attribute(XReturn)?.Value, out bool booReturn) ? booReturn : DefaultReturnResult;

            Action<SendOrPostCallback, object> dispatcher;
            if (isSyncContext) dispatcher = syncContext.Send;
            else dispatcher = syncContext.Post;

            try
            {
                dispatcher.Invoke((state) =>
                {
                    object[] parameters = StringExtensions.SplitToObjectArray(actionElement.Attribute(XParams)?.Value);
                    bool result = InstanceExtensions.CallInstanceMethod(targetObject, methodName, parameters, out object value);

                    if (state == null) return;
                    XElement returnElement = XElement.Parse($"<Return Result=\"{result}\" Value=\"{value}\" />");

                    if (state.GetType() == typeof(MessageEventArgs))
                    {
                        MessageEventArgs eventArgs = state as MessageEventArgs;
                        eventArgs.Action.AddFirst(returnElement);

                        (eventArgs.Sender as IAsyncClient)?.SendMessage(eventArgs.Action.ToString());
                        (eventArgs.Sender as IAsyncServer)?.SendMessage(eventArgs.Action.ToString(), eventArgs.EndPoint);
                    }
                }, isReturnResult && internalEventArgs != null ? internalEventArgs : null);
            }
            catch (Exception ex)
            {
                Logger.Error($"执行 XML 数据指令错误：{actionElement}");
                Logger.Error(ex.ToString());
            }
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            UninstallNetworkServices();

            NetworkServices?.Clear();
            NetworkServices = null;

            AccessObjects?.Clear();
            AccessObjects = null;

            MessageGroups?.Clear();
            MessageGroups = null;

            syncContext = null;
        }

        /// <summary>
        /// 创建控制消息
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParams"></param>
        /// <param name="sync"></param>
        /// <param name="isReturn"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string CreateMessage4Method(string targetName, string methodName, string methodParams, bool sync = true, bool isReturn = true)
        {
            if (String.IsNullOrWhiteSpace(targetName) || String.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException($"{nameof(targetName)},{nameof(methodName)}", "关键参数不能为空");

            if (methodParams == null)
                return $"<{XAction} {XTarget}=\"{targetName}\" {XMethod}=\"{methodName}\" {XSync}=\"{sync}\" {XReturn}=\"{isReturn}\" />";
            else
                return $"<{XAction} {XTarget}=\"{targetName}\" {XMethod}=\"{methodName}\" {XParams}=\"{methodParams}\" {XSync}=\"{sync}\" {XReturn}=\"{isReturn}\" />";
        }
        /// <summary>
        /// 创建控制消息
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <param name="sync"></param>
        /// <param name="isReturn"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string CreateMessage4Property(string targetName, string propertyName, string propertyValue, bool sync = false, bool isReturn = true)
        {
            if (String.IsNullOrWhiteSpace(targetName) || String.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException($"{nameof(targetName)},{nameof(propertyName)}", "关键参数不能为空");

            return $"<{XAction} {XTarget}=\"{targetName}\" {XProperty}=\"{propertyName}\" {XValue}=\"{(propertyValue == null ? "null" : propertyValue)}\" {XSync}=\"{sync}\" {XReturn}=\"{isReturn}\" />";
        }
    }

    /// <inheritdoc/>
    public class ReflectionSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// 当前线程的同步上下文
        /// </summary>
        private SynchronizationContext CurrentContext = SynchronizationContext.Current;

        /// <summary>
        /// 创建 <see cref="ReflectionSynchronizationContext"/> 类的新实例
        /// </summary>
        public ReflectionSynchronizationContext()
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

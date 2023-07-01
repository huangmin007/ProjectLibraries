using System;
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

namespace SpaceCG.Generic
{
    /// <summary>
    /// 更名, 建议更换使用 <see cref="ReflectionInterface"/>
    /// </summary>
    [Obsolete("更名, 建议更换使用 ReflectionInterface", false)]
    public class ControlInterface : ReflectionInterface
    {
    }

    /// <summary>
    /// 网络消息事件参数
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// Message
        /// </summary>
        public XElement Message { get; internal set; }

        /// <summary>
        /// EndPoint
        /// </summary>
        public EndPoint EndPoint { get; internal set; }

        /// <summary>
        /// Handle Reflection Control, Default Value is true
        /// </summary>
        public bool HandleReflection { get; set; } = true;

        /// <summary>
        /// 网络消息事件参数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="endPoint"></param>
        public MessageEventArgs(XElement message, EndPoint endPoint)
        {
            this.Message = message;
            this.EndPoint = endPoint;
        }
    }

    /// <summary>
    /// 反射接口对象, 用于网络、键盘、代码、配置、或是其它方式的控制或访问对象
    /// <para>消息协议(XML)：&lt;Action Target="ObjectName" Method="MethodName" Params="MethodParams" Return="False" Sync="True" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
    /// <para>消息协议(XML)：&lt;Action Target="ObjectName" Property="PropertyName" Value="NewValue" Return="True" Sync="False"/&gt; 读写对象的一个属性值，如果 Value 属性不存在，则表示获取属性的值</para>
    /// <para>消息协议(XML)：&lt;ObjectName PropertyName1="Value" PropertyName2="Value" PropertyName3="Value" Sync="True"/&gt; 设置对象的多个属性及其值</para>
    /// <para>网络接口可以有返回值(XML)：&lt;Return Result="True/False" Value="value" /&gt; 属性 Result 表示远程执行返回状态(成功/失败)，Value 表示远程执行返回值 (Method 返回值，或是 Property 值)</para>
    /// </summary>
    public class ReflectionInterface : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ReflectionInterface));

        /// <summary> <see cref="XEvent"/> Name </summary>
        public const string XEvent = "Event";
        /// <summary> <see cref="XType"/> Name </summary>
        public const string XType = "Type";
        /// <summary> <see cref="XName"/> Name </summary>
        public const string XName = "Name";

        /// <summary> <see cref="XAction"/> Name </summary>
        public const string XAction = "Action";
        /// <summary> <see cref="XMethod"/> Name </summary>
        public const string XMethod = "Method";
        /// <summary> <see cref="XTarget"/> Name </summary>
        public const string XTarget = "Target";
        /// <summary> <see cref="XParams"/> Name </summary>
        public const string XParams = "Params";

        /// <summary> <see cref="XProperty"/> Name </summary>
        public const string XProperty = "Property";
        /// <summary> <see cref="XValue"/> Name </summary>
        public const string XValue = "Value";
        /// <summary> <see cref="XSync"/> Name </summary>
        public const string XSync = "Sync";

        /// <summary>
        /// Network Message Event Handler
        /// </summary>
        public event EventHandler<MessageEventArgs> NetworkMessageEvent;

        /// <summary>
        /// 网络访问接口服务对象
        /// </summary>
        private Dictionary<String, IDisposable> NetworkServices = new Dictionary<String, IDisposable>(8);

        /// <summary>
        /// 组合访问消息的集合，调用方式 <see cref="CallMessageGroup(int)"/>
        /// <para>主要用于键盘控制，或其它形式的组合操作</para>
        /// <para>key 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息</para>
        /// </summary>
        public Dictionary<int, ICollection<String>> MessageGroups { get; private set; } = new Dictionary<int, ICollection<String>>(16);

        /// <summary>
        /// 可控制、访问的对象集合，就是可以通过反射技术访问的对象集合
        /// <para>值键对 &lt;name, object&gt; name 表示唯一的对象名称</para>
        /// </summary>
        public Dictionary<String, Object> AccessObjects { get; private set; } = new Dictionary<String, Object>(16);

        /// <summary>
        /// 可控制、访问的对象的方法过滤集合，指定对象的方法不在控制范围内；字符格式为：objectName.methodName, objectName 支持通配符 '*'
        /// <para>例如："*.Dispose" 禁止反射访问所有对象的 Dispose 方法，默认已添加</para>
        /// </summary>
        public List<String> MethodFilters { get; } = new List<String>(16) { "*.Dispose" };

        /// <summary>
        /// 可控制、访问的对象的属性过滤集合，指定对象的属性不在访问范围内；字符格式为：objectName.propertyName, objectName 支持通配符 '*'
        /// <para>例如："*.Name" 禁止反射访问所有对象的 Name 属性，默认已添加</para>
        /// </summary>
        public List<String> PropertyFilters { get; } = new List<String>(16) { "*.Name" };

        /// <summary>
        /// 默认情况下, 使用同步执行控制或是访问对象, 默认为 true
        /// <para>可以跟据消息协议属性 Sync 动态调整当前消息是使用同步执行还是异步执行</para>
        /// </summary>
        public bool SyncControl { get; set; } = true;

        /// <summary>
        /// 当前同步上下文
        /// </summary>
        private SynchronizationContext SyncContext { get; set; } = SynchronizationContext.Current;

        /// <summary>
        /// 反射接口对象, 用于网络、键盘、代码、配置、或是其它方式的控制或访问对象
        /// <para>消息协议(XML)：&lt;Action Target="ObjectName" Method="MethodName" Params="MethodParams" Return="False" Sync="True" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// <para>消息协议(XML)：&lt;Action Target="ObjectName" Property="PropertyName" Value="NewValue" Return="True" Sync="False"/&gt; 读写对象的一个属性值，如果 Value 属性不存在，则表示获取属性的值</para>
        /// <para>消息协议(XML)：&lt;ObjectName PropertyName1="Value" PropertyName2="Value" PropertyName3="Value" Sync="True"/&gt; 设置对象的多个属性及其值</para>
        /// <para>网络接口可以有返回值(XML)：&lt;Return Result="True/False" Value="value" /&gt; 属性 Result 表示远程执行返回状态(成功/失败)，Value 表示远程执行返回值 (Method 返回值，或是 Property 值)</para>
        /// <param name="localPort">是否启用网络服务访问，小于 1024 则不启动服务接口</param>
        /// </summary>
        public ReflectionInterface(ushort localPort = 2023)
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
        public bool InstallNetworkService(ushort port, String address = null)
        {
            if (port <= 1024) return false;

            try
            {
                if (!String.IsNullOrWhiteSpace(address) && IPAddress.TryParse(address, out IPAddress ipAddress) && ipAddress.ToString() != "0.0.0.0")
                {
                    IAsyncClient Client = new AsyncTcpClient();
                    if (Client.Connect(ipAddress, port) && !NetworkServices.ContainsKey($"{ipAddress}:{port}"))
                    {
                        Client.DataReceived += Network_DataReceived;
                        Client.Disconnected += Client_Disconnected;
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
        /// On Client Disconnected Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_Disconnected(object sender, AsyncEventArgs e)
        {
            Task.Run(() =>
            {
                Thread.Sleep(1000);
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
            String message = Encoding.Default.GetString(e.Bytes);
            if (!VerifyControlMessage(message, out XElement actionElement))
            {
                Logger.Warn($"不支持的控制消息：{message}");
                return;
            }

            MessageEventArgs eventArgs = new MessageEventArgs(actionElement, e.EndPoint);
            NetworkMessageEvent?.Invoke(this, eventArgs);
            if (!eventArgs.HandleReflection) return;

            bool result = this.TryParseControlMessage(actionElement, out object returnValue);
            String returnMessage = $"<Return Result=\"{result}\" Value=\"{returnValue}\" />";

            Logger.Info($"{e.EndPoint} Call {(result ? "Success!" : "Failed!")}  {returnMessage}");
            if (bool.TryParse(actionElement.Attribute("Return")?.Value, out bool isReturn) && isReturn)
            {
                (sender as IConnection)?.SendMessage(returnMessage);
            }
        }

        /// <summary>
        /// 安装键盘控制接口服务
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public bool InstallKeyboardService(bool enabled)
        {
            return false;
        }
        /// <summary>
        /// 卸载键盘控制接口服务
        /// </summary>
        /// <returns></returns>
        public bool UnistallKeyboardServices()
        {
            return false;
        }

        /// <summary>
        /// 执行/调用组合消息 <see cref="MessageGroups"/>
        /// </summary>
        /// <param name="keyValue">keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息</param>
        /// <returns>调用成功返回 true, 否则返回 false</returns>
        public void CallMessageGroup(int keyValue)
        {
            if (MessageGroups == null) return;

            if (MessageGroups.ContainsKey(keyValue) && MessageGroups.TryGetValue(keyValue, out ICollection<string> messages))
            {
                foreach (string message in messages)
                {
                    this.TryParseControlMessage(message, out _);
                }
            }
        }

        /// <summary>
        /// 验证检查网络/文本类型控制消息是否符合要求
        /// </summary>
        /// <param name="xmlMessage"></param>
        /// <param name="actionElement"></param>
        /// <returns></returns>
        private bool VerifyControlMessage(String xmlMessage, out XElement actionElement)
        {
            actionElement = null;
            if (string.IsNullOrWhiteSpace(xmlMessage) || AccessObjects?.Count() <= 0) return false;

            try
            {
                actionElement = XElement.Parse(xmlMessage);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"控制消息 XML 格式数据 {xmlMessage} 解析错误：{ex}");
                return false;
            }
        }

        /// <summary>
        /// 试图解析 xml 格式消息，在 <see cref="AccessObjects"/> 字典中查找实例对象，并调用实例对象的方法或属性
        /// </summary>
        /// <param name="xmlMessage"></param>
        /// <param name="returnResult"></param>
        /// <returns>调用成功返回 true, 否则返回 false</returns>
        public bool TryParseControlMessage(string xmlMessage, out object returnResult)
        {
            returnResult = null;
            if (!VerifyControlMessage(xmlMessage, out XElement actionElement)) return false;

            return TryParseControlMessage(actionElement, out returnResult);
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 <see cref="AccessObjects"/> 字典中查找实例对象，并调用实例对象的方法或属性
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        public bool TryParseControlMessage(XElement actionElement, out object returnResult)
        {
            returnResult = null;
            if (actionElement == null || AccessObjects?.Count() <= 0) return false;

            if (actionElement.Name.LocalName == XAction)
            {
                if (actionElement.Attribute(XTarget) != null && actionElement.Attribute(XMethod) != null)
                {
                    return TryParseCallMethod(actionElement, out returnResult);
                }
                else if (actionElement.Attribute(XTarget) != null && actionElement.Attribute(XProperty) != null)
                {
                    return TryParseChangeValue(actionElement, out returnResult);
                }
                else
                {
                    Logger.Error($"XML 格式数据错误 {actionElement} 不支持的格式");
                    return false;
                }
            }
            else
            {
                returnResult = "void";
                return TryParseChangeValues(actionElement);
            }
        }
        /// <summary>
        /// 试图解析多个 xml 格式消息的集合, 在 <see cref="AccessObjects"/> 字典中查找实例对象，并调用实例对象的方法或属性
        /// </summary>
        /// <param name="actionElements"></param>
        /// <returns></returns>
        public bool TryParseControlMessage(IEnumerable<XElement> actionElements)
        {
            if (actionElements == null || actionElements.Count() <= 0) return false;

            foreach (var action in actionElements) 
                TryParseControlMessage(action, out _);
            
            return true;
        }

        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典找实例对象，并调用实例对象的方法
        /// <para>XML 格式：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        private bool TryParseCallMethod(XElement actionElement, out object returnResult)
        {
            returnResult = null;
            if (MethodFilters.IndexOf("*.*") != -1) return false;

            String objectName = actionElement.Attribute(XTarget).Value;
            String methodName = actionElement.Attribute(XMethod).Value;
            if (String.IsNullOrWhiteSpace(objectName) || String.IsNullOrWhiteSpace(methodName))
            {
                Logger.Warn($"控制消息 XML 格式数数据错误，节点名称应为 {XAction}, 且属性 {XTarget}, {XMethod} 值不能为空");
                return false;
            }
            if (!AccessObjects.TryGetValue(objectName, out Object targetObject))
            {
                Logger.Warn($"未找到目标实例对象 {objectName} ");
                return false;
            }
            //不可操作的对象类型
            if (targetObject.GetType() == typeof(ReflectionInterface)) return false;
            //不可操作的对象方法
            if (MethodFilters.IndexOf($"*.{methodName}") != -1 || MethodFilters.IndexOf($"{objectName}.{methodName}") != -1) return false;

            bool sync = SyncControl;
            if (actionElement.Attribute(XSync) != null)
                sync = bool.TryParse(actionElement.Attribute(XSync).Value, out bool result) && result;

            try
            {
                TaskResult taskResult = new TaskResult(false, null);
                object[] parameters = StringExtensions.SplitParameters(actionElement.Attribute(XParams)?.Value);

                if (sync)
                {
                    if (SyncContext != null)
                    {
                        SyncContext.Send((o) =>
                        {
                            taskResult.Success = InstanceExtensions.CallInstanceMethod(targetObject, methodName, parameters, out object value);
                            taskResult.ReturnValue = value;
                        }, taskResult);
                    }
                    else
                    {
                        taskResult.Success = InstanceExtensions.CallInstanceMethod(targetObject, methodName, parameters, out object value);
                        taskResult.ReturnValue = value;
                    }
                }
                else
                {
                    if (SyncContext != null)
                    {
                        ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                        SyncContext.Post((o) =>
                        {
                            taskResult.Success = InstanceExtensions.CallInstanceMethod(targetObject, methodName, parameters, out object value);
                            taskResult.ReturnValue = value;
                            manualResetEvent.Set();
                        }, taskResult);
                        bool wait = manualResetEvent.WaitOne(2_000);
                        manualResetEvent.Dispose();
                    }
                    else
                    {
                        taskResult = Task.Run(() =>
                        {
                            TaskResult tr = new TaskResult(false, null);
                            tr.Success = InstanceExtensions.CallInstanceMethod(targetObject, methodName, parameters, out object value);
                            tr.ReturnValue = value;
                            return tr;
                        }).Result;
                    }
                }

                if (taskResult.Success) returnResult = taskResult.ReturnValue;
                return taskResult.Success;
            }
            catch (Exception ex)
            {
                Logger.Error($"执行 XML 数据指令错误：{actionElement}");
                Logger.Error(ex.ToString());
            }

            return false;
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典找实例对象，并设置/获取实例对象属性的值
        /// <para>XML 格式：&lt;Action Target="object key name" Property="property name" Value="newValue" /&gt; 如果 Value 属性不存在，则表示获取属性的值</para>
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        private bool TryParseChangeValue(XElement actionElement, out object returnResult)
        {
            returnResult = null;
            if (PropertyFilters.IndexOf("*.*") != -1) return false;

            String objectName = actionElement.Attribute(XTarget).Value;
            String propertyName = actionElement.Attribute(XProperty).Value;
            if (String.IsNullOrWhiteSpace(objectName) || String.IsNullOrWhiteSpace(propertyName))
            {
                Logger.Warn($"控制消息 XML 格式数数据错误，节点名称应为 {XAction}, 且属性 {XTarget}, {XProperty} 值不能为空");
                return false;
            }
            if (!AccessObjects.TryGetValue(objectName, out Object targetObject))
            {
                Logger.Warn($"未找到目标实例对象 {objectName} ");
                return false;
            }

            //不可操作的对象类型
            if (targetObject.GetType() == typeof(ReflectionInterface)) return false;
            //不可操作的对象属性
            if (PropertyFilters.IndexOf($"*.{propertyName}") != -1 || PropertyFilters.IndexOf($"{objectName}.{propertyName}") != -1) return false;

            bool sync = SyncControl;
            if (actionElement.Attribute(XSync) != null)
                sync = bool.TryParse(actionElement.Attribute(XSync).Value, out bool result) && result;

            try
            {
                TaskResult taskResult = new TaskResult(false, null);

                if (sync)
                {
                    if (SyncContext != null)
                    {
                        SyncContext.Send((o) =>
                        {
                            if (actionElement.Attribute(XValue) != null)
                                InstanceExtensions.SetInstancePropertyValue(targetObject, propertyName, actionElement.Attribute(XValue).Value);

                            taskResult.Success = InstanceExtensions.GetInstancePropertyValue(targetObject, propertyName, out object value);
                            taskResult.ReturnValue = value;
                        }, taskResult);
                    }
                    else
                    {
                        if (actionElement.Attribute(XValue) != null)
                            InstanceExtensions.SetInstancePropertyValue(targetObject, propertyName, actionElement.Attribute(XValue).Value);

                        taskResult.Success = InstanceExtensions.GetInstancePropertyValue(targetObject, propertyName, out object value);
                        taskResult.ReturnValue = value;
                    }
                }
                else
                {
                    if (SyncContext != null)
                    {
                        ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                        SyncContext.Post((o) =>
                        {
                            if (actionElement.Attribute(XValue) != null)
                                InstanceExtensions.SetInstancePropertyValue(targetObject, propertyName, actionElement.Attribute(XValue).Value);

                            taskResult.Success = InstanceExtensions.GetInstancePropertyValue(targetObject, propertyName, out object value);
                            taskResult.ReturnValue = value;
                            manualResetEvent.Set();
                        }, taskResult);
                        bool wait = manualResetEvent.WaitOne(1_000);
                        manualResetEvent.Dispose();
                    }
                    else
                    {
                        taskResult = Task.Run(() =>
                        {
                            TaskResult tr = new TaskResult(false, null);
                            if (actionElement.Attribute(XValue) != null)
                                InstanceExtensions.SetInstancePropertyValue(targetObject, propertyName, actionElement.Attribute(XValue).Value);

                            tr.Success = InstanceExtensions.GetInstancePropertyValue(targetObject, propertyName, out object value);
                            tr.ReturnValue = value;
                            return tr;
                        }).Result;
                    }
                }

                if (taskResult.Success) returnResult = taskResult.ReturnValue;
                return taskResult.Success;
            }
            catch (Exception ex)
            {
                Logger.Error($"执行 XML 数据指令错误：{actionElement}");
                Logger.Error(ex.ToString());
            }

            return false;
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典找实例对象，并设置/获取实例对象的多个属性的值
        /// </summary>
        /// <param name="objectElement"></param>
        /// <returns></returns>
        private bool TryParseChangeValues(XElement objectElement)
        {
            if (objectElement == null) return false;
            if (PropertyFilters.IndexOf("*.*") != -1) return false;

            string objectName = objectElement.Name.LocalName;
            if (!AccessObjects.TryGetValue(objectName, out Object targetObject))
            {
                Logger.Warn($"XML 数据格式错误, 未找到以节点名称为目标实例的对象 {objectName} {objectElement}");
                return false;
            }

            //移除不可操作的对象属性
            XElement objectElementClone = XElement.Parse(objectElement.ToString());
            foreach (XAttribute attribute in objectElementClone.Attributes())
            {
                if (PropertyFilters.IndexOf($"*.{attribute.Name}") != -1 || PropertyFilters.IndexOf($"{objectName}.{attribute.Name}") != -1)
                {
                    attribute.Remove();
                }
            }

            bool sync = SyncControl;  //同步执行
            if (objectElement.Attribute(XSync) != null)
            {
                objectElementClone.Attribute(XSync).Remove();
                sync = bool.TryParse(objectElement.Attribute(XSync).Value, out bool result) && result;
            }

            try
            {
                if (sync)
                {
                    if (SyncContext != null)
                    {
                        SyncContext.Send((o) => { InstanceExtensions.SetInstancePropertyValues(targetObject, objectElementClone.Attributes()); }, null);
                    }
                    else
                    {
                        InstanceExtensions.SetInstancePropertyValues(targetObject, objectElementClone.Attributes());
                    }
                }
                else
                {
                    if (SyncContext != null)
                    {
                        SyncContext.Post((o) => { InstanceExtensions.SetInstancePropertyValues(targetObject, objectElementClone.Attributes()); }, null);
                    }
                    else
                    {
                        Task.Run(() => { InstanceExtensions.SetInstancePropertyValues(targetObject, objectElementClone.Attributes()); });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"执行 XML 数据指令错误：{objectElement}");
                Logger.Error(ex.ToString());
            }

            return false;
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

            SyncContext = null;
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
                return $"<{XAction} {XTarget}=\"{targetName}\" {XMethod}=\"{methodName}\" {XSync}=\"{sync}\" Return=\"{isReturn}\" />";
            else
                return $"<{XAction} {XTarget}=\"{targetName}\" {XMethod}=\"{methodName}\" {XParams}=\"{methodParams}\" {XSync}=\"{sync}\" Return=\"{isReturn}\" />";
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

            return $"<{XAction} {XTarget}=\"{targetName}\" {XProperty}=\"{propertyName}\" {XValue}=\"{(propertyValue == null ? "null" : propertyValue)}\" {XSync}=\"{sync}\" Return=\"{isReturn}\" />";
        }
    }

    internal class TaskResult
    {
        internal bool Success { get; set; }
        internal object ReturnValue { get; set; }

        public TaskResult(bool success, object value)
        {
            Success = success;
            ReturnValue = value;
        }
    }
}

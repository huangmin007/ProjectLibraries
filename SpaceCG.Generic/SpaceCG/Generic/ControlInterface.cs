using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceCG.Extensions;
using SpaceCG.Net;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 事件参数
    /// </summary>
    public class ControlEventArgs : EventArgs
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
        /// Handle Reflection, default value is true
        /// </summary>
        public bool HandleReflection { get; set; } = true;

        /// <summary>
        /// 控制事件参数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="endPoint"></param>
        public ControlEventArgs(XElement message, EndPoint endPoint)
        {
            this.Message = message;
            this.EndPoint = endPoint;
        }
    }

    /// <summary>
    /// 控制器接口对象
    /// <para>控制协议(XML)：&lt;Action Target="object name" Method="method name" Params="method params" Return="False" Sync="True" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
    /// <para>控制协议(XML)：&lt;Action Target="object name" Property="property name" Value="newValue" Return="True" Sync="False"/&gt; 如果 Value 属性不存在，则表示获取属性的值</para>
    /// <para>网络接口返回(XML)：&lt;Return Result="True/False" Value="value" /&gt; 属性 Result 表示远程执行返回状态(成功/失败)，Value 表示远程执行返回值 (Method 返回值，或是 Property 值)</para>
    /// </summary>
    public sealed class ControlInterface : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ControlInterface));

        /// <summary>
        /// Control Event Handler
        /// </summary>
        public event EventHandler<ControlEventArgs> ControlEvent;

        /// <summary>
        /// 网络控制接口服务对象
        /// </summary>
        private Dictionary<String, IDisposable> NetworkServices = new Dictionary<String, IDisposable>(8);

        /// <summary>
        /// 组合控制消息的集合，调用方式 <see cref="CallMessageGroup(int)"/>
        /// <para>主要用于键盘控制，或其它形式的组合操作</para>
        /// <para>key值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息</para>
        /// </summary>
        public Dictionary<int, ICollection<String>> MessageGroups { get; private set; } = new Dictionary<int, ICollection<String>>(16);

        /// <summary>
        /// 可访问或可控制对象的集合，可以通过反射技术访问的对象集合
        /// <para>值键对 &lt;name, object&gt; name 表示唯一的对象名称</para>
        /// </summary>
        public Dictionary<String, Object> AccessObjects { get; private set; } = new Dictionary<String, Object>(16);

        /// <summary>
        /// 可访问对象的方法过滤集，指定对象的方法不在控制范围内；字符格式为：objectName.methodName, objectName 支持通配符 '*'
        /// <para>例如："*.Dispose" 禁可访所有对象的 Dispose 方法，默认已添加</para>
        /// </summary>
        public List<String> MethodFilters { get; } = new List<String>(16) { "*.Dispose" };

        /// <summary>
        /// 可访问对象的属性过滤集，指定对象的属性不在控制范围内；字符格式为：objectName.propertyName, objectName 支持通配符 '*'
        /// <para>例如："*.Name" 禁访问所有对象的 Name 属性，默认已添加</para>
        /// </summary>
        public List<String> PropertyFilters { get; } = new List<String>(16) { "*.Name" };

        /// <summary>
        /// 默认情况下, 使用同步执行控制, 默认为 true
        /// <para>可以跟据控制指令属性 Sync 动态调整当前指令是使用同步执行还是异步执行</para>
        /// </summary>
        public bool SyncControl { get; set; } = true;

        /// <summary>
        /// 当前同步上下文
        /// </summary>
        private SynchronizationContext SyncContext { get; set; } = SynchronizationContext.Current;

        /// <summary>
        /// 反射控制接口对象
        /// <para>控制协议(XML)：&lt;Action Target="object name" Method="method name" Params="method params" Return="False" Sync="True" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// <para>控制协议(XML)：&lt;Action Target="object name" Property="property name" Value="newValue" Return="True" Sync="False"/&gt; 如果 Value 属性不存在，则表示获取属性的值</para>
        /// <param name="localPort">服务端口，小于 1024 则不启动服务接口</param>
        /// </summary>
        public ControlInterface(ushort localPort = 2023)
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
                        Client.DataReceived += Client_DataReceived;
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
                        Server.ClientDataReceived += Server_ClientDataReceived;
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

            foreach (var obj in NetworkServices)
            {
                if (typeof(IDisposable).IsAssignableFrom(obj.Value.GetType()))
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
            IAsyncClient client = (IAsyncClient)sender;
            client.Connect((IPEndPoint)e.EndPoint);

            if (Logger.IsDebugEnabled) Logger.Debug($"客户 {client} 端准备重新连接 {e.EndPoint}");
        }
        /// <summary>
        /// On Client Receive Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_DataReceived(object sender, AsyncDataEventArgs e)
        {
            String message = Encoding.Default.GetString(e.Bytes);
            if (!VerifyControlMessage(message, out XElement actionElement))
            {
                Logger.Error($"不支持的控制消息：{message}");
                return;
            }

            ControlEventArgs eventArgs = new ControlEventArgs(actionElement, e.EndPoint);
            ControlEvent?.Invoke(this, eventArgs);
            if (!eventArgs.HandleReflection) return;

            bool result = this.TryParseControlMessage(actionElement, out object returnValue);
            String returnMessage = $"<Return Result=\"{result}\" Value=\"{returnValue}\" />";

            Logger.Info($"{e.EndPoint} Call {(result ? "Success!" : "Failed!")}  {returnMessage}");
            if (bool.TryParse(actionElement.Attribute("Return")?.Value, out bool isReturn) && isReturn)
            {
                ((IAsyncClient)sender).SendMessage(returnMessage);
            }            
        }
        /// <summary>
        /// On Server Receive Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Server_ClientDataReceived(object sender, AsyncDataEventArgs e)
        {
            String message = Encoding.Default.GetString(e.Bytes);
            if (!VerifyControlMessage(message, out XElement actionElement))
            {
                Logger.Error($"不支持的控制消息：{message}");
                return;
            }
            ControlEventArgs eventArgs = new ControlEventArgs(actionElement, e.EndPoint);
            ControlEvent?.Invoke(this, eventArgs);
            if (!eventArgs.HandleReflection) return;

            bool result = this.TryParseControlMessage(actionElement, out object returnValue);
            String returnMessage = $"<Return Result=\"{result}\" Value=\"{returnValue}\" />";
            
            Logger.Info($"{e.EndPoint} Call {(result ? "Success!" : "Failed!")}  {returnMessage}");
            if (bool.TryParse(actionElement.Attribute("Return")?.Value, out bool isReturn) && isReturn)
            {
                ((IAsyncServer)sender).SendMessage(returnMessage, e.EndPoint);
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
        public bool CallMessageGroup(int keyValue)
        {
            if (MessageGroups == null) return false;

            if (MessageGroups.ContainsKey(keyValue) && MessageGroups.TryGetValue(keyValue, out ICollection<String> messages))
            {
                bool result = false;
                foreach (String message in messages)
                {
                    result |= this.TryParseControlMessage(message, out object returnValue);
                    String returnMessage = $"<Return Result=\"{result}\" Value=\"{returnValue}\">";
                    Logger.Info($"KeyValue:{keyValue}  {returnMessage}");
                }
                return result;
            }
            return false;
        }

        /// <summary>
        /// 验证检查控制消息是否符合要求
        /// </summary>
        /// <param name="xmlMessage"></param>
        /// <param name="actionElement"></param>
        /// <returns></returns>
        private bool VerifyControlMessage(String xmlMessage, out XElement actionElement)
        {
            actionElement = null;
            if (String.IsNullOrWhiteSpace(xmlMessage) || AccessObjects == null) return false;

            XElement tempElement;

            try
            {
                tempElement = XElement.Parse(xmlMessage);
            }
            catch (Exception ex)
            {
                Logger.Warn($"控制消息 XML 格式数据 {xmlMessage} 解析错误：{ex}");
                return false;
            }

            if (tempElement.Name?.LocalName != "Action")
            {
                Logger.Warn($"控制消息 XML 格式数据 {xmlMessage} 错误，节点名称应为 Action");
                return false;
            }

            if (String.IsNullOrWhiteSpace(tempElement.Attribute("Target")?.Value))
            {
                Logger.Warn($"控制消息 XML 格式数据 {xmlMessage} 错误，节点属性 Target 不能为空");
                return false;
            }

            if (tempElement.Attribute("Method") == null && tempElement.Attribute("Property") == null)
            {
                Logger.Warn($"控制消息 XML 格式数据 {xmlMessage} 错误，必须指定节点属性 Method 或 Property 其中之一");
                return false;
            }

            actionElement = tempElement;
            return true;
        }

        /// <summary>
        /// 试图解析 xml 格式消息，在 Objects 字典中查找实例对象，并调用实例对象的方法或属性
        /// <para>XML 格式："&lt;Action Target='object key name' Method='method name' Params='method params' /&gt;" 跟据调用的 Method 决定 Params 可选属性值</para>
        /// <para>XML 格式："&lt;Action Target='object key name' Property='property name' Value='value' /&gt;" 如果 Value 属性不存在，则表示获取属性的值</para>
        /// </summary>
        /// <param name="xmlMessage"></param>
        /// <param name="returnResult"></param>
        /// <returns>调用成功返回 true, 否则返回 false</returns>
        public bool TryParseControlMessage(String xmlMessage, out object returnResult)
        {
            returnResult = null;
            if (!VerifyControlMessage(xmlMessage, out XElement actionElement)) return false;

            return TryParseControlMessage(actionElement, out returnResult);
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 Objects 字典中查找实例对象，并调用实例对象的方法或属性
        /// <para>XML 格式："&lt;Action Target='object key name' Method='method name' Params='method params' /&gt;" 跟据调用的 Method 决定 Params 可选属性值</para>
        /// <para>XML 格式："&lt;Action Target='object key name' Property='property name' Value='value' /&gt;" 如果 Value 属性不存在，则表示获取属性的值</para>
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        public bool TryParseControlMessage(XElement actionElement, out object returnResult)
        {
            returnResult = null;
            if (actionElement == null) return false;

            if (actionElement.Attribute("Method") != null)
            {
                return TryParseCallMethod(actionElement, out returnResult);
            }
            else if (actionElement.Attribute("Property") != null)
            {
                return TryParseChangeValue(actionElement, out returnResult);
            }
            else
            {
                Logger.Error($"XML 格式数数据错误 {actionElement} 不支持的格式");
            }

            return false;
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典找实例对象，并调用实例对象的方法
        /// <para>XML 格式：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        public bool TryParseCallMethod(XElement actionElement, out object returnResult)
        {
            returnResult = null;
            if (actionElement?.Name.LocalName != "Action" || AccessObjects?.Count() <= 0 || MethodFilters.IndexOf("*.*") != -1) return false;

            String objectName = actionElement.Attribute("Target")?.Value;
            String methodName = actionElement.Attribute("Method")?.Value; 
            if (String.IsNullOrWhiteSpace(objectName) || String.IsNullOrWhiteSpace(methodName))
            {
                Logger.Error($"控制消息 XML 格式数数据错误，节点名称应为 Action, 且属性 Target, Method 值不能为空");
                return false;
            }
            if (!AccessObjects.TryGetValue(objectName, out Object targetObject))
            {
                Logger.Error($"未找到目标实例对象 {objectName} ");
                return false;
            }
            //不可操作的对象类型
            if (targetObject.GetType() == typeof(ControlInterface)) return false;
            //不可操作的对象方法
            if (MethodFilters.IndexOf($"*.{methodName}") != -1 || MethodFilters.IndexOf($"{objectName}.{methodName}") != -1) return false;

            bool sync = SyncControl;
            if (actionElement.Attribute("Sync") != null)
                sync = bool.TryParse(actionElement.Attribute("Sync").Value, out bool result) && result;

            try
            {
                TaskResult taskResult = new TaskResult(false, null);
                object[] parameters = StringExtensions.SplitParameters(actionElement.Attribute("Params")?.Value);

                if(sync)
                {
                    SyncContext.Send((o) =>
                    {
                        taskResult.Success = InstanceExtensions.CallInstanceMethod(targetObject, methodName, parameters, out object value);
                        taskResult.ReturnValue = value;
                    }, taskResult);
                }
                else
                {
                    ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                    SyncContext.Post((o) =>
                    {
                        taskResult.Success = InstanceExtensions.CallInstanceMethod(targetObject, methodName, parameters, out object value);
                        taskResult.ReturnValue = value;
                        manualResetEvent.Set();
                    }, taskResult);
                    bool wait = manualResetEvent.WaitOne(1_000);
                    manualResetEvent.Dispose();
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
        public bool TryParseChangeValue(XElement actionElement, out object returnResult)
        {
            returnResult = null;
            if (actionElement?.Name.LocalName != "Action" || AccessObjects?.Count() <= 0 || PropertyFilters.IndexOf("*.*") != -1) return false;

            String objectName = actionElement.Attribute("Target")?.Value;
            String propertyName = actionElement.Attribute("Property")?.Value;
            if (String.IsNullOrWhiteSpace(objectName) || String.IsNullOrWhiteSpace(propertyName))
            {
                Logger.Error($"控制消息 XML 格式数数据错误，节点名称应为 Action, 且属性 Target, Property 值不能为空");
                return false;
            }
            if (!AccessObjects.TryGetValue(objectName, out Object targetObject))
            {
                Logger.Error($"未找到目标实例对象 {objectName} ");
                return false;
            }
            //不可操作的对象类型
            if (targetObject.GetType() == typeof(ControlInterface)) return false;
            //不可操作的对象属性
            if (PropertyFilters.IndexOf($"*.{propertyName}") != -1 || PropertyFilters.IndexOf($"{objectName}.{propertyName}") != -1) return false;

            bool sync = SyncControl;
            if (actionElement.Attribute("Sync") != null)
                sync = bool.TryParse(actionElement.Attribute("Sync").Value, out bool result) && result;

            try
            {
                TaskResult taskResult = new TaskResult(false, null);
                //object[] parameters = StringExtensions.SplitParameters(actionElement.Attribute("Value")?.Value);

                if(sync)
                {
                    SyncContext.Send((o) =>
                    {
                        if (actionElement.Attribute("Value") != null)
                            InstanceExtensions.SetInstancePropertyValue(targetObject, propertyName, actionElement.Attribute("Value").Value);

                        taskResult.Success = InstanceExtensions.GetInstancePropertyValue(targetObject, propertyName, out object value);
                        taskResult.ReturnValue = value;
                    }, taskResult);
                }
                else
                {
                    ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                    SyncContext.Post((o) =>
                    {
                        if (actionElement.Attribute("Value") != null)
                            InstanceExtensions.SetInstancePropertyValue(targetObject, propertyName, actionElement.Attribute("Value").Value);

                        taskResult.Success = InstanceExtensions.GetInstancePropertyValue(targetObject, propertyName, out object value);
                        taskResult.ReturnValue = value;
                        manualResetEvent.Set();
                    }, taskResult);
                    bool wait = manualResetEvent.WaitOne(1_000);
                    manualResetEvent.Dispose();
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

            if(methodParams == null)
                return $"<Action Target=\"{targetName}\" Method=\"{methodName}\" Sync=\"{sync}\" Return=\"{isReturn}\" />";
            else
                return $"<Action Target=\"{targetName}\" Method=\"{methodName}\" Params=\"{methodParams}\" Sync=\"{sync}\" Return=\"{isReturn}\" />";
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

            return $"<Action Target=\"{targetName}\" Property=\"{propertyName}\" Value=\"{(propertyValue == null ? "null" : propertyValue)}\" Sync=\"{sync}\" Return=\"{isReturn}\" />";
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

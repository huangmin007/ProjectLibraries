using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceCG.Extensions;
using SpaceCG.Net;
using System.Net;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 控制器接口对象
    /// <para>控制协议(XML)：&lt;Action Target="object name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
    /// <para>控制协议(XML)：&lt;Action Target="object name" Property="property name" Value="newValue" /&gt; 如果 Value 属性不存在，则表示获取属性的值</para>
    /// <para>网络接口返回(XML)：&lt;Return Result="True/False" Value="value" /&gt; 属性 Result 表示远程执行返回状态(成功/失败)，Value 表示远程执行返回值 (Method 返回值，或是 Property 值)</para>
    /// </summary>
    public sealed class ControllerInterface : IDisposable
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(ControllerInterface));

        /// <summary>
        /// 网络控制接口服务参数，是否返回网络执行结果信息，默认为 true
        /// </summary>
        public bool ReturnNetworkResult { get; set; } = true;

        /// <summary>
        /// 网络控制接口服务对象
        /// </summary>
        private Dictionary<String, IDisposable> NetworkServices = new Dictionary<String, IDisposable>(8);

        /// <summary>
        /// 组合控制消息的集合
        /// </summary>
        private ConcurrentDictionary<int, String[]> MessageGroups = new ConcurrentDictionary<int, String[]>(2, 8);

        /// <summary>
        /// 可访问或可控制对象的集合，可以通过反射技术访问的对象集合
        /// <para>值键对 &lt;name, object&gt; </para>
        /// </summary>
        private ConcurrentDictionary<String, Object> ControlObjects = new ConcurrentDictionary<String, Object>(2, 8);

        /// <summary>
        /// 反射控制接口对象
        /// <para>控制协议(XML)：&lt;Action Target="object name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// <para>控制协议(XML)：&lt;Action Target="object name" Property="property name" Value="newValue" /&gt; 如果 Value 属性不存在，则表示获取属性的值</para>
        /// </summary>
        /// <param name="localPort">服务端口，小于 1024 则不启动服务接口</param>
        public ControllerInterface(ushort localPort = 2023)
        {
            InstallNetworkService(localPort);
        }

        /// <summary>
        /// 安装网络 (TCP Server/Client) 控制接口服务，可以安装多个不同类型(TCP)网络服务接口
        /// <para>地址为 "0.0.0.0" 或解析失败时, 则创建 Tcp 服务端，反之创建 Tcp 客户端</para>
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
                    if (Client.Connect(ipAddress, port))
                    {
                        NetworkServices.Add($"{ipAddress}:{port}", Client);
                        Client.DataReceived += Client_DataReceived;
                        return true;
                    }
                    Client?.Dispose();
                }
                else
                {
                    IAsyncServer Server = new AsyncTcpServer(port);
                    if (Server.Start())
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
                Logger.Error(ex);
            }
            return false;
        }
        /// <summary>
        /// 卸载指定的网络服务接口
        /// <para>IPEndPoint == $"{address}:{port}"</para>
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public bool UninstallNetworkService(IPEndPoint endPoint)
        {
            if (NetworkServices?.Count() == 0) return false;
            if (!NetworkServices.ContainsKey(endPoint.ToString())) return false;

            String keyName = endPoint.ToString();
            foreach (var obj in NetworkServices)
            {
                if (obj.Key != keyName) continue;

                if (typeof(IDisposable).IsAssignableFrom(obj.Value.GetType()))
                {
                    IDisposable server = (IDisposable)obj.Value;
                    server?.Dispose();
                    return NetworkServices.Remove(keyName);
                }                
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
                    IDisposable server = (IDisposable)obj.Value;
                    server?.Dispose();
                }               
            }

            NetworkServices?.Clear();
        }

        /// <summary>
        /// On Client Receive Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_DataReceived(object sender, AsyncDataEventArgs e)
        {
            String message = Encoding.Default.GetString(e.Bytes);
            bool result = this.TryParseControlMessage(message, out object returnValue);
            String returnMessage = $"<Return Result=\"{result}\" Value=\"{returnValue}\">";

            if (result)
                Logger.Info($"Call Success! {returnMessage}");
            else
                Logger.Warn($"Call Failed! {returnMessage}");

            if (ReturnNetworkResult)
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
            bool result = this.TryParseControlMessage(message, out object returnValue);
            String returnMessage = $"<Return Result=\"{result}\" Value=\"{returnValue}\">";

            if (result)
                Logger.Info($"Call Success! {returnMessage}");
            else
                Logger.Warn($"Call Failed! {returnMessage}");

            if (ReturnNetworkResult)
            {
                byte[] bytes = Encoding.Default.GetBytes(returnMessage);
                ((IAsyncServer)sender).SendBytes(bytes, e.EndPoint);
            }
        }

        /// <summary>
        /// 安装键盘控制接口服务
        /// </summary>
        /// <param name="global"></param>
        /// <returns></returns>
        public bool InstallKeyboardService(bool global)
        {
            //if (KeyboardMouseHook != null) return true;

            //KeyboardMouseHook = global?  Hook.GlobalEvents() : Hook.AppEvents();
            //KeyboardMouseHook.KeyUp += KeyboardMouseHook_KeyUp;
            //-KeyboardMouseHook.KeyDown += KeyboardMouseHook_KeyDown;
            //-KeyboardMouseHook.KeyPress += KeyboardMouseHook_KeyPress;

            return false;
        }
        /// <summary>
        /// 卸载键盘控制接口服务
        /// </summary>
        /// <returns></returns>
        public bool UnistallKeyboardServices()
        {
            //if (KeyboardMouseHook == null) return true;

            //KeyboardMouseHook.KeyUp -= KeyboardMouseHook_KeyUp;
            //-KeyboardMouseHook.KeyDown -= KeyboardMouseHook_KeyDown;
            //-KeyboardMouseHook.KeyPress -= KeyboardMouseHook_KeyPress;
            //KeyboardMouseHook.Dispose();
            //KeyboardMouseHook = null;

            return true;
        }
#if false
        private IKeyboardMouseEvents KeyboardMouseHook;
        private void KeyboardMouseHook_KeyUp(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"KeyDown: KeyValue:{e.KeyValue}  Code:{e.KeyCode} KeyData:{e.KeyData}  {(int)e.KeyData}");

            int key = (int)e.KeyData;
            CallGroupMessages((int)e.KeyData);
        }
        private void KeyboardMouseHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            Keys key = Keys.A | Keys.Control;
            Console.WriteLine($"KeyDown: KeyValue:{e.KeyValue}  Code:{e.KeyCode} KeyData:{e.KeyData == key}  {(int)e.KeyData}");
        }
        private void KeyboardMouseHook_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            Console.WriteLine("KeyPress: \t{0} {1}", (int)e.KeyChar, e.KeyChar);
        }
#endif

        /// <summary>
        /// 添加组合消息配置。keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息
        /// <para>如果该键已存在，则返回 false</para>
        /// </summary>
        /// <param name="keyValue">可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息</param>
        /// <param name="xmlControlMessages">控制消息或控制消息的集合</param>
        /// <returns>如果添加成功，返回 true, 反之 false </returns>
        public bool AddMessageGroup(int keyValue, params String[] xmlControlMessages) => MessageGroups.TryAdd(keyValue, xmlControlMessages);
        /// <summary>
        /// 移除指定的组合消息。keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息
        /// </summary>
        /// <param name="keyValue">keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息</param>
        /// <returns>如果成功地移除，则为 true；否则为 false。</returns>
        public bool RemoveMessageGroup(int keyValue) => MessageGroups.TryRemove(keyValue, out String[] messages);
        /// <summary>
        /// 称除所有的组合消息
        /// </summary>
        public void RemoveMessageGroups() => MessageGroups?.Clear();
        /// <summary>
        /// 获取所有的组合消息
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<int, String[]> GetMessageGroups() => MessageGroups;
        /// <summary>
        /// 执行/调用组合消息。keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息
        /// </summary>
        /// <param name="keyValue"></param>
        public bool CallMessageGroup(int keyValue)
        {
            if (MessageGroups.ContainsKey(keyValue) && MessageGroups.TryGetValue(keyValue, out String[] messages))
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
        /// 添加控制对象
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool AddControlObject(String name, Object obj)
        {
            if (String.IsNullOrWhiteSpace(name) || obj == null) return false;

            if (ControlObjects.ContainsKey(name))
            {
                Logger.Warn($"添加可访问控制对象 {name}:{obj} 失败, 该对象已存在");
                return false;
            }

            bool result = ControlObjects.TryAdd(name, obj);
            Logger.Info($"添加可访问控制对象 {name}/{obj}/{obj.GetType()} 状态：{result}");
            return result;           
        }
        /// <summary>
        /// 移除控制对象
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RemoveControlObject(String name)
        {
            if (String.IsNullOrWhiteSpace(name)) return false;

            if (!ControlObjects.ContainsKey(name))
            {
                Logger.Warn($"移除可访问控制对象 {name} 失败, 不存在该对象");
                return false;
            }

            bool result = ControlObjects.TryRemove(name, out Object obj);
            Logger.Info($"移除可访问控制对象 {name}/{obj}/{obj?.GetType()} 状态：{result}");
            return result;
        }
        /// <summary>
        /// 将所有控制对象移除
        /// </summary>
        public void RemoveControlObjects() => ControlObjects.Clear();
        /// <summary>
        /// 获取可控制对象的集合
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<String, Object> GetControlObjects() => ControlObjects;

        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典中查找实例对象，并调用实例对象的方法
        /// <para>XML 格式："&lt;Action Target='object key name' Method='method name' Params='method params' /&gt;" 跟据调用的 Method 决定 Params 可选属性值</para>
        /// <para>XML 格式："&lt;Action Target='object key name' Property='property name' Value='value' /&gt;" 如果 Value 属性不存在，则表示获取属性的值</para>
        /// </summary>
        /// <param name="xmlMessage"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        public bool TryParseControlMessage(String xmlMessage, out object returnResult)
        {
            return ControllerInterface.TryParseControlMessage(xmlMessage, ControlObjects, out returnResult);
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
            return ControllerInterface.TryParseCallMethod(actionElement, ControlObjects, out returnResult);
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
            return ControllerInterface.TryParseChangeValue(actionElement, ControlObjects, out returnResult);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            UninstallNetworkServices();

            NetworkServices?.Clear();
            NetworkServices = null;

            ControlObjects?.Clear();
            ControlObjects = null;

            MessageGroups?.Clear();
            MessageGroups = null;
        }

        /// <summary>
        /// 试图解析 xml 格式的控制消息，在 Object 字典中查找实例对象，并调用实例对象的方法
        /// <para>XML 格式："&lt;Action Target='object key name' Method='method name' Params='method params' /&gt;" 跟据调用的 Method 决定 Params 可选属性值</para>
        /// <para>XML 格式："&lt;Action Target='object key name' Property='property name' Value='value' /&gt;" 如果 Value 属性不存在，则表示获取属性的值</para>
        /// </summary>
        /// <param name="xmlMessage">xml 格式消息</param>
        /// <param name="accessObjects">可访问对象的集合</param>
        /// <param name="returnResult"> Method 或 Property 的返回值</param>
        /// <returns>Method 调用成功则返回 true, 反之返回 false</returns>
        public static bool TryParseControlMessage(String xmlMessage, IReadOnlyDictionary<String, Object> accessObjects, out object returnResult)
        {
            returnResult = null;
            if (String.IsNullOrWhiteSpace(xmlMessage) || accessObjects == null) return false;

            XElement actionElement;

            try
            {
                actionElement = XElement.Parse(xmlMessage);
            }
            catch (Exception ex)
            {
                Logger.Warn($"XML 格式数据 {xmlMessage} 解析错误：{ex}");
                return false;
            }

            if (actionElement.Name?.LocalName != "Action")
            {
                Logger.Warn($"XML 格式数据 {xmlMessage} 错误，节点名称应为 Action");
                return false;
            }
            if(String.IsNullOrWhiteSpace(actionElement.Attribute("Target")?.Value))
            {
                Logger.Warn($"XML 格式数据 {xmlMessage} 错误，节点属性 Target 不能为空");
                return false;
            }

            if(actionElement.Attribute("Method") != null)
            {
                return TryParseCallMethod(actionElement, accessObjects, out returnResult);
            }
            else if(actionElement.Attribute("Property") != null)
            {
                return TryParseChangeValue(actionElement, accessObjects, out returnResult);
            }
            else
            {
                Logger.Warn($"XML 格式数数据错误 {actionElement} 不支持的格式");
            }

            return false;
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典找实例对象，并调用实例对象的方法
        /// <para>XML 格式：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="accessObjects">可访问对象的集合</param>
        /// <param name="returnResult">Method 执行的返回结果</param>
        /// <returns>Method 调用成功则返回 true, 反之返回 false</returns>
        public static bool TryParseCallMethod(XElement actionElement, IReadOnlyDictionary<String, Object> accessObjects, out object returnResult)
        {
            returnResult = null;
            if (actionElement == null || accessObjects == null) return false;

            if (actionElement.Name?.LocalName != "Action" ||
                String.IsNullOrWhiteSpace(actionElement.Attribute("Target")?.Value) ||
                String.IsNullOrWhiteSpace(actionElement.Attribute("Method")?.Value))
            {
                Logger.Warn($"XML 格式数数据错误，节点名称应为 Action, 且属性 Target, Method 不能为空");
                return false;
            }

            try
            {
                String objectName = actionElement.Attribute("Target").Value;
                String methodName = actionElement.Attribute("Method").Value;

                if (!accessObjects.TryGetValue(objectName, out Object targetObject))
                {
                    Logger.Warn($"未找到目标实例对象 {objectName} ");
                    return false;
                }

                returnResult = Task.Run<Object>(() =>
                {
                    return InstanceExtensions.CallInstanceMethod(targetObject, methodName, StringExtensions.SplitParameters(actionElement.Attribute("Params")?.Value));
                }).Result;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"执行 XML 数据指令错误：{actionElement}");
                Logger.Error(ex);
            }

            return false;
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典找实例对象，并设置/获取实例对象属性的值
        /// <para>XML 格式：&lt;Action Target="object key name" Property="property name" Value="newValue" /&gt; 如果 Value 属性不存在，则表示获取属性的值</para>
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="accessObjects"></param>
        /// <param name="returnResult">设置或获取属性的返回结果</param>
        /// <returns>Property 调用成功则返回 true, 反之返回 false</returns>
        public static bool TryParseChangeValue(XElement actionElement, IReadOnlyDictionary<String, Object> accessObjects, out object returnResult)
        {
            returnResult = null;
            if (actionElement == null || accessObjects == null) return false;

            if (actionElement.Name?.LocalName != "Action" ||
                String.IsNullOrWhiteSpace(actionElement.Attribute("Target")?.Value) ||
                String.IsNullOrWhiteSpace(actionElement.Attribute("Property")?.Value))
            {
                Logger.Warn($"XML 格式数数据错误，节点名称应为 Action, 且属性 Target, Property 不能为空");
                return false;
            }

            String objectName = actionElement.Attribute("Target").Value;
            String propertyName = actionElement.Attribute("Property").Value;

            if (!accessObjects.TryGetValue(objectName, out Object targetObject))
            {
                Logger.Warn($"未找到目标实例对象 {objectName} ");
                return false;
            }

            bool changeResult = true;
            try
            {
                returnResult = Task.Run<Object>(() =>
                {
                    if(actionElement.Attribute("Value") != null)
                        changeResult = InstanceExtensions.SetInstancePropertyValue(targetObject, propertyName, actionElement.Attribute("Value").Value);

                    return InstanceExtensions.GetInstancePropertyValue(targetObject, propertyName);
                }).Result;
            }
            catch(Exception ex)
            {
                changeResult = false;

                Logger.Warn($"执行 XML 数据指令错误：{actionElement}");
                Logger.Error(ex);
            }

            return changeResult;
        }
    }
}

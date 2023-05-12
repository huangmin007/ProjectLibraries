using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Gma.System.MouseKeyHook;
using HPSocket;
using SpaceCG.Extensions;

namespace SpaceCG.Module.Reflection
{
    /// <summary>
    /// 控制器接口对象
    /// <para>控制协议(XML)：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
    /// <para>控制协议(XML)：&lt;Action Target="object key name" Property="property name" Value="newValue" /&gt; 如果 Value 属性不存在，则表示获取属性的值</para>
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
        private Dictionary<String, Object> NetworkServices = new Dictionary<string, Object>(8);

        /// <summary>
        /// 组合控制消息的集合
        /// </summary>
        private ConcurrentDictionary<int, String[]> GroupMessages = new ConcurrentDictionary<int, String[]>(2, 8);

        /// <summary>
        /// 可访问或可控制对象的集合，可以通过反射技术访问的对象集合
        /// <para>值键对 &lt;name, object&gt; </para>
        /// </summary>
        private ConcurrentDictionary<String, Object> ControlObjects = new ConcurrentDictionary<String, Object>(2, 8);

        /// <summary>
        /// 反射控制接口对象
        /// <para>控制协议(XML)：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// <para>控制协议(XML)：&lt;Action Target="object key name" Property="property name" Value="newValue" /&gt; 如果 Value 属性不存在，则表示获取属性的值</para>
        /// </summary>
        /// <param name="localPort">服务端口，小于 1024 则不启动服务接口</param>
        public ControllerInterface(ushort localPort = 2023)
        {
            InstallNetworkService("TCP-SERVER", "0.0.0.0", localPort);
        }

        /// <summary>
        /// 安装网络 (TCP/UDP-Server/Client) 控制接口服务
        /// <para>网络类型支持：TCP-Server, UDP-Server, TCP-Client, UDP-Client</para>
        /// </summary>
        /// <param name="type">TCP-Server, UDP-Server, TCP-Client, UDP-Client</param>
        /// <param name="address"></param>
        /// <param name="port">端口不得小于 1024 否则返回 false </param>
        /// <returns></returns>
        public bool InstallNetworkService(String type, String address, ushort port)
        {
            if (String.IsNullOrWhiteSpace(type) || String.IsNullOrWhiteSpace(address) || port <= 1024) return false;

            String configKey = $"{type}:{address}:{port}";
            if (NetworkServices.ContainsKey(configKey)) return false;

            if (type.ToUpper().IndexOf("SERVER") != -1)
            {
                IServer Server = HPSocketExtensions.CreateNetworkServer(configKey, OnServerReceiveEventHandler);
                if (Server != null) NetworkServices.Add(configKey, Server);
                else return false;
            }
            else if(type.ToUpper().IndexOf("CLIENT") != -1)
            {
                IClient Client = HPSocketExtensions.CreateNetworkClient(configKey, OnClientReceiveEventHandler);
                if (Client != null) NetworkServices.Add(configKey, Client);
                else return false;
            }
            else
            {
                IServer Server = HPSocketExtensions.CreateNetworkServer(configKey, OnServerReceiveEventHandler);
                if (Server != null) NetworkServices.Add(configKey, Server);
                else return false;
            }

            return true;
        }
        /// <summary>
        /// 卸载指定的网络服务接口
        /// <para>KeyName == $"{type}:{address}:{port}"</para>
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public bool UninstallNetworkService(String keyName)
        {
            if (NetworkServices?.Count() == 0) return false;
            if (!NetworkServices.ContainsKey(keyName)) return false;

            foreach (var obj in NetworkServices)
            {
                if (obj.Key != keyName) continue;

                if (typeof(HPSocket.IServer).IsAssignableFrom(obj.Value.GetType()))
                {
                    HPSocket.IServer server = (HPSocket.IServer)obj.Value;
                    HPSocketExtensions.DisposeNetworkServer(ref server);
                    return NetworkServices.Remove(keyName);
                }
                else if (typeof(HPSocket.IClient).IsAssignableFrom(obj.Value.GetType()))
                {
                    HPSocket.IClient client = (HPSocket.IClient)obj.Value;
                    HPSocketExtensions.DisposeNetworkClient(ref client);
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
                if (typeof(HPSocket.IServer).IsAssignableFrom(obj.Value.GetType()))
                {
                    HPSocket.IServer server = (HPSocket.IServer)obj.Value;
                    HPSocketExtensions.DisposeNetworkServer(ref server);
                }
                else if (typeof(HPSocket.IClient).IsAssignableFrom(obj.Value.GetType()))
                {
                    HPSocket.IClient client = (HPSocket.IClient)obj.Value;
                    HPSocketExtensions.DisposeNetworkClient(ref client);
                }
            }

            NetworkServices?.Clear();
        }
        
        /// <summary>
        /// On Client Receive Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private HandleResult OnClientReceiveEventHandler(IClient sender, byte[] data)
        {
            String message = Encoding.UTF8.GetString(data);
            bool result = this.TryParseControlMessage(message, out object returnValue);
            String returnMessage = $"<Return Result=\"{result}\" Value=\"{returnValue}\">";

            if (result)
                Logger.Info($"Call Success! {returnMessage}");
            else
                Logger.Warn($"Call Failed! {returnMessage}");

            if (ReturnNetworkResult)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(returnMessage);
                return sender.Send(bytes, bytes.Length) ? HandleResult.Ok : HandleResult.Error;
            }

            return HandleResult.Ok;
        }
        /// <summary>
        /// On Server Receive Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="connId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private HandleResult OnServerReceiveEventHandler(IServer sender, IntPtr connId, byte[] data)
        {
            String message = Encoding.UTF8.GetString(data);
            bool result = this.TryParseControlMessage(message, out object returnValue);
            String returnMessage = $"<Return Result=\"{result}\" Value=\"{returnValue}\">";

            if (result)
                Logger.Info($"Call Success! {returnMessage}");
            else
                Logger.Warn($"Call Failed! {returnMessage}");

            if (ReturnNetworkResult)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(returnMessage);
                return sender.Send(connId, bytes, bytes.Length) ? HandleResult.Ok : HandleResult.Error;
            }

            return HandleResult.Ok;
        }

#if false
        private IKeyboardMouseEvents KeyboardMouseHook;
        /// <summary>
        /// 安装键盘控制接口服务
        /// </summary>
        /// <param name="global"></param>
        /// <returns></returns>
        public bool InstallKeyboardService(bool global)
        {
            if (KeyboardMouseHook != null) return true;

            KeyboardMouseHook = global?  Hook.GlobalEvents() : Hook.AppEvents();
            KeyboardMouseHook.KeyUp += KeyboardMouseHook_KeyUp;
            //KeyboardMouseHook.KeyDown += KeyboardMouseHook_KeyDown;
            //KeyboardMouseHook.KeyPress += KeyboardMouseHook_KeyPress;

            return true;
        }
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
        /// <summary>
        /// 卸载键盘控制接口服务
        /// </summary>
        /// <returns></returns>
        public bool UnistallKeyboardServices()
        {
            if (KeyboardMouseHook == null) return true;

            KeyboardMouseHook.KeyUp -= KeyboardMouseHook_KeyUp;
            //KeyboardMouseHook.KeyDown -= KeyboardMouseHook_KeyDown;
            //KeyboardMouseHook.KeyPress -= KeyboardMouseHook_KeyPress;
            KeyboardMouseHook.Dispose();
            KeyboardMouseHook = null;

            return true;
        }
#endif

        /// <summary>
        /// 添加组合消息配置。keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息
        /// </summary>
        /// <param name="keyValue">可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息</param>
        /// <param name="xmlControlMessages">控制消息或控制消息的集合</param>
        /// <returns>如果添加成功，返回 true, 反之 false </returns>
        public bool AddGroupMessage(int keyValue, params String[] xmlControlMessages) => GroupMessages.TryAdd(keyValue, xmlControlMessages);
        /// <summary>
        /// 移除指定的组合消息。keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息
        /// </summary>
        /// <param name="keyValue">keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息</param>
        /// <returns>如果成功地移除，则为 true；否则为 false。</returns>
        public bool RemoveGroupMessage(int keyValue) => GroupMessages.TryRemove(keyValue, out String[] messages);
        /// <summary>
        /// 称除所有的组合消息
        /// </summary>
        public void RemoveGroupMessages() => GroupMessages?.Clear();
        /// <summary>
        /// 获取所有的组合消息
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<int, String[]> GetGroupMessages() => GroupMessages;
        /// <summary>
        /// 执行/调用组合消息。keyValue 值可以是 UID 值、键盘值、鼠标值，等其它关联的有效数据信息
        /// </summary>
        /// <param name="keyValue"></param>
        public bool CallGroupMessages(int keyValue)
        {
            if (GroupMessages.ContainsKey(keyValue) && GroupMessages.TryGetValue(keyValue, out String[] messages))
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

            GroupMessages?.Clear();
            GroupMessages = null;
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
                Logger.Warn($"XML 格式数据解析错误：{ex}");
                return false;
            }

            if (actionElement.Name?.LocalName != "Action")
            {
                Logger.Warn($"XML 格式数数据错误，节点名称应为 Action");
                return false;
            }
            if(String.IsNullOrWhiteSpace(actionElement.Attribute("Target")?.Value))
            {
                Logger.Warn($"XML 格式数数据错误，节点属性 Target 不能为空");
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

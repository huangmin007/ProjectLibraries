using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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

        private HPSocket.IServer TcpServer;
        private HPSocket.IServer UdpServer;

        private HPSocket.IClient TcpClient;
        private HPSocket.IClient UdpClient;

        /// <summary>
        /// 网络接口控制时，是否返回执行结果信息，默认为 true
        /// </summary>
        public bool NetworkReturnResult { get; set; } = true;

        /// <summary>
        /// 可访问或可控制对象的集合，可以通过反射技术访问的对象集合
        /// <para>值键对 &lt;name, object&gt; </para>
        /// </summary>
        internal ConcurrentDictionary<String, Object> AccessObjects { get; set; } = new ConcurrentDictionary<String, Object>(2, 8);

        /// <summary>
        /// 反射控制接口对象
        /// <para>控制协议(XML)：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// <para>控制协议(XML)：&lt;Action Target="object key name" Property="property name" Value="newValue" /&gt; 如果 Value 属性不存在，则表示获取属性的值</para>
        /// </summary>
        /// <param name="localPort">服务端口，小于 1024 则不启动服务接口</param>
        public ControllerInterface(ushort localPort = 0)
        {
            InstallServer(localPort);
        }

        /// <summary>
        /// 安装 TCP/UDP 服务接口
        /// </summary>
        /// <param name="localPort">服务端口，小于 1024 则不启动服务接口</param>
        /// <returns></returns>
        public bool InstallServer(ushort localPort)
        {
            if (TcpServer == null && UdpServer == null && localPort > 1024)
            {
                TcpServer = HPSocketExtensions.CreateNetworkServer<HPSocket.Tcp.TcpServer>("0.0.0.0", localPort, OnServerReceiveEventHandler);
                UdpServer = HPSocketExtensions.CreateNetworkServer<HPSocket.Udp.UdpServer>("0.0.0.0", localPort, OnServerReceiveEventHandler);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 安装 TCP/UDP 客户端接口
        /// </summary>
        /// <param name="remoteAddress"></param>
        /// <param name="remotePort"></param>
        /// <returns></returns>
        public bool InstallClient(String remoteAddress, ushort remotePort)
        {
            if (TcpClient == null && UdpClient == null && remotePort > 1024)
            {
                TcpClient = HPSocketExtensions.CreateNetworkClient<HPSocket.Tcp.TcpClient>(remoteAddress, remotePort, OnClientReceiveEventHandler);
                UdpClient = HPSocketExtensions.CreateNetworkClient<HPSocket.Udp.UdpClient>(remoteAddress, remotePort, OnClientReceiveEventHandler);
                return true;
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

            if (AccessObjects.ContainsKey(name))
            {
                Logger.Warn($"添加可访问控制对象 {name}:{obj} 失败, 已包含对象");
                return false;
            }

            return AccessObjects.TryAdd(name, obj);
        }
        /// <summary>
        /// 移除控制对象
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RemoveControlObject(String name)
        {
            if (String.IsNullOrWhiteSpace(name)) return false;

            if (!AccessObjects.ContainsKey(name))
            {
                Logger.Warn($"移除可访问控制对象 {name} 失败, 不存在该对象");
                return false;
            }

            return AccessObjects.TryRemove(name, out Object value);
        }
        /// <summary>
        /// 将所有控制对象移除
        /// </summary>
        public void ClearControlObjects() => AccessObjects.Clear();
        /// <summary>
        /// 获取可控制对象的集合
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<String, Object> GetControlObjects() => AccessObjects;

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

            if (NetworkReturnResult)
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

            if (NetworkReturnResult)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(returnMessage);
                return sender.Send(connId, bytes, bytes.Length) ? HandleResult.Ok : HandleResult.Error;
            }

            return HandleResult.Ok;
        }

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
            return ControllerInterface.TryParseControlMessage(xmlMessage, AccessObjects, out returnResult);
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
            return ControllerInterface.TryParseCallMethod(actionElement, AccessObjects, out returnResult);
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
            return ControllerInterface.TryParseChangeValue(actionElement, AccessObjects, out returnResult);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            AccessObjects?.Clear();
            AccessObjects = null;

            HPSocketExtensions.DisposeNetworkClient(ref TcpClient);
            HPSocketExtensions.DisposeNetworkClient(ref UdpClient);

            HPSocketExtensions.DisposeNetworkServer(ref TcpServer);
            HPSocketExtensions.DisposeNetworkServer(ref UdpServer);
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

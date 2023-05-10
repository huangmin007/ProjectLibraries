using System;
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
    /// 反射控制器接口对象
    /// <para>控制协议(XML)：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
    /// </summary>
    public sealed class ControllerInterface : IDisposable
    {
        protected static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(ControllerInterface));

        private HPSocket.IServer TcpServer;
        private HPSocket.IServer UdpServer;

        private HPSocket.IClient TcpClient;
        private HPSocket.IClient UdpClient;

        /// <summary>
        /// 可访问或可控制对象的集合，可以通过反射技术访问的对象集合
        /// <para>值键对 &lt;name, object&gt; </para>
        /// </summary>
        public IReadOnlyDictionary<String, IDisposable> AccessObjects { get; set; } = null;

        /// <summary>
        /// 反射控制接口对象
        /// <para>控制协议(XML)：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
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
                TcpClient = HPSocketExtensions.CreateNetworkClient<HPSocket.Tcp.TcpClient>(remoteAddress, remotePort, onClientReceiveEventHandler);
                UdpClient = HPSocketExtensions.CreateNetworkClient<HPSocket.Udp.UdpClient>(remoteAddress, remotePort, onClientReceiveEventHandler);
                return true;
            }

            return false;
        }

        /// <summary>
        /// On Client Receive Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private HandleResult onClientReceiveEventHandler(IClient sender, byte[] data)
        {
            String message = Encoding.UTF8.GetString(data);
            if (TryParseCallMethod(message, AccessObjects, out object result))
            {
                Logger.Info($"Call Success! Return Result: {result}");
            }
            else
            {
                Logger.Warn($"Call Failed!");
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
            if (TryParseCallMethod(message, AccessObjects, out object result))
            {
                Logger.Info($"Call Success! Return Result: {result}");
            }
            else
            {
                Logger.Warn($"Call Failed!");
            }

            return HandleResult.Ok;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            AccessObjects = null;

            HPSocketExtensions.DisposeNetworkClient(ref TcpClient);
            HPSocketExtensions.DisposeNetworkClient(ref UdpClient);

            HPSocketExtensions.DisposeNetworkServer(ref TcpServer);
            HPSocketExtensions.DisposeNetworkServer(ref UdpServer);
        }

        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典中查找实例对象，并调用实例对象的方法
        /// <para>XML 格式："&lt;Action Target='object key name' Method='method name' Params='method params' /&gt;" 跟据调用的 Method 决定 Params 可选属性值</para>
        /// </summary>
        /// <param name="xmlMessage">xml 格式消息</param>
        /// <param name="accessObjects">可访问对象的集合</param>
        /// <param name="returnResult">Method 的返回值</param>
        /// <returns>Method 调用成功则返回 true, 反之返回 false</returns>
        public static bool TryParseCallMethod(String xmlMessage, IReadOnlyDictionary<String, IDisposable> accessObjects, out object returnResult)
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

            return TryParseCallMethod(actionElement, accessObjects, out returnResult);
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典找实例对象，并调用实例对象的方法
        /// <para>XML 格式：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="accessObjects">可访问对象的集合</param>
        /// <param name="returnResult">Method 的返回值</param>
        /// <returns>Method 调用成功则返回 true, 反之返回 false</returns>
        public static bool TryParseCallMethod(XElement actionElement, IReadOnlyDictionary<String, IDisposable> accessObjects, out object returnResult)
        {
            returnResult = null;
            if (actionElement == null || accessObjects == null) return false;

            if (actionElement.Name?.LocalName != "Action")
            {
                Logger.Warn($"XML 格式数数据错误，节点名称应为 Action");
                return false;
            }

            try
            {
                if (String.IsNullOrWhiteSpace(actionElement.Attribute("Target")?.Value) ||
                    String.IsNullOrWhiteSpace(actionElement.Attribute("Method")?.Value)) return false;

                String objectName = actionElement.Attribute("Target").Value;
                String methodName = actionElement.Attribute("Method").Value;

                if (!accessObjects.TryGetValue(objectName, out IDisposable targetObject))
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
    }
}

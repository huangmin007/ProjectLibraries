using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SpaceCG.Net
{
    /// <summary>
    /// 简单的异步客户端接口，为可跨平台编译准备的
    /// </summary>
    public interface IAsyncClient : IDisposable
    {
        /// <summary>
        /// 与服务端的连接状态
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        /// 服务端绑定的本地端口号
        /// </summary>
        int RemotePort { get; }
        /// <summary>
        /// 服务端绑定的 IP 地址
        /// </summary>
        IPAddress RemoteAddress { get; }
        /// <summary>
        /// 连接对象绑定的本地端口号
        /// </summary>
        int LocalPort { get; }
        /// <summary>
        /// 连接对象绑定的本地 IP 地址
        /// </summary>
        IPAddress LocalAddress { get; }

        /// <summary>
        /// 这是一个同步参数？？？获取或设置一个值，该值指定之后同步 Overload:System.Net.Sockets.Socket.Receive 调用将超时的时间长度。
        /// <para>超时值（以毫秒为单位）。默认值为 0，指示超时期限无限大。指定 -1 还会指示超时期限无限大。</para>
        /// </summary>
        int ReadTimeout { get; set; }
        /// <summary>
        /// 这是一个同步参数？？？获取或设置一个值，该值指定之后同步 Overload:System.Net.Sockets.Socket.Send 调用将超时的时间长度。
        /// <para>超时值（以毫秒为单位）。如果将该属性设置为 1 到 499 之间的值，该值将被更改为 500。默认值为 0，指示超时期限无限大。指定 -1 还会指示超时期限无限大。</para>
        /// </summary>
        int WriteTimeout { get; set; }

        /// <summary>
        /// 与服务端的连接已建立事件
        /// </summary>
        event EventHandler<AsyncEventArgs> Connected;
        /// <summary>
        /// 与服务端的连接已断开事件
        /// </summary>
        event EventHandler<AsyncEventArgs> Disconnected;
        /// <summary>
        /// 接收到服务端数据事件
        /// </summary>
        event EventHandler<AsyncDataEventArgs> DataReceived;
        /// <summary>
        /// 异常事件处理
        /// </summary>
        event EventHandler<AsyncExceptionEventArgs> Exception;

        /// <summary>
        /// 关闭连接远程主机
        /// </summary>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool Close();

        /// <summary>
        /// 连接远程主机
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool Connect(IPEndPoint remoteEP);
        /// <summary>
        /// 连接远程主机
        /// </summary>
        /// <param name="remoteAddress"></param>
        /// <param name="remotePort"></param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool Connect(IPAddress remoteAddress, int remotePort);
        /// <summary>
        /// 连接远程主机
        /// </summary>
        /// <param name="remoteIPAddress"></param>
        /// <param name="remotePort"></param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool Connect(String remoteIPAddress, int remotePort);

        /// <summary>
        /// 异步发送数据到远程服务端
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool SendBytes(byte[] data);
        /// <summary>
        /// 异步发送数据到远程服务端
        /// </summary>
        /// <param name="message"></param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool SendMessage(String message);
    }
}

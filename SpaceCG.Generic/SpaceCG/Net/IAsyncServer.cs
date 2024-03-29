﻿using System;
using System.Collections.Generic;
using System.Net;

namespace SpaceCG.Net
{
    /// <summary>
    /// 简单的异步服务端接口，为可跨平台编译准备的
    /// </summary>
    public interface IAsyncServer : IConnection, IDisposable
    {
        /// <summary>
        /// 与客户端的连接已建立事件
        /// </summary>
        event EventHandler<AsyncEventArgs> ClientConnected;
        /// <summary>
        /// 与客户端的连接已断开事件
        /// </summary>
        event EventHandler<AsyncEventArgs> ClientDisconnected;
        /// <summary>
        /// 接收到客户端数据事件
        /// </summary>
        event EventHandler<AsyncDataEventArgs> ClientDataReceived;
        /// <summary>
        /// 产生了异常事件
        /// </summary>
        event EventHandler<AsyncExceptionEventArgs> ExceptionEvent;

        /// <summary>
        /// 客户端对象集合
        /// </summary>
        IReadOnlyCollection<EndPoint> Clients { get; }
        /// <summary>
        /// 客户端的连接数量
        /// </summary>
        int ClientCount { get; }
        /// <summary>
        /// 服务是否正在运行
        /// </summary>
        bool IsListening { get; }
        /// <summary>
        /// 绑定的本地 IP 地址和端口号
        /// </summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool Start();
        /// <summary>
        /// 停止服务
        /// </summary>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool Stop();

        /// <summary>
        /// 异步发送数据到远程客户端
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="remote">远程地址</param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool SendBytes(byte[] data, EndPoint remote);
        /// <summary>
        /// 异步发送数据到远程客户端
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="ipAddress">远程 IP 地址</param>
        /// <param name="port">远程端口号</param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool SendBytes(byte[] data, string ipAddress, int port);
        /// <summary>
        /// 步发送文本消息到远程客户端
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <param name="remote">远程地址</param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool SendMessage(string message, EndPoint remote);
        /// <summary>
        /// 步发送文本消息到远程客户端
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <param name="ipAddress">远程 IP 地址</param>
        /// <param name="port">远程端口号</param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        bool SendMessage(string message,  string ipAddress, int port);
    }

    /// <summary>
    /// 连接对象事件参数
    /// </summary>
    public class AsyncEventArgs : EventArgs
    {
        /// <summary>
        /// 连接对象地址
        /// </summary>
        public EndPoint EndPoint { get; private set; }

        /// <summary>
        /// 异步连接对象事件参数
        /// </summary>
        /// <param name="endPoint"></param>
        public AsyncEventArgs(EndPoint endPoint)
        {
            this.EndPoint = endPoint;
        }
    }

    /// <summary>
    /// 连接对象数据事件参数
    /// </summary>
    public class AsyncDataEventArgs : AsyncEventArgs
    {
        /// <summary>
        /// 连接对象发送过来的数据
        /// </summary>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// 异步连接对象数据事件参数
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="bytes"></param>
        public AsyncDataEventArgs(EndPoint endPoint, byte[] bytes) : base(endPoint)
        {
            this.Bytes = bytes;
        }
    }

    /// <summary>
    /// 连接对象异常事件参数
    /// </summary>
    public class AsyncExceptionEventArgs : AsyncEventArgs
    {
        /// <summary>
        /// 异常错误信息
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// 异步连接对象异常事件参数
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="exception"></param>
        public AsyncExceptionEventArgs(EndPoint endPoint, Exception exception) : base(endPoint)
        {
            this.Exception = exception;
        }
    }

}

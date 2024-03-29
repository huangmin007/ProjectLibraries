﻿using System;
using SpaceCG.Generic;
using System.Xml.Linq;
using System.Net;

namespace SpaceCG.Net
{
    /// <summary>
    /// 连接类型
    /// </summary>
    public enum ConnectionType
    {
        /// <summary> <see cref="Unknown"/> 未知的 或 不确定的类型  </summary>
        Unknown,

        /// <summary> <see cref="SerialPort"/> 类型 </summary>
        SerialPort,

        /// <summary> <see cref="TcpClient"/> 类型 </summary>
        TcpClient,

        /// <summary> <see cref="UdpClient"/> 类型 </summary>
        UdpClient,

        /// <summary> <see cref="TcpServer"/> 类型 </summary>
        TcpServer,

        /// <summary> <see cref="UdpServer"/> 类型 </summary>
        UdpServer,

        /// <summary> <see cref="ModbusRtu"/> 类型, SerialPort Modbus RTU 协议 </summary>
        ModbusRtu,

        /// <summary> <see cref="ModbusTcp"/> 类型, Modbus TCP 协议 </summary>
        ModbusTcp,

        /// <summary> <see cref="ModbusUdp"/> 类型, Modbus UDP 协议 </summary>
        ModbusUdp,

        /// <summary> <see cref="ModbusUdp"/> 类型, Modbus TCP RTU 协议 </summary>
        ModbusTcpRtu,

        /// <summary> <see cref="ModbusUdpRtu"/> 类型, Modbus UDP RTU 协议 </summary>
        ModbusUdpRtu,

        /// <summary> <see cref="WebSocket"/> 类型, WebSocket 协议 </summary>
        WebSocket,

        /// <summary> <see cref="HTTPRequest"/> 类型, Http Request 协议 </summary>
        HTTPRequest,
    }

    /// <summary>
    /// 连接对象接口
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// 连接对象的类型
        /// </summary>
        ConnectionType Type { get; }

        /// <summary>
        /// 连接对象的名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 连接对象的连接状态
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 向连接的另一端发送数据，或是广播数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns>发送成功则返回 True, 否则返回 False</returns>
        bool SendBytes(byte[] data);

        /// <summary>
        /// 向连接的另一端发送数据，或是广播数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        bool SendBytes(byte[] data, int offset, int count);

        /// <summary>
        /// 向连接的另一端发送文本消息，或是广播文本消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns>发送成功则返回 True, 否则返回 False</returns>
        bool SendMessage(string message);
    }

}

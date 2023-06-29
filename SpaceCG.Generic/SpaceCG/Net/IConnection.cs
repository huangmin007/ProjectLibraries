using System;

namespace SpaceCG.Net
{
#pragma warning disable CS1591
    /// <summary>
    /// 连接类型
    /// </summary>
    public enum ConnectionType
    {
        Unknow,

        SERIAL,

        SERIAL_RTU,

        TCP_CLIENT,

        UDP_CLIENT,

        TCP_CLIENT_RTU,

        UDP_CLIENT_RTU,

        TCP_SERVER,

        UDP_SERVER,
    }

    /// <summary>
    /// 连接接口
    /// </summary>
    public interface IConnection : IDisposable
    {
        event EventHandler<EventArgs> ConnectedEvent;

        event EventHandler<EventArgs> DisconnectedEvent;

        string Name { get; set; }
        bool IsConnected { get; }

        int ReadTimeout { get; set; }

        int WriteTimeout { get; set; }

        void Connect(string hostOrComm, uint portOrRate, params object[] args);

        void Close();

        void SendBytes(byte[] data);

        void SendMessage(String message);

        //int ReceiveBytes(byte[] data, int offset, int length);

        //String ReceiveMessage(int offset, int length);

    }
#pragma warning restore CA1591
}

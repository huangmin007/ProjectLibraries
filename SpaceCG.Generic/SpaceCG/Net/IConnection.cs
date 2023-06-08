using System;

namespace SpaceCG.Net
{
    public interface IConnection : IDisposable
    {
        event EventHandler<EventArgs> ConnectedEvent;

        event EventHandler<EventArgs> DisconnectedEvent;

        bool IsConnected { get; }

        int ReadTimeout { get; set; }

        int WriteTimeout { get; set; }

        void Connect(string hostOrComm, uint portOrRate);

        void Close();

        void SendBytes(byte[] data);

        void SendMessage(String message);

        int ReceiveBytes(byte[] data, int offset, int length);

        String ReceiveMessage(int offset, int length);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceCG.Extensions
{
    public static class HPSocketExtensions
    {
        public static bool SendBytes(this IClient client, byte[] buffer)
        {
            if(client.IsConnect)
            {
                client.SendBytes(buffer, buffer.Length);
            }
        }
    }
}

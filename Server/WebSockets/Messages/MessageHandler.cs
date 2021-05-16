using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages
{
    public abstract class MessageHandler
    {
        public virtual async Task<string> HandleMessage(WebSocketConnection connection, BinaryReader reader)
        {
            return string.Empty;
        }

        public abstract string ProtocolName { get; }

        public static Guid ReadMinecraftUUIDFromBinaryReader(BinaryReader br)
        {
            int length = br.ReadInt32();
            byte[] data = br.ReadBytes(length);

            return Guid.Parse(Encoding.UTF8.GetString(data));
        }

    }
}

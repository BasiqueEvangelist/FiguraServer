using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages
{
    public class MessageHandler
    {
        public int bodyLength;

        public virtual async Task<string> HandleHeader(WebSocketConnection connection, BinaryReader reader) {
            bodyLength = reader.ReadInt32();
            return string.Empty;
        }

        public virtual async Task<string> HandleBody(WebSocketConnection connection, BinaryReader reader)
        {
            return string.Empty;
        }

        public virtual bool ExpectBody()
        {
            return bodyLength > 0;
        }

        public static Guid ReadMinecraftUUIDFromBinaryReader(BinaryReader br)
        {
            int length = br.ReadInt32();
            byte[] data = br.ReadBytes(length);

            return Guid.Parse(Encoding.UTF8.GetString(data));
        }

    }
}

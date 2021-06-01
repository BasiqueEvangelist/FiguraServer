using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages
{
    public abstract class MessageSender
    {
        public abstract string ProtocolName { get; }

        public virtual async Task SendData(WebSocketConnection connection)
        {
            //Build & Send Message
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write(connection.Registry.GetMessageId(ProtocolName));

                    await Write(bw);

                    await connection.socket.SendAsync(new ArraySegment<byte>(ms.ToArray()), System.Net.WebSockets.WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }

        public virtual async Task Write(BinaryWriter writer)
        {

        }

        public static void WriteMinecraftUUIDToBinaryWriter(Guid id, BinaryWriter bw)
        {
            string guid = id.ToString();

            byte[] data = Encoding.UTF8.GetBytes(guid);

            bw.Write(data.Length);
            bw.Write(data);
        }
    }
}

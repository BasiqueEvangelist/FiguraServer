using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Senders
{
    public class MessageSender
    {
        public sbyte messageID;

        public MessageSender(sbyte ID)
        {
            messageID = ID;
        }


        public virtual async Task SendData(WebSocketConnection connection)
        {

            byte[] bodyTemp = null;

            //Build Body.
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    await WriteBody(bw);

                    if (ms.Length > 0)
                        bodyTemp = ms.ToArray();
                }
            }

            //Build & Send Header
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(messageID);

                    if (bodyTemp != null)
                        bw.Write(bodyTemp.Length);
                    else
                        bw.Write((int)0);

                    await WriteHeader(bw);

                    await connection.socket.SendAsync(new ArraySegment<byte>(ms.ToArray()), System.Net.WebSockets.WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }

            //Send Body.
            await connection.socket.SendAsync(new ArraySegment<byte>(bodyTemp), System.Net.WebSockets.WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        public virtual async Task WriteHeader(BinaryWriter writer)
        {

        }

        public virtual async Task WriteBody(BinaryWriter writer)
        {

        }
    }
}

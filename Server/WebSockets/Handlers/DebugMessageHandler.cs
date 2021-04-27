using FiguraServer.Server.WebSockets.Senders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Handlers
{
    public class DebugMessageHandler : MessageHandler
    {

        public async override Task<string> HandleHeader(WebSocketConnection connection, BinaryReader reader)
        {
            await base.HandleHeader(connection, reader);

            //Console.WriteLine(reader.ReadInt32());

            return string.Empty;
        }

        public async override Task<string> HandleBody(WebSocketConnection connection, BinaryReader reader)
        {
            await base.HandleBody(connection, reader);

            //Console.WriteLine(reader.ReadInt32());

            DebugMessageSender sender = new DebugMessageSender();

            await sender.SendData(connection);

            return string.Empty;
        }

        public override bool ExpectBody()
        {
            return true;
        }
    }
}

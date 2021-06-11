using FiguraServer.Server.WebSockets.Messages.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Pings
{
    public class PingMessageHandler : MessageHandler
    {
        public override async Task<string> HandleMessage(WebSocketConnection connection, BinaryReader reader)
        {
            if (!connection.pingRateLimiter.TryTakePoints(1))
            {
                connection.SendMessage(new ErrorMessageSender(ErrorMessageSender.PING_RATE_LIMIT));
                return string.Empty;
            }

            await base.HandleMessage(connection, reader);

            int dataSize = reader.ReadInt32();

            //Remove overhead of 1 short for the ping count for batching.
            if (!connection.pingByteRateLimiter.TryTakePoints(dataSize - sizeof(short)))
            {
                connection.SendMessage(new ErrorMessageSender(ErrorMessageSender.PING_BYTE_RATE_LIMIT));
                return string.Empty;
            }

            byte[] data = new byte[dataSize];
            reader.Read(data);

            PingMessageSender sender = new PingMessageSender(data, connection.playerID);

            PubSubManager.SendMessage(connection.playerID, sender);

            return string.Empty;
        }

        public override string ProtocolName => "figura_v1:ping";
    }
}

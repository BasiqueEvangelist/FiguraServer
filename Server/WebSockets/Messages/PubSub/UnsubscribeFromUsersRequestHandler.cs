using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.PubSub
{
    public class UnsubscribeFromUsersRequestHandler : MessageHandler
    {
        public override async Task<string> HandleMessage(WebSocketConnection connection, BinaryReader reader)
        {

            int count = Math.Min(reader.ReadInt32(), 256);

            for (int i = 0; i < count; i++)
            {
                Guid targetID = ReadMinecraftUUIDFromBinaryReader(reader);

                PubSubManager.Unsubscribe(targetID, connection.playerID);
            }

            return string.Empty;
        }

        public override string ProtocolName => "figura_v1:user_events_unsub";
    }
}

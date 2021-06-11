using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.PubSub
{

    //Handles when a user requests to send an avatar update on their channel.
    public class ChannelAvatarUpdateRequestHandler : MessageHandler
    {
        public override async Task<string> HandleMessage(WebSocketConnection connection, BinaryReader reader)
        {
            ChannelAvatarUpdateMessageSender sender = new ChannelAvatarUpdateMessageSender(connection.playerID);

            PubSubManager.SendMessage(connection.playerID, sender);

            return string.Empty;
        }

        public override string ProtocolName => "figura_v1:channel_avatar_update";
    }
}

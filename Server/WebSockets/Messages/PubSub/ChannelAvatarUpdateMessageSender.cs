using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.PubSub
{

    //Sends the data for an avatar update to other users
    public class ChannelAvatarUpdateMessageSender : ChannelMessageSender
    {
        public ChannelAvatarUpdateMessageSender(Guid sourceUser) : base(sourceUser){}

        public override async Task Write(BinaryWriter writer)
        {
            await base.Write(writer);

            Logger.LogMessage("SEND AVATAR UPDATE");
        }

        public override string ProtocolName => "figura_v1:channel_avatar_update";
    }
}

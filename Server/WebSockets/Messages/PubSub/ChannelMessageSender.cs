using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace FiguraServer.Server.WebSockets.Messages.PubSub
{
    /// <summary>
    /// Sends a message on a channel.
    /// </summary>
    public class ChannelMessageSender : MessageSender
    {

        public Guid sourceUser;

        public ChannelMessageSender(Guid sourceUser)
        {
            this.sourceUser = sourceUser;
        }

        public override async Task Write(BinaryWriter writer)
        {
            await base.Write(writer);

            WriteMinecraftUUIDToBinaryWriter(sourceUser, writer);
        }

        public override string ProtocolName => "figura_v1:channel_message";
    }
}

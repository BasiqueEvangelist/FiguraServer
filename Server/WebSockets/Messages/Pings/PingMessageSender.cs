using FiguraServer.Server.WebSockets.Messages.PubSub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Pings
{
    public class PingMessageSender : ChannelMessageSender
    {

        public byte[] data;

        public PingMessageSender(byte[] data, Guid senderID) : base(senderID)
        {
            this.data = data;
        }

        public override async Task Write(BinaryWriter writer)
        {
            await base.Write(writer);

            writer.Write(data);
        }

        public override string ProtocolName => "figura_v1:ping_handle";
    }
}

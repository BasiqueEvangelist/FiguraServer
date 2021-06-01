using System;
using System.IO;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages
{
    public class AuthenticateResponse : MessageSender
    {
        private readonly Guid playerId;

        public AuthenticateResponse(Guid playerId)
        {
            this.playerId = playerId;
        }

        public override string ProtocolName => "figura_v1:authenticate";

        public override async Task Write(BinaryWriter writer)
        {
            await base.Write(writer);

            WriteMinecraftUUIDToBinaryWriter(playerId, writer);
        }
    }
}

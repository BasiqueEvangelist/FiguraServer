using System;
using System.IO;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages
{
    public class AuthenticateResponse : MessageSender
    {
        private readonly Guid playerId;

        public AuthenticateResponse(Guid playerId) : base(MessageIDs.AUTHENTICATE_RESPONSE_ID)
        {
            this.playerId = playerId;
        }

        public override async Task Write(BinaryWriter writer)
        {
            await base.Write(writer);

            WriteMinecraftUUIDToBinaryWriter(playerId, writer);
        }
    }
}

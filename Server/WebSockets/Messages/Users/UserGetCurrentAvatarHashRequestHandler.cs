using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Users
{
    public class UserGetCurrentAvatarHashRequestHandler : MessageHandler
    {

        public async override Task<string> HandleHeader(WebSocketConnection connection, BinaryReader reader)
        {
            await base.HandleHeader(connection, reader);

            Guid id = ReadMinecraftUUIDFromBinaryReader(reader);

            connection.SendMessage(new UserAvatarHashProvideResponse(id));

            return string.Empty;
        }

    }
}

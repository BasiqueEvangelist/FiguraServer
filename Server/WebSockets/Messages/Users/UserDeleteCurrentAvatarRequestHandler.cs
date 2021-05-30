using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Users
{
    public class UserDeleteCurrentAvatarRequestHandler : MessageHandler
    {
        public async override Task<string> HandleMessage(WebSocketConnection connection, BinaryReader reader)
        {
            if (connection.connectionUser == null) return "";

            await base.HandleMessage(connection, reader);

            await connection.connectionUser.TryDeleteCurrentAvatar();

            return string.Empty;
        }
    }
}

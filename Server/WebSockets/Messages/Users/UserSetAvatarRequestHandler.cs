using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Users
{
    public class UserSetAvatarRequestHandler : MessageHandler
    {

        public async override Task<string> HandleHeader(WebSocketConnection connection, BinaryReader reader)
        {
            await base.HandleHeader(connection, reader);

            Guid id = ReadMinecraftUUIDFromBinaryReader(reader);
            bool shouldDelete = reader.ReadSByte() == 1;

            if(shouldDelete)
                await connection.connectionUser.TryDeleteCurrentAvatar();
            await connection.connectionUser.SetCurrentAvatar(id);

            return string.Empty;
        }

    }
}

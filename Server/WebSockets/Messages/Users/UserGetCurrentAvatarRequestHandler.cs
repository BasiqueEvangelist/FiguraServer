using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Users
{
    public class UserGetCurrentAvatarRequestHandler : MessageHandler
    {

        public async override Task<string> HandleHeader(WebSocketConnection connection, BinaryReader reader)
        {
            await base.HandleHeader(connection, reader);

            Guid userID = ReadMinecraftUUIDFromBinaryReader(reader);

            byte[] data;
            using (DatabaseAccessor accessor = new DatabaseAccessor())
            {
                data = await accessor.GetAvatarDataForUser(userID);
            }

            if (data == null)
            {
                return string.Empty;
            }

            connection.SendMessage(new UserAvatarProvideResponse(userID, data));

            return String.Empty;
        }

    }
}

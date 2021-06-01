using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Users
{
    public class UserGetCurrentAvatarHashRequestHandler : MessageHandler
    {

        public async override Task<string> HandleMessage(WebSocketConnection connection, BinaryReader reader)
        {
            await base.HandleMessage(connection, reader);

            Guid id = ReadMinecraftUUIDFromBinaryReader(reader);


            byte[] getHash;
            using (DatabaseAccessor accessor = new DatabaseAccessor())
            {
                string s = await accessor.GetAvatarHashForUser(id);

                if (s == null)
                {
                    return string.Empty;
                }

                getHash = Encoding.UTF8.GetBytes(s);
            }

            connection.SendMessage(new UserAvatarHashProvideResponse(id, getHash));

            return string.Empty;
        }

        public override string ProtocolName => "figura_v1:user_get_current_avatar_hash";
    }
}

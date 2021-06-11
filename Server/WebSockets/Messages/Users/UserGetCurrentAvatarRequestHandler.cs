using FiguraServer.Server.WebSockets.Messages.Utility;
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

        public async override Task<string> HandleMessage(WebSocketConnection connection, BinaryReader reader)
        {
            if (!connection.avatarRequestRateLimiter.TryTakePoints(1))
            {
                connection.SendMessage(new ErrorMessageSender(ErrorMessageSender.AVATAR_REQUEST_RATE_LIMIT));
                return string.Empty;
            }

            await base.HandleMessage(connection, reader);

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

        public override string ProtocolName => "figura_v1:user_get_current_avatar";
    }
}

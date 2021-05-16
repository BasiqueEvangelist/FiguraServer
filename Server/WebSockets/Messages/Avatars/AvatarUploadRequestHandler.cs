using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Avatars
{
    public class AvatarUploadRequestHandler : MessageHandler
    {

        public async override Task<string> HandleMessage(WebSocketConnection connection, BinaryReader reader)
        {
            int avatarLength = reader.ReadInt32();

            (sbyte retCode, Guid avatarID) = await connection.connectionUser.TryAddAvatar(reader.ReadBytes(avatarLength));

            switch (retCode)
            {
                //Success
                case 0:
                    OnSuccess(avatarID, connection);
                    break;
                //Fail, too many avatars
                case 1:
                    OnTooManyAvatars(connection);
                    break;
                //Fail, empty avatar data
                case 2:
                    OnEmptyAvatar(connection);
                    break;
                //Fail, not enough space.
                case 3:
                    OnNotEnoughSpace(connection);
                    break;
            }

            return string.Empty;
        }

        public void OnSuccess(Guid avatarID, WebSocketConnection connection)
        {
            connection.SendMessage(new AvatarUploadResponse(avatarID));
        }

        public void OnTooManyAvatars(WebSocketConnection connection)
        {
            connection.SendMessage(new AvatarUploadResponse(1));
        }

        public void OnEmptyAvatar(WebSocketConnection connection)
        {
            connection.SendMessage(new AvatarUploadResponse(2));
        }

        public void OnNotEnoughSpace(WebSocketConnection connection)
        {
            connection.SendMessage(new AvatarUploadResponse(3));
        }

        public override string ProtocolName => "figura_v1:avatar_upload";
    }
}

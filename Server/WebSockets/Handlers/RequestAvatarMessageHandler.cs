using FiguraServer.Data;
using FiguraServer.Server.WebSockets.Senders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Handlers
{
    /// <summary>
    /// This class is responsible for handling requests to download avatars.
    /// Header Format :
    /// 16 bytes - UUID of avatar requested
    /// 1 byte - Full data or Partial data (partial data is just the avatar NBT, full data is the full avatar description/id/etc)
    /// </summary>
    public class RequestAvatarMessageHandler : MessageHandler
    {

        /*public override async Task<string> HandleHeader(WebSocketConnection connection, BinaryReader reader)
        {
            //Get avatar GUID from the header
            Guid avatarID = new Guid(reader.ReadBytes(16));
            //Determines if the avatar request was full data (including stuff like metadata and owner) or just the bytes for the NBT
            bool isFullData = reader.ReadBoolean();

            //Return header.
            await base.HandleHeader(context);

            //Send full data.
            if (isFullData)
            {
                //Avatar fullAvatar = await context.accessor.GetAvatar(avatarID);
            }
            else
            {
                //Send data back to the player.
                await new ByteArraySender(
                    MessageHandlerProtocolV1.REPLY_AVATAR_DATA_ID,
                    await context.accessor.GetAvatarData(avatarID)
                ).SendData(context);
            }

            //Return.
            return string.Empty;
        }*/

    }
}

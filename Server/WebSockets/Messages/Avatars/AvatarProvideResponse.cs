using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Avatars
{
    //The most common response, gives an avatar's NBT data to the given connection
    public class AvatarProvideResponse : MessageSender
    {
        private byte[] responseData;

        public AvatarProvideResponse(byte[] responseData) : base(MessageIDs.AVATAR_PROVIDE_RESPONSE_ID)
        {
            this.responseData = responseData;
        }

        public async override Task WriteBody(BinaryWriter writer)
        {
            await base.WriteBody(writer);

            writer.Write(responseData);
        }

    }
}

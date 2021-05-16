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

        public AvatarProvideResponse(byte[] responseData)
        {
            this.responseData = responseData;
        }

        public async override Task Write(BinaryWriter writer)
        {
            await base.Write(writer);

            writer.Write(responseData.Length);
            writer.Write(responseData);
        }

        public override string ProtocolName => "figura_v1:avatar_provide";
    }
}

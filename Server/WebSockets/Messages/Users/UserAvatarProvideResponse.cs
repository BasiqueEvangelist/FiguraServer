using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Users
{
    //The response that provides the current UUID of an avatar.
    public class UserAvatarProvideResponse : MessageSender
    {
        public Guid uuid;
        public byte[] payload;

        public UserAvatarProvideResponse(Guid uuid, byte[] payload)
        {
            this.uuid = uuid;
            this.payload = payload;
        }

        public async override Task Write(BinaryWriter writer)
        {
            await base.Write(writer);

            WriteMinecraftUUIDToBinaryWriter(uuid, writer);
            writer.Write(payload.Length);
            writer.Write(payload);
        }

        public override string ProtocolName => "figura_v1:user_avatar_provide";
    }
}

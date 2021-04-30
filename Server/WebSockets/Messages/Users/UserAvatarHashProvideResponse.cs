using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Users
{
    public class UserAvatarHashProvideResponse : MessageSender
    {
        public Guid userUUID;
        public byte[] hash;

        public UserAvatarHashProvideResponse(Guid id, byte[] hash) : base(MessageIDs.USER_AVATAR_HASH_PROVIDE_RESPONSE_ID)
        {
            this.userUUID = id;
        }

        public async override Task WriteHeader(BinaryWriter writer)
        {
            await base.WriteHeader(writer);

            WriteMinecraftUUIDToBinaryWriter(userUUID, writer);
            writer.Write(hash.Length);
            writer.Write(hash);
        }

    }
}

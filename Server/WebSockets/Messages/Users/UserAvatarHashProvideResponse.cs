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

        public UserAvatarHashProvideResponse(Guid id) : base(MessageIDs.USER_AVATAR_HASH_PROVIDE_RESPONSE_ID)
        {
            this.userUUID = id;
        }

        public async override Task WriteHeader(BinaryWriter writer)
        {
            await base.WriteHeader(writer);

            byte[] getHash;
            using (DatabaseAccessor accessor = new DatabaseAccessor()) {
                string s = await accessor.GetAvatarHashForUser(userUUID);

                if (s == null)
                {
                    return;
                }

                getHash = Encoding.UTF8.GetBytes(s);
            }

            WriteMinecraftUUIDToBinaryWriter(userUUID, writer);
            writer.Write(getHash.Length);
            writer.Write(getHash);
        }

    }
}

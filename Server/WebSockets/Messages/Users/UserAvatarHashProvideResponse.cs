﻿using System;
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

        public UserAvatarHashProvideResponse(Guid id, byte[] hash)
        {
            this.userUUID = id;
            this.hash = hash;
        }

        public async override Task Write(BinaryWriter writer)
        {
            await base.Write(writer);

            WriteMinecraftUUIDToBinaryWriter(userUUID, writer);
            writer.Write(hash.Length);
            writer.Write(hash);
        }

        public override string ProtocolName => "figura_v1:user_avatar_hash_provide";
    }
}

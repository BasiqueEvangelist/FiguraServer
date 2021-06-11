using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Utility
{
    public class ErrorMessageSender : MessageSender
    {
        public static readonly short BYTE_RATE_LIMIT = 0;
        public static readonly short MESSAGE_RATE_LIMIT = 1;
        public static readonly short AVATAR_UPLOAD_RATE_LIMIT = 2;
        public static readonly short AVATAR_REQUEST_RATE_LIMIT = 3;
        public static readonly short PING_BYTE_RATE_LIMIT = 4;
        public static readonly short PING_RATE_LIMIT = 5;


        public short code;

        public ErrorMessageSender(short code)
        {
            this.code = code;
        }

        public override async Task Write(BinaryWriter writer)
        {
            await base.Write(writer);
            writer.Write(code);
        }

        public override string ProtocolName => "figura_v1:error";
    }
}

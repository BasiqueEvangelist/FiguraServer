using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Senders
{
    public class DebugMessageSender : MessageSender
    {

        public DebugMessageSender() : base(0)
        {

        }

        public async override Task WriteHeader(BinaryWriter writer)
        {
            await base.WriteHeader(writer);

            writer.Write((int)987654321);
        }

        public async override Task WriteBody(BinaryWriter writer)
        {
            await base.WriteBody(writer);

            writer.Write(123456789);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets
{
    public struct MessageContext
    {
        public WebSocketConnection connection;
        public WebSocketReceiveResult message;
        public MemoryStream stream;
        public DatabaseAccessor accessor;
        public BinaryReader reader;
        public byte[] headerBuffer;
    }
}

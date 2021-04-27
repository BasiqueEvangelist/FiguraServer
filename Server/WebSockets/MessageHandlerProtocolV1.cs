using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using FiguraServer.Server.WebSockets.Handlers;

namespace FiguraServer.Server.WebSockets
{
    public class MessageHandlerProtocolV1 : MessageHandlerProtocol
    {
        public const sbyte REPLY_AVATAR_DATA_ID = sbyte.MinValue;

        public MessageHandlerProtocolV1()
        {
            registeredMessages[sbyte.MinValue] = new RequestAvatarMessageHandler();
            registeredMessages[sbyte.MinValue + 1] = new UploadAvatarRequestHandler();
            registeredMessages[0] = new DebugMessageHandler();
        }
    }
}

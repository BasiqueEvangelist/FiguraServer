using FiguraServer.Server.WebSockets.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets
{
    public class MessageHandlerProtocol
    {
        //The message 
        public Dictionary<sbyte, MessageHandler> registeredMessages = new Dictionary<sbyte, MessageHandler>();
    }
}

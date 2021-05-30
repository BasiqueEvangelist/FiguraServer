using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using FiguraServer.Server.WebSockets.Messages;
using FiguraServer.Server.WebSockets.Messages.Avatars;
using FiguraServer.Server.WebSockets.Messages.Users;

namespace FiguraServer.Server.WebSockets
{
    public class MessageHandlerProtocolV1 : MessageHandlerProtocol
    {
        public MessageHandlerProtocolV1()
        {
            registeredMessages[MessageIDs.AVATAR_REQUEST_HANDLER_ID] = new AvatarRequestHandler();
            registeredMessages[MessageIDs.AVATAR_UPLOAD_HANDLER_ID] = new AvatarUploadRequestHandler();

            //registeredMessages[MessageIDs.USER_AVATAR_REQUEST_HANDLER_ID] = new UserAvatarRequestHandler();
            registeredMessages[MessageIDs.USER_SET_AVATAR_REQUEST_HANDLER_ID] = new UserSetAvatarRequestHandler();
            registeredMessages[MessageIDs.USER_DELETE_CURRENT_AVATAR_REQUEST_HANDLER_ID] = new UserDeleteCurrentAvatarRequestHandler();
            registeredMessages[MessageIDs.USER_GET_CURRENT_AVATAR_REQUEST_HANDLER_ID] = new UserGetCurrentAvatarRequestHandler();
            registeredMessages[MessageIDs.USER_GET_CURRENT_AVATAR_HASH_REQUEST_HANDLER_ID] = new UserGetCurrentAvatarHashRequestHandler();
            registeredMessages[MessageIDs.AUTHENTICATE_REQUEST_HANDLER_ID] = new AuthenticateRequestHandler();
        }
    }
}

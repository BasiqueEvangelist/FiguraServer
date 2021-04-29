using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Avatars
{
    public class DeleteAvatarResponse : MessageSender
    {
        public DeleteAvatarResponse() : base(MessageIDs.AVATAR_DELETE_RESPONSE_ID)
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Avatars
{
    public class DeleteAvatarResponse : MessageSender
    {
        public DeleteAvatarResponse()
        {

        }

        public override string ProtocolName => "figura_v1:delete_avatar";
    }
}

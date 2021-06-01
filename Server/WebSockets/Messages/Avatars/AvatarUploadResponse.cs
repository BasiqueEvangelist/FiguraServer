using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages.Avatars
{
    //Response to an upload, letting the user know the status of the upload.
    public class AvatarUploadResponse : MessageSender
    {
        public sbyte retCode = 0;
        public Guid uuid = Guid.Empty;

        //Creates an upload response with a valid avatar.
        public AvatarUploadResponse(Guid uuid)
        {
            this.uuid = uuid;
        }

        //Creates a response that has an error return code
        public AvatarUploadResponse(sbyte retCode)
        {
            this.retCode = retCode;
        }

        public async override Task Write(BinaryWriter writer)
        {
            await base.Write(writer);

            //Write the code for this response
            writer.Write(retCode);

            if (retCode == 0) //Write the 16 byes for the UUID, if we suceeded
                WriteMinecraftUUIDToBinaryWriter(uuid, writer);
        }

        public override string ProtocolName => "figura_v1:avatar_upload";
    }
}

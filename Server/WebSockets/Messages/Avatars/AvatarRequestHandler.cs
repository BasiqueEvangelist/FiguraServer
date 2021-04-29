using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FiguraServer.Data;

namespace FiguraServer.Server.WebSockets.Messages.Avatars
{
    public class AvatarRequestHandler : MessageHandler
    {

        public async override Task<string> HandleHeader(WebSocketConnection connection, BinaryReader reader)
        {
            await base.HandleHeader(connection, reader);

            //Get GUID
            Guid id = ReadMinecraftUUIDFromBinaryReader(reader);

            try
            {
                //Open connection to database
                using (DatabaseAccessor dba = new DatabaseAccessor())
                {
                    //Get avatar data from the database
                    byte[] avatarData = await dba.GetAvatarData(id);

                    //If the array is empty, there is no data for this avatar.
                    if (avatarData == null)
                    {
                        return "NO AVATAR";
                    }

                    //Reply with avatar data
                    connection.SendMessage(new AvatarProvideResponse(avatarData));

                    return "SUCCESS";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return "N/A";
        }

    }
}

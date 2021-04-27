using FiguraServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fNbt;
using System.IO;
using FiguraServer;

namespace FiguraServer.Data
{
    public class Avatar
    {
        public Guid id;
        public Guid authorID;
        public NbtCompound avatarData;
        public NbtCompound metadata;

        //Constructor takes the sql row and constructs it into this class.
        public Avatar(dynamic data)
        {
            id = Guid.Parse(data.uuid);
            authorID = Guid.Parse(data.author);
            avatarData = Extensions.GetNbtCompoundFromBytes(data.nbt);
            metadata = Extensions.GetNbtCompoundFromBytes(data.metadata);
        }

        //Turns the data for the object into a dictionary to be saved to mysql
        //Theoretically, you should only save individual components, but for now this works.
        public object GetData()
        {
            return new Dictionary<string, object>()
            {
                { "uuid", id.ToString("N") },
                { "author", authorID.ToString("N") },
                { "nbt", Extensions.GetBytesFromNBTCompound(avatarData) },
                { "metadata", Extensions.GetBytesFromNBTCompound(metadata) },
            };
        }
    }
}

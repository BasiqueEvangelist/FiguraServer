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
        public byte[] nbt;
        public string tags;
        public string hash;

        public Avatar()
        {
            id = Guid.Empty;
            nbt = new byte[0];
            tags = string.Empty;
            hash = string.Empty;
        }

        //Constructor takes the sql row and constructs it into this class.
        public Avatar(dynamic data)
        {
            id = Guid.Parse(data.uuid);
            nbt = data.nbt;
            tags = data.tags;
            hash = data.hash;
        }

        //Turns the data for the object into a dictionary to be saved to mysql
        public object GetData()
        {
            return new
            {
                uuid = id.ToString("N"),
                nbt = nbt,
                tags = tags,
                hash = hash,
                size = nbt.Length
            };
        }
    }
}

using fNbt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Data
{
    public class Pack
    {
        public Guid id;
        public Guid author;
        public NbtCompound avatarList;
        public NbtCompound metadata;

        //Constructor takes the sql row and constructs it into this class.
        public Pack(dynamic data)
        {
            id = Guid.Parse(data.uuid);
            author = Guid.Parse(data.author);
            avatarList = Extensions.GetNbtCompoundFromBytes(data.avatar_list);
            metadata = Extensions.GetNbtCompoundFromBytes(data.metadata);
        }
    }
}

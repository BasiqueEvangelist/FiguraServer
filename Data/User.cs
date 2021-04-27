using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fNbt;

namespace FiguraServer.Data
{
    public class User
    {
        public Guid id;
        public Guid currentAvatarID;
        public double karma;
        public NbtCompound trustData;
        public NbtCompound favoriteData;
        public NbtCompound config;

        //Constructor takes the sql row and constructs it into this class.
        public User(dynamic data)
        {
            id = Guid.Parse(data.uuid);
            currentAvatarID = Guid.Parse(data.current_avatar);
            karma = data.karma;
            trustData = Extensions.GetNbtCompoundFromBytes(data.trust_data);
            favoriteData = Extensions.GetNbtCompoundFromBytes(data.favorite_data);
            config = Extensions.GetNbtCompoundFromBytes(data.config);
        }

        public User(Guid id)
        {
            this.id = id;
            this.currentAvatarID = Guid.Empty;
            karma = 0;
            trustData = new NbtCompound("trust");
            favoriteData = new NbtCompound("favorite");
            config = new NbtCompound("config");
        }

        //Turns the data for the object into a dictionary to be saved to mysql
        //Theoretically, you should only save individual components, but for now this works.
        public object GetData()
        {
            return new Dictionary<string, object>()
            {
                { "uuid", id.ToString("N") },
                { "current_avatar", currentAvatarID.ToString("N") },
                { "karma", karma },
                { "trust_data", Extensions.GetBytesFromNBTCompound(trustData) },
                { "favorite_data", Extensions.GetBytesFromNBTCompound(favoriteData) },
                { "config", Extensions.GetBytesFromNBTCompound(config) },
            };
        }
    }
}

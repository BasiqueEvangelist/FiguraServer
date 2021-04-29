using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FiguraServer.Server;
using fNbt;

namespace FiguraServer.Data
{
    public class User
    {
        public const int MAX_USER_DATA_SIZE = 1024 * 100;
        public static readonly SHA256 hasher = SHA256.Create();

        private Guid id;
        private Guid currentAvatarID;
        private double karma;
        private int totalAvatarSize;
        private List<Guid> ownedAvatars;
        private List<Guid> ownedPacks;

        //Constructor takes the sql row and constructs it into this class.
        public User(dynamic data)
        {
            id = Guid.Parse(data.uuid);
            currentAvatarID = Guid.Parse(data.current_avatar);
            karma = data.karma;
            totalAvatarSize = data.total_avatar_size;
            ownedAvatars = StringToGuidList(data.owned_avatars);
            ownedPacks = StringToGuidList(data.owned_packs);
        }

        public User(Guid id)
        {
            this.id = id;
            this.currentAvatarID = Guid.Empty;
            karma = 0;
            totalAvatarSize = 0;
            ownedAvatars = new List<Guid>();
            ownedPacks = new List<Guid>();
        }

        //Turns the data for the object into a dictionary to be saved to mysql
        public object GetData()
        {
            return new
            {
                uuid = id.ToString("N"),
                current_avatar = currentAvatarID.ToString("N"),
                total_avatar_size = totalAvatarSize,
                owned_avatars = GuidListToString(ownedAvatars),
                owned_packs = GuidListToString(ownedPacks),
                karma = 0
            };
        }

        #region Operations

        //Tries to add an avatar via this user.
        public async Task<(sbyte, Guid)> TryAddAvatar(byte[] data)
        {
            //Max avatars a person can have is 100, no matter what.
            if (ownedAvatars.Count >= 100)
                return (1, Guid.Empty);

            //If the avatar is 0 bytes long, ignore it.
            if (data.Length == 0)
                return (2, Guid.Empty);

            //If the avatar would be too large to fit into the userdata, cancel.
            if (totalAvatarSize + data.Length > MAX_USER_DATA_SIZE)
                return (3, Guid.Empty);


            //Generate new avatar using the data the user provided.
            Guid newId = Guid.NewGuid();

            Avatar newAvatar = new Avatar()
            {
                id = newId,
                nbt = data,
                hash = Encoding.UTF8.GetString(hasher.ComputeHash(data)),
                tags = string.Empty
            };

            //Add the avatar to the list of owned avatars
            ownedAvatars.Add(newId);
            //Increase avatar size
            totalAvatarSize += data.Length;

            using (DatabaseAccessor accessor = new DatabaseAccessor())
            {
                //Build the user update table
                Dictionary<string, object> userUpdate = new Dictionary<string, object>()
                {
                    { "total_avatar_size", totalAvatarSize},
                    { "owned_avatars", GuidListToString(ownedAvatars) }
                };

                //Update the user with the new info for the posted avatar
                int userUpdateResponse = await accessor.UpdateUser(id, userUpdate);

                //Post the avatar
                int avatarPostResponse = await accessor.PostAvatar(newAvatar);
            }

            return (0, newId);
        }

        public async Task<sbyte> TryDeleteCurrentAvatar() {
            if(currentAvatarID != Guid.Empty)
                return await TryDeleteAvatar(currentAvatarID);
            else 
                return 1;
        }

        public async Task<sbyte> TryDeleteAvatar(Guid avatarID)
        {
            //Attempt to remove avatar.
            if (!ownedAvatars.Remove(avatarID))
                return 1;

            //If the avatar is the current avatar ID, clear current avatar.
            if (avatarID == currentAvatarID)
                currentAvatarID = Guid.Empty;

            //Push to database.
            using (DatabaseAccessor accessor = new DatabaseAccessor())
            {
                //Detract from avatar size.
                int avatarSize = await accessor.GetAvatarSize(avatarID);
                totalAvatarSize -= avatarSize;

                //Build the user update table
                Dictionary<string, object> userUpdate = new Dictionary<string, object>()
                {
                    { "total_avatar_size", totalAvatarSize },
                    { "owned_avatars", GuidListToString(ownedAvatars)},
                    { "current_avatar", currentAvatarID.ToString("N")},
                };

                //Update the user with the new info after the avatar was deleted
                int userUpdateResponse = await accessor.UpdateUser(id, userUpdate);

                //Delete the avatar off the database
                await accessor.DeleteAvatar(avatarID);
            }

            return 0;
        }

        public async Task SetCurrentAvatar(Guid avatarID)
        {
            currentAvatarID = avatarID;

            using (DatabaseAccessor accessor = new DatabaseAccessor())
            {
                //Build the user update table
                Dictionary<string, object> userUpdate = new Dictionary<string, object>()
                {
                    { "current_avatar", currentAvatarID.ToString("N")},
                };

                //Update the user with the new info for the posted avatar
                int userUpdateResponse = await accessor.UpdateUser(id, userUpdate);
            }
        }

        public static string GuidListToString(List<Guid> list, char separationChar = ';')
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(list[i].ToString("N"));
                sb.Append(separationChar);
            }

            return sb.ToString();
        }

        public static List<Guid> StringToGuidList(string list, char separationChar = ';')
        {
            List<Guid> ret = new List<Guid>();
            string[] parts = list.Split(separationChar);

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 32)
                    ret.Add(Guid.Parse(parts[i]));
            }

            return ret;
        }

        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using FiguraServer.Data;
using MySqlConnector;
using SqlKata.Compilers;
using SqlKata.Execution;
using SqlKata.Extensions;
namespace FiguraServer.Server
{
    public class DatabaseAccessor : IDisposable
    {
        public SqlConnection Connection { get; }
        public MySqlCompiler Compiler { get; }
        public QueryFactory QueryFactory { get; }

        public DatabaseAccessor()
        {
            Connection = new SqlConnection(Program.sqlConnectionString);
            Compiler = new MySqlCompiler();

            QueryFactory = new QueryFactory(Connection, Compiler);
        }

        public void Dispose()
        {
            QueryFactory.Dispose();
            Connection.Dispose();
        }

        #region Avatar


        //Returns the NBT data for the avatar from the UUID.
        public async Task<byte[]> GetAvatarData(Guid uuid)
        {
            var avatars = await QueryFactory.Query("avatar_data").Select("nbt").Where("uuid", uuid.ToString("N")).GetAsync();

            if (avatars.Count() == 0)
                return null;

            return avatars.First().nbt;
        }

        //Returns the full avatar from the UUID.
        public async Task<Avatar> GetAvatar(Guid uuid)
        {
            var avatars = await QueryFactory.Query("avatar_data").Where("uuid", uuid.ToString("N")).GetAsync();

            if(avatars.Count() == 0)
                return null;

            return new Avatar(avatars.First());
        }

        //Gets the avatar a user is currently using, by the user's UUID
        public async Task<Avatar> GetAvatarForUser(Guid userUUID)
        {
            var userData = await QueryFactory.Query("user_data").Where("uuid", userUUID.ToString("N")).GetAsync();

            if (userData.Count() == 0)
            {
                return null;
            }

            var firstResult = userData.First();

            return await GetAvatar(firstResult.current_avatar);
        }

        //Posts an avatar to the database
        public async Task PostAvatar(Avatar avatar)
        {
            bool avatarExists = GetAvatar(avatar.id) != null;

            object avatarOutput = avatar.GetData();

            await QueryFactory.Query("avatar_data").When(
                avatarExists,
                q => q.AsUpdate(avatarOutput),
                q => q.AsInsert(avatarOutput)
            ).GetAsync();
        }
        #endregion

        #region User

        //Gets a user from the database
        //If no user exists, creates one.
        public async Task<User> GetOrCreateUser(Guid uuid)
        {
            var users = await QueryFactory.Query("user_data").Where("uuid", uuid.ToString("N")).GetAsync();

            //If no user exists, create one.
            if(users.Count() == 0)
            {
                User newUser = new User(uuid);
                await QueryFactory.Query("user_data").InsertAsync(newUser.GetData());

                return newUser;
            }

            //Return first user.
            return new User(users.First());
        }

        #region Updates
        public async Task UpdateUserAvatar(User user)
        {
            await QueryFactory.Query("user_data").Where("id", user.id.ToString("N")).UpdateAsync(new
            {
                current_avatar = user.currentAvatarID.ToString("N")
            });
        }
        public async Task UpdateUserKarma(User user)
        {
            await QueryFactory.Query("user_data").Where("id", user.id.ToString("N")).UpdateAsync(new
            {
                karma = user.karma
            });
        }
        public async Task UpdateUserTrustData(User user)
        {
            await QueryFactory.Query("user_data").Where("id", user.id.ToString("N")).UpdateAsync(new
            {
                trust_data = Extensions.GetBytesFromNBTCompound(user.trustData)
            });
        }
        public async Task UpdateUserFavoriteData(User user)
        {
            await QueryFactory.Query("user_data").Where("id", user.id.ToString("N")).UpdateAsync(new
            {
                favorite_data = Extensions.GetBytesFromNBTCompound(user.favoriteData)
            });
        }
        public async Task UpdateUserConfig(User user)
        {
            await QueryFactory.Query("user_data").Where("id", user.id.ToString("N")).UpdateAsync(new
            {
                config = Extensions.GetBytesFromNBTCompound(user.config)
            });
        }
        #endregion

        #endregion

        #region Pack


        public async Task<Pack> GetPack(Guid uuid)
        {
            var users = await QueryFactory.Query("pack_data").Where("uuid", uuid.ToString("N")).GetAsync();

            if (users.Count() == 0)
            {
                return null;
            }

            return new Pack(users.First());
        }

        #endregion
    }
}

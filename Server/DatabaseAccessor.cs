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
        public MySqlConnection Connection { get; }
        public MySqlCompiler Compiler { get; }
        public QueryFactory QueryFactory { get; }

        public DatabaseAccessor()
        {
            Connection = new MySqlConnection(Program.sqlConnectionString);
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
            try
            {
                var avatars = await QueryFactory.Query("avatar_data").Select("nbt").Where("uuid", uuid.ToString("N")).GetAsync();

                if (avatars.Count() == 0)
                    return null;

                return avatars.First().nbt;
            } catch (Exception e)
            {
                return null;
            }
        }

        public async Task<string> GetAvatarHash(Guid uuid)
        {
            try
            {
                var avatars = await QueryFactory.Query("avatar_data").Select("hash").Where("uuid", uuid.ToString("N")).GetAsync();

                if (avatars.Count() == 0)
                    return null;

                return avatars.First().hash;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        public async Task<int> GetAvatarSize(Guid uuid)
        {
            try
            {
                var avatars = await QueryFactory.Query("avatar_data").Select("size").Where("uuid", uuid.ToString("N")).GetAsync();

                if (avatars.Count() == 0)
                    return -1;

                return avatars.First().size;
            }
            catch (Exception e)
            {
                return -1;
            }
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

        //Gets the avatar a user is currently using, by the user's UUID
        public async Task<byte[]> GetAvatarDataForUser(Guid userUUID)
        {
            var userData = await QueryFactory.Query("user_data").Where("uuid", userUUID.ToString("N")).GetAsync();

            if (userData.Count() == 0)
            {
                return null;
            }

            var firstResult = userData.First();

            Guid id = new Guid(firstResult.current_avatar);

            return await GetAvatarData(id);
        }

        //Gets the avatar a user is currently using, by the user's UUID
        public async Task<string> GetAvatarHashForUser(Guid userUUID)
        {
            var userData = await QueryFactory.Query("user_data").Where("uuid", userUUID.ToString("N")).GetAsync();

            if (userData.Count() == 0)
            {
                return null;
            }

            var firstResult = userData.First();

            Guid id = new Guid(firstResult.current_avatar);

            return await GetAvatarHash(id);
        }

        //Posts an avatar to the database
        public async Task<int> PostAvatar(Avatar avatar)
        {
            return await QueryFactory.Query("avatar_data").InsertAsync(avatar.GetData());
        }

        public async Task<int> UpdateAvatar(Guid uuid, Dictionary<string, object> toUpdate)
        {
            return await QueryFactory.Query("avatar_data").Where("uuid", uuid.ToString("N")).UpdateAsync(toUpdate, timeout: 1000);
        }

        public async Task DeleteAvatar(Guid uuid)
        {
            Console.Out.WriteLine("Deleting avatar with UUID " + uuid);
            await QueryFactory.Query("avatar_data").Where("uuid", uuid.ToString("N")).DeleteAsync(timeout: 1000);
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

        public async Task<int> UpdateUser(Guid uuid, Dictionary<string, object> toUpdate)
        {
            return await QueryFactory.Query("user_data").Where("uuid", uuid.ToString("N")).UpdateAsync(toUpdate, timeout: 1000);
        }

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

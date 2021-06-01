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
                Logger.LogMessage("Getting avatar data with UUID " + uuid);
                var avatar = await QueryFactory.Query("avatar_data").Select("nbt").Where("uuid", uuid.ToString("N")).FirstOrDefaultAsync();

                if (avatar == null)
                {
                    Logger.LogMessage("No avatar with that UUID exists.");
                    return null;
                }

                Logger.LogMessage("Got avatar data.");
                return avatar.nbt;
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return null;
            }
        }

        public async Task<string> GetAvatarHash(Guid uuid)
        {
            try
            {
                Logger.LogMessage("Getting avatar Hash with UUID " + uuid);

                var avatar = await QueryFactory.Query("avatar_data").Select("hash").Where("uuid", uuid.ToString("N")).FirstOrDefaultAsync();

                if (avatar == null)
                {
                    Logger.LogMessage("No avatar with that UUID exists.");
                    return null;
                }

                Logger.LogMessage("Got avatar hash.");
                return avatar.hash;
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return null;
            }
        }


        public async Task<int> GetAvatarSize(Guid uuid)
        {
            try
            {
                Logger.LogMessage("Getting avatar size with UUID " + uuid);
                var avatar = await QueryFactory.Query("avatar_data").Select("size").Where("uuid", uuid.ToString("N")).FirstOrDefaultAsync();

                if (avatar == null)
                {
                    Logger.LogMessage("No avatar with that UUID exists.");
                    return -1;
                }

                Logger.LogMessage("Got avatar size.");
                return avatar.size;
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return -1;
            }
        }

        //Returns the full avatar from the UUID.
        public async Task<Avatar> GetAvatar(Guid uuid)
        {
            try
            {
                Logger.LogMessage("Getting FULL avatar with UUID " + uuid);

                var avatar = await QueryFactory.Query("avatar_data").Where("uuid", uuid.ToString("N")).FirstOrDefaultAsync();

                if (avatar == null)
                {
                    Logger.LogMessage("No avatar with that UUID exists.");
                    return null;
                }

                Logger.LogMessage("Got full avatar.");
                return new Avatar(avatar);
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return null;
            }
        }

        //Gets the avatar a user is currently using, by the user's UUID
        public async Task<Avatar> GetAvatarForUser(Guid userUUID)
        {
            try
            {
                Logger.LogMessage("Getting FULL avatar for user with UUID " + userUUID);

                var avatar = await QueryFactory
                    .Query("user_data")
                    .Where("user_data.uuid", userUUID.ToString("N"))
                    .Join("avatar_data", "avatar_data.uuid", "user_data.current_avatar")
                    .Select("avatar_data.*")
                    .FirstOrDefaultAsync();

                if (avatar == null)
                {
                    Logger.LogMessage("No user with that UUID exists.");
                    return null;
                }

                return new Avatar(avatar);
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return null;
            }
        }

        //Gets the avatar a user is currently using, by the user's UUID
        public async Task<byte[]> GetAvatarDataForUser(Guid userUUID)
        {
            try
            {
                Logger.LogMessage("Getting avatar data for user with UUID " + userUUID);
                var avatar = await QueryFactory
                    .Query("user_data")
                    .Where("user_data.uuid", userUUID.ToString("N"))
                    .Join("avatar_data", "avatar_data.uuid", "user_data.current_avatar")
                    .Select("avatar_data.nbt")
                    .FirstOrDefaultAsync();

                if (avatar == null)
                {
                    Logger.LogMessage("No user with that UUID exists.");
                    return null;
                }

                return avatar.nbt;
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return null;
            }
        }

        //Gets the avatar a user is currently using, by the user's UUID
        public async Task<string> GetAvatarHashForUser(Guid userUUID)
        {
            try
            {
                Logger.LogMessage("Getting avatar hash for user with UUID " + userUUID);

                var avatar = await QueryFactory
                    .Query("user_data")
                    .Where("user_data.uuid", userUUID.ToString("N"))
                    .Join("avatar_data", "avatar_data.uuid", "user_data.current_avatar")
                    .Select("avatar_data.hash")
                    .FirstOrDefaultAsync();

                if (avatar == null)
                {
                    Logger.LogMessage("No user with that UUID exists.");
                    return null;
                }

                return avatar.hash;
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return null;
            }
        }

        //Posts an avatar to the database
        public async Task<int> PostAvatar(Avatar avatar)
        {
            try
            {
                Logger.LogMessage("Posting avatar with UUID " + avatar.id);

                return await QueryFactory.Query("avatar_data").InsertAsync(avatar.GetData());
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return -1;
            }
        }

        public async Task<int> UpdateAvatar(Guid uuid, Dictionary<string, object> toUpdate)
        {
            try
            {
                Logger.LogMessage("Updating avatar with UUID " + uuid);

                return await QueryFactory.Query("avatar_data").Where("uuid", uuid.ToString("N")).UpdateAsync(toUpdate, timeout: 1000);
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return -1;
            }
        }

        public async Task DeleteAvatar(Guid uuid)
        {
            try
            {
                Logger.LogMessage("Deleting avatar with UUID " + uuid);
                await QueryFactory.Query("avatar_data").Where("uuid", uuid.ToString("N")).DeleteAsync(timeout: 1000);
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
            }
        }
        #endregion

        #region User

        //Gets a user from the database
        //If no user exists, creates one.
        public async Task<User> GetOrCreateUser(Guid uuid)
        {
            try
            {
                Logger.LogMessage("Getting or creating user with UUID " + uuid);

                var user = await QueryFactory.Query("user_data").Where("uuid", uuid.ToString("N")).FirstOrDefaultAsync();

                //If no user exists, create one.
                if (user == null)
                {
                    Logger.LogMessage("No user found, creating... ");

                    User newUser = new User(uuid);
                    await QueryFactory.Query("user_data").InsertAsync(newUser.GetData());

                    return newUser;
                }

                Logger.LogMessage("Created user.");

                //Return first user.
                return new User(user);
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return null;
            }
        }

        public async Task<int> UpdateUser(Guid uuid, Dictionary<string, object> toUpdate)
        {
            try
            {
                Logger.LogMessage("Updating user with UUID " + uuid);

                return await QueryFactory.Query("user_data").Where("uuid", uuid.ToString("N")).UpdateAsync(toUpdate, timeout: 1000);
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return -1;
            }
        }

        #endregion

        #region Pack


        public async Task<Pack> GetPack(Guid uuid)
        {
            try
            {
                var user = await QueryFactory.Query("pack_data").Where("uuid", uuid.ToString("N")).FirstOrDefaultAsync();

                if (user == null)
                {
                    return null;
                }

                return new Pack(user);
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
                return null;
            }
        }

        #endregion
    }
}

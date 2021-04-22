using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
namespace FiguraServer.Controllers
{
    [ApiController]
    [Route("avatar")]
    public class AvatarController : Controller
    {
        public AppDB Db { get; }

        public AvatarController(AppDB db)
        {
            Db = db;
        }


        //Gets an avatar from the database.
        [HttpGet("get")]
        public async Task<Object> Get(string id)
        {
            await Db.Connection.OpenAsync();

            string sqlRequest = "SELECT * FROM figura_data.avatar_data WHERE uuid=@uuid";
            MySqlCommand cmd = new MySqlCommand(sqlRequest, Db.Connection);
            cmd.Parameters.AddWithValue("@uuid", id);
            MySqlDataReader rdr = await cmd.ExecuteReaderAsync();

            List<string> retObject = new List<string>();

            while (rdr.Read())
            {
                string creatorID = (string)rdr[1];
                byte[] nbt = (byte[])rdr[2];
                byte[] metaData = (byte[])rdr[3];
            }

            await rdr.CloseAsync();

            return Ok(retObject);
        }

        //Uploads an avatar to the database.
        [HttpPut("put")]
        public async Task<Object> Put(string JWT)
        {
            return Ok();
        }
    }
}

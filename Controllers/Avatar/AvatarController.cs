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

        [HttpGet("get/{id}")]
        public async Task<Object> Get(string id)
        {
            await Db.Connection.OpenAsync();

            string sqlRequest = "SELECT Name, HeadOfState FROM Country WHERE Continent='Oceania'";
            MySqlCommand cmd = new MySqlCommand(sqlRequest, Db.Connection);
            MySqlDataReader rdr = await cmd.ExecuteReaderAsync();

            List<string> retObject = new List<string>();

            while (rdr.Read())
            {
                retObject.Add(rdr[0] + " --- " + rdr[1]);
            }

            await rdr.CloseAsync();

            return Ok(retObject);
        }
    }
}

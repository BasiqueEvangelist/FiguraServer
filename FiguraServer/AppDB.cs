using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;

namespace FiguraServer
{
    public class AppDB : IDisposable
    {

        public MySqlConnection Connection { get; }

        public AppDB(string connectionString)
        {
            Connection = new MySqlConnection(connectionString);
        }

        public void Dispose() => Connection.Dispose();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FiguraServer.FiguraServer.Auth
{
    /// <summary>
    /// This is a fake minecraft server, used to authenticate users and prove they own the minecraft account they say they own.
    /// </summary>
    public static class FiguraAuthServer
    {
        private static TcpListener serverListener;
        private static Task serverTask;

        public static bool isRunning = false;

        /// <summary>
        /// Starts the fake minecraft server
        /// </summary>
        /// <param name="port">The port to listen for traffic on. Defaults to 25565, the default minecraft port.</param>
        public static async Task Start(int port = 25565)
        {
            isRunning = false;
            await serverTask;

            serverTask = new Task(() => RunServer(port));
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task Stop()
        {
            isRunning = false;
            await serverTask;
        }


        private static async Task RunServer(int port)
        {
            serverListener = new TcpListener(IPAddress.Any, port);

            while (isRunning)
            {
                if (!serverListener.Pending())
                {
                    await Task.Delay(10);
                }
                else
                {
                    await GetNextConnection();
                }
            }
        }

        private static async Task GetNextConnection()
        {
            TcpClient client = await serverListener.AcceptTcpClientAsync();
        }

    }
}

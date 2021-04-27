using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FiguraServer.Server.Auth
{
    /// <summary>
    /// This is a fake minecraft server, used to authenticate users and prove they own the minecraft account they say they own.
    /// </summary>
    public static class FiguraAuthServer
    {
        //Static HTTP client used for mojang auth shenanigans.
        public static HttpClient httpClient = new HttpClient();

        private static TcpListener serverListener;
        private static Task serverTask;

        public static bool isRunning = false;


        /// <summary>
        /// Starts the fake minecraft server
        /// </summary>
        /// <param name="port">The port to listen for traffic on. Defaults to 25565, the default minecraft port.</param>
        public static Task Start(int port = 25565)
        {
            isRunning = false;
            if (serverTask != null)
                serverTask.Wait();

            isRunning = true;
            serverTask = new Task(async () => await RunServer(port));

            return serverTask;
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
            serverListener.Start();
            //Console.WriteLine("Started 'Minecraft' server on port " + port );

            while (isRunning)
            {
                if (!serverListener.Pending())
                {
                    await Task.Delay(5);
                }
                else
                {
                    await GetNextConnection();
                }
            }

            //Console.WriteLine("Stopping 'Minecraft' Server");
            serverListener.Stop();
        }

        private static async Task GetNextConnection()
        {
            //Console.WriteLine("Connection started");
            TcpClient client = await serverListener.AcceptTcpClientAsync();

            MinecraftClientConnection mcc = new MinecraftClientConnection(client);

            await mcc.Start();
        }



        #region Mojang Auth

        //Determines if a player has joined a server that they claim they have.
        public static async Task<JoinedResponse> HasJoined(string username, string serverId)
        {
            string result = $"https://sessionserver.mojang.com/session/minecraft/hasJoined?username={username}&serverId={serverId}";
            using (HttpResponseMessage response = await httpClient.GetAsync(result))
            {
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<JoinedResponse>(await response.Content.ReadAsStringAsync());
                }
            }

            return null;
        }

        public class JoinedResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string PlayerName { get; set; }

            [JsonProperty("properties")]
            public List<JoinedProperty> Properties { get; set; }
        }

        public class JoinedProperty
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("signature")]
            public string Signature { get; set; }
        }
        #endregion
    }
}

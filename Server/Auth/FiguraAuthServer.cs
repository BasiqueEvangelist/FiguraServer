﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
using System.Threading;
using System.Threading.Tasks;

namespace FiguraServer.Server.Auth
{
    /// <summary>
    /// This is a fake minecraft server, used to authenticate users and prove they own the minecraft account they say they own.
    /// </summary>
    public class FiguraAuthServer : BackgroundService
    {
        //Static HTTP client used for mojang auth shenanigans.
        public static HttpClient httpClient = new HttpClient();

        private TcpListener serverListener;

        private readonly FiguraAuthServer.Config config;

        public static bool isRunning = false;

        public FiguraAuthServer(IOptions<FiguraAuthServer.Config> config)
        {
            this.config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int port = 25565;

            serverListener = new TcpListener(IPAddress.Any, port);
            serverListener.Start();
            Logger.LogMessage("Started 'Minecraft' server on port " + port);

            try
            {
                using (stoppingToken.Register(serverListener.Stop))
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await GetNextConnection();
                    }
            }
            finally
            {
                serverListener.Stop();
            }
        }

        private async Task GetNextConnection()
        {
            TcpClient client = await serverListener.AcceptTcpClientAsync();

            Logger.LogMessage("Connection started");
            MinecraftClientConnection mcc = new MinecraftClientConnection(client, config);

            await mcc.Start();
        }



        #region Mojang Auth

        //Determines if a player has joined a server that they claim they have.
        public static async Task<JoinedResponse> HasJoined(string username, string serverId)
        {
            Logger.LogMessage($"Checking if user {username} has joined the server with the server ID ${serverId}");
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

        public class Config
        {
            public bool OfflineMode { get; set; }
        }
    }
}

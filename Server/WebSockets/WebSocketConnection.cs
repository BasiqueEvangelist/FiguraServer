using FiguraServer.Data;
using FiguraServer.Server.Auth;
using FiguraServer.Server.WebSockets.Messages;
using FiguraServer.Server.WebSockets.Messages.Avatars;
using FiguraServer.Server.WebSockets.Messages.Users;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace FiguraServer.Server.WebSockets
{

    public class WebSocketConnection
    {
        private readonly static List<MessageHandler> messageHandlers = new List<MessageHandler>()
        {
            new AvatarRequestHandler(),
            new AvatarUploadRequestHandler(),
            new UserSetAvatarRequestHandler(),
            new UserDeleteCurrentAvatarRequestHandler(),
            new UserGetCurrentAvatarRequestHandler(),
            new UserGetCurrentAvatarHashRequestHandler(),
            new AuthenticateRequestHandler(),
        };

        private static ConcurrentQueue<byte[]> messageHeaderPool = new ConcurrentQueue<byte[]>();
        private static ConcurrentQueue<Task<byte[]>> messageTaskList = new ConcurrentQueue<Task<byte[]>>();

        public WebSocket socket;
        public Guid playerID;
        public MessageRegistry Registry = new();


        //The User object for this connection. We keep this around because it might be modified, a lot.
        public User connectionUser = null;

        public WebSocketConnection(WebSocket socket)
        {
            lastSendTask = new Task(() => { });
            lastSendTask.Start();
            this.socket = socket;
        }

        public async Task Run()
        {
            bool success = await SetupConnection();

            if (socket.State != WebSocketState.Open || success == false)
                return;
            await MessageLoop();

            Logger.LogMessage("Closed connection!");
        }

        public async Task SetUser(Guid id)
        {
            playerID = id;
            using DatabaseAccessor accessor = new();
            connectionUser = await accessor.GetOrCreateUser(playerID);
        }

        public async Task<byte[]> GetNextMessage()
        {
            Logger.LogMessage("Getting Message.");

            byte[] buffer = new byte[1024];

            try
            {
                var msg = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), GetTimeoutCancelToken(1000 * 60 * 15)); //Keep connection open for 15 seconds

                using (MemoryStream ms = new MemoryStream())
                {

                    //While not end of message and while close status is empty (not closed)
                    while (msg.CloseStatus == null)
                    {
                        await ms.WriteAsync(buffer, 0, msg.Count);

                        //Stop getting if this is the end of the message.
                        if (msg.EndOfMessage)
                        {
                            break;
                        }

                        msg = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), GetTimeoutCancelToken(1000));

                        //If size of message is too large
                        if (ms.Length >= 1024 * 110)
                        {
                            Logger.LogMessage("Message is too large.");

                            int tries = 0;
                            //Read rest of message, do nothing with the data tho
                            while (tries < 1000 && msg.EndOfMessage == false && msg.CloseStatus == null)
                            {
                                msg = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), GetTimeoutCancelToken(1000));

                                tries++;
                            }

                            return new byte[0];
                        }
                    }

                    //If connection closed, return 0-length byte array.
                    if (msg.CloseStatus != null)
                    {
                        Logger.LogMessage("Connection was closed from client");
                        return new byte[0];
                    }

                    Logger.LogMessage("End Of Message. Length:" + ms.Length);
                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
            }

            return new byte[0];
        }

        //Sets up the connection, verifies JWT, all of that jazz.
        public async Task<bool> SetupConnection()
        {
            Logger.LogMessage("Setup connection!");

            try
            {
                //Get the client registry message.
                byte[] registryMessage = await GetNextMessage();
                using var ms = new MemoryStream(registryMessage);
                using var br = new BinaryReader(ms, Encoding.UTF8);
                Registry.ReadRegistryMessage(br);

                await SendServerRegistry();
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);

                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Error During Setup", CancellationToken.None);
                return false;
            }

            return true;
        }

        public Task SendServerRegistry()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8);

            bw.Write(messageHandlers.Count);
            foreach (MessageHandler handler in messageHandlers)
            {
                byte[] data = Encoding.UTF8.GetBytes(handler.ProtocolName);
                bw.Write(data.Length);
                bw.Write(data);
            }

            return socket.SendAsync(new ArraySegment<byte>(ms.ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        //Receives and processes messages.
        public async Task MessageLoop()
        {
            byte[] messageData = await GetNextMessage();

            while (messageData.Length != 0 && socket.CloseStatus != WebSocketCloseStatus.Empty)
            {
                //Create memory stream and binary reader from message array
                using (MemoryStream ms = new MemoryStream(messageData))
                {
                    using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
                    {
                        int handlerID = br.ReadSByte() - sbyte.MinValue - 1;

                        //Get the handler by ID
                        if (messageHandlers.Count > handlerID)
                        {
                            MessageHandler handler = messageHandlers[handlerID];
                            Logger.LogMessage("Handling message with Handler ID:" + handlerID + " and protocol name " + handler.ProtocolName);

                            try
                            {
                                await handler.HandleMessage(this, br);
                                Logger.LogMessage("Handled.");
                            }
                            catch (Exception e)
                            {
                                Logger.LogMessage(e.ToString());
                            }
                        }
                        else
                        {
                            Logger.LogMessage("No message with Handler ID:" + handlerID);
                        }
                    }
                }

                messageData = await GetNextMessage();
            }
        }

        public string GetStringFromMessage(byte[] buffer)
        {
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        private Task lastSendTask;

        public void SendMessage(MessageSender sender)
        {
            lastSendTask = lastSendTask.ContinueWith(async (t) => { await SendMessageReal(sender); });
        }

        private async Task SendMessageReal(MessageSender sender)
        {
            try
            {
                await sender.SendData(this);
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
            }
        }

        private CancellationToken GetTimeoutCancelToken(int ms)
        {
            return new CancellationTokenSource(TimeSpan.FromMilliseconds(ms)).Token;
        }

    }
}

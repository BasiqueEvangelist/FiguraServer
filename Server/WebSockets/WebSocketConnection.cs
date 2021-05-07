using FiguraServer.Data;
using FiguraServer.Server.Auth;
using FiguraServer.Server.WebSockets.Messages;
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
        private static Dictionary<int, MessageHandlerProtocol> allMessageHandlers = new Dictionary<int, MessageHandlerProtocol>()
        {
            { 0, new MessageHandlerProtocolV1() },
        };

        private static ConcurrentQueue<byte[]> messageHeaderPool = new ConcurrentQueue<byte[]>();
        private static ConcurrentQueue<Task<byte[]>> messageTaskList = new ConcurrentQueue<Task<byte[]>>();

        public WebSocket socket;
        public Guid playerID;


        private MessageHandlerProtocol handlerProtocol = null;

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

            //Retreive player for this.
            using (DatabaseAccessor accessor = new DatabaseAccessor())
                connectionUser = await accessor.GetOrCreateUser(playerID);

            if (socket.State != WebSocketState.Open || success == false)
                return;
            await MessageLoop();

            Logger.LogMessage("Closed connection!");
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

            //First, grab the JWT for the client.
            byte[] jwtMessage = await GetNextMessage();

            if (jwtMessage.Length == 0)
            {
                Logger.LogMessage("No JWT received");
                return false;
            }

            try
            {
                //Get token from string.
                string token = GetStringFromMessage(jwtMessage);

                Logger.LogMessage("Token is " + token);

                //Verify token.
                if (AuthenticationManager.IsTokenValid(token, out var claims))
                {
                    //Token verified, pull user UUID from the JWT.
                    playerID = Guid.Parse(claims.First().Value);
                    Logger.LogMessage("Connection verified for player " + playerID);

                    //Get the protocol message.
                    byte[] protocolVersion = await GetNextMessage();

                    //Parse a JSON Object from the protocol message.
                    JObject protocolObject = JObject.Parse(GetStringFromMessage(protocolVersion));

                    try
                    {
                        //Set protocol and get the message handler for it.
                        int protocolValue = (int)protocolObject["protocol"];
                        handlerProtocol = allMessageHandlers[protocolValue];

                        Logger.LogMessage("Protocol is version " + protocolValue);
                    }
                    catch
                    {
                        Logger.LogMessage("Invalid Protocol Version.");
                        await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid Protocol Version", CancellationToken.None);
                        return false;
                    }
                }
                else
                {
                    Logger.LogMessage("Invalid Token.");

                    await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid Authentication", CancellationToken.None);
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.LogMessage(e);

                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Error During Setup", CancellationToken.None);
                return false;
            }

            return true;
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
                        sbyte handlerID = br.ReadSByte();

                        //Get the handler by ID
                        if (handlerProtocol.registeredMessages.TryGetValue(handlerID, out var handler))
                        {
                            Logger.LogMessage("Handling message with Handler ID:" + handlerID + " and name " + handler.GetType().Name);

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

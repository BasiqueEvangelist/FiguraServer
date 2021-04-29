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

            Console.Out.WriteLine("Closed connection!");
        }

        public async Task<byte[]> GetNextMessage()
        {
            Console.Out.WriteLine("Getting Message.");

            byte[] buffer = new byte[1024];

            try
            {

                var msg = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), GetTimeoutCancelToken(1000 * 15)); //Keep connection open for 15 seconds

                using (MemoryStream ms = new MemoryStream())
                {

                    //While not end of message and while close status is empty (not closed)
                    while (msg.CloseStatus != WebSocketCloseStatus.Empty)
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
                            Console.Out.WriteLine("Message is too large.");

                            //Read rest of message, do nothing with the data tho
                            while (msg.EndOfMessage == false && msg.CloseStatus != WebSocketCloseStatus.Empty)
                                msg = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), GetTimeoutCancelToken(1000));

                            return new byte[0];
                        }
                    }

                    //If connection closed, return 0-length byte array.
                    if (msg.CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        Console.Out.WriteLine("Connection was closed from client");
                        return new byte[0];
                    }

                    Console.Out.WriteLine("End Of Message. Length:" + ms.Length);
                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {

            }

            return new byte[0];
        }

        //Sets up the connection, verifies JWT, all of that jazz.
        public async Task<bool> SetupConnection()
        {
            Console.Out.WriteLine("Setup connection!");

            //First, grab the JWT for the client.
            byte[] jwtMessage = await GetNextMessage();

            if (jwtMessage.Length == 0)
            {
                Console.Out.WriteLine("No JWT received");
                return false;
            }

            try
            {
                //Get token from string.
                string token = GetStringFromMessage(jwtMessage);

                Console.Out.WriteLine("Token is " + token);

                //Verify token.
                if (AuthenticationManager.IsTokenValid(token, out var claims))
                {
                    //Token verified, pull user UUID from the JWT.
                    playerID = Guid.Parse(claims.First().Value);
                    Console.Out.WriteLine("Connection verified for player " + playerID);

                    //Get the protocol message.
                    byte[] protocolVersion = await GetNextMessage();

                    //Parse a JSON Object from the protocol message.
                    JObject protocolObject = JObject.Parse(GetStringFromMessage(protocolVersion));

                    try
                    {
                        //Set protocol and get the message handler for it.
                        int protocolValue = (int)protocolObject["protocol"];
                        handlerProtocol = allMessageHandlers[protocolValue];

                        Console.Out.WriteLine("Protocol is version " + protocolValue);
                    }
                    catch
                    {
                        Console.Out.WriteLine("Invalid Protocol Version.");
                        await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid Protocol Version", CancellationToken.None);
                        return false;
                    }
                }
                else
                {
                    Console.Out.WriteLine("Invalid Token.");

                    await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid Authentication", CancellationToken.None);
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Error During Setup", CancellationToken.None);
                return false;
            }

            return true;
        }

        //Receives and processes messages.
        public async Task MessageLoop()
        {
            byte[] messageData = await GetNextMessage();
            MessageHandler lastHandler = null;

            while (messageData.Length != 0 && socket.State == WebSocketState.Open)
            {
                //Create memory stream and binary reader from message array
                using (MemoryStream ms = new MemoryStream(messageData))
                {
                    using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
                    {
                        //If there is no handler expecting a body
                        if (lastHandler == null)
                        {
                            sbyte handlerID = br.ReadSByte();

                            //Get the handler by ID
                            if (handlerProtocol.registeredMessages.TryGetValue(handlerID, out var handler))
                            {
                                Console.Out.WriteLine("Handling header with Handler ID:" + handlerID);

                                try
                                {
                                    await handler.HandleHeader(this, br);
                                    Console.Out.WriteLine("Handled.");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }

                                if (handler.ExpectBody())
                                {
                                    lastHandler = handler;
                                }
                            }
                            else
                            {
                                Console.Out.WriteLine("No header with Handler ID:" + handlerID);
                            }
                        }
                        else //If there IS a handler expecting a body.
                        {
                            Console.Out.WriteLine("Handling Body.");

                            try
                            {
                                await lastHandler.HandleBody(this, br);
                                Console.Out.WriteLine("Handled.");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            lastHandler = null;
                        }
                    }

                    messageData = await GetNextMessage();
                }
            }
        }

        //Checks for a disconnect.
        //Returns true if disconnected.
        public bool CheckForDisconnect(WebSocketReceiveResult result)
        {
            return result.CloseStatus != WebSocketCloseStatus.Empty;
        }

        public string GetStringFromMessage(byte[] buffer)
        {
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        private Task lastSendTask = new Task(() => { });

        public void SendMessage(MessageSender sender)
        {
            lastSendTask = lastSendTask.ContinueWith(async (t) => { await sender.SendData(this); });
        }

        private CancellationToken GetTimeoutCancelToken(int ms)
        {
            return new CancellationTokenSource(TimeSpan.FromMilliseconds(ms)).Token;
        }

    }
}

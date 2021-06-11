using FiguraServer.Data;
using FiguraServer.Server.Auth;
using FiguraServer.Server.WebSockets.Messages;
using FiguraServer.Server.WebSockets.Messages.Users;
using FiguraServer.Server.WebSockets.Messages.PubSub;
using FiguraServer.Server.WebSockets.Messages.Avatars;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using FiguraServer.Server.WebSockets.Messages.Pings;
using FiguraServer.Server.WebSockets.Messages.Utility;

using Timer = System.Timers.Timer;

namespace FiguraServer.Server.WebSockets
{

    public class WebSocketConnection : IDisposable
    {
        private readonly static List<MessageHandler> messageHandlers = new List<MessageHandler>()
        {
            new AvatarRequestHandler(),
            new AvatarUploadRequestHandler(),
            new UserSetAvatarRequestHandler(),
            new UserDeleteCurrentAvatarRequestHandler(),
            new UserGetCurrentAvatarRequestHandler(),
            new UserGetCurrentAvatarHashRequestHandler(),
            new SubscribeToUsersRequestHandler(),
            new UnsubscribeFromUsersRequestHandler(),
            new ChannelAvatarUpdateRequestHandler(),
            new PingMessageHandler(),
        };

        private static ConcurrentQueue<byte[]> messageHeaderPool = new ConcurrentQueue<byte[]>();
        private static ConcurrentQueue<Task<byte[]>> messageTaskList = new ConcurrentQueue<Task<byte[]>>();

        private static ConcurrentDictionary<Guid, WebSocketConnection> openedConnections = new ConcurrentDictionary<Guid, WebSocketConnection>();
        private static ConcurrentDictionary<Guid, Tuple<Timer, RateLimiterGroup>> rateLimitGroups = new ConcurrentDictionary<Guid, Tuple<Timer, RateLimiterGroup>>();

        public WebSocket socket;
        public Guid playerID = Guid.Empty;
        public MessageRegistry Registry = new();
        private List<Guid> subscribedIDs = new List<Guid>();

        public bool isAuthenticated = false;

        public RateLimiterGroup rateGroup = new RateLimiterGroup();

        //Byte limiter, limits the maximum byte throughput of the entire connection.
        //200kb capacity
        //20kb/s recovery speed.
        public RateLimiter byteRateLimiter => rateGroup.byteRateLimiter;

        //Message rate limiter, limits the maximum count of messages through the entire connection.
        //2048 capacity
        //256/s recovery speed
        public RateLimiter messageRateLimiter => rateGroup.messageRateLimiter;

        //Avatar upload rate limiter
        //4 capacity
        //1/s recovery speed
        public RateLimiter avatarUploadRateLimiter => rateGroup.avatarUploadRateLimiter;

        //Avatar request rate limiter
        //2048 capacity
        //1/s recovery speed
        public RateLimiter avatarRequestRateLimiter => rateGroup.avatarRequestRateLimiter;

        //Ping byte rate limiter
        //2kb capacity
        //1kb/s recovery speed
        public RateLimiter pingByteRateLimiter => rateGroup.pingByteRateLimiter;

        //Ping rate limiter, limits the maximum amount of ping messages that can be sent through the connection.
        //21 capacity
        //21/s recovery speed
        public RateLimiter pingRateLimiter => rateGroup.pingRateLimiter;

        //The User object for this connection. We keep this around because it might be modified, a lot.
        public User connectionUser = null;

        public WebSocketConnection(WebSocket socket)
        {
            this.socket = socket;
        }

        public void Dispose()
        {
            openedConnections.TryRemove(playerID, out _);
            PubSubManager.Unsubscribe(playerID, playerID);

            //Unsubscribe!
            foreach(Guid id in subscribedIDs)
                PubSubManager.Unsubscribe(id, playerID);

            //Start rate limit cleanup timer.
            if(rateLimitGroups.TryGetValue(playerID, out var v))
            {
                v.Item1.Start();
            }
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

                    //Check byte rate limit.
                    if (byteRateLimiter.TryTakePoints(ms.Length))
                    {
                        return ms.ToArray();
                    } 
                    else
                    {
                        Logger.LogMessage("Byte Ratelimit hit.");
                        SendMessage(new ErrorMessageSender(ErrorMessageSender.BYTE_RATE_LIMIT));
                        return new byte[0];
                    }
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

                    //Get the client registry message.
                    byte[] registryMessage = await GetNextMessage();
                    using var ms = new MemoryStream(registryMessage);
                    using var br = new BinaryReader(ms, Encoding.UTF8);
                    Registry.ReadRegistryMessage(br);

                    await SendServerRegistry();

                    //Get a group, or create one if needed.
                    var timerGroupPair = rateLimitGroups.GetOrAdd(playerID, (guid) =>
                    {
                        RateLimiterGroup ng = new RateLimiterGroup();
                        Timer t = new Timer(60000);

                        t.Elapsed += (_, _) => {
                            rateLimitGroups.TryRemove(guid, out _);
                            t.Stop();
                        };

                        return new Tuple<Timer, RateLimiterGroup>(t, ng);
                    });

                    //Stop timer that we've grabbed
                    timerGroupPair.Item1.Stop();

                    //Set our rate group to the given group
                    rateGroup = timerGroupPair.Item2;

                    openedConnections.AddOrUpdate(playerID, this, (k, v) => this);
                    PubSubManager.Subscribe(playerID, playerID, this);
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

                //Check message rate limit.
                if (!messageRateLimiter.TryTakePoints(1))
                {
                    SendMessage(new ErrorMessageSender(ErrorMessageSender.MESSAGE_RATE_LIMIT));
                    continue;
                }
            }
        }

        public string GetStringFromMessage(byte[] buffer)
        {
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        private Task lastSendTask = Task.CompletedTask;
        private object taskLock = new object();

        public void SendMessage(MessageSender sender)
        {
            lock(taskLock)
                lastSendTask = lastSendTask.ContinueWith(async (t) => { await SendMessageReal(sender); });
        }

        private async Task SendMessageReal(MessageSender sender)
        {
            if (socket.CloseStatus != WebSocketCloseStatus.Empty && socket.CloseStatus != null)
                return;

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

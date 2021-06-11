using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FiguraServer.Server.WebSockets.Messages;

namespace FiguraServer.Server.WebSockets
{
    public class PubSubManager
    {
        public static ConcurrentDictionary<Guid, PubSubChannel> currentChannels = new ConcurrentDictionary<Guid, PubSubChannel>();

        private static PubSubChannel MakeChannel(Guid id)
        {
            var ret = new PubSubChannel();
            ret.ownerID = id;
            ret.onEmptied = () => OnEmptied(id);
            return ret;
        }

        private static bool TryGetChannel(Guid target, out PubSubChannel psc) => currentChannels.TryGetValue(target, out psc);

        private static PubSubChannel GetChannel(Guid target) => currentChannels.GetOrAdd(target, MakeChannel);

        public static void Subscribe(Guid target, Guid myID, WebSocketConnection connection) => GetChannel(target).dict.TryAdd(myID, connection);

        public static void Unsubscribe(Guid target, Guid myID) => GetChannel(target).RemoveSubscription(myID);

        public static void SendMessage(Guid target, MessageSender message, Action onFinished = null) => GetChannel(target).SendMessage(message, onFinished);

        private static void OnEmptied(Guid id) => currentChannels.TryRemove(id, out var psc);

        public class PubSubChannel
        {
            public Guid ownerID;
            public bool isValid = true;
            public Dictionary<Guid, WebSocketConnection> dict = new Dictionary<Guid, WebSocketConnection>();

            private object lockObject = new object();
            private Task workTask = Task.CompletedTask;

            public Action onEmptied;

            //Adds a subscription
            public void AddSubscription(Guid id, WebSocketConnection connection)
            {
                AddTask(() =>
                {
                    if (!isValid)
                        return;

                    dict[id] = connection;
                });
            }

            //Removes a subscription
            public void RemoveSubscription(Guid id)
            {
                AddTask(() =>
                {
                    if (!isValid)
                        return;

                    dict.Remove(id, out _);

                    if (dict.Count == 0)
                    {
                        isValid = false;
                        onEmptied?.Invoke();
                    }
                });
            }

            //Sends a message to all subscribed members
            public void SendMessage(MessageSender sender, Action onFinished = null)
            {
                AddTask(() =>
                {
                    if (!isValid)
                        return;

                    //Foreach subscription
                    foreach(var kvp in dict)
                    {
                        //If subscription is the owner of this channel, don't send a message.
                        if (kvp.Key == ownerID)
                            continue;

                        //If the subscription has a channel.
                        if(TryGetChannel(kvp.Key, out var psc))
                        {
                            //Add task to subscription.
                            psc.AddTask(()=> {
                                //If subscription is also subscribed to this channel
                                if(psc.dict.ContainsKey(ownerID))
                                    kvp.Value.SendMessage(sender);
                            });
                        }
                    }

                    onFinished?.Invoke();
                });
            }

            public void Close()
            {
                AddTask(()=> {
                    isValid = false;
                });
            }

            //Adds a task to be done by this channel when available.
            private void AddTask(Action a)
            {
                lock (lockObject)
                {
                    if (!isValid)
                        return;

                    workTask = workTask.ContinueWith((t) => a());
                }
            }
        }
    }
}

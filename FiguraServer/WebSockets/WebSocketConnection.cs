using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;


namespace FiguraServer.FiguraServer.WebSockets
{

    public class WebSocketConnection
    {

        private WebSocket socket;

        public WebSocketConnection(WebSocket socket)
        {
            this.socket = socket;
        }

        public async Task Run()
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!receiveResult.CloseStatus.HasValue)
            {
                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    Console.Out.WriteLine("TEST!! GOT TEXT!");
                    Console.Out.WriteLine("------------");
                    Console.Out.WriteLine(System.Text.UTF8Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
                    Console.Out.WriteLine("------------");
                }
                else if (receiveResult.MessageType == WebSocketMessageType.Binary)
                {
                    Console.Out.WriteLine("TEST!! GOT BINARY!");
                }

                receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            Console.Out.WriteLine("Closed connection!");
        }

    }

}

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FiguraServer.Server.Auth;

namespace FiguraServer.Server.WebSockets.Messages
{
    public class AuthenticateRequestHandler : MessageHandler
    {
        public override string ProtocolName => "figura_v1:authenticate";

        public override async Task<string> HandleMessage(WebSocketConnection connection, BinaryReader reader)
        {
            string token = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
            Logger.LogMessage("Token is " + token);

            //Verify token.
            if (AuthenticationManager.IsTokenValid(token, out var claims))
            {
                //Token verified, pull user UUID from the JWT.
                Guid playerId = Guid.Parse(claims.First().Value);
                await connection.SetUser(playerId);
                Logger.LogMessage("Connection verified for player " + connection.playerID);
                connection.SendMessage(new AuthenticateResponse(playerId));
            }
            else
            {
                Logger.LogMessage("Invalid Token.");

                connection.SendMessage(new AuthenticateResponse(Guid.Empty));
            }

            return "";
        }
    }
}

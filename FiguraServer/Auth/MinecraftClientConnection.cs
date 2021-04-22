using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.FiguraServer.Auth
{
    /// <summary>
    /// This class manages the individual connection between the fake server and a minecraft client.
    /// It comes with all the functions required to complete the vanilla minecraft authentication and encryption.
    /// 
    /// Once auth and encryption is verified, we will tell the auth system to generate a JWT for this client, and kick the user with the JWT as the message.
    /// </summary>
    public class MinecraftClientConnection
    {

    }
}

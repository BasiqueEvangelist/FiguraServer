using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace FiguraServer.Server.Auth
{
    public static class AuthenticationManager
    {
        private static string TOKEN_ISSUER = "FIGURA AUTH SERVER";

        //The keypair of the server.
        //THIS SHOULD NEVER LEAVE THIS CLASS.
        private static RsaSecurityKey securityKey;

        private static JwtHeader jwtHeader;
        private static TokenValidationParameters tokenValidationParameters;

        //Static constructor
        static AuthenticationManager()
        {
            //Create RSA keypair.
            RSA rsa = RSA.Create(2048);
            securityKey = new RsaSecurityKey(rsa);

            RSACryptoServiceProvider prov = new RSACryptoServiceProvider();

            jwtHeader = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory()
                {
                    CacheSignatureProviders = false
                }
            });

            tokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidateAudience = false,
                ValidIssuer = TOKEN_ISSUER,
                IssuerSigningKey = securityKey,
                CryptoProviderFactory = new CryptoProviderFactory()
                {
                    CacheSignatureProviders = false
                }
            };
        }

        public static bool IsTokenValid(string token, out IEnumerable<Claim> claims)
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            try
            {
                handler.ValidateToken(token, tokenValidationParameters, out var validToken);
                claims = ((JwtSecurityToken)validToken).Claims;
                return true;
            }
            catch (Exception e)
            {
                claims = null;
                return false;
            }
        }

        public static string GenerateToken(string playerName)
        {
            string playerResponse = Get(string.Format("https://api.mojang.com/users/profiles/minecraft/{0}", playerName));
            JObject obj = JObject.Parse(playerResponse);

            if (obj.ContainsKey("error"))
            {
                return "";
            }

            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)t.TotalSeconds;

            //All the claims that should be shoved into the JWT
            Claim[] claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, (string)obj["id"]), //Subject - Username of the player who this JWT is issued to.
                new Claim(JwtRegisteredClaimNames.Iss, TOKEN_ISSUER), //Issuer - This server!
                new Claim(JwtRegisteredClaimNames.Iat, secondsSinceEpoch.ToString()), //Issued date
                new Claim(JwtRegisteredClaimNames.Exp, (secondsSinceEpoch + (60 * 30)).ToString()), //Expiration date - 30 mins from the time of creation
            };

            JwtPayload payload = new JwtPayload(claims);

            JwtSecurityToken token = new JwtSecurityToken(jwtHeader, payload);

            bool test = IsTokenValid(new JwtSecurityTokenHandler().WriteToken(token), out var _);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}

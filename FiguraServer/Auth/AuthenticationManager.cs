using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace FiguraServer.FiguraServer.Auth
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
                ValidIssuer = TOKEN_ISSUER,
                IssuerSigningKey = securityKey,
                CryptoProviderFactory = new CryptoProviderFactory()
                {
                    CacheSignatureProviders = false
                }
            };
        }

        public static bool IsTokenValid(string token, out ClaimsPrincipal claims)
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            try
            {
                claims = handler.ValidateToken(token, tokenValidationParameters, out var validToken);
                return true;
            }
            catch
            {
                claims = null;
                return false;
            }
        }

        public static string GenerateToken(string playerName)
        {
            //All the claims that should be shoved into the JWT
            Claim[] claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, playerName), //Subject - UUID of the player who this JWT is issued to.
                new Claim(JwtRegisteredClaimNames.Iss, TOKEN_ISSUER), //Issuer - This server!
                new Claim(JwtRegisteredClaimNames.Exp, DateTime.Now.AddMinutes(30).ToUniversalTime().ToString()), //Expiration date - 30 mins from the time of creation
            };

            JwtPayload payload = new JwtPayload(claims);

            JwtSecurityToken token = new JwtSecurityToken(jwtHeader, payload);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

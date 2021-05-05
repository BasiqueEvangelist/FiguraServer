using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FiguraServer.Server.Auth
{
    /// <summary>
    /// This class manages the individual connection between the fake server and a minecraft client.
    /// It comes with all the functions required to complete the vanilla minecraft authentication and encryption.
    /// 
    /// Once auth and encryption is verified, we will tell the auth system to generate a JWT for this client, and kick the user with the JWT as the message.
    /// </summary>
    public class MinecraftClientConnection
    {
        public delegate Task PacketHandler(byte[] data, MemoryStream stream, MinecraftClientConnection connection);

        /// <summary>
        /// Returns the handler for specific packets based on the connection's state.
        /// [state][packetID] = handler
        /// </summary>
        public static Dictionary<int, Dictionary<int, PacketHandler>> statePacketHandlers = new Dictionary<int, Dictionary<int, PacketHandler>>()
        {
            //Waiting for handshake.
            {
                0,
                new Dictionary<int, PacketHandler>(){
                    { 0, HandshakePacketHandler },
                }
            },
            //Status.
            {
                1,
                new Dictionary<int, PacketHandler>()
                {
                    { 0, StatusRequestHandler},
                    { 1, PingRequestHandler},
                }
            },
            //Login.
            {
                2,
                new Dictionary<int, PacketHandler>()
                {
                    { 0, LoginStartHandler},
                    { 1, ServerAuthRequestHandler},
                }
            }
        };

        private static string _servstat;
        public static string serverStatusResponse
        {
            get
            {
                if (_servstat == null)
                    _servstat = GetResponse().ToString();
                return _servstat;
            }
        }


        private FakeServerEncryptionState encryptionState;
        private TcpClient client;
        private NetworkStream stream;
        public Task processingTask;
        public bool isRunning = false;

        public int state = 0;

        //Username of the user who's connecting
        public string connectingUsername;
        public byte[] randomToken;
        public byte[] sharedKey;
        public byte[] secret;

        public bool enableEncryption;

        public MinecraftClientConnection(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
        }


        public async Task Start()
        {
            isRunning = false;
            if (processingTask != null)
                await processingTask;

            isRunning = true;
            //Create and start new processing task.
            processingTask = ProcessPackets();
        }

        public async Task Stop()
        {
            isRunning = false;
            await processingTask;
        }

        /// <summary>
        /// A continuous function, used to process packets sent from a client.
        /// </summary>
        /// <returns></returns>
        public async Task ProcessPackets()
        {
            try //MAIN BLOCK
            {
                //While running
                while (isRunning)
                {

                    //Get next packet, wrap in data stream.
                    (int id, byte[] data) = await GetNextPacketAsync();
                    //Some packets use a stream better than a raw byte array, so, we have both in case we need them.
                    MemoryStream packetDataStream = new MemoryStream(data);

                    //Try to get the handlers we should use for this packet, based on the onnection state.
                    if (statePacketHandlers.TryGetValue(state, out var handlerSet))
                    {
                        //Try to get the specific handler for this ID. Invalid IDs are ignored.
                        if (handlerSet.TryGetValue(id, out var handler))
                        {
                            //Try to handle packet.
                            try
                            {
                                //Handle the data.
                                await handler(data, packetDataStream, this);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                                //If handle fails, close connection.
                                client.Close();
                                return;
                            }

                            //Check if connection was closed.
                            if (!client.Connected)
                            {
                                return;
                            }

                            //Check if we've stopped.
                            if (!isRunning)
                            {
                                //Close connection.
                                client.Close();
                                return;
                            }
                        }
                    }
                    else
                    {
                        //Connection has reached an invalid state (no packets for this state), close connection.
                        client.Close();
                        return;
                    }
                }
            }
            catch (Exception e) //END MAIN BLOCK
            {
                Console.WriteLine(e.ToString());
            }
        }

        #region Packet Handling

        private async Task<(int id, byte[] data)> GetNextPacketAsync()
        {
            int length = await ReadVarIntAsync(stream);
            byte[] receivedData = new byte[length];

            await stream.ReadAsync(receivedData.AsMemory(0, length));

            int packetId = 0;
            byte[] packetData = Array.Empty<byte>();

            using (var packetStream = new MemoryStream(receivedData))
            {
                try
                {
                    packetId = await ReadVarIntAsync(packetStream);
                    int arlen = 0;

                    if (length - GetVarIntLength(packetId) > -1)
                        arlen = length - GetVarIntLength(packetId);

                    packetData = new byte[arlen];
                    await packetStream.ReadAsync(packetData.AsMemory(0, packetData.Length));
                }
                catch
                {
                    throw;
                }
            }

            return (packetId, packetData);
        }


        //Handshake.
        private static async Task HandshakePacketHandler(byte[] data, MemoryStream stream, MinecraftClientConnection connection)
        {
            HandshakePacket packet = new HandshakePacket();
            await packet.Read(stream);

            connection.state = packet.nextState;
            Console.WriteLine("Handshake with state " + packet.nextState);
        }

        //Status.
        private static async Task StatusRequestHandler(byte[] data, MemoryStream stream, MinecraftClientConnection connection)
        {
            Console.WriteLine("Status Request");

            connection.WriteString(serverStatusResponse);
            connection.Flush(0);
        }
        private static async Task PingRequestHandler(byte[] data, MemoryStream stream, MinecraftClientConnection connection)
        {
            Console.WriteLine("Ping Request");

            long l = BitConverter.ToInt64(data, 0);
            connection.WriteLong(l);
            connection.Flush(1);
        }

        //Login.
        private static async Task LoginStartHandler(byte[] data, MemoryStream stream, MinecraftClientConnection connection)
        {
            Console.WriteLine("Login Start");

            //Grab username.
            connection.connectingUsername = await ReadStringAsync(stream);

            //Generate encryption data needed for connection.
            connection.encryptionState = new FakeServerEncryptionState();
            connection.encryptionState.GenerateKeyPair();
            var keyTokenPair = connection.encryptionState.GeneratePublicKeyAndToken();
            connection.randomToken = keyTokenPair.randomToken;

            //Server ID (useless)
            connection.WriteString("");
            //Public Key.
            connection.WriteVarInt(keyTokenPair.publicKey.Length);
            connection.Write(keyTokenPair.publicKey);
            //Token.
            connection.WriteVarInt(keyTokenPair.randomToken.Length);
            connection.Write(keyTokenPair.randomToken);

            connection.Flush(1);
        }

        private static async Task ServerAuthRequestHandler(byte[] data, MemoryStream stream, MinecraftClientConnection connection)
        {

            Console.WriteLine("Server Auth Request");

            //Read secret.
            int secretLength = await ReadVarIntAsync(stream);
            byte[] secret = new byte[secretLength];
            ReadBytesAsync(stream, secret).Wait();

            //Verify token.
            int encryptedTokenLength = await ReadVarIntAsync(stream);
            byte[] encryptedToken = new byte[encryptedTokenLength];
            ReadBytesAsync(stream, encryptedToken).Wait();

            //Decrypt shared key from secret.
            connection.sharedKey = connection.encryptionState.Decrypt(secret);
            //Decrypt the token.
            byte[] token = connection.encryptionState.Decrypt(encryptedToken);

            if (!token.SequenceEqual(connection.randomToken))
            {
                //Failed to verify token.
                //Close connection.
                connection.isRunning = false;
                return;
            }

            //Get the ID of the server the client joined, from the public key.
            string serverID = MinecraftShaDigest(connection.sharedKey.Concat(connection.encryptionState.publicKey).ToArray());
            //Verify the player has joined the server they say they have, using Mojang's auth.
            FiguraAuthServer.JoinedResponse hasJoinedResponse = await FiguraAuthServer.HasJoined(connection.connectingUsername, serverID);

            if(hasJoinedResponse == null)
            {
                //Player hasn't actually joined this server, auth failed, close connection.
                connection.isRunning = false;
                return;
            }

            //Auth success!!!
            Console.WriteLine("Auth Complete for user " + connection.connectingUsername);

            //Turn on encryption
            connection.enableEncryption = true;

            //Respond with JWT in kick message.
            connection.WriteString(GetKickMessage(await AuthenticationManager.GenerateToken(connection.connectingUsername)));
            connection.Flush(0);

            connection.client.Close();
        }

        //Special packet just for the handshake. Very special.
        //❄❄❄❄❄❄❄❄❄
        public class HandshakePacket
        {
            public int protocolVersion;
            public string serverAddr;
            public short port; //UNUSED
            public int nextState;

            public async Task Read(Stream stream)
            {
                protocolVersion = await ReadVarIntAsync(stream);

                serverAddr = await ReadStringAsync(stream);

                byte[] dt = new byte[2];
                await MinecraftClientConnection.ReadBytesAsync(stream, dt);
                port = BitConverter.ToInt16(dt);

                nextState = await ReadVarIntAsync(stream);
            }
        }

        //Generates the JSON response for server status.
        public static JObject GetResponse()
        {
            JObject response = new JObject();
            JObject version = new JObject();
            JObject players = new JObject();
            JObject description = new JObject();

            version["name"] = "1.16.4";
            version["protocol"] = 754;
            response["version"] = version;

            players["max"] = 1;
            players["online"] = 0;
            players["sample"] = new JArray();
            response["players"] = players;

            description["text"] = "-! Figura Auth Server !-";
            description["color"] = "yellow";
            response["description"] = description;

            response["favicon"] = string.Empty;
            return response;
        }

        public static string GetKickMessage(string jwt)
        {
            JObject disconnectResponse = new JObject();
            disconnectResponse["text"] = "This is the Figura Auth Server V2.0!\n";
            disconnectResponse["color"] = "aqua";

            JObject clarification = new JObject();
            clarification["text"] = "Here is your auth token.\n\n\n";
            clarification["color"] = "aqua";

            JObject code = new JObject();
            code["text"] = $"{jwt}";
            code["color"] = "aqua";
            code["obfuscated"] = true;

            JObject funny = new JObject();
            funny["text"] = "(Just kidding! :D)";
            funny["color"] = "aqua";

            JArray extra = new JArray(clarification, code, funny);

            disconnectResponse["extra"] = extra;

            return disconnectResponse.ToString();
        }

        #endregion

        #region Minecraft Network Protocol Handlers

        public static string MinecraftShaDigest(byte[] data)
        {
            var hash = new SHA1Managed().ComputeHash(data);
            // Reverse the bytes since BigInteger uses little endian
            Array.Reverse(hash);

            var b = new BigInteger(hash);
            // very annoyingly, BigInteger in C# tries to be smart and puts in
            // a leading 0 when formatting as a hex number to allow roundtripping 
            // of negative numbers, thus we have to trim it off.
            if (b < 0)
            {
                // toss in a negative sign if the interpreted number is negative
                return $"-{(-b).ToString("x").TrimStart('0')}";
            }
            else
            {
                return b.ToString("x").TrimStart('0');
            }
        }

        /*internal static byte ReadByte(byte[] buffer, ref int index)
        {
            return buffer[index++];
        }

        internal static byte[] Read(byte[] buffer, int length, ref int index)
        {
            var data = new byte[length];
            Array.Copy(buffer, index, data, 0, length);
            index += length;
            return data;
        }*/

        public static int GetVarIntLength(int val)
        {
            int amount = 0;
            do
            {
                val >>= 7;
                amount++;
            } while (val != 0);

            return amount;
        }

        /*internal static int ReadVarInt(byte[] buffer, ref int index)
        {
            var value = 0;
            var size = 0;
            int b;
            while (((b = buffer[index++]) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 5)
                {
                    throw new IOException("This VarInt is an imposter!");
                }
            }
            return value | ((b & 0x7F) << (size * 7));
        }

        internal static long ReadVarLong(byte[] buffer, ref int index)
        {
            var value = 0;
            var size = 0;
            int b;
            while (((b = buffer[index++]) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 10)
                {
                    throw new IOException("This VarInt is an imposter!");
                }
            }
            return value | ((b & 0x7F) << (size * 7));
        }

        internal static string ReadString(byte[] buffer, int length, ref int index)
        {
            var data = Read(buffer, length, ref index);
            return Encoding.UTF8.GetString(data);
        }*/


        internal static async Task<byte> ReadByteAsync(Stream stream)
        {
            var b = new byte[1];
            await stream.ReadAsync(b.AsMemory(0, 1));
            return b[0];
        }

        internal static async Task ReadBytesAsync(Stream stream, byte[] target)
        {
            await stream.ReadAsync(target.AsMemory(0, target.Length));
        }


        internal static async Task<int> ReadVarIntAsync(Stream stream)
        {
            int numRead = 0;
            int result = 0;
            byte read;
            do
            {
                read = await ReadByteAsync(stream);
                int value = read & 0b01111111;
                result |= value << (7 * numRead);

                numRead++;
                if (numRead > 5)
                {
                    throw new InvalidOperationException("VarInt is too big");
                }
            } while ((read & 0b10000000) != 0);

            return result;
        }

        internal static async Task<long> ReadVarLongAsync(Stream stream)
        {
            int numRead = 0;
            int result = 0;
            byte read;
            do
            {
                read = await ReadByteAsync(stream);
                int value = read & 0b01111111;
                result |= value << (7 * numRead);

                numRead++;
                if (numRead > 10)
                {
                    throw new InvalidOperationException("VarInt is too big");
                }
            } while ((read & 0b10000000) != 0);

            return result;
        }

        internal static async Task<string> ReadStringAsync(Stream stream, int maxLength = 32767)
        {
            int length = await ReadVarIntAsync(stream);
            byte[] buffer = new byte[length];
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            await stream.ReadAsync(buffer, 0, length);

            var value = Encoding.UTF8.GetString(buffer);
            if (maxLength > 0 && value.Length > maxLength)
            {
                throw new ArgumentException($"string ({value.Length}) exceeded maximum length ({maxLength})", nameof(value));
            }
            return value;
        }


        public List<byte> _buffer = new List<byte>();

        internal void WriteVarInt(int value)
        {
            var unsigned = (uint)value;

            do
            {
                var temp = (byte)(unsigned & 127);
                unsigned >>= 7;

                if (unsigned != 0)
                    temp |= 128;

                _buffer.Add(temp);
            }
            while (unsigned != 0);
        }

        internal void WriteVarLong(long value)
        {
            while ((value & 128) != 0)
            {
                _buffer.Add((byte)(value & 127 | 128));
                value = (long)((ulong)value) >> 7;
            }
            _buffer.Add((byte)value);
        }

        internal void WriteShort(short value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void WriteString(string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            WriteVarInt(buffer.Length);
            _buffer.AddRange(buffer);
        }

        internal void Write(byte b)
        {
            _buffer.Add(b);
        }

        internal void Write(byte[] b)
        {
            _buffer.AddRange(b);
        }

        internal void WriteLong(long l)
        {
            _buffer.AddRange(BitConverter.GetBytes(l));
        }

        internal void Flush(int id = -1)
        {
            //Cache buffer locally
            var buffer = _buffer.ToArray();
            _buffer.Clear();

            //Write ID
            if (id >= 0)
                WriteVarInt(id);

            //Re-cache.
            _buffer.AddRange(buffer);
            buffer = _buffer.ToArray();
            _buffer.Clear();

            //Write buffer length.
            WriteVarInt(buffer.Length);

            //Re-cache
            _buffer.AddRange(buffer);
            buffer = _buffer.ToArray();
            _buffer.Clear();


            if (enableEncryption)
            {
                buffer = encryptionState.Encrypt(buffer, sharedKey);
            }

            stream.Write(buffer, 0, buffer.Length);

            //Console.WriteLine("Writing " + (buffer.Length) + " bytes");
        }

        #endregion

        #region Encryption
        //Fake state for the server, used to encrypt and such.
        //One is provided per connection.
        public class FakeServerEncryptionState
        {
            //Keypair generator for the server
            public  RsaKeyPairGenerator provider;

            //Ciphers for encryption
            public IAsymmetricBlockCipher encryptCipher;
            public IAsymmetricBlockCipher decryptCipher;

            //The actual keypair we have for the server
            public AsymmetricCipherKeyPair keyPair;

            public byte[] verifyToken { get; set; }
            public byte[] publicKey { get; set; }

            public  (byte[] publicKey, byte[] randomToken) GeneratePublicKeyAndToken()
            {
                var randomToken = new byte[4];
                using var provider = new RNGCryptoServiceProvider();
                provider.GetBytes(randomToken);

                verifyToken = randomToken;
                publicKey = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public).ToAsn1Object().GetDerEncoded();

                return (publicKey, verifyToken);
            }

            public  AsymmetricCipherKeyPair GenerateKeyPair()
            {
                if (provider is null)
                {
                    try
                    {
                        provider = new RsaKeyPairGenerator();
                        provider.Init(new KeyGenerationParameters(new SecureRandom(), 1024));
                        encryptCipher = new Pkcs1Encoding(new RsaEngine());
                        decryptCipher = new Pkcs1Encoding(new RsaEngine());
                        keyPair = provider.GenerateKeyPair();

                        encryptCipher.Init(true, keyPair.Public);
                        decryptCipher.Init(false, keyPair.Private);
                    }
                    catch
                    {
                        throw;
                    }
                }

                return keyPair;
            }

            public  byte[] Decrypt(byte[] toDecrypt) => decryptCipher.ProcessBlock(toDecrypt, 0, toDecrypt.Length);
            public  byte[] Encrypt(byte[] toEncrypt) => encryptCipher.ProcessBlock(toEncrypt, 0, toEncrypt.Length);

            public  byte[] Encrypt(byte[] data, byte[] key)
            {
                var keyParam = ParameterUtilities.CreateKeyParameter("AES", key);
                var parametersWithIv = new ParametersWithIV(keyParam, key);

                var cipher = CipherUtilities.GetCipher("AES/CFB8");

                cipher.Init(true, parametersWithIv);

                byte[] cipherFinal = cipher.DoFinal(data);

                return cipherFinal;
            }
        }
        #endregion
    }
}

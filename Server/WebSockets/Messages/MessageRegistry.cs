using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FiguraServer.Server.WebSockets.Messages
{
    public class MessageRegistry
    {
        private readonly Dictionary<string, sbyte> mapping = new();

        public void ReadRegistryMessage(BinaryReader br)
        {
            int count = br.ReadInt32();
            Logger.LogMessage($"Received client registry message! {count} handlers");
            sbyte currentId = sbyte.MinValue + 1;
            for (int i = 0; i < count; i++)
            {
                int strLen = br.ReadInt32();
                string protocolName = Encoding.UTF8.GetString(br.ReadBytes(strLen));
                mapping[protocolName] = currentId;
                Logger.LogMessage($"'{protocolName}' is mapped to sbyte 0x{currentId:X2}");
                currentId++;
            }
        }

        public sbyte GetMessageId(string protocolName)
            => mapping[protocolName];
    }
}

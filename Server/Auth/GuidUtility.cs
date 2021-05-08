// This is a modified version of https://github.com/Faithlife/FaithlifeUtility/blob/master/src/Faithlife.Utility/GuidUtility.cs, which is licensed under MIT.
// https://github.com/Faithlife/FaithlifeUtility/blob/master/LICENSE

using System;
using System.Security.Cryptography;

namespace FiguraServer.Server.Auth
{
    public static class GuidUtility
    {
        public static Guid CreateOfflineUuid(byte[] nameBytes)
        {
            byte[] data = nameBytes;
            byte[] hash;
            using (HashAlgorithm algorithm = MD5.Create())
                hash = algorithm.ComputeHash(data);

            byte[] newGuid = new byte[16];
            Array.Copy(hash, 0, newGuid, 0, 16);

            newGuid[6] = (byte)((newGuid[6] & 0x0F) | (3 << 4));

            newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

            SwapByteOrder(newGuid);
            return new Guid(newGuid);
        }
        internal static void SwapByteOrder(byte[] guid)
        {
            SwapBytes(guid, 0, 3);
            SwapBytes(guid, 1, 2);
            SwapBytes(guid, 4, 5);
            SwapBytes(guid, 6, 7);
        }

        private static void SwapBytes(byte[] guid, int left, int right)
        {
            byte temp = guid[left];
            guid[left] = guid[right];
            guid[right] = temp;
        }
    }
}

using fNbt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Data
{
    public static class Extensions
    {

        public static NbtCompound GetNbtCompoundFromBytes(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                NbtReader reader = new NbtReader(stream);
                return (NbtCompound)reader.ReadAsTag();
            }
        }

        public static byte[] GetBytesFromNBTCompound(NbtCompound nbt)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                NbtFile file = new NbtFile(nbt);

                file.SaveToStream(stream, NbtCompression.GZip);

                return stream.ToArray();
            }
        }

    }
}

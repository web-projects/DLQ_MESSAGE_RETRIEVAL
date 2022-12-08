using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DLQ.Common.Utilities
{
    public class ArrayUtils
    {
        public static byte[] ToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static Object FromByteArray(byte[] data)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream(data))
            {
                return bf.Deserialize(ms);
            }
        }
    }
}

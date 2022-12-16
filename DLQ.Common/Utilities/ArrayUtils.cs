using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DLQ.Common.Utilities
{
    public static class ArrayUtils
    {
        public static byte[] ToByteArray(Object obj)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    bf.Serialize(ms, obj);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in ToByteArray - {ex}");
            }

            return null;
        }

        public static Object FromByteArray(byte[] data)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (var ms = new MemoryStream(data))
                {
                    return bf.Deserialize(ms);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in ToByteArray - {ex}");
            }

            return null;
        }
    }
}

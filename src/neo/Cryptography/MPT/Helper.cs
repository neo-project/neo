using System;

namespace Neo.Cryptography.MPT
{
    internal static class Helper
    {
        public static byte[] ToNibbles(this byte[] path)
        {
            if (path is null) return Array.Empty<byte>();
            var result = new byte[path.Length * 2];
            for (int i = 0; i < path.Length; i++)
            {
                result[i * 2] = (byte)(path[i] >> 4);
                result[i * 2 + 1] = (byte)(path[i] & 0x0F);
            }
            return result;
        }
    }
}

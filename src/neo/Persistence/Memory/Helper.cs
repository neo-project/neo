using System;

namespace Neo.Persistence.Memory
{
    internal static class Helper
    {
        public static byte[] EnsureNotNull(this byte[] source)
        {
            return source ?? Array.Empty<byte>();
        }
    }
}

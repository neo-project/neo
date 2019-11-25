namespace Neo.Persistence.Memory
{
    internal static class Helper
    {
        private static readonly byte[] EmptyBytes = new byte[0];

        public static byte[] EnsureNotNull(this byte[] source)
        {
            return source ?? EmptyBytes;
        }
    }
}

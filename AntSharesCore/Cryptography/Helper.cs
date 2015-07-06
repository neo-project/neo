using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace AntShares.Cryptography
{
    public static class Helper
    {
        private static ThreadLocal<SHA256Cng> _sha256 = new ThreadLocal<SHA256Cng>(() => new SHA256Cng());
        private static ThreadLocal<RIPEMD160Managed> _ripemd160 = new ThreadLocal<RIPEMD160Managed>(() => new RIPEMD160Managed());

        public static UInt32 Checksum(this IEnumerable<byte> value)
        {
            return BitConverter.ToUInt32(value.Sha256().Sha256(), 0);
        }

        public static byte[] RIPEMD160(this IEnumerable<byte> value)
        {
            return _ripemd160.Value.ComputeHash(value.ToArray());
        }

        public static byte[] Sha256(this IEnumerable<byte> value)
        {
            return _sha256.Value.ComputeHash(value.ToArray());
        }

        public static byte[] Sha256(this byte[] value, int offset, int count)
        {
            return _sha256.Value.ComputeHash(value, offset, count);
        }
    }
}

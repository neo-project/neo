using Neo.Cryptography;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.IO;

namespace Neo.Wallets
{
    public static class Helper
    {
        public static byte[] Sign(this IVerifiable verifiable, KeyPair key, uint magic)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            writer.Write(magic);
            verifiable.SerializeUnsigned(writer);
            writer.Flush();
            return Crypto.Sign(ms.ToArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
        }

        public static string ToAddress(this UInt160 scriptHash, byte version)
        {
            Span<byte> data = stackalloc byte[21];
            data[0] = version;
            scriptHash.ToArray().CopyTo(data[1..]);
            return Base58.Base58CheckEncode(data);
        }

        public static UInt160 ToScriptHash(this string address, byte version)
        {
            byte[] data = address.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException();
            if (data[0] != version)
                throw new FormatException();
            return new UInt160(data.AsSpan(1));
        }

        internal static byte[] XOR(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) throw new ArgumentException();
            byte[] r = new byte[x.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = (byte)(x[i] ^ y[i]);
            return r;
        }
    }
}

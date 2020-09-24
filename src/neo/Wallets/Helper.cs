using Neo.Cryptography;
using Neo.IO;
using Neo.Models;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Wallets
{
    public static class Helper
    {
        public static byte[] Sign(this IWitnessed verifiable, KeyPair key)
        {
            return Sign(verifiable, key, ProtocolSettings.Default.Magic);
        }

        public static byte[] Sign(this IWitnessed verifiable, KeyPair key, uint magic)
        {
            return Crypto.Sign(verifiable.GetHashData(magic), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
        }

        public static string ToAddress(this UInt160 scriptHash)
        {
            Span<byte> data = stackalloc byte[21];
            data[0] = ProtocolSettings.Default.AddressVersion;
            scriptHash.ToArray().CopyTo(data[1..]);
            return Base58.Base58CheckEncode(data);
        }

        public static UInt160 ToScriptHash(this string address)
        {
            byte[] data = address.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException();
            if (data[0] != ProtocolSettings.Default.AddressVersion)
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

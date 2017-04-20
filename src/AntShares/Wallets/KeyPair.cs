using AntShares.Core;
using AntShares.Cryptography;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    public class KeyPair : IEquatable<KeyPair>
    {
        public readonly byte[] PrivateKey;
        public readonly Cryptography.ECC.ECPoint PublicKey;
        public readonly UInt160 PublicKeyHash;

        public KeyPair(byte[] privateKey)
        {
            if (privateKey.Length != 32 && privateKey.Length != 96 && privateKey.Length != 104)
                throw new ArgumentException();
            this.PrivateKey = new byte[32];
            Buffer.BlockCopy(privateKey, privateKey.Length - 32, PrivateKey, 0, 32);
            if (privateKey.Length == 32)
            {
                this.PublicKey = Cryptography.ECC.ECCurve.Secp256r1.G * privateKey;
            }
            else
            {
                this.PublicKey = Cryptography.ECC.ECPoint.FromBytes(privateKey, Cryptography.ECC.ECCurve.Secp256r1);
            }
            this.PublicKeyHash = PublicKey.EncodePoint(true).ToScriptHash();
#if NET461
            ProtectedMemory.Protect(PrivateKey, MemoryProtectionScope.SameProcess);
#endif
        }

        public IDisposable Decrypt()
        {
#if NET461
            return new ProtectedMemoryContext(PrivateKey, MemoryProtectionScope.SameProcess);
#else
            return new System.IO.MemoryStream(0);
#endif
        }

        public bool Equals(KeyPair other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return PublicKeyHash.Equals(other.PublicKeyHash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as KeyPair);
        }

        public string Export()
        {
            using (Decrypt())
            {
                byte[] data = new byte[38];
                data[0] = 0x80;
                Buffer.BlockCopy(PrivateKey, 0, data, 1, 32);
                data[33] = 0x01;
                byte[] checksum = Crypto.Default.Hash256(data.Take(data.Length - 4).ToArray());
                Buffer.BlockCopy(checksum, 0, data, data.Length - 4, 4);
                string wif = Base58.Encode(data);
                Array.Clear(data, 0, data.Length);
                return wif;
            }
        }

        public override int GetHashCode()
        {
            return PublicKeyHash.GetHashCode();
        }
    }
}

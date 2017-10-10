using Neo.Core;
using Neo.Cryptography;
using Neo.SmartContract;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Wallets
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
                byte[] data = new byte[34];
                data[0] = 0x80;
                Buffer.BlockCopy(PrivateKey, 0, data, 1, 32);
                data[33] = 0x01;
                string wif = data.Base58CheckEncode();
                Array.Clear(data, 0, data.Length);
                return wif;
            }
        }

        public string Export(string passphrase)
        {
            using (Decrypt())
            {
                UInt160 script_hash = Contract.CreateSignatureRedeemScript(PublicKey).ToScriptHash();
                string address = Wallet.ToAddress(script_hash);
                byte[] addresshash = Encoding.ASCII.GetBytes(address).Sha256().Sha256().Take(4).ToArray();
                byte[] derivedkey = SCrypt.DeriveKey(Encoding.UTF8.GetBytes(passphrase), addresshash, 16384, 8, 8, 64);
                byte[] derivedhalf1 = derivedkey.Take(32).ToArray();
                byte[] derivedhalf2 = derivedkey.Skip(32).ToArray();
                byte[] encryptedkey = XOR(PrivateKey, derivedhalf1).AES256Encrypt(derivedhalf2);
                byte[] buffer = new byte[39];
                buffer[0] = 0x01;
                buffer[1] = 0x42;
                buffer[2] = 0xe0;
                Buffer.BlockCopy(addresshash, 0, buffer, 3, addresshash.Length);
                Buffer.BlockCopy(encryptedkey, 0, buffer, 7, encryptedkey.Length);
                return buffer.Base58CheckEncode();
            }
        }

        public override int GetHashCode()
        {
            return PublicKeyHash.GetHashCode();
        }

        private static byte[] XOR(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) throw new ArgumentException();
            return x.Zip(y, (a, b) => (byte)(a ^ b)).ToArray();
        }
    }
}

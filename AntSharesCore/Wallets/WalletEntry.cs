using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    public class WalletEntry : IEquatable<WalletEntry>
    {
        public readonly byte[] PrivateKey;
        public readonly byte[] PublicKey;
        public readonly byte[] RedeemScript;
        public readonly UInt160 ScriptHash;

        private string _address;
        public string Address
        {
            get
            {
                if (_address == null)
                {
                    _address = Wallet.ToAddress(ScriptHash);
                }
                return _address;
            }
        }

        internal WalletEntry(byte[] redeemScript, byte[] privateKey)
        {
            if (privateKey.Length != 32 && privateKey.Length != 96 && privateKey.Length != 104)
                throw new ArgumentException();
            this.PrivateKey = new byte[32];
            Buffer.BlockCopy(privateKey, privateKey.Length - 32, PrivateKey, 0, 32);
            if (privateKey.Length == 32)
            {
                ECPoint p = ECCurve.Secp256r1.G * privateKey;
                this.PublicKey = p.EncodePoint(false).Skip(1).ToArray();
            }
            else
            {
                this.PublicKey = new byte[64];
                Buffer.BlockCopy(privateKey, privateKey.Length - 96, PublicKey, 0, 64);
            }
            this.RedeemScript = redeemScript;
            this.ScriptHash = redeemScript.ToScriptHash();
            ProtectedMemory.Protect(PrivateKey, MemoryProtectionScope.SameProcess);
        }

        public IDisposable Decrypt()
        {
            return new ProtectedMemoryContext(PrivateKey, MemoryProtectionScope.SameProcess);
        }

        public bool Equals(WalletEntry other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return ScriptHash.Equals(other.ScriptHash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WalletEntry);
        }

        public string Export()
        {
            using (Decrypt())
            {
                byte[] data = new byte[38];
                data[0] = 0x80;
                Buffer.BlockCopy(PrivateKey, 0, data, 1, 32);
                data[33] = 0x01;
                byte[] checksum = data.Sha256(0, data.Length - 4).Sha256();
                Buffer.BlockCopy(checksum, 0, data, data.Length - 4, 4);
                string wif = Base58.Encode(data);
                Array.Clear(data, 0, data.Length);
                return wif;
            }
        }

        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode();
        }

        public override string ToString()
        {
            return Address;
        }
    }
}

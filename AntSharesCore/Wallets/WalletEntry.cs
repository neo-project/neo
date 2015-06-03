using AntShares.Cryptography;
using System;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    public class WalletEntry : IEquatable<WalletEntry>
    {
        public byte[][] PrivateKey;
        public readonly byte[] RedeemScript;
        public readonly UInt160 ScriptHash;

        internal WalletEntry(byte[] redeemScript, params byte[][] privateKey)
        {
            this.PrivateKey = privateKey;
            this.RedeemScript = redeemScript;
            this.ScriptHash = new UInt160(redeemScript.Sha256().RIPEMD160());
            foreach (byte[] data in privateKey)
            {
                ProtectedMemory.Protect(data, MemoryProtectionScope.SameProcess);
            }
        }

        public IDisposable Decrypt(int index)
        {
            return new ProtectionContext(PrivateKey[index]);
        }

        public bool Equals(WalletEntry other)
        {
            if (object.ReferenceEquals(other, null))
                return false;
            if (object.ReferenceEquals(this, other))
                return true;
            return ScriptHash.Equals(other.ScriptHash);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null))
                return false;
            if (!(obj is WalletEntry))
                return false;
            return this.Equals((WalletEntry)obj);
        }

        //public string Export()
        //{
        //    using (this.Decrypt())
        //    {
        //        byte[] data = new byte[38];
        //        data[0] = 0x80;
        //        Buffer.BlockCopy(PrivateKey, 0, data, 1, 32);
        //        data[33] = 0x01;
        //        byte[] checksum = data.Sha256(0, data.Length - 4).Sha256();
        //        Buffer.BlockCopy(checksum, 0, data, data.Length - 4, 4);
        //        string wif = Base58.Encode(data);
        //        Array.Clear(data, 0, data.Length);
        //        return wif;
        //    }
        //}

        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode();
        }
    }
}

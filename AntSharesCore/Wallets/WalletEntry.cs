using AntShares.Core;
using AntShares.Cryptography;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    public class WalletEntry : IEquatable<WalletEntry>
    {
        public byte[][] PrivateKeys;
        public byte[][] PublicKeys;
        public readonly byte[] RedeemScript;
        public readonly UInt160 ScriptHash;

        public byte M
        {
            get
            {
                return (byte)(RedeemScript[0] - 0x50);
            }
        }

        public byte N
        {
            get
            {
                return (byte)((RedeemScript.Length - 3) / 34);
            }
        }

        internal WalletEntry(byte[] redeemScript, params byte[][] privateKeys)
        {
            this.PrivateKeys = new byte[privateKeys.Length][];
            this.PublicKeys = new byte[privateKeys.Length][];
            for (int i = 0; i < privateKeys.Length; i++)
            {
                if (privateKeys[i].Length != 32 && privateKeys[i].Length != 96 && privateKeys[i].Length != 104)
                    throw new ArgumentException();
                PrivateKeys[i] = new byte[32];
                Buffer.BlockCopy(privateKeys[i], privateKeys[i].Length - 32, PrivateKeys[i], 0, 32);
                if (privateKeys[i].Length == 32)
                {
                    Secp256r1Point p = Secp256r1Curve.G * privateKeys[i];
                    PublicKeys[i] = p.EncodePoint(false).Skip(1).ToArray();
                }
                else
                {
                    PublicKeys[i] = new byte[64];
                    Buffer.BlockCopy(privateKeys[i], privateKeys[i].Length - 96, PublicKeys[i], 0, 64);
                }
            }
            this.RedeemScript = redeemScript;
            this.ScriptHash = redeemScript.ToScriptHash();
            for (int i = 0; i < PrivateKeys.Length; i++)
            {
                ProtectedMemory.Protect(PrivateKeys[i], MemoryProtectionScope.SameProcess);
            }
        }

        public IDisposable Decrypt(int index)
        {
            return new ProtectionContext(PrivateKeys[index]);
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

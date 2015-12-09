using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Wallets
{
    public abstract class Contract : IEquatable<Contract>, ISerializable
    {
        public byte[] RedeemScript;
        public UInt160 PublicKeyHash;

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

        private UInt160 _scriptHash;
        public UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = RedeemScript.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        public abstract void Deserialize(BinaryReader reader);

        public bool Equals(Contract other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return ScriptHash.Equals(other.ScriptHash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Contract);
        }

        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode();
        }

        public abstract bool IsCompleted(ECPoint[] publicKeys);

        public abstract void Serialize(BinaryWriter writer);

        public override string ToString()
        {
            return Address;
        }
    }
}

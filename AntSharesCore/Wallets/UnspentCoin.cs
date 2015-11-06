using AntShares.Core;
using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Wallets
{
    public class UnspentCoin : IEquatable<UnspentCoin>, ISerializable
    {
        public TransactionInput Input;
        public UInt256 AssetId;
        public Fixed8 Value;
        public UInt160 ScriptHash;

        private string _address = null;
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

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Input = reader.ReadSerializable<TransactionInput>();
            AssetId = reader.ReadSerializable<UInt256>();
            Value = reader.ReadSerializable<Fixed8>();
            ScriptHash = reader.ReadSerializable<UInt160>();
        }

        public bool Equals(UnspentCoin other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return Input.Equals(other.Input);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UnspentCoin);
        }

        public override int GetHashCode()
        {
            return Input.GetHashCode();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Input);
            writer.Write(AssetId);
            writer.Write(Value);
            writer.Write(ScriptHash);
        }
    }
}

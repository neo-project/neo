using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;
using System.Linq;

namespace Neo.Ledger
{
    public class MPTKey : IEquatable<MPTKey>, ISerializable
    {
        public UInt160 ScriptHash;
        public UInt256 HashKey;

        int ISerializable.Size => ScriptHash.Size + HashKey.Size;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ScriptHash = reader.ReadSerializable<UInt160>();
            HashKey = reader.ReadSerializable<UInt256>();
        }

        public bool Equals(MPTKey other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return ScriptHash.Equals(other.ScriptHash) && HashKey.Equals(other.HashKey);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (!(obj is MPTKey)) return false;
            return Equals((MPTKey)obj);
        }

        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode() + HashKey.GetHashCode();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(ScriptHash);
            writer.Write(HashKey);
        }
    }
}

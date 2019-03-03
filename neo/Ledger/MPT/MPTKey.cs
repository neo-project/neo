using Neo.IO;
using System;
using System.IO;

namespace Neo.Ledger.MPT
{
    /// <inheritdoc />
    /// <summary>
    /// MPTKey for the DataCache.
    /// </summary>
    public class MPTKey : IEquatable<MPTKey>, ISerializable
    {
        public UInt160 ScriptHash;
        public UInt256 HashKey;

        /// <inheritdoc />
        int ISerializable.Size => ScriptHash.Size + HashKey.Size;

        /// <inheritdoc />
        public bool Equals(MPTKey other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return ScriptHash.Equals(other.ScriptHash) && HashKey.Equals(other.HashKey);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is MPTKey key && Equals(key);
        }

        /// <inheritdoc />
        public override int GetHashCode() => ScriptHash.GetHashCode() + HashKey.GetHashCode();

        /// <inheritdoc />
        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(ScriptHash);
            writer.Write(HashKey);
        }

        /// <inheritdoc />
        void ISerializable.Deserialize(BinaryReader reader)
        {
            ScriptHash = reader.ReadSerializable<UInt160>();
            HashKey = reader.ReadSerializable<UInt256>();
        }

        /// <inheritdoc />
        public override string ToString() => $"{{'ScriptHash':{ScriptHash}, 'HashKey':{HashKey}}}";
    }
}
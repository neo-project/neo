using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class TransactionInput : IEquatable<TransactionInput>, ISerializable
    {
        public UInt256 PrevTxId;
        public ushort PrevIndex;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.PrevTxId = reader.ReadSerializable<UInt256>();
            this.PrevIndex = reader.ReadUInt16();
        }

        public bool Equals(TransactionInput other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return PrevTxId.Equals(other.PrevTxId) && PrevIndex.Equals(other.PrevIndex);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (ReferenceEquals(null, obj)) return false;
            if (!(obj is TransactionInput)) return false;
            return Equals((TransactionInput)obj);
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(PrevTxId.ToArray(), 0) + PrevIndex;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(PrevTxId);
            writer.Write(PrevIndex);
        }
    }
}

using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Network.Payloads
{
    internal class InventoryVector : IEquatable<InventoryVector>, ISerializable
    {
        public InventoryType Type;
        public UInt256 Hash;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Type = (InventoryType)reader.ReadUInt32();
            if (!Enum.IsDefined(typeof(InventoryType), Type))
                throw new FormatException();
            Hash = reader.ReadSerializable<UInt256>();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InventoryVector)) return false;
            return Equals((InventoryVector)obj);
        }

        public bool Equals(InventoryVector other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hash == other.Hash;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((uint)Type);
            writer.Write(Hash);
        }
    }
}

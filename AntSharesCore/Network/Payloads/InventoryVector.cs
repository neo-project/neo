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
            this.Type = (InventoryType)reader.ReadUInt32();
            this.Hash = reader.ReadSerializable<UInt256>();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is InventoryVector))
                return false;
            return this.Equals((InventoryVector)obj);
        }

        public bool Equals(InventoryVector other)
        {
            if (other == null)
                return false;
            if (this == other)
                return true;
            return this.Hash == other.Hash;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((UInt32)Type);
            writer.Write(Hash);
        }
    }
}

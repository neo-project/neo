using AntShares.IO;
using System.IO;
using System.Linq;

namespace AntShares.Network.Payloads
{
    internal class InvPayload : ISerializable
    {
        public InventoryVector[] Inventories;

        public static InvPayload Create(InventoryType type, params UInt256[] hashes)
        {
            return new InvPayload
            {
                Inventories = hashes.Select(p => new InventoryVector { Type = type, Hash = p }).ToArray()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Inventories = reader.ReadSerializableArray<InventoryVector>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Inventories);
        }
    }
}

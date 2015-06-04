using AntShares.IO;
using System.IO;
using System.Linq;

namespace AntShares.Network.Payloads
{
    internal class GetDataPayload : ISerializable
    {
        public InventoryVector[] Inventories;

        public static GetDataPayload Create(InventoryType type, params UInt256[] hashes)
        {
            return new GetDataPayload
            {
                Inventories = hashes.Select(p => new InventoryVector
                {
                    Type = type,
                    Hash = p
                }).ToArray()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.Inventories = reader.ReadSerializableArray<InventoryVector>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Inventories);
        }
    }
}

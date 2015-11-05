using AntShares.IO;
using System.IO;

namespace AntShares.Network.Payloads
{
    internal class GetDataPayload : ISerializable
    {
        public InventoryVector[] Inventories;

        public static GetDataPayload Create(InventoryVector[] vectors)
        {
            return new GetDataPayload
            {
                Inventories = vectors
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

using AntShares.IO;
using System.IO;

namespace AntShares.Network.Payloads
{
    internal class InvPayload : ISerializable
    {
        public InventoryVector[] Inventories;

        public static InvPayload Create(params InventoryVector[] inventories)
        {
            return new InvPayload
            {
                Inventories = inventories
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

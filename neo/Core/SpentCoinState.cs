using Neo.IO;
using System.Collections.Generic;
using System.IO;

namespace Neo.Core
{
    public class SpentCoinState : StateBase
    {
        public UInt256 TransactionHash;
        public uint TransactionHeight;
        public Dictionary<ushort, uint> Items;

        public override int Size => base.Size + TransactionHash.Size + sizeof(uint)
            + IO.Helper.GetVarSize(Items.Count) + Items.Count * (sizeof(ushort) + sizeof(uint));

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TransactionHash = reader.ReadSerializable<UInt256>();
            TransactionHeight = reader.ReadUInt32();
            int count = (int)reader.ReadVarInt();
            Items = new Dictionary<ushort, uint>(count);
            for (int i = 0; i < count; i++)
            {
                ushort index = reader.ReadUInt16();
                uint height = reader.ReadUInt32();
                Items.Add(index, height);
            }
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(TransactionHash);
            writer.Write(TransactionHeight);
            writer.WriteVarInt(Items.Count);
            foreach (var pair in Items)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }
    }
}

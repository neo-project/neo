using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace AntShares.Core
{
    public class SpentCoinState : ISerializable
    {
        public const byte StateVersion = 0;
        public UInt256 TransactionHash;
        public uint TransactionHeight;
        public Dictionary<ushort, uint> Items;

        int ISerializable.Size => sizeof(byte) + TransactionHash.Size + sizeof(uint)
            + IO.Helper.GetVarSize(Items.Count) + Items.Count * (sizeof(ushort) + sizeof(uint));

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != StateVersion) throw new FormatException();
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

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StateVersion);
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

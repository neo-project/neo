using Neo.Cryptography;
using Neo.IO;
using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class MerkleBlockPayload : ISerializable
    {
        public Header Header;
        public int TxCount;
        public UInt256[] Hashes;
        public byte[] Flags;

        public int Size => Header.Size + sizeof(int) + Hashes.GetVarSize() + Flags.GetVarSize();

        public static MerkleBlockPayload Create(Block block, BitArray flags)
        {
            MerkleTree tree = new MerkleTree(block.Transactions.Select(p => p.Hash).ToArray());
            tree.Trim(flags);
            byte[] buffer = new byte[(flags.Length + 7) / 8];
            flags.CopyTo(buffer, 0);
            return new MerkleBlockPayload
            {
                Header = block.Header,
                TxCount = block.Transactions.Length,
                Hashes = tree.ToHashArray(),
                Flags = buffer
            };
        }

        public void Deserialize(BinaryReader reader)
        {
            Header = reader.ReadSerializable<Header>();
            TxCount = (int)reader.ReadVarInt(Block.MaxTransactionsPerBlock);
            Hashes = reader.ReadSerializableArray<UInt256>(TxCount);
            Flags = reader.ReadVarBytes((Math.Max(TxCount, 1) + 7) / 8);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Header);
            writer.WriteVarInt(TxCount);
            writer.Write(Hashes);
            writer.WriteVarBytes(Flags);
        }
    }
}

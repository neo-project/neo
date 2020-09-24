using Neo.Cryptography;
using Neo.IO;
using Neo.Models;
using System.Collections;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class MerkleBlockPayload : BlockBase
    {
        public int ContentCount;
        public UInt256[] Hashes;
        public byte[] Flags;

        public MerkleBlockPayload() : base(ProtocolSettings.Default.Magic)
        {
        }

        public override int Size => base.Size + sizeof(int) + Hashes.GetVarSize() + Flags.GetVarSize();

        public static MerkleBlockPayload Create(Block block, BitArray flags, uint? magic = null)
        {
            MerkleTree tree = new MerkleTree(block.Transactions.Select(p => p.Hash).Prepend(block.ConsensusData.Hash).ToArray());
            byte[] buffer = new byte[(flags.Length + 7) / 8];
            flags.CopyTo(buffer, 0);
            return new MerkleBlockPayload()
            {
                Version = block.Version,
                PrevHash = block.PrevHash,
                MerkleRoot = block.MerkleRoot,
                Timestamp = block.Timestamp,
                Index = block.Index,
                NextConsensus = block.NextConsensus,
                Witness = block.Witness,
                ContentCount = block.Transactions.Length + 1,
                Hashes = tree.ToHashArray(),
                Flags = buffer
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ContentCount = (int)reader.ReadVarInt(Block.MaxTransactionsPerBlock + 1);
            Hashes = reader.ReadSerializableArray<UInt256>(ContentCount);
            Flags = reader.ReadVarBytes((ContentCount + 7) / 8);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarInt(ContentCount);
            writer.Write(Hashes);
            writer.WriteVarBytes(Flags);
        }
    }
}

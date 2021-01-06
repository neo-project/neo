using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.IO;
using System.Linq;

namespace Neo.Consensus
{
    public class PrepareRequest : ConsensusMessage
    {
        public uint Version;
        public UInt256 PrevHash;
        public ulong Timestamp;
        public ulong Nonce;
        public UInt256[] TransactionHashes;

        public override int Size => base.Size
            + sizeof(uint)                      //Version
            + UInt256.Length                    //PrevHash
            + sizeof(ulong)                     //Timestamp
            + sizeof(ulong)                     //Nonce
            + TransactionHashes.GetVarSize();   //TransactionHashes

        public PrepareRequest()
            : base(ConsensusMessageType.PrepareRequest)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt64();
            Nonce = reader.ReadUInt64();
            TransactionHashes = reader.ReadSerializableArray<UInt256>(Block.MaxTransactionsPerBlock);
            if (TransactionHashes.Distinct().Count() != TransactionHashes.Length)
                throw new FormatException();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(Timestamp);
            writer.Write(Nonce);
            writer.Write(TransactionHashes);
        }
    }
}

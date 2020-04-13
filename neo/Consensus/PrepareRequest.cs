using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.IO;
using System.Linq;

namespace Neo.Consensus
{
    public class PrepareRequest : ConsensusMessage
    {
        public uint Timestamp;
        public ulong Nonce;
        public UInt160 NextConsensus;
        public UInt256[] TransactionHashes;
        public MinerTransaction MinerTransaction;

        public uint Version;
        public uint Index;
        public UInt256 PreHash;
        public UInt256 Root;

        public override int Size => base.Size
            + sizeof(uint)                      //Timestamp
            + sizeof(ulong)                     //Nonce
            + NextConsensus.Size                //NextConsensus
            + TransactionHashes.GetVarSize()    //TransactionHashes
            + MinerTransaction.Size             //MinerTransaction

            + sizeof(uint)                      //Version
            + sizeof(uint)                      //Index
            + PreHash.Size                      //PreHash
            + Root.Size;                  //StateRoot_

        public PrepareRequest()
            : base(ConsensusMessageType.PrepareRequest)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Timestamp = reader.ReadUInt32();
            Nonce = reader.ReadUInt64();
            NextConsensus = reader.ReadSerializable<UInt160>();
            TransactionHashes = reader.ReadSerializableArray<UInt256>(Block.MaxTransactionsPerBlock);
            if (TransactionHashes.Distinct().Count() != TransactionHashes.Length)
                throw new FormatException();
            MinerTransaction = reader.ReadSerializable<MinerTransaction>();
            if (MinerTransaction.Hash != TransactionHashes[0])
                throw new FormatException();

            Version = reader.ReadUInt32();
            Index = reader.ReadUInt32();
            PreHash = reader.ReadSerializable<UInt256>();
            Root = reader.ReadSerializable<UInt256>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Timestamp);
            writer.Write(Nonce);
            writer.Write(NextConsensus);
            writer.Write(TransactionHashes);
            writer.Write(MinerTransaction);

            writer.Write(Version);
            writer.Write(Index);
            writer.Write(PreHash);
            writer.Write(Root);
        }
    }
}

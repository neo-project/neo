using System;
using System.IO;
using System.Linq;
using Neo.IO;
using Neo.Network.P2P.Payloads;

namespace Neo.Consensus
{
    internal class RecoveryMessage : ConsensusMessage
    {
        public byte[][] ChangeViewWitnessInvocationScripts;
        public uint[] ChangeViewTimestamps;
        public byte[] OriginalChangeViewNumbers;

        // The following 4 fields are to be able to regenerate the PrepareRequest.
        public UInt256[] TransactionHashes;
        // The following 3 fields are not serialized if TransactionHashes is null, indicating there is
        // no PrepareRequest present in the RecoveryMessage.
        public ulong Nonce;
        public UInt160 NextConsensus;
        public MinerTransaction MinerTransaction;

        /// The PreparationHash in case the PrepareRequest hasn't been received yet.
        /// This can be null if the PrepareRequest information is present, since it can be derived in that case.
        public UInt256 PreparationHash;
        public byte[][] PrepareWitnessInvocationScripts;
        public uint[] PrepareTimestamps;
        public byte[][] CommitSignatures;

        public RecoveryMessage() : base(ConsensusMessageType.RecoveryMessage)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ChangeViewWitnessInvocationScripts = reader.ReadVarBytesArray();
            if (ChangeViewWitnessInvocationScripts != null)
            {
                ChangeViewTimestamps = reader.ReadUIntArray(ChangeViewWitnessInvocationScripts.Length);
                OriginalChangeViewNumbers = reader.ReadVarBytes(ConsensusService.MaxValidatorsCount);
            }
            var txHashCount = reader.ReadVarInt(ConsensusService.MaxTransactionsPerBlock);
            if (txHashCount == 0)
            {
                TransactionHashes = null;
                Nonce = 0;
                NextConsensus = null;
                MinerTransaction = null;
            }
            else
            {
                TransactionHashes = new UInt256[txHashCount];
                for (int i = 0; i < TransactionHashes.Length; i++)
                {
                    TransactionHashes[i] = new UInt256();
                    ((ISerializable) TransactionHashes[i]).Deserialize(reader);
                }
                Nonce = reader.ReadUInt64();
                NextConsensus = reader.ReadSerializable<UInt160>();

                if (TransactionHashes.Distinct().Count() != TransactionHashes.Length)
                    throw new FormatException();
                MinerTransaction = reader.ReadSerializable<MinerTransaction>();
                if (MinerTransaction.Hash != TransactionHashes[0])
                    throw new FormatException();
            }
            PreparationHash = reader.ReadVarInt(32) == 0 ? null : reader.ReadSerializable<UInt256>();
            PrepareWitnessInvocationScripts = reader.ReadVarBytesArray(ConsensusService.MaxValidatorsCount);
            PrepareTimestamps = reader.ReadUIntArray(PrepareWitnessInvocationScripts.Length);
            CommitSignatures = reader.ReadVarBytesArray(ConsensusService.MaxValidatorsCount);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytesArray(ChangeViewWitnessInvocationScripts);
            if (ChangeViewWitnessInvocationScripts != null)
            {
                writer.Write(ChangeViewTimestamps);
                writer.WriteVarBytes(OriginalChangeViewNumbers);
            }
            if (TransactionHashes == null)
                writer.WriteVarInt(0);
            else
            {
                writer.Write(TransactionHashes);
                writer.Write(Nonce);
                writer.Write(NextConsensus);
                writer.Write(MinerTransaction);
            }
            if (PreparationHash == null)
                writer.WriteVarInt(0);
            else
            {
                writer.WriteVarInt(PreparationHash.Size);
                writer.Write(PreparationHash);
            }
            writer.WriteVarBytesArray(PrepareWitnessInvocationScripts);
            writer.Write(PrepareTimestamps);
            writer.WriteVarBytesArray(CommitSignatures);
        }
    }
}

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

        public RecoveryMessage() : base(ConsensusMessageType.RecoveryMessage)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ChangeViewWitnessInvocationScripts = new byte[reader.ReadVarInt(255)][];
            for (int i = 0; i < ChangeViewWitnessInvocationScripts.Length; i++)
            {
                int signatureBytes = (int) reader.ReadVarInt(1024);
                if (signatureBytes == 0)
                    ChangeViewWitnessInvocationScripts[i] = null;
                else
                    ChangeViewWitnessInvocationScripts[i] = reader.ReadBytes(signatureBytes);
            }

            var txHashCount = reader.ReadVarInt(ushort.MaxValue);
            if (txHashCount == 0)
                TransactionHashes = null;
            else
            {
                TransactionHashes = new UInt256[txHashCount];
                for (int i = 0; i < TransactionHashes.Length; i++)
                {
                    TransactionHashes[i] = new UInt256();
                    ((ISerializable) TransactionHashes[i]).Deserialize(reader);
                }
                TransactionHashes = reader.ReadSerializableArray<UInt256>(ushort.MaxValue);
                Nonce = reader.ReadUInt64();
                NextConsensus = reader.ReadSerializable<UInt160>();

                if (TransactionHashes.Distinct().Count() != TransactionHashes.Length)
                    throw new FormatException();
                MinerTransaction = reader.ReadSerializable<MinerTransaction>();
                if (MinerTransaction.Hash != TransactionHashes[0])
                    throw new FormatException();
            }

            PreparationHash = reader.ReadVarInt(32) == 0 ? null : reader.ReadSerializable<UInt256>();
            PrepareWitnessInvocationScripts = new byte[reader.ReadVarInt(255)][];
            for (int i = 0; i < PrepareWitnessInvocationScripts.Length; i++)
            {
                int signatureBytes = (int) reader.ReadVarInt(1024);
                if (signatureBytes == 0)
                    PrepareWitnessInvocationScripts[i] = null;
                else
                    PrepareWitnessInvocationScripts[i] = reader.ReadBytes(signatureBytes);
            }
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarInt(ChangeViewWitnessInvocationScripts.Length);
            foreach (var witnessInvocationScript in ChangeViewWitnessInvocationScripts)
            {
                if (witnessInvocationScript == null)
                    writer.WriteVarInt(0);
                else
                    writer.WriteVarBytes(witnessInvocationScript);
            }

            writer.Write(TransactionHashes);
            if (TransactionHashes.Length > 0)
            {
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
            writer.WriteVarInt(PrepareWitnessInvocationScripts.Length);
            foreach (var witnessInvocationScript in PrepareWitnessInvocationScripts)
            {
                if (witnessInvocationScript == null)
                    writer.WriteVarInt(0);
                else
                    writer.WriteVarBytes(witnessInvocationScript);
            }
        }
    }
}
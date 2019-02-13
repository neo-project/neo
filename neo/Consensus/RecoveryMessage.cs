using System.IO;
using Neo.IO;

namespace Neo.Consensus
{
    internal class RecoveryMessage : ConsensusMessage
    {
        public byte[][] ChangeViewWitnessInvocationScripts;
        public uint[] ChangeViewTimestamps;
        public byte[] OriginalChangeViewNumbers;
        public PrepareRequest PrepareRequestMessage;

        /// The PreparationHash in case the PrepareRequest hasn't been received yet.
        /// This can be null if the PrepareRequest information is present, since it can be derived in that case.
        public UInt256 PreparationHash;
        public byte[][] PrepareWitnessInvocationScripts;
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

            if (reader.ReadBoolean()) PrepareRequestMessage = reader.ReadSerializable<PrepareRequest>();
            PreparationHash = reader.ReadVarInt(32) == 0 ? null : reader.ReadSerializable<UInt256>();
            PrepareWitnessInvocationScripts = reader.ReadVarBytesArray(ConsensusService.MaxValidatorsCount);
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

            bool hasPrepareRequestMessage = PrepareRequestMessage != null;
            writer.Write(hasPrepareRequestMessage);
            if (hasPrepareRequestMessage)
                writer.Write(PrepareRequestMessage);
            if (PreparationHash == null)
                writer.WriteVarInt(0);
            else
            {
                writer.WriteVarInt(PreparationHash.Size);
                writer.Write(PreparationHash);
            }
            writer.WriteVarBytesArray(PrepareWitnessInvocationScripts);
            writer.WriteVarBytesArray(CommitSignatures);
        }
    }
}

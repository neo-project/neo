using Neo.IO;
using Neo.Ledger;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Consensus
{
    internal partial class RecoveryMessage : ConsensusMessage
    {
        public Dictionary<ushort, ChangeViewPayloadCompact> ChangeViewMessages;
        public PrepareRequest PrepareRequestMessage;
        /// The PreparationHash in case the PrepareRequest hasn't been received yet.
        /// This can be null if the PrepareRequest information is present, since it can be derived in that case.
        public UInt256 PreparationHash;
        public Dictionary<ushort, PreparationPayloadCompact> PreparationMessages;
        public Dictionary<ushort, CommitPayloadCompact> CommitMessages;

        public RecoveryMessage() : base(ConsensusMessageType.RecoveryMessage)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ChangeViewMessages = reader.ReadSerializableArray<ChangeViewPayloadCompact>(Blockchain.MaxValidators).ToDictionary(p => p.ValidatorIndex);
            if (reader.ReadBoolean())
                PrepareRequestMessage = reader.ReadSerializable<PrepareRequest>();
            else
                PreparationHash = reader.ReadSerializable<UInt256>();
            PreparationMessages = reader.ReadSerializableArray<PreparationPayloadCompact>(Blockchain.MaxValidators).ToDictionary(p => p.ValidatorIndex);
            CommitMessages = reader.ReadSerializableArray<CommitPayloadCompact>(Blockchain.MaxValidators).ToDictionary(p => p.ValidatorIndex);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ChangeViewMessages.Values.ToArray());
            bool hasPrepareRequestMessage = PrepareRequestMessage != null;
            writer.Write(hasPrepareRequestMessage);
            if (hasPrepareRequestMessage)
                writer.Write(PrepareRequestMessage);
            else
                writer.Write(PreparationHash);
            writer.Write(PreparationMessages.Values.ToArray());
            writer.Write(CommitMessages.Values.ToArray());
        }
    }
}

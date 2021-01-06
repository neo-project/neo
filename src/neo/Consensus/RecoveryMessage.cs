using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Consensus
{
    public partial class RecoveryMessage : ConsensusMessage
    {
        public Dictionary<int, ChangeViewPayloadCompact> ChangeViewMessages;
        public PrepareRequest PrepareRequestMessage;
        /// The PreparationHash in case the PrepareRequest hasn't been received yet.
        /// This can be null if the PrepareRequest information is present, since it can be derived in that case.
        public UInt256 PreparationHash;
        public Dictionary<int, PreparationPayloadCompact> PreparationMessages;
        public Dictionary<int, CommitPayloadCompact> CommitMessages;

        public override int Size => base.Size
            + /* ChangeViewMessages */ ChangeViewMessages?.Values.GetVarSize() ?? 0
            + /* PrepareRequestMessage */ 1 + PrepareRequestMessage?.Size ?? 0
            + /* PreparationHash */ PreparationHash?.Size ?? 0
            + /* PreparationMessages */ PreparationMessages?.Values.GetVarSize() ?? 0
            + /* CommitMessages */ CommitMessages?.Values.GetVarSize() ?? 0;

        public RecoveryMessage() : base(ConsensusMessageType.RecoveryMessage)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ChangeViewMessages = reader.ReadSerializableArray<ChangeViewPayloadCompact>(ProtocolSettings.Default.ValidatorsCount).ToDictionary(p => (int)p.ValidatorIndex);
            if (reader.ReadBoolean())
                PrepareRequestMessage = reader.ReadSerializable<PrepareRequest>();
            else
            {
                int preparationHashSize = UInt256.Zero.Size;
                if (preparationHashSize == (int)reader.ReadVarInt((ulong)preparationHashSize))
                    PreparationHash = new UInt256(reader.ReadFixedBytes(preparationHashSize));
            }

            PreparationMessages = reader.ReadSerializableArray<PreparationPayloadCompact>(ProtocolSettings.Default.ValidatorsCount).ToDictionary(p => (int)p.ValidatorIndex);
            CommitMessages = reader.ReadSerializableArray<CommitPayloadCompact>(ProtocolSettings.Default.ValidatorsCount).ToDictionary(p => (int)p.ValidatorIndex);
        }

        internal ExtensiblePayload[] GetChangeViewPayloads(ConsensusContext context)
        {
            return ChangeViewMessages.Values.Select(p => context.CreatePayload(new ChangeView
            {
                BlockIndex = BlockIndex,
                ValidatorIndex = p.ValidatorIndex,
                ViewNumber = p.OriginalViewNumber,
                Timestamp = p.Timestamp
            }, p.InvocationScript)).ToArray();
        }

        internal ExtensiblePayload[] GetCommitPayloadsFromRecoveryMessage(ConsensusContext context)
        {
            return CommitMessages.Values.Select(p => context.CreatePayload(new Commit
            {
                BlockIndex = BlockIndex,
                ValidatorIndex = p.ValidatorIndex,
                ViewNumber = p.ViewNumber,
                Signature = p.Signature
            }, p.InvocationScript)).ToArray();
        }

        internal ExtensiblePayload GetPrepareRequestPayload(ConsensusContext context)
        {
            if (PrepareRequestMessage == null) return null;
            if (!PreparationMessages.TryGetValue(context.Block.ConsensusData.PrimaryIndex, out PreparationPayloadCompact compact))
                return null;
            return context.CreatePayload(PrepareRequestMessage, compact.InvocationScript);
        }

        internal ExtensiblePayload[] GetPrepareResponsePayloads(ConsensusContext context)
        {
            UInt256 preparationHash = PreparationHash ?? context.PreparationPayloads[context.Block.ConsensusData.PrimaryIndex]?.Hash;
            if (preparationHash is null) return Array.Empty<ExtensiblePayload>();
            return PreparationMessages.Values.Where(p => p.ValidatorIndex != context.Block.ConsensusData.PrimaryIndex).Select(p => context.CreatePayload(new PrepareResponse
            {
                BlockIndex = BlockIndex,
                ValidatorIndex = p.ValidatorIndex,
                ViewNumber = ViewNumber,
                PreparationHash = preparationHash
            }, p.InvocationScript)).ToArray();
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
            {
                if (PreparationHash == null)
                    writer.WriteVarInt(0);
                else
                    writer.WriteVarBytes(PreparationHash.ToArray());
            }

            writer.Write(PreparationMessages.Values.ToArray());
            writer.Write(CommitMessages.Values.ToArray());
        }
    }
}

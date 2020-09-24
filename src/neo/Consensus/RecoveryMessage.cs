using Neo.IO;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
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
            + /* PreparationHash */ (PreparationHash != null ? UInt256.Length : 0)
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
                int preparationHashSize = UInt256.Length;
                if (preparationHashSize == (int)reader.ReadVarInt((ulong)preparationHashSize))
                    PreparationHash = new UInt256(reader.ReadFixedBytes(preparationHashSize));
            }

            PreparationMessages = reader.ReadSerializableArray<PreparationPayloadCompact>(ProtocolSettings.Default.ValidatorsCount).ToDictionary(p => (int)p.ValidatorIndex);
            CommitMessages = reader.ReadSerializableArray<CommitPayloadCompact>(ProtocolSettings.Default.ValidatorsCount).ToDictionary(p => (int)p.ValidatorIndex);
        }

        internal ConsensusPayload[] GetChangeViewPayloads(ConsensusContext context, ConsensusPayload payload)
        {
            return ChangeViewMessages.Values.Select(p => new ConsensusPayload
            {
                Version = payload.Version,
                PrevHash = payload.PrevHash,
                BlockIndex = payload.BlockIndex,
                ValidatorIndex = p.ValidatorIndex,
                ConsensusMessage = new ChangeView
                {
                    ViewNumber = p.OriginalViewNumber,
                    Timestamp = p.Timestamp
                },
                Witness = new Witness
                {
                    InvocationScript = p.InvocationScript,
                    VerificationScript = Contract.CreateSignatureRedeemScript(context.Validators[p.ValidatorIndex])
                }
            }).ToArray();
        }

        internal ConsensusPayload[] GetCommitPayloadsFromRecoveryMessage(ConsensusContext context, ConsensusPayload payload)
        {
            return CommitMessages.Values.Select(p => new ConsensusPayload
            {
                Version = payload.Version,
                PrevHash = payload.PrevHash,
                BlockIndex = payload.BlockIndex,
                ValidatorIndex = p.ValidatorIndex,
                ConsensusMessage = new Commit
                {
                    ViewNumber = p.ViewNumber,
                    Signature = p.Signature
                },
                Witness = new Witness
                {
                    InvocationScript = p.InvocationScript,
                    VerificationScript = Contract.CreateSignatureRedeemScript(context.Validators[p.ValidatorIndex])
                }
            }).ToArray();
        }

        internal ConsensusPayload GetPrepareRequestPayload(ConsensusContext context, ConsensusPayload payload)
        {
            if (PrepareRequestMessage == null) return null;
            if (!PreparationMessages.TryGetValue(context.Block.ConsensusData.PrimaryIndex, out PreparationPayloadCompact compact))
                return null;
            return new ConsensusPayload
            {
                Version = payload.Version,
                PrevHash = payload.PrevHash,
                BlockIndex = payload.BlockIndex,
                ValidatorIndex = context.Block.ConsensusData.PrimaryIndex,
                ConsensusMessage = PrepareRequestMessage,
                Witness = new Witness
                {
                    InvocationScript = compact.InvocationScript,
                    VerificationScript = Contract.CreateSignatureRedeemScript(context.Validators[context.Block.ConsensusData.PrimaryIndex])
                }
            };
        }

        internal ConsensusPayload[] GetPrepareResponsePayloads(ConsensusContext context, ConsensusPayload payload)
        {
            UInt256 preparationHash = PreparationHash ?? context.PreparationPayloads[context.Block.ConsensusData.PrimaryIndex]?.Hash;
            if (preparationHash is null) return new ConsensusPayload[0];
            return PreparationMessages.Values.Where(p => p.ValidatorIndex != context.Block.ConsensusData.PrimaryIndex).Select(p => new ConsensusPayload
            {
                Version = payload.Version,
                PrevHash = payload.PrevHash,
                BlockIndex = payload.BlockIndex,
                ValidatorIndex = p.ValidatorIndex,
                ConsensusMessage = new PrepareResponse
                {
                    ViewNumber = ViewNumber,
                    PreparationHash = preparationHash
                },
                Witness = new Witness
                {
                    InvocationScript = p.InvocationScript,
                    VerificationScript = Contract.CreateSignatureRedeemScript(context.Validators[p.ValidatorIndex])
                }
            }).ToArray();
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

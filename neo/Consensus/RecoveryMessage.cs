using Neo.IO;
using Neo.Ledger;
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
            + /* PreparationHash */ PreparationHash?.Size ?? 0
            + /* PreparationMessages */ PreparationMessages?.Values.GetVarSize() ?? 0
            + /* CommitMessages */ CommitMessages?.Values.GetVarSize() ?? 0;

        public RecoveryMessage() : base(ConsensusMessageType.RecoveryMessage)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ChangeViewMessages = reader.ReadSerializableArray<ChangeViewPayloadCompact>(Blockchain.MaxValidators).ToDictionary(p => (int)p.ValidatorIndex);
            if (reader.ReadBoolean())
                PrepareRequestMessage = reader.ReadSerializable<PrepareRequest>();
            else
            {
                int preparationHashSize = UInt256.Zero.Size;
                if (preparationHashSize == (int)reader.ReadVarInt((ulong)preparationHashSize))
                    PreparationHash = new UInt256(reader.ReadBytes(preparationHashSize));
            }

            PreparationMessages = reader.ReadSerializableArray<PreparationPayloadCompact>(Blockchain.MaxValidators).ToDictionary(p => (int)p.ValidatorIndex);
            CommitMessages = reader.ReadSerializableArray<CommitPayloadCompact>(Blockchain.MaxValidators).ToDictionary(p => (int)p.ValidatorIndex);
        }

        public ConsensusPayload[] GetChangeViewPayloads(IConsensusContext context, ConsensusPayload payload)
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

        public ConsensusPayload[] GetCommitPayloadsFromRecoveryMessage(IConsensusContext context, ConsensusPayload payload)
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

        public ConsensusPayload GetPrepareRequestPayload(IConsensusContext context, ConsensusPayload payload)
        {
            if (PrepareRequestMessage == null) return null;
            if (!PreparationMessages.TryGetValue((int)context.PrimaryIndex, out RecoveryMessage.PreparationPayloadCompact compact))
                return null;
            return new ConsensusPayload
            {
                Version = payload.Version,
                PrevHash = payload.PrevHash,
                BlockIndex = payload.BlockIndex,
                ValidatorIndex = (ushort)context.PrimaryIndex,
                ConsensusMessage = PrepareRequestMessage,
                Witness = new Witness
                {
                    InvocationScript = compact.InvocationScript,
                    VerificationScript = Contract.CreateSignatureRedeemScript(context.Validators[context.PrimaryIndex])
                }
            };
        }

        public ConsensusPayload[] GetPrepareResponsePayloads(IConsensusContext context, ConsensusPayload payload)
        {
            UInt256 preparationHash = PreparationHash ?? context.PreparationPayloads[context.PrimaryIndex]?.Hash;
            if (preparationHash is null) return new ConsensusPayload[0];
            return PreparationMessages.Values.Where(p => p.ValidatorIndex != context.PrimaryIndex).Select(p => new ConsensusPayload
            {
                Version = payload.Version,
                PrevHash = payload.PrevHash,
                BlockIndex = payload.BlockIndex,
                ValidatorIndex = p.ValidatorIndex,
                ConsensusMessage = new PrepareResponse
                {
                    ViewNumber = ViewNumber,
                    PreparationHash = preparationHash,
                    StateRootSignature = p.StateRootSignature
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

// Copyright (C) 2015-2024 The Neo Project.
//
// RecoveryMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Messages
{
    public partial class RecoveryMessage : ConsensusMessage
    {
        public Dictionary<byte, ChangeViewPayloadCompact> ChangeViewMessages;
        public PrepareRequest PrepareRequestMessage;
        /// The PreparationHash in case the PrepareRequest hasn't been received yet.
        /// This can be null if the PrepareRequest information is present, since it can be derived in that case.
        public UInt256 PreparationHash;
        public Dictionary<byte, PreparationPayloadCompact> PreparationMessages;
        public Dictionary<byte, CommitPayloadCompact> CommitMessages;

        public override int Size => base.Size
            + /* ChangeViewMessages */ ChangeViewMessages?.Values.GetVarSize() ?? 0
            + /* PrepareRequestMessage */ 1 + PrepareRequestMessage?.Size ?? 0
            + /* PreparationHash */ PreparationHash?.Size ?? 0
            + /* PreparationMessages */ PreparationMessages?.Values.GetVarSize() ?? 0
            + /* CommitMessages */ CommitMessages?.Values.GetVarSize() ?? 0;

        public RecoveryMessage() : base(ConsensusMessageType.RecoveryMessage) { }

        public override void Deserialize(ref MemoryReader reader)
        {
            base.Deserialize(ref reader);
            ChangeViewMessages = reader.ReadSerializableArray<ChangeViewPayloadCompact>(byte.MaxValue).ToDictionary(p => p.ValidatorIndex);
            if (reader.ReadBoolean())
            {
                PrepareRequestMessage = reader.ReadSerializable<PrepareRequest>();
            }
            else
            {
                int preparationHashSize = UInt256.Zero.Size;
                if (preparationHashSize == (int)reader.ReadVarInt((ulong)preparationHashSize))
                    PreparationHash = new UInt256(reader.ReadMemory(preparationHashSize).Span);
            }

            PreparationMessages = reader.ReadSerializableArray<PreparationPayloadCompact>(byte.MaxValue).ToDictionary(p => p.ValidatorIndex);
            CommitMessages = reader.ReadSerializableArray<CommitPayloadCompact>(byte.MaxValue).ToDictionary(p => p.ValidatorIndex);
        }

        public override bool Verify(ProtocolSettings protocolSettings)
        {
            if (!base.Verify(protocolSettings)) return false;
            return (PrepareRequestMessage is null || PrepareRequestMessage.Verify(protocolSettings))
                && ChangeViewMessages.Values.All(p => p.ValidatorIndex < protocolSettings.ValidatorsCount)
                && PreparationMessages.Values.All(p => p.ValidatorIndex < protocolSettings.ValidatorsCount)
                && CommitMessages.Values.All(p => p.ValidatorIndex < protocolSettings.ValidatorsCount);
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
            if (!PreparationMessages.TryGetValue(context.Block.PrimaryIndex, out PreparationPayloadCompact compact))
                return null;
            return context.CreatePayload(PrepareRequestMessage, compact.InvocationScript);
        }

        internal ExtensiblePayload[] GetPrepareResponsePayloads(ConsensusContext context)
        {
            UInt256 preparationHash = PreparationHash ?? context.PreparationPayloads[context.Block.PrimaryIndex]?.Hash;
            if (preparationHash is null) return Array.Empty<ExtensiblePayload>();
            return PreparationMessages.Values.Where(p => p.ValidatorIndex != context.Block.PrimaryIndex).Select(p => context.CreatePayload(new PrepareResponse
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

// Copyright (C) 2015-2025 The Neo Project.
//
// ConsensusContext.Get.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.SmartContract;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Plugins.DBFTPlugin.Consensus
{
    partial class ConsensusContext
    {
        public ConsensusMessage GetMessage(ExtensiblePayload payload)
        {
            if (payload is null) return null;
            if (!cachedMessages.TryGetValue(payload.Hash, out ConsensusMessage message))
                cachedMessages.Add(payload.Hash, message = ConsensusMessage.DeserializeFrom(payload.Data));
            return message;
        }

        public T GetMessage<T>(ExtensiblePayload payload) where T : ConsensusMessage
        {
            return (T)GetMessage(payload);
        }

        private RecoveryMessage.ChangeViewPayloadCompact GetChangeViewPayloadCompact(ExtensiblePayload payload)
        {
            ChangeView message = GetMessage<ChangeView>(payload);
            return new RecoveryMessage.ChangeViewPayloadCompact
            {
                ValidatorIndex = message.ValidatorIndex,
                OriginalViewNumber = message.ViewNumber,
                Timestamp = message.Timestamp,
                InvocationScript = payload.Witness.InvocationScript
            };
        }

        private RecoveryMessage.CommitPayloadCompact GetCommitPayloadCompact(ExtensiblePayload payload)
        {
            Commit message = GetMessage<Commit>(payload);
            return new RecoveryMessage.CommitPayloadCompact
            {
                ViewNumber = message.ViewNumber,
                ValidatorIndex = message.ValidatorIndex,
                Signature = message.Signature,
                InvocationScript = payload.Witness.InvocationScript
            };
        }

        private RecoveryMessage.PreparationPayloadCompact GetPreparationPayloadCompact(ExtensiblePayload payload)
        {
            return new RecoveryMessage.PreparationPayloadCompact
            {
                ValidatorIndex = GetMessage(payload).ValidatorIndex,
                InvocationScript = payload.Witness.InvocationScript
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetPrimaryIndex(byte viewNumber)
        {
            int p = ((int)Block.Index - viewNumber) % Validators.Length;
            return p >= 0 ? (byte)p : (byte)(p + Validators.Length);
        }

        public UInt160 GetSender(int index)
        {
            return Contract.CreateSignatureRedeemScript(Validators[index]).ToScriptHash();
        }

        /// <summary>
        /// Return the expected block size
        /// </summary>
        public int GetExpectedBlockSize()
        {
            return GetExpectedBlockSizeWithoutTransactions(Transactions.Count) + // Base size
                Transactions.Values.Sum(u => u.Size);   // Sum Txs
        }

        /// <summary>
        /// Return the expected block system fee
        /// </summary>
        public long GetExpectedBlockSystemFee()
        {
            return Transactions.Values.Sum(u => u.SystemFee);  // Sum Txs
        }

        /// <summary>
        /// Return the expected block size without txs
        /// </summary>
        /// <param name="expectedTransactions">Expected transactions</param>
        internal int GetExpectedBlockSizeWithoutTransactions(int expectedTransactions)
        {
            return
                sizeof(uint) +      // Version
                UInt256.Length +    // PrevHash
                UInt256.Length +    // MerkleRoot
                sizeof(ulong) +     // Timestamp
                sizeof(ulong) +     // Nonce
                sizeof(uint) +      // Index
                sizeof(byte) +      // PrimaryIndex
                UInt160.Length +    // NextConsensus
                1 + _witnessSize +  // Witness
                expectedTransactions.GetVarSize();
        }
    }
}

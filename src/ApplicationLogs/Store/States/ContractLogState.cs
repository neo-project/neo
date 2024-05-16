// Copyright (C) 2015-2024 The Neo Project.
//
// ContractLogState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract;

namespace ApplicationLogs.Store.States
{
    public class ContractLogState : NotifyLogState, IEquatable<ContractLogState>
    {
        public UInt256 TransactionHash { get; private set; } = new();
        public TriggerType Trigger { get; private set; } = TriggerType.All;

        public static ContractLogState Create(Blockchain.ApplicationExecuted applicationExecuted, NotifyEventArgs notifyEventArgs, Guid[] stackItemIds) =>
            new()
            {
                TransactionHash = applicationExecuted.Transaction?.Hash ?? new(),
                ScriptHash = notifyEventArgs.ScriptHash,
                Trigger = applicationExecuted.Trigger,
                EventName = notifyEventArgs.EventName,
                StackItemIds = stackItemIds,
            };

        #region ISerializable

        public override int Size =>
            TransactionHash.Size +
            sizeof(byte) +
            base.Size;

        public override void Deserialize(ref MemoryReader reader)
        {
            TransactionHash.Deserialize(ref reader);
            Trigger = (TriggerType)reader.ReadByte();
            base.Deserialize(ref reader);
        }

        public override void Serialize(BinaryWriter writer)
        {
            TransactionHash.Serialize(writer);
            writer.Write((byte)Trigger);
            base.Serialize(writer);
        }

        #endregion

        #region IEquatable

        public bool Equals(ContractLogState other) =>
            Trigger == other.Trigger && EventName == other.EventName &&
            TransactionHash == other.TransactionHash && StackItemIds.SequenceEqual(other.StackItemIds);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as ContractLogState);
        }

        public override int GetHashCode() =>
            HashCode.Combine(TransactionHash, Trigger, base.GetHashCode());

        #endregion
    }
}

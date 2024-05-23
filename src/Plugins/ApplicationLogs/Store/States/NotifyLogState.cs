// Copyright (C) 2015-2024 The Neo Project.
//
// NotifyLogState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.IO;
using Neo.SmartContract;

namespace ApplicationLogs.Store.States
{
    public class NotifyLogState : ISerializable, IEquatable<NotifyLogState>
    {
        public UInt160 ScriptHash { get; protected set; } = new();
        public string EventName { get; protected set; } = string.Empty;
        public Guid[] StackItemIds { get; protected set; } = [];

        public static NotifyLogState Create(NotifyEventArgs notifyItem, Guid[] stackItemsIds) =>
            new()
            {
                ScriptHash = notifyItem.ScriptHash,
                EventName = notifyItem.EventName,
                StackItemIds = stackItemsIds,
            };

        #region ISerializable

        public virtual int Size =>
            ScriptHash.Size +
            EventName.GetVarSize() +
            StackItemIds.Sum(s => s.ToByteArray().GetVarSize());

        public virtual void Deserialize(ref MemoryReader reader)
        {
            ScriptHash.Deserialize(ref reader);
            EventName = reader.ReadVarString();

            // It should be safe because it filled from a transaction's notifications.
            uint aLen = reader.ReadUInt32();
            StackItemIds = new Guid[aLen];
            for (var i = 0; i < aLen; i++)
                StackItemIds[i] = new Guid(reader.ReadVarMemory().Span);
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            ScriptHash.Serialize(writer);
            writer.WriteVarString(EventName ?? string.Empty);

            writer.Write((uint)StackItemIds.Length);
            for (var i = 0; i < StackItemIds.Length; i++)
                writer.WriteVarBytes(StackItemIds[i].ToByteArray());
        }

        #endregion

        #region IEquatable

        public bool Equals(NotifyLogState other) =>
            EventName == other.EventName && ScriptHash == other.ScriptHash &&
            StackItemIds.SequenceEqual(other.StackItemIds);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as NotifyLogState);
        }

        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(ScriptHash);
            h.Add(EventName);
            foreach (var id in StackItemIds)
                h.Add(id);
            return h.ToHashCode();
        }

        #endregion
    }
}

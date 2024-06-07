// Copyright (C) 2015-2024 The Neo Project.
//
// TransactionLogState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;

namespace Neo.Plugins.ApplicationLogs.Store.States
{
    public class TransactionLogState : ISerializable, IEquatable<TransactionLogState>
    {
        public Guid[] NotifyLogIds { get; private set; } = Array.Empty<Guid>();

        public static TransactionLogState Create(Guid[] notifyLogIds) =>
            new()
            {
                NotifyLogIds = notifyLogIds,
            };

        #region ISerializable

        public virtual int Size =>
            sizeof(uint) +
            NotifyLogIds.Sum(s => s.ToByteArray().GetVarSize());

        public virtual void Deserialize(ref MemoryReader reader)
        {
            // It should be safe because it filled from a transaction's notifications.
            uint aLen = reader.ReadUInt32();
            NotifyLogIds = new Guid[aLen];
            for (int i = 0; i < aLen; i++)
                NotifyLogIds[i] = new Guid(reader.ReadVarMemory().Span);
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write((uint)NotifyLogIds.Length);
            for (int i = 0; i < NotifyLogIds.Length; i++)
                writer.WriteVarBytes(NotifyLogIds[i].ToByteArray());
        }

        #endregion

        #region IEquatable

        public bool Equals(TransactionLogState other) =>
            NotifyLogIds.SequenceEqual(other.NotifyLogIds);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as TransactionLogState);
        }

        public override int GetHashCode()
        {
            var h = new HashCode();
            foreach (var id in NotifyLogIds)
                h.Add(id);
            return h.ToHashCode();
        }

        #endregion
    }
}

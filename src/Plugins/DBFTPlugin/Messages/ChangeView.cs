// Copyright (C) 2015-2025 The Neo Project.
//
// ChangeView.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Plugins.DBFTPlugin.Types;
using System.IO;

namespace Neo.Plugins.DBFTPlugin.Messages
{
    public class ChangeView : ConsensusMessage
    {
        /// <summary>
        /// NewViewNumber is always set to the current ViewNumber asking changeview + 1
        /// </summary>
        public byte NewViewNumber => (byte)(ViewNumber + 1);

        /// <summary>
        /// Timestamp of when the ChangeView message was created. This allows receiving nodes to ensure
        /// they only respond once to a specific ChangeView request (it thus prevents replay of the ChangeView
        /// message from repeatedly broadcasting RecoveryMessages).
        /// </summary>
        public ulong Timestamp;

        /// <summary>
        /// Reason
        /// </summary>
        public ChangeViewReason Reason;

        public override int Size => base.Size +
            sizeof(ulong) +             // Timestamp
            sizeof(ChangeViewReason);   // Reason

        public ChangeView() : base(ConsensusMessageType.ChangeView) { }

        public override void Deserialize(ref MemoryReader reader)
        {
            base.Deserialize(ref reader);
            Timestamp = reader.ReadUInt64();
            Reason = (ChangeViewReason)reader.ReadByte();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Timestamp);
            writer.Write((byte)Reason);
        }
    }
}

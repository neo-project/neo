// Copyright (C) 2015-2025 The Neo Project.
//
// RecoveryRequest.cs file belongs to the neo project and is free
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
    public class RecoveryRequest : ConsensusMessage
    {
        /// <summary>
        /// Timestamp of when the ChangeView message was created. This allows receiving nodes to ensure
        /// they only respond once to a specific RecoveryRequest request.
        /// In this sense, it prevents replay of the RecoveryRequest message from the repeatedly broadcast of Recovery's messages.
        /// </summary>
        public ulong Timestamp;

        public override int Size => base.Size
            + sizeof(ulong); //Timestamp

        public RecoveryRequest() : base(ConsensusMessageType.RecoveryRequest) { }

        public override void Deserialize(ref MemoryReader reader)
        {
            base.Deserialize(ref reader);
            Timestamp = reader.ReadUInt64();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Timestamp);
        }
    }
}

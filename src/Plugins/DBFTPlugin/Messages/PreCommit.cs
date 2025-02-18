// Copyright (C) 2015-2024 The Neo Project.
//
// PreCommit.cs file belongs to the neo project and is free
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
    public class PreCommit : ConsensusMessage
    {
        public UInt256 PreparationHash;

        // priority or fallback
        public uint PId;
        public override int Size => base.Size + PreparationHash.Size + sizeof(uint);

        public PreCommit() : base(ConsensusMessageType.PreCommit) { }

        public override void Deserialize(ref MemoryReader reader)
        {
            base.Deserialize(ref reader);
            PreparationHash = reader.ReadSerializable<UInt256>();
            PId = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PreparationHash);
            writer.Write(PId);
        }
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// Commit.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.IO;

namespace Neo.Plugins.DBFTPlugin.Messages
{
    public class Commit : ConsensusMessage
    {
        public ReadOnlyMemory<byte> Signature;

        public override int Size => base.Size + Signature.Length;

        public Commit() : base(ConsensusMessageType.Commit) { }

        public override void Deserialize(ref MemoryReader reader)
        {
            base.Deserialize(ref reader);
            Signature = reader.ReadMemory(64);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Signature.Span);
        }
    }
}

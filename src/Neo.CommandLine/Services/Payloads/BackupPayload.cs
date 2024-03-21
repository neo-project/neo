// Copyright (C) 2015-2024 The Neo Project.
//
// BackupPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System.IO;

namespace Neo.CommandLine.Services.Payloads
{
    internal sealed class BackupPayload : ISerializable
    {
        public uint StartBlockIndex { get; private set; } = 0u;
        public uint EndBlockIndex { get; private set; } = uint.MaxValue;

        public static BackupPayload Create(uint start, uint end = uint.MaxValue) =>
            new()
            {
                StartBlockIndex = start,
                EndBlockIndex = end,
            };

        #region ISerializable

        int ISerializable.Size =>
            sizeof(uint) +  // StartBlockIndex
            sizeof(uint);   // EndBlockIndex

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            StartBlockIndex = reader.ReadUInt32();
            EndBlockIndex = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StartBlockIndex);
            writer.Write(EndBlockIndex);
        }

        #endregion
    }
}

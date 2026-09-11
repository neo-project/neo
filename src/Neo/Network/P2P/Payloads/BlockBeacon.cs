// Copyright (C) 2015-2026 The Neo Project.
//
// BlockBeacon.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Fixed-size random beacon committed with a block. Not yet a header field (TC: header vs native).
    /// neo-node will attach this after dBFT aggregation.
    /// </summary>
    public sealed class BlockBeacon : ISerializable
    {
        public byte[] Value
        {
            get;
            set
            {
                if (value is null || value.Length != RandomBeacon.Size)
                    throw new ArgumentException($"Beacon must be {RandomBeacon.Size} bytes.", nameof(value));
                field = value;
            }
        } = new byte[RandomBeacon.Size];

        public int Size => RandomBeacon.Size;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Value = reader.ReadMemory(RandomBeacon.Size).ToArray();
        }
    }
}

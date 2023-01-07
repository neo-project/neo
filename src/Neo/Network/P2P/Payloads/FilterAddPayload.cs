// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
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
    /// This message is sent to update the items for the <see cref="BloomFilter"/>.
    /// </summary>
    public class FilterAddPayload : ISerializable
    {
        /// <summary>
        /// The items to be added.
        /// </summary>
        public ReadOnlyMemory<byte> Data;

        public int Size => Data.GetVarSize();

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            Data = reader.ReadVarMemory(520);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Data.Span);
        }
    }
}

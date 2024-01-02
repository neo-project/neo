// Copyright (C) 2015-2024 The Neo Project.
//
// FilterLoadPayload.cs file belongs to the neo project and is free
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
    /// This message is sent to load the <see cref="BloomFilter"/>.
    /// </summary>
    public class FilterLoadPayload : ISerializable
    {
        /// <summary>
        /// The data of the <see cref="BloomFilter"/>.
        /// </summary>
        public ReadOnlyMemory<byte> Filter;

        /// <summary>
        /// The number of hash functions used by the <see cref="BloomFilter"/>.
        /// </summary>
        public byte K;

        /// <summary>
        /// Used to generate the seeds of the murmur hash functions.
        /// </summary>
        public uint Tweak;

        public int Size => Filter.GetVarSize() + sizeof(byte) + sizeof(uint);

        /// <summary>
        /// Creates a new instance of the <see cref="FilterLoadPayload"/> class.
        /// </summary>
        /// <param name="filter">The fields in the filter will be copied to the payload.</param>
        /// <returns>The created payload.</returns>
        public static FilterLoadPayload Create(BloomFilter filter)
        {
            byte[] buffer = new byte[filter.M / 8];
            filter.GetBits(buffer);
            return new FilterLoadPayload
            {
                Filter = buffer,
                K = (byte)filter.K,
                Tweak = filter.Tweak
            };
        }

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            Filter = reader.ReadVarMemory(36000);
            K = reader.ReadByte();
            if (K > 50) throw new FormatException();
            Tweak = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Filter.Span);
            writer.Write(K);
            writer.Write(Tweak);
        }
    }
}

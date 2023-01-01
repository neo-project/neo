// Copyright (C) 2015-2023 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// This message is sent to request for blocks by hash.
    /// </summary>
    public class GetBlocksPayload : ISerializable
    {
        /// <summary>
        /// The starting hash of the blocks to request.
        /// </summary>
        public UInt256 HashStart;

        /// <summary>
        /// The number of blocks to request.
        /// </summary>
        public short Count;

        public int Size => sizeof(short) + HashStart.Size;

        /// <summary>
        /// Creates a new instance of the <see cref="GetBlocksPayload"/> class.
        /// </summary>
        /// <param name="hash_start">The starting hash of the blocks to request.</param>
        /// <param name="count">The number of blocks to request. Set this parameter to -1 to request as many blocks as possible.</param>
        /// <returns>The created payload.</returns>
        public static GetBlocksPayload Create(UInt256 hash_start, short count = -1)
        {
            return new GetBlocksPayload
            {
                HashStart = hash_start,
                Count = count
            };
        }

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            HashStart = reader.ReadSerializable<UInt256>();
            Count = reader.ReadInt16();
            if (Count < -1 || Count == 0) throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(HashStart);
            writer.Write(Count);
        }
    }
}

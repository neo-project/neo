// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// This message is sent to relay inventories.
    /// </summary>
    public class InvPayload : ISerializable
    {
        /// <summary>
        /// Indicates the maximum number of inventories sent each time.
        /// </summary>
        public const int MaxHashesCount = 500;

        /// <summary>
        /// The type of the inventories.
        /// </summary>
        public InventoryType Type;

        /// <summary>
        /// The hashes of the inventories.
        /// </summary>
        public UInt256[] Hashes;

        public int Size => sizeof(InventoryType) + Hashes.GetVarSize();

        /// <summary>
        /// Creates a new instance of the <see cref="InvPayload"/> class.
        /// </summary>
        /// <param name="type">The type of the inventories.</param>
        /// <param name="hashes">The hashes of the inventories.</param>
        /// <returns>The created payload.</returns>
        public static InvPayload Create(InventoryType type, params UInt256[] hashes)
        {
            return new InvPayload
            {
                Type = type,
                Hashes = hashes
            };
        }

        /// <summary>
        /// Creates a group of the <see cref="InvPayload"/> instance.
        /// </summary>
        /// <param name="type">The type of the inventories.</param>
        /// <param name="hashes">The hashes of the inventories.</param>
        /// <returns>The created payloads.</returns>
        public static IEnumerable<InvPayload> CreateGroup(InventoryType type, UInt256[] hashes)
        {
            for (int i = 0; i < hashes.Length; i += MaxHashesCount)
            {
                int endIndex = i + MaxHashesCount;
                if (endIndex > hashes.Length) endIndex = hashes.Length;
                yield return new InvPayload
                {
                    Type = type,
                    Hashes = hashes[i..endIndex]
                };
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Type = (InventoryType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(InventoryType), Type))
                throw new FormatException();
            Hashes = reader.ReadSerializableArray<UInt256>(MaxHashesCount);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(Hashes);
        }
    }
}

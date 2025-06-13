// Copyright (C) 2015-2025 The Neo Project.
//
// Node.Hash.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using System;
using System.IO;

namespace Neo.Cryptography.MPTTrie
{
    partial class Node
    {
        public static Node NewHash(UInt256 hash)
        {
            ArgumentNullException.ThrowIfNull(hash);
            return new Node
            {
                Type = NodeType.HashNode,
                _hash = hash,
            };
        }

        protected static int HashSize => UInt256.Length;

        private void SerializeHash(BinaryWriter writer)
        {
            writer.Write(_hash);
        }

        private void DeserializeHash(ref MemoryReader reader)
        {
            _hash = reader.ReadSerializable<UInt256>();
        }
    }
}

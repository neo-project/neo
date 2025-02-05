// Copyright (C) 2015-2025 The Neo Project.
//
// Node.Hash.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
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
            if (hash is null) throw new ArgumentNullException(nameof(NewHash));
            var n = new Node
            {
                type = NodeType.HashNode,
                hash = hash,
            };
            return n;
        }

        protected int HashSize => hash.Size;

        private void SerializeHash(BinaryWriter writer)
        {
            writer.Write(hash);
        }

        private void DeserializeHash(ref MemoryReader reader)
        {
            hash = reader.ReadSerializable<UInt256>();
        }
    }
}

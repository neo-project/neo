// Copyright (C) 2015-2024 The Neo Project.
//
// Node.Leaf.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Cryptography.MPTTrie
{
    partial class Node
    {
        public const int MaxValueLength = 3 + ApplicationEngine.MaxStorageValueSize + sizeof(bool);
        public ReadOnlyMemory<byte> Value;

        public static Node NewLeaf(byte[] value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            var n = new Node
            {
                type = NodeType.LeafNode,
                Value = value,
                Reference = 1,
            };
            return n;
        }

        protected int LeafSize => Value.GetVarSize();

        private void SerializeLeaf(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value.Span);
        }

        private void DeserializeLeaf(ref MemoryReader reader)
        {
            Value = reader.ReadVarMemory();
        }
    }
}

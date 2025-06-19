// Copyright (C) 2015-2025 The Neo Project.
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
        public ReadOnlyMemory<byte> Value { get; set; } = ReadOnlyMemory<byte>.Empty;

        public static Node NewLeaf(byte[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new Node
            {
                Type = NodeType.LeafNode,
                Value = value,
                Reference = 1,
            };
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

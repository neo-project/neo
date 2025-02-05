// Copyright (C) 2015-2025 The Neo Project.
//
// Node.Extension.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
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
        public const int MaxKeyLength = (ApplicationEngine.MaxStorageKeySize + sizeof(int)) * 2;
        public ReadOnlyMemory<byte> Key;
        public Node Next;

        public static Node NewExtension(byte[] key, Node next)
        {
            if (key is null || next is null) throw new ArgumentNullException(nameof(NewExtension));
            if (key.Length == 0) throw new InvalidOperationException(nameof(NewExtension));
            var n = new Node
            {
                type = NodeType.ExtensionNode,
                Key = key,
                Next = next,
                Reference = 1,
            };
            return n;
        }

        protected int ExtensionSize => Key.GetVarSize() + Next.SizeAsChild;

        private void SerializeExtension(BinaryWriter writer)
        {
            writer.WriteVarBytes(Key.Span);
            Next.SerializeAsChild(writer);
        }

        private void DeserializeExtension(ref MemoryReader reader)
        {
            Key = reader.ReadVarMemory();
            var n = new Node();
            n.Deserialize(ref reader);
            Next = n;
        }
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// Node.Extension.cs file belongs to the neo project and is free
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
        public const int MaxKeyLength = (ApplicationEngine.MaxStorageKeySize + sizeof(int)) * 2;
        public ReadOnlyMemory<byte> Key { get; set; } = ReadOnlyMemory<byte>.Empty;

        internal Node _next;

        public Node Next
        {
            get => _next;
            set { _next = value; }
        }

        public static Node NewExtension(byte[] key, Node next)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(next);

            if (key.Length == 0) throw new InvalidOperationException(nameof(NewExtension));

            return new Node
            {
                Type = NodeType.ExtensionNode,
                Key = key,
                Next = next,
                Reference = 1,
            };
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

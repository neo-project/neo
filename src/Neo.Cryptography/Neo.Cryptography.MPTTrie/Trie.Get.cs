// Copyright (C) 2015-2024 The Neo Project.
//
// Trie.Get.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.Cryptography.MPTTrie
{
    partial class Trie
    {
        public byte[] this[byte[] key]
        {
            get
            {
                var path = ToNibbles(key);
                if (path.Length == 0)
                    throw new ArgumentException("could not be empty", nameof(key));
                if (path.Length > Node.MaxKeyLength)
                    throw new ArgumentException("exceeds limit", nameof(key));
                var result = TryGet(ref root, path, out var value);
                return result ? value.ToArray() : throw new KeyNotFoundException();
            }
        }

        public bool TryGetValue(byte[] key, out byte[] value)
        {
            value = default;
            var path = ToNibbles(key);
            if (path.Length == 0)
                throw new ArgumentException("could not be empty", nameof(key));
            if (path.Length > Node.MaxKeyLength)
                throw new ArgumentException("exceeds limit", nameof(key));
            var result = TryGet(ref root, path, out var val);
            if (result)
                value = val.ToArray();
            return result;
        }

        private bool TryGet(ref Node node, ReadOnlySpan<byte> path, out ReadOnlySpan<byte> value)
        {
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (path.IsEmpty)
                        {
                            value = node.Value.Span;
                            return true;
                        }
                        break;
                    }
                case NodeType.Empty:
                    break;
                case NodeType.HashNode:
                    {
                        var newNode = cache.Resolve(node.Hash);
                        if (newNode is null) throw new InvalidOperationException("Internal error, can't resolve hash when mpt get");
                        node = newNode;
                        return TryGet(ref node, path, out value);
                    }
                case NodeType.BranchNode:
                    {
                        if (path.IsEmpty)
                        {
                            return TryGet(ref node.Children[Node.BranchChildCount - 1], path, out value);
                        }
                        return TryGet(ref node.Children[path[0]], path[1..], out value);
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.StartsWith(node.Key.Span))
                        {
                            return TryGet(ref node.Next, path[node.Key.Length..], out value);
                        }
                        break;
                    }
            }
            value = default;
            return false;
        }
    }
}

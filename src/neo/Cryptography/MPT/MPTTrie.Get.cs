using Neo.IO;
using System;
using System.Collections.Generic;

namespace Neo.Cryptography.MPT
{
    partial class MPTTrie<TKey, TValue>
    {
        public TValue this[TKey key]
        {
            get
            {
                var path = ToNibbles(key.ToArray());
                if (path.Length == 0) return null;
                var result = TryGet(ref root, path, out var value);
                return result ? value.AsSerializable<TValue>() : null;
            }
        }

        private bool TryGet(ref MPTNode node, ReadOnlySpan<byte> path, out ReadOnlySpan<byte> value)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (path.IsEmpty)
                        {
                            value = leafNode.Value;
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmpty) break;
                        var newNode = Resolve(hashNode.Hash);
                        if (newNode is null) throw new InvalidOperationException("Internal error, can't resolve hash when mpt get");
                        node = newNode;
                        return TryGet(ref node, path, out value);
                    }
                case BranchNode branchNode:
                    {
                        if (path.IsEmpty)
                        {
                            return TryGet(ref branchNode.Children[BranchNode.ChildCount - 1], path, out value);
                        }
                        return TryGet(ref branchNode.Children[path[0]], path[1..], out value);
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.StartsWith(extensionNode.Key))
                        {
                            return TryGet(ref extensionNode.Next, path[extensionNode.Key.Length..], out value);
                        }
                        break;
                    }
            }
            value = default;
            return false;
        }
    }
}

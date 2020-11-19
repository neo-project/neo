using Neo.IO;
using System;

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
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (path.IsEmpty)
                        {
                            value = node.Value;
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
                            return TryGet(ref node.Children[MPTNode.BranchChildCount - 1], path, out value);
                        }
                        return TryGet(ref node.Children[path[0]], path[1..], out value);
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.StartsWith(node.Key))
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

using System;
using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public partial class MPTTrie
    {
        public bool TryGet(ReadOnlySpan<byte> key, out byte[] value)
        {
            var path = ToNibbles(key);
            return TryGet(ref root, path, out value);
        }

        private bool TryGet(ref MPTNode node, ReadOnlySpan<byte> path, out byte[] value)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (path.IsEmpty)
                        {
                            value = (byte[])leafNode.Value.Clone();
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        var newNode = Resolve(hashNode);
                        if (newNode is null)
                            throw new KeyNotFoundException("Internal error, can't resolve hash when mpt get");
                        node = newNode;
                        return TryGet(ref node, path, out value);
                    }
                case BranchNode branchNode:
                    {
                        if (path.IsEmpty)
                            return TryGet(ref branchNode.Children[BranchNode.ChildCount - 1], path, out value);
                        return TryGet(ref branchNode.Children[path[0]], path.Slice(1), out value);
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.StartsWith(extensionNode.Key))
                            return TryGet(ref extensionNode.Next, path.Slice(extensionNode.Key.Length), out value);
                        break;
                    }
            }
            value = default;
            return false;
        }
    }
}

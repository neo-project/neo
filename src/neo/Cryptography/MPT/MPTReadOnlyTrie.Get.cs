using Neo.IO;
using System;

namespace Neo.Cryptography.MPT
{
    public partial class MPTReadOnlyTrie<TKey, TValue>
        where TKey : notnull, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        public TValue Get(TKey key)
        {
            var path = ToNibbles(key.ToArray());
            if (path.Length < 1) return null;
            var result = TryGet(ref root, path, out var value);
            return result ? value.AsSerializable<TValue>() : null;
        }

        private bool TryGet(ref MPTNode node, ReadOnlySpan<byte> path, out ReadOnlySpan<byte> value)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (path.Length < 1)
                        {
                            value = leafNode.Value;
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmpty) break;
                        var newNode = Resolve(hashNode);
                        if (newNode is null) break;
                        node = newNode;
                        return TryGet(ref node, path, out value);
                    }
                case BranchNode branchNode:
                    {
                        if (path.Length < 1)
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

        public TValue this[TKey key]
        {
            get
            {
                return Get(key);
            }
        }
    }
}

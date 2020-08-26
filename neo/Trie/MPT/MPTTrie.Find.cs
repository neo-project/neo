using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.Helper;

namespace Neo.Trie.MPT
{
    public partial class MPTTrie
    {
        private ReadOnlySpan<byte> Seek(ref MPTNode node, ReadOnlySpan<byte> path, out MPTNode start)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (path.Length < 1)
                        {
                            start = leafNode;
                            return default;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        var newNode = Resolve(hashNode);
                        if (newNode is null) throw new KeyNotFoundException("Internal error, can't resolve hash when mpt seek");
                        node = newNode;
                        return Seek(ref node, path, out start);
                    }
                case BranchNode branchNode:
                    {
                        if (path.Length < 1)
                        {
                            start = branchNode;
                            return Array.Empty<byte>();
                        }
                        return Concat(new byte[] { path[0] }, Seek(ref branchNode.Children[path[0]], path.Slice(1), out start));
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.Length < 1)
                        {
                            start = extensionNode.Next;
                            return extensionNode.Key;
                        }
                        if (path.StartsWith(extensionNode.Key))
                        {
                            return Concat(extensionNode.Key, Seek(ref extensionNode.Next, path.Slice(extensionNode.Key.Length), out start));
                        }
                        if (extensionNode.Key.AsSpan().StartsWith(path))
                        {
                            start = extensionNode.Next;
                            return extensionNode.Key;
                        }
                        break;
                    }
            }
            start = null;
            return Array.Empty<byte>();
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Find(byte[] prefix)
        {
            var path = ToNibbles(prefix);
            path = Seek(ref root, path, out MPTNode start);
            return Travers(start, path.ToArray())
                .Select(p => (FromNibbles(p.Key), p.Value));
        }

        private IEnumerable<(byte[] Key, byte[] Value)> Travers(MPTNode node, byte[] path)
        {
            if (node is null) yield break;
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        yield return (path, (byte[])leafNode.Value.Clone());
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        var newNode = Resolve(hashNode);
                        if (newNode is null) throw new KeyNotFoundException("Internal error, can't resolve hash when mpt find");
                        node = newNode;
                        foreach (var item in Travers(node, path))
                            yield return item;
                        break;
                    }
                case BranchNode branchNode:
                    {
                        for (int i = 0; i < BranchNode.ChildCount; i++)
                        {
                            foreach (var item in Travers(branchNode.Children[i], i == BranchNode.ChildCount - 1 ? path : Concat(path, new byte[] { (byte)i })))
                                yield return item;
                        }
                        break;
                    }
                case ExtensionNode extensionNode:
                    {
                        foreach (var item in Travers(extensionNode.Next, Concat(path, extensionNode.Key)))
                            yield return item;
                        break;
                    }
            }
        }
    }
}

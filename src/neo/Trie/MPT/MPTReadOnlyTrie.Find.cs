using Neo.IO;
using System;
using System.Collections.Generic;
using static Neo.Helper;

namespace Neo.Trie.MPT
{
    public partial class MPTReadOnlyTrie<TKey, TValue>
        where TKey : notnull, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        private byte[] Seek(ref MPTNode node, byte[] path, out MPTNode start)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (path.Length < 1)
                        {
                            start = leafNode;
                            return Array.Empty<byte>();
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        var newNode = Resolve(hashNode);
                        if (newNode is null) break;
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
                        return Concat(path[..1], Seek(ref branchNode.Children[path[0]], path[1..], out start));
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.Length < 1)
                        {
                            start = extensionNode;
                            return Array.Empty<byte>();
                        }
                        if (path.AsSpan().StartsWith(extensionNode.Key))
                        {
                            return Concat(extensionNode.Key, Seek(ref extensionNode.Next, path[extensionNode.Key.Length..], out start));
                        }
                        if (extensionNode.Key.AsSpan().StartsWith(path))
                        {
                            start = extensionNode;
                            return extensionNode.Key[path.Length..];
                        }
                        break;
                    }
            }
            start = null;
            return Array.Empty<byte>();
        }

        public IEnumerable<(TKey Key, TValue Value)> Find(byte[] prefix)
        {
            var path = prefix.ToNibbles();
            path = Seek(ref root, path, out MPTNode start);
            foreach (var item in Travers(start, path))
                yield return (item.Key.AsSerializable<TKey>(), item.Value.AsSerializable<TValue>());
        }

        private IEnumerable<(byte[] Key, byte[] Value)> Travers(MPTNode node, byte[] path)
        {
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
                        if (newNode is null) break;
                        node = newNode;
                        foreach (var item in Travers(node, path))
                            yield return item;
                        break;
                    }
                case BranchNode branchNode:
                    {
                        for (int i = 0; i < BranchNode.ChildCount; i++)
                        {
                            foreach (var item in Travers(branchNode.Children[i], Concat(path, new byte[] { (byte)i })))
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

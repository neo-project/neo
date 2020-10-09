using Neo.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.Helper;

namespace Neo.Cryptography.MPT
{
    partial class MPTTrie<TKey, TValue>
    {
        private ReadOnlySpan<byte> Seek(ref MPTNode node, ReadOnlySpan<byte> path, out MPTNode start)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (path.IsEmpty)
                        {
                            start = leafNode;
                            return ReadOnlySpan<byte>.Empty;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmpty) break;
                        var newNode = Resolve(hashNode.Hash);
                        if (newNode is null) throw new KeyNotFoundException("Internal error, can't resolve hash when mpt seek");
                        node = newNode;
                        return Seek(ref node, path, out start);
                    }
                case BranchNode branchNode:
                    {
                        if (path.IsEmpty)
                        {
                            start = branchNode;
                            return ReadOnlySpan<byte>.Empty;
                        }
                        return Concat(path[..1], Seek(ref branchNode.Children[path[0]], path[1..], out start));
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.IsEmpty)
                        {
                            start = extensionNode.Next;
                            return extensionNode.Key;
                        }
                        if (path.StartsWith(extensionNode.Key))
                        {
                            return Concat(extensionNode.Key, Seek(ref extensionNode.Next, path[extensionNode.Key.Length..], out start));
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
            return ReadOnlySpan<byte>.Empty;
        }

        public IEnumerable<(TKey Key, TValue Value)> Find(ReadOnlySpan<byte> prefix)
        {
            var path = ToNibbles(prefix);
            path = Seek(ref root, path, out MPTNode start).ToArray();
            return Travers(start, path)
                .Select(p => (FromNibbles(p.Key).AsSerializable<TKey>(), p.Value.AsSerializable<TValue>()));
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
                        if (hashNode.IsEmpty) break;
                        var newNode = Resolve(hashNode.Hash);
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

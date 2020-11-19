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
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (path.IsEmpty)
                        {
                            start = node;
                            return ReadOnlySpan<byte>.Empty;
                        }
                        break;
                    }
                case NodeType.Empty:
                    break;
                case NodeType.HashNode:
                    {
                        var newNode = cache.Resolve(node.Hash);
                        if (newNode is null) throw new InvalidOperationException("Internal error, can't resolve hash when mpt seek");
                        node = newNode;
                        return Seek(ref node, path, out start);
                    }
                case NodeType.BranchNode:
                    {
                        if (path.IsEmpty)
                        {
                            start = node;
                            return ReadOnlySpan<byte>.Empty;
                        }
                        return Concat(path[..1], Seek(ref node.Children[path[0]], path[1..], out start));
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.IsEmpty)
                        {
                            start = node.Next;
                            return node.Key;
                        }
                        if (path.StartsWith(node.Key))
                        {
                            return Concat(node.Key, Seek(ref node.Next, path[node.Key.Length..], out start));
                        }
                        if (node.Key.AsSpan().StartsWith(path))
                        {
                            start = node.Next;
                            return node.Key;
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
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        yield return (path, (byte[])node.Value.Clone());
                        break;
                    }
                case NodeType.Empty:
                    break;
                case NodeType.HashNode:
                    {
                        var newNode = cache.Resolve(node.Hash);
                        if (newNode is null) throw new InvalidOperationException("Internal error, can't resolve hash when mpt find");
                        node = newNode;
                        foreach (var item in Travers(node, path))
                            yield return item;
                        break;
                    }
                case NodeType.BranchNode:
                    {
                        for (int i = 0; i < MPTNode.BranchChildCount; i++)
                        {
                            foreach (var item in Travers(node.Children[i], i == MPTNode.BranchChildCount - 1 ? path : Concat(path, new byte[] { (byte)i })))
                                yield return item;
                        }
                        break;
                    }
                case NodeType.ExtensionNode:
                    {
                        foreach (var item in Travers(node.Next, Concat(path, node.Key)))
                            yield return item;
                        break;
                    }
            }
        }
    }
}

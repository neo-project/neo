// Copyright (C) 2015-2024 The Neo Project.
//
// Trie.Find.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.Helper;

namespace Neo.Cryptography.MPTTrie
{
    partial class Trie
    {
        private ReadOnlySpan<byte> Seek(ref Node node, ReadOnlySpan<byte> path, out Node start)
        {
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (path.IsEmpty)
                        {
                            start = node;
                            return [];
                        }
                        break;
                    }
                case NodeType.Empty:
                    break;
                case NodeType.HashNode:
                    {
                        var newNode = cache.Resolve(node.Hash) ?? throw new InvalidOperationException("Internal error, can't resolve hash when mpt seek");
                        node = newNode;
                        return Seek(ref node, path, out start);
                    }
                case NodeType.BranchNode:
                    {
                        if (path.IsEmpty)
                        {
                            start = node;
                            return [];
                        }
                        return Concat(path[..1], Seek(ref node.Children[path[0]], path[1..], out start));
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.IsEmpty)
                        {
                            start = node.Next;
                            return node.Key.Span;
                        }
                        if (path.StartsWith(node.Key.Span))
                        {
                            return Concat(node.Key.Span, Seek(ref node.Next, path[node.Key.Length..], out start));
                        }
                        if (node.Key.Span.StartsWith(path))
                        {
                            start = node.Next;
                            return node.Key.Span;
                        }
                        break;
                    }
            }
            start = null;
            return [];
        }

        public IEnumerable<(ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value)> Find(ReadOnlySpan<byte> prefix, byte[] from = null)
        {
            var path = ToNibbles(prefix);
            int offset = 0;
            if (from is null) from = [];
            if (0 < from.Length)
            {
                if (!from.AsSpan().StartsWith(prefix))
                    throw new InvalidOperationException("invalid from key");
                from = ToNibbles(from.AsSpan());
            }
            if (path.Length > Node.MaxKeyLength || from.Length > Node.MaxKeyLength)
                throw new ArgumentException("exceeds limit");
            path = Seek(ref root, path, out Node start).ToArray();
            if (from.Length > 0)
            {
                for (int i = 0; i < from.Length && i < path.Length; i++)
                {
                    if (path[i] < from[i]) return [];
                    if (path[i] > from[i])
                    {
                        offset = from.Length;
                        break;
                    }
                }
                if (offset == 0)
                {
                    offset = Math.Min(path.Length, from.Length);
                }
            }
            return Travers(start, path, from, offset).Select(p => (new ReadOnlyMemory<byte>(FromNibbles(p.Key.Span)), p.Value));
        }

        private IEnumerable<(ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value)> Travers(Node node, byte[] path, byte[] from, int offset)
        {
            if (node is null) yield break;
            if (offset < 0) throw new InvalidOperationException("invalid offset");
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (from.Length <= offset && !path.SequenceEqual(from))
                            yield return (path, node.Value);
                        break;
                    }
                case NodeType.Empty:
                    break;
                case NodeType.HashNode:
                    {
                        var newNode = cache.Resolve(node.Hash) ?? throw new InvalidOperationException("Internal error, can't resolve hash when mpt find");
                        node = newNode;
                        foreach (var item in Travers(node, path, from, offset))
                            yield return item;
                        break;
                    }
                case NodeType.BranchNode:
                    {
                        if (offset < from.Length)
                        {
                            for (int i = 0; i < Node.BranchChildCount - 1; i++)
                            {
                                if (from[offset] < i)
                                    foreach (var item in Travers(node.Children[i], Concat(path, new byte[] { (byte)i }), from, from.Length))
                                        yield return item;
                                else if (i == from[offset])
                                    foreach (var item in Travers(node.Children[i], Concat(path, new byte[] { (byte)i }), from, offset + 1))
                                        yield return item;
                            }
                        }
                        else
                        {
                            foreach (var item in Travers(node.Children[Node.BranchChildCount - 1], path, from, offset))
                                yield return item;
                            for (int i = 0; i < Node.BranchChildCount - 1; i++)
                            {
                                foreach (var item in Travers(node.Children[i], Concat(path, new byte[] { (byte)i }), from, offset))
                                    yield return item;
                            }
                        }
                        break;
                    }
                case NodeType.ExtensionNode:
                    {
                        if (offset < from.Length && from.AsSpan()[offset..].StartsWith(node.Key.Span))
                            foreach (var item in Travers(node.Next, Concat(path, node.Key.Span), from, offset + node.Key.Length))
                                yield return item;
                        else if (from.Length <= offset || 0 < node.Key.Span.CompareTo(from.AsSpan(offset)))
                            foreach (var item in Travers(node.Next, Concat(path, node.Key.Span), from, from.Length))
                                yield return item;
                        break;
                    }
            }
        }
    }
}

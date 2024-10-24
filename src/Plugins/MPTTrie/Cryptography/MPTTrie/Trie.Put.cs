// Copyright (C) 2015-2024 The Neo Project.
//
// Trie.Put.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Cryptography.MPTTrie
{
    partial class Trie
    {
        private static ReadOnlySpan<byte> CommonPrefix(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            var minLen = a.Length <= b.Length ? a.Length : b.Length;
            int i = 0;
            if (a.Length != 0 && b.Length != 0)
            {
                for (i = 0; i < minLen; i++)
                {
                    if (a[i] != b[i]) break;
                }
            }
            return a[..i];
        }

        public void Put(byte[] key, byte[] value)
        {
            var path = ToNibbles(key);
            var val = value;
            if (path.Length is 0 or > Node.MaxKeyLength)
                throw new ArgumentException("invalid", nameof(key));
            if (val.Length > Node.MaxValueLength)
                throw new ArgumentException("exceed limit", nameof(value));
            var n = Node.NewLeaf(val);
            Put(ref root, path, n);
        }

        private void Put(ref Node root, ReadOnlySpan<byte> path, Node val)
        {
            switch (root.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (path.IsEmpty)
                        {
                            if (!full) cache.DeleteNode(root.Hash);
                            root = val;
                            cache.PutNode(root);
                            return;
                        }
                        var branch = Node.NewBranch();
                        branch.Children[Node.BranchChildCount - 1] = root;
                        Put(ref branch.Children[path[0]], path[1..], val);
                        cache.PutNode(branch);
                        root = branch;
                        break;
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.StartsWith(root.Key.Span))
                        {
                            var oldHash = root.Hash;
                            Put(ref root.Next, path[root.Key.Length..], val);
                            if (!full) cache.DeleteNode(oldHash);
                            root.SetDirty();
                            cache.PutNode(root);
                            return;
                        }
                        if (!full) cache.DeleteNode(root.Hash);
                        var prefix = CommonPrefix(root.Key.Span, path);
                        var pathRemain = path[prefix.Length..];
                        var keyRemain = root.Key.Span[prefix.Length..];
                        var child = Node.NewBranch();
                        Node grandChild = new Node();
                        if (keyRemain.Length == 1)
                        {
                            child.Children[keyRemain[0]] = root.Next;
                        }
                        else
                        {
                            var exNode = Node.NewExtension(keyRemain[1..].ToArray(), root.Next);
                            cache.PutNode(exNode);
                            child.Children[keyRemain[0]] = exNode;
                        }
                        if (pathRemain.IsEmpty)
                        {
                            Put(ref grandChild, pathRemain, val);
                            child.Children[Node.BranchChildCount - 1] = grandChild;
                        }
                        else
                        {
                            Put(ref grandChild, pathRemain[1..], val);
                            child.Children[pathRemain[0]] = grandChild;
                        }
                        cache.PutNode(child);
                        if (prefix.Length > 0)
                        {
                            var exNode = Node.NewExtension(prefix.ToArray(), child);
                            cache.PutNode(exNode);
                            root = exNode;
                        }
                        else
                        {
                            root = child;
                        }
                        break;
                    }
                case NodeType.BranchNode:
                    {
                        var oldHash = root.Hash;
                        if (path.IsEmpty)
                        {
                            Put(ref root.Children[Node.BranchChildCount - 1], path, val);
                        }
                        else
                        {
                            Put(ref root.Children[path[0]], path[1..], val);
                        }
                        if (!full) cache.DeleteNode(oldHash);
                        root.SetDirty();
                        cache.PutNode(root);
                        break;
                    }
                case NodeType.Empty:
                    {
                        Node newNode;
                        if (path.IsEmpty)
                        {
                            newNode = val;
                        }
                        else
                        {
                            newNode = Node.NewExtension(path.ToArray(), val);
                            cache.PutNode(newNode);
                        }
                        root = newNode;
                        if (val.Type == NodeType.LeafNode) cache.PutNode(val);
                        break;
                    }
                case NodeType.HashNode:
                    {
                        Node newNode = cache.Resolve(root.Hash);
                        if (newNode is null) throw new InvalidOperationException("Internal error, can't resolve hash when mpt put");
                        root = newNode;
                        Put(ref root, path, val);
                        break;
                    }
                default:
                    throw new InvalidOperationException("unsupport node type");
            }
        }
    }
}

// Copyright (C) 2015-2024 The Neo Project.
//
// Trie.Delete.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.


namespace Neo.Cryptography.MPTTrie
{
    partial class Trie
    {
        public bool Delete(byte[] key)
        {
            var path = ToNibbles(key);
            if (path.Length == 0)
                throw new ArgumentException("could not be empty", nameof(key));
            if (path.Length > Node.MaxKeyLength)
                throw new ArgumentException("exceeds limit", nameof(key));
            return TryDelete(ref root, path);
        }

        private bool TryDelete(ref Node node, ReadOnlySpan<byte> path)
        {
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (path.IsEmpty)
                        {
                            if (!full) cache.DeleteNode(node.Hash);
                            node = new Node();
                            return true;
                        }
                        return false;
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.StartsWith(node.Key.Span))
                        {
                            var oldHash = node.Hash;
                            var result = TryDelete(ref node.Next, path[node.Key.Length..]);
                            if (!result) return false;
                            if (!full) cache.DeleteNode(oldHash);
                            if (node.Next.IsEmpty)
                            {
                                node = node.Next;
                                return true;
                            }
                            if (node.Next.Type == NodeType.ExtensionNode)
                            {
                                if (!full) cache.DeleteNode(node.Next.Hash);
                                node.Key = Utility.Utility.Concat(node.Key.Span, node.Next.Key.Span);
                                node.Next = node.Next.Next;
                            }
                            node.SetDirty();
                            cache.PutNode(node);
                            return true;
                        }
                        return false;
                    }
                case NodeType.BranchNode:
                    {
                        bool result;
                        var oldHash = node.Hash;
                        if (path.IsEmpty)
                        {
                            result = TryDelete(ref node.Children[Node.BranchChildCount - 1], path);
                        }
                        else
                        {
                            result = TryDelete(ref node.Children[path[0]], path[1..]);
                        }
                        if (!result) return false;
                        if (!full) cache.DeleteNode(oldHash);
                        List<byte> childrenIndexes = new List<byte>(Node.BranchChildCount);
                        for (int i = 0; i < Node.BranchChildCount; i++)
                        {
                            if (node.Children[i].IsEmpty) continue;
                            childrenIndexes.Add((byte)i);
                        }
                        if (childrenIndexes.Count > 1)
                        {
                            node.SetDirty();
                            cache.PutNode(node);
                            return true;
                        }
                        var lastChildIndex = childrenIndexes[0];
                        var lastChild = node.Children[lastChildIndex];
                        if (lastChildIndex == Node.BranchChildCount - 1)
                        {
                            node = lastChild;
                            return true;
                        }
                        if (lastChild.Type == NodeType.HashNode)
                        {
                            lastChild = cache.Resolve(lastChild.Hash);
                            if (lastChild is null) throw new InvalidOperationException("Internal error, can't resolve hash");
                        }
                        if (lastChild.Type == NodeType.ExtensionNode)
                        {
                            if (!full) cache.DeleteNode(lastChild.Hash);
                            lastChild.Key = Utility.Utility.Concat(childrenIndexes.ToArray(), lastChild.Key.Span);
                            lastChild.SetDirty();
                            cache.PutNode(lastChild);
                            node = lastChild;
                            return true;
                        }
                        node = Node.NewExtension(childrenIndexes.ToArray(), lastChild);
                        cache.PutNode(node);
                        return true;
                    }
                case NodeType.Empty:
                    {
                        return false;
                    }
                case NodeType.HashNode:
                    {
                        var newNode = cache.Resolve(node.Hash);
                        if (newNode is null) throw new InvalidOperationException("Internal error, can't resolve hash when mpt delete");
                        node = newNode;
                        return TryDelete(ref node, path);
                    }
                default:
                    return false;
            }
        }
    }
}

using Neo.IO;
using System;
using System.Collections.Generic;
using static Neo.Helper;

namespace Neo.Cryptography.MPT
{
    partial class MPTTrie<TKey, TValue>
    {
        public bool Delete(TKey key)
        {
            var path = ToNibbles(key.ToArray());
            if (path.Length == 0) return false;
            return TryDelete(ref root, path);
        }

        private bool TryDelete(ref MPTNode node, ReadOnlySpan<byte> path)
        {
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (path.IsEmpty)
                        {
                            if (!full) cache.DeleteNode(node.Hash);
                            node = new MPTNode();
                            return true;
                        }
                        return false;
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.StartsWith(node.Key))
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
                                node.Key = Concat(node.Key, node.Next.Key);
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
                            result = TryDelete(ref node.Children[MPTNode.BranchChildCount - 1], path);
                        }
                        else
                        {
                            result = TryDelete(ref node.Children[path[0]], path[1..]);
                        }
                        if (!result) return false;
                        if (!full) cache.DeleteNode(oldHash);
                        List<byte> childrenIndexes = new List<byte>(MPTNode.BranchChildCount);
                        for (int i = 0; i < MPTNode.BranchChildCount; i++)
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
                        if (lastChildIndex == MPTNode.BranchChildCount - 1)
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
                            lastChild.Key = Concat(childrenIndexes.ToArray(), lastChild.Key);
                            lastChild.SetDirty();
                            cache.PutNode(lastChild);
                            node = lastChild;
                            return true;
                        }
                        node = MPTNode.NewExtension(childrenIndexes.ToArray(), lastChild);
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

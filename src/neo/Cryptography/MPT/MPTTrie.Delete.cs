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
            switch (node)
            {
                case LeafNode _:
                    {
                        if (path.IsEmpty)
                        {
                            node = HashNode.EmptyNode;
                            return true;
                        }
                        return false;
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.StartsWith(extensionNode.Key))
                        {
                            var result = TryDelete(ref extensionNode.Next, path[extensionNode.Key.Length..]);
                            if (!result) return false;
                            if (extensionNode.Next is HashNode hashNode && hashNode.IsEmpty)
                            {
                                node = extensionNode.Next;
                                return true;
                            }
                            if (extensionNode.Next is ExtensionNode sn)
                            {
                                extensionNode.Key = Concat(extensionNode.Key, sn.Key);
                                extensionNode.Next = sn.Next;
                            }
                            extensionNode.SetDirty();
                            PutToStore(extensionNode);
                            return true;
                        }
                        return false;
                    }
                case BranchNode branchNode:
                    {
                        bool result;
                        if (path.IsEmpty)
                        {
                            result = TryDelete(ref branchNode.Children[BranchNode.ChildCount - 1], path);
                        }
                        else
                        {
                            result = TryDelete(ref branchNode.Children[path[0]], path[1..]);
                        }
                        if (!result) return false;
                        List<byte> childrenIndexes = new List<byte>(BranchNode.ChildCount);
                        for (int i = 0; i < BranchNode.ChildCount; i++)
                        {
                            if (branchNode.Children[i] is HashNode hn && hn.IsEmpty) continue;
                            childrenIndexes.Add((byte)i);
                        }
                        if (childrenIndexes.Count > 1)
                        {
                            branchNode.SetDirty();
                            PutToStore(branchNode);
                            return true;
                        }
                        var lastChildIndex = childrenIndexes[0];
                        var lastChild = branchNode.Children[lastChildIndex];
                        if (lastChildIndex == BranchNode.ChildCount - 1)
                        {
                            node = lastChild;
                            return true;
                        }
                        if (lastChild is HashNode hashNode)
                        {
                            lastChild = Resolve(hashNode);
                            if (lastChild is null) return false;
                        }
                        if (lastChild is ExtensionNode exNode)
                        {
                            exNode.Key = Concat(childrenIndexes.ToArray(), exNode.Key);
                            exNode.SetDirty();
                            PutToStore(exNode);
                            node = exNode;
                            return true;
                        }
                        node = new ExtensionNode()
                        {
                            Key = childrenIndexes.ToArray(),
                            Next = lastChild,
                        };
                        PutToStore(node);
                        return true;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmpty)
                        {
                            return true;
                        }
                        var newNode = Resolve(hashNode);
                        if (newNode is null) return false;
                        node = newNode;
                        return TryDelete(ref node, path);
                    }
                default:
                    return false;
            }
        }
    }
}

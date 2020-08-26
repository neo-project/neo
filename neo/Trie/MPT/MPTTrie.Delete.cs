using System;
using System.Collections.Generic;
using static Neo.Helper;

namespace Neo.Trie.MPT
{
    public partial class MPTTrie
    {
        public bool TryDelete(byte[] key)
        {
            var path = ToNibbles(key);
            return TryDelete(ref root, path);
        }

        private bool TryDelete(ref MPTNode node, ReadOnlySpan<byte> path)
        {
            switch (node)
            {
                case LeafNode ln:
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
                            var result = TryDelete(ref extensionNode.Next, path.Slice(extensionNode.Key.Length));
                            if (!result) return false;
                            if (extensionNode.Next.IsEmptyNode)
                            {
                                node = HashNode.EmptyNode;
                                return true;
                            }
                            if (extensionNode.Next is ExtensionNode en)
                            {
                                extensionNode.Key = Concat(extensionNode.Key, en.Key);
                                extensionNode.Next = en.Next;
                            }
                            extensionNode.SetDirty();
                            db.Put(extensionNode);
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
                            result = TryDelete(ref branchNode.Children[path[0]], path.Slice(1));
                        }
                        if (!result) return false;
                        branchNode.SetDirty();
                        List<byte> childrenIndexes = new List<byte>(BranchNode.ChildCount);
                        for (int i = 0; i < BranchNode.ChildCount; i++)
                        {
                            if (branchNode.Children[i].IsEmptyNode) continue;
                            childrenIndexes.Add((byte)i);
                        }
                        if (childrenIndexes.Count > 1)
                        {
                            db.Put(branchNode);
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
                            if (lastChild is null)
                            {
                                throw new System.ArgumentNullException("Invalid hash node");
                            }
                        }
                        if (lastChild is ExtensionNode exNode)
                        {
                            exNode.Key = Concat(childrenIndexes.ToArray(), exNode.Key);
                            exNode.SetDirty();
                            db.Put(exNode);
                            node = exNode;
                            return true;
                        }
                        var newNode = new ExtensionNode()
                        {
                            Key = childrenIndexes.ToArray(),
                            Next = lastChild,
                        };
                        node = newNode;
                        db.Put(newNode);
                        return true;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode)
                        {
                            return false;
                        }
                        var new_node = Resolve(hashNode);
                        if (new_node is null) throw new KeyNotFoundException("Internal error, can't resolve hash when mpt delete");
                        node = new_node;
                        return TryDelete(ref node, path);
                    }
                default:
                    return false;
            }
        }
    }
}

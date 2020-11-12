using Neo.IO;
using System;
using System.Collections.Generic;

namespace Neo.Cryptography.MPT
{
    partial class MPTTrie<TKey, TValue>
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

        public bool Put(TKey key, TValue value)
        {
            var path = ToNibbles(key.ToArray());
            var val = value.ToArray();
            if (path.Length == 0 || path.Length > ExtensionNode.MaxKeyLength)
                return false;
            if (val.Length > LeafNode.MaxValueLength)
                return false;
            if (val.Length == 0)
                return TryDelete(ref root, path);
            var n = new LeafNode(val);
            return Put(ref root, path, n);
        }

        private bool Put(ref MPTNode node, ReadOnlySpan<byte> path, MPTNode val)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (val is LeafNode v)
                        {
                            if (path.IsEmpty)
                            {
                                node = v;
                                PutNode(node);
                                if (!full) DeleteNode(leafNode.Hash);
                                return true;
                            }
                            var branch = new BranchNode();
                            branch.Children[BranchNode.ChildCount - 1] = leafNode;
                            Put(ref branch.Children[path[0]], path[1..], v);
                            PutNode(branch);
                            node = branch;
                            return true;
                        }
                        return false;
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.StartsWith(extensionNode.Key))
                        {
                            var result = Put(ref extensionNode.Next, path[extensionNode.Key.Length..], val);
                            if (result)
                            {
                                if (!full) DeleteNode(extensionNode.Hash);
                                extensionNode.SetDirty();
                                PutNode(extensionNode);
                            }
                            return result;
                        }
                        if (!full) DeleteNode(extensionNode.Hash);
                        var prefix = CommonPrefix(extensionNode.Key, path);
                        var pathRemain = path[prefix.Length..];
                        var keyRemain = extensionNode.Key.AsSpan(prefix.Length);
                        var child = new BranchNode();
                        MPTNode grandChild1 = MPTNode.EmptyNode;
                        MPTNode grandChild2 = MPTNode.EmptyNode;
                        if (keyRemain.Length == 1)
                        {
                            child.Children[keyRemain[0]] = extensionNode.Next;
                        }
                        else
                        {
                            var exNode = new ExtensionNode
                            {
                                Key = keyRemain[1..].ToArray(),
                                Next = extensionNode.Next,
                            };
                            PutNode(exNode);
                            child.Children[keyRemain[0]] = exNode;
                        }
                        if (pathRemain.IsEmpty)
                        {
                            Put(ref grandChild2, pathRemain, val);
                            child.Children[BranchNode.ChildCount - 1] = grandChild2;
                        }
                        else
                        {
                            Put(ref grandChild2, pathRemain[1..], val);
                            child.Children[pathRemain[0]] = grandChild2;
                        }
                        PutNode(child);
                        if (prefix.Length > 0)
                        {
                            var exNode = new ExtensionNode()
                            {
                                Key = prefix.ToArray(),
                                Next = child,
                            };
                            PutNode(exNode);
                            node = exNode;
                        }
                        else
                        {
                            node = child;
                        }
                        return true;
                    }
                case BranchNode branchNode:
                    {
                        bool result;
                        if (path.IsEmpty)
                        {
                            result = Put(ref branchNode.Children[BranchNode.ChildCount - 1], path, val);
                        }
                        else
                        {
                            result = Put(ref branchNode.Children[path[0]], path[1..], val);
                        }
                        if (result)
                        {
                            if (!full) DeleteNode(branchNode.Hash);
                            branchNode.SetDirty();
                            PutNode(branchNode);
                        }
                        return result;
                    }
                case HashNode hashNode:
                    {
                        MPTNode newNode;
                        if (hashNode.IsEmpty)
                        {
                            if (path.IsEmpty)
                            {
                                newNode = val;
                            }
                            else
                            {
                                newNode = new ExtensionNode()
                                {
                                    Key = path.ToArray(),
                                    Next = val,
                                };
                                PutNode(newNode);
                            }
                            node = newNode;
                            if (val is LeafNode) PutNode(val);
                            return true;
                        }
                        newNode = Resolve(hashNode.Hash);
                        if (newNode is null) throw new InvalidOperationException("Internal error, can't resolve hash when mpt put");
                        node = newNode;
                        return Put(ref node, path, val);
                    }
                default:
                    return false;
            }
        }
    }
}

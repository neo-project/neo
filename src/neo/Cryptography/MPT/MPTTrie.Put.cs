using Neo.IO;
using System;

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
            if (path.Length == 0 || path.Length > MPTNode.MaxKeyLength)
                return false;
            if (val.Length > MPTNode.MaxValueLength)
                return false;
            if (val.Length == 0)
                return TryDelete(ref root, path);
            var n = MPTNode.NewLeaf(val);
            return Put(ref root, path, n);
        }

        private bool Put(ref MPTNode node, ReadOnlySpan<byte> path, MPTNode val)
        {
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (val.Type == NodeType.LeafNode)
                        {
                            if (path.IsEmpty)
                            {
                                if (!full) cache.DeleteNode(node.Hash);
                                node = val;
                                cache.PutNode(node);
                                return true;
                            }
                            var branch = MPTNode.NewBranch();
                            branch.Children[MPTNode.BranchChildCount - 1] = node;
                            Put(ref branch.Children[path[0]], path[1..], val);
                            cache.PutNode(branch);
                            node = branch;
                            return true;
                        }
                        return false;
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.StartsWith(node.Key))
                        {
                            var oldHash = node.Hash;
                            var result = Put(ref node.Next, path[node.Key.Length..], val);
                            if (result)
                            {
                                if (!full) cache.DeleteNode(oldHash);
                                node.SetDirty();
                                cache.PutNode(node);
                            }
                            return result;
                        }
                        if (!full) cache.DeleteNode(node.Hash);
                        var prefix = CommonPrefix(node.Key, path);
                        var pathRemain = path[prefix.Length..];
                        var keyRemain = node.Key.AsSpan(prefix.Length);
                        var child = MPTNode.NewBranch();
                        MPTNode grandChild = new MPTNode();
                        if (keyRemain.Length == 1)
                        {
                            child.Children[keyRemain[0]] = node.Next;
                        }
                        else
                        {
                            var exNode = MPTNode.NewExtension(keyRemain[1..].ToArray(), node.Next);
                            cache.PutNode(exNode);
                            child.Children[keyRemain[0]] = exNode;
                        }
                        if (pathRemain.IsEmpty)
                        {
                            Put(ref grandChild, pathRemain, val);
                            child.Children[MPTNode.BranchChildCount - 1] = grandChild;
                        }
                        else
                        {
                            Put(ref grandChild, pathRemain[1..], val);
                            child.Children[pathRemain[0]] = grandChild;
                        }
                        cache.PutNode(child);
                        if (prefix.Length > 0)
                        {
                            var exNode = MPTNode.NewExtension(prefix.ToArray(), child);
                            cache.PutNode(exNode);
                            node = exNode;
                        }
                        else
                        {
                            node = child;
                        }
                        return true;
                    }
                case NodeType.BranchNode:
                    {
                        bool result;
                        var oldHash = node.Hash;
                        if (path.IsEmpty)
                        {
                            result = Put(ref node.Children[MPTNode.BranchChildCount - 1], path, val);
                        }
                        else
                        {
                            result = Put(ref node.Children[path[0]], path[1..], val);
                        }
                        if (result)
                        {
                            if (!full) cache.DeleteNode(oldHash);
                            node.SetDirty();
                            cache.PutNode(node);
                        }
                        return result;
                    }
                case NodeType.Empty:
                    {
                        MPTNode newNode;
                        if (path.IsEmpty)
                        {
                            newNode = val;
                        }
                        else
                        {
                            newNode = MPTNode.NewExtension(path.ToArray(), val);
                            cache.PutNode(newNode);
                        }
                        node = newNode;
                        if (val.Type == NodeType.LeafNode) cache.PutNode(val);
                        return true;
                    }
                case NodeType.HashNode:
                    {
                        MPTNode newNode = cache.Resolve(node.Hash);
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

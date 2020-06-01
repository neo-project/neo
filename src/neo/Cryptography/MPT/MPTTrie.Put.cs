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
                                PutToStore(node);
                                return true;
                            }
                            var branch = new BranchNode();
                            branch.Children[BranchNode.ChildCount - 1] = leafNode;
                            Put(ref branch.Children[path[0]], path[1..], v);
                            PutToStore(branch);
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
                                extensionNode.SetDirty();
                                PutToStore(extensionNode);
                            }
                            return result;
                        }
                        var prefix = CommonPrefix(extensionNode.Key, path);
                        var pathRemain = path[prefix.Length..];
                        var keyRemain = extensionNode.Key.AsSpan(prefix.Length);
                        var son = new BranchNode();
                        MPTNode grandSon1 = HashNode.EmptyNode;
                        MPTNode grandSon2 = HashNode.EmptyNode;

                        Put(ref grandSon1, keyRemain[1..], extensionNode.Next);
                        son.Children[keyRemain[0]] = grandSon1;

                        if (pathRemain.IsEmpty)
                        {
                            Put(ref grandSon2, pathRemain, val);
                            son.Children[BranchNode.ChildCount - 1] = grandSon2;
                        }
                        else
                        {
                            Put(ref grandSon2, pathRemain[1..], val);
                            son.Children[pathRemain[0]] = grandSon2;
                        }
                        PutToStore(son);
                        if (prefix.Length > 0)
                        {
                            var exNode = new ExtensionNode()
                            {
                                Key = prefix.ToArray(),
                                Next = son,
                            };
                            PutToStore(exNode);
                            node = exNode;
                        }
                        else
                        {
                            node = son;
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
                            branchNode.SetDirty();
                            PutToStore(branchNode);
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
                                PutToStore(newNode);
                            }
                            node = newNode;
                            if (val is LeafNode) PutToStore(val);
                            return true;
                        }
                        newNode = Resolve(hashNode);
                        if (newNode is null) return false;
                        node = newNode;
                        return Put(ref node, path, val);
                    }
                default:
                    return false;
            }
        }
    }
}

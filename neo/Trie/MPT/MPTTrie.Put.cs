using Neo.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Trie.MPT
{
    public partial class MPTTrie
    {
        private ReadOnlySpan<byte> CommonPrefix(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            var minLen = a.Length <= b.Length ? a.Length : b.Length;
            int i = 0;
            for (i = 0; i < minLen; i++)
            {
                if (a[i] != b[i]) break;
            }
            return a.Slice(0, i);
        }

        public bool Put(byte[] key, byte[] value)
        {
            if (key.Length > ExtensionNode.MaxKeyLength)
                throw new ArgumentOutOfRangeException("key out of mpt limit");
            if (value.Length > LeafNode.MaxValueLength)
                throw new ArgumentOutOfRangeException("value out of mpt limit");
            var path = ToNibbles(key);
            if (value.Length == 0)
            {
                return TryDelete(ref root, path);
            }
            var n = new LeafNode(value);
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
                                if (leafNode.Value.AsSpan().SequenceEqual(v.Value.AsSpan()))
                                    return true;
                                node = v;
                                db.Put(v);
                                return true;
                            }
                            var branch = new BranchNode();
                            branch.Children[BranchNode.ChildCount - 1] = leafNode;
                            Put(ref branch.Children[path[0]], path.Slice(1), v);
                            db.Put(branch);
                            node = branch;
                            return true;
                        }
                        return false;
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.StartsWith(extensionNode.Key))
                        {
                            if (Put(ref extensionNode.Next, path.Slice(extensionNode.Key.Length), val))
                            {
                                extensionNode.SetDirty();
                                db.Put(extensionNode);
                                return true;
                            }
                            return false;
                        }
                        var prefix = CommonPrefix(extensionNode.Key, path);
                        var pathRemain = path.Slice(prefix.Length);
                        var keyRemain = extensionNode.Key.AsSpan().Slice(prefix.Length);
                        var child = new BranchNode();
                        MPTNode grandChild1 = HashNode.EmptyNode;
                        MPTNode grandChild2 = HashNode.EmptyNode;
                        if (keyRemain.Length == 1)
                        {
                            child.Children[keyRemain[0]] = extensionNode.Next;
                        }
                        else
                        {
                            var exNode = new ExtensionNode
                            {
                                Key = keyRemain.Slice(1).ToArray(),
                                Next = extensionNode.Next,
                            };
                            db.Put(exNode);
                            child.Children[keyRemain[0]] = exNode;
                        }
                        if (pathRemain.Length == 0)
                        {
                            Put(ref grandChild2, pathRemain, val);
                            child.Children[BranchNode.ChildCount - 1] = grandChild2;
                        }
                        else
                        {
                            Put(ref grandChild2, pathRemain.Slice(1), val);
                            child.Children[pathRemain[0]] = grandChild2;
                        }
                        db.Put(child);
                        if (prefix.Length > 0)
                        {
                            var exNode = new ExtensionNode()
                            {
                                Key = prefix.ToArray(),
                                Next = child,
                            };
                            db.Put(exNode);
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
                            result = Put(ref branchNode.Children[path[0]], path.Slice(1), val);
                        }
                        if (result)
                        {
                            branchNode.SetDirty();
                            db.Put(branchNode);
                        }
                        return result;
                    }
                case HashNode hashNode:
                    {
                        MPTNode newNode;
                        if (hashNode.IsEmptyNode)
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
                                db.Put(newNode);
                            }
                            node = newNode;
                            if (val is LeafNode ln) db.Put(ln);
                            return true;
                        }
                        newNode = Resolve(hashNode);
                        if (newNode is null) throw new KeyNotFoundException("Internal error, can't resolve hash when mpt put");
                        node = newNode;
                        return Put(ref node, path, val);
                    }
                default:
                    throw new System.InvalidOperationException("Invalid node type.");
            }
        }
    }
}

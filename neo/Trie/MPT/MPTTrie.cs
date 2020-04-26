using Neo.IO.Json;
using System;
using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public class MPTTrie : MPTReadOnlyTrie
    {
        public static byte Version = 0;
        private readonly MPTDb db;

        public MPTTrie(UInt256 root, IKVStore store) : base(root, store)
        {
            this.db = new MPTDb(store);
        }

        public bool Put(byte[] key, byte[] value)
        {
            if (key.Length > ExtensionNode.MaxKeyLength)
                throw new ArgumentOutOfRangeException("key out of mpt limit");
            if (value.Length > LeafNode.MaxValueLength)
                throw new ArgumentOutOfRangeException("value out of mpt limit");
            var path = key.ToNibbles();
            if (value.Length == 0)
            {
                return TryDelete(ref root, path);
            }
            var n = new LeafNode(value);
            return Put(ref root, path, n);
        }

        private bool Put(ref MPTNode node, byte[] path, MPTNode val)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (val is LeafNode v)
                        {
                            if (path.Length == 0)
                            {
                                node = v;
                                db.Put(node);
                                return true;
                            }
                            var branch = new BranchNode();
                            branch.Children[BranchNode.ChildCount - 1] = leafNode;
                            Put(ref branch.Children[path[0]], path.Skip(1), v);
                            db.Put(branch);
                            node = branch;
                            return true;
                        }
                        return false;
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.AsSpan().StartsWith(extensionNode.Key))
                        {
                            var result = Put(ref extensionNode.Next, path.Skip(extensionNode.Key.Length), val);
                            if (result)
                            {
                                extensionNode.SetDirty();
                                db.Put(extensionNode);
                            }
                            return result;
                        }
                        var prefix = extensionNode.Key.CommonPrefix(path);
                        var pathRemain = path.Skip(prefix.Length);
                        var keyRemain = extensionNode.Key.Skip(prefix.Length);
                        var son = new BranchNode();
                        MPTNode grandSon1 = HashNode.EmptyNode();
                        MPTNode grandSon2 = HashNode.EmptyNode();

                        Put(ref grandSon1, keyRemain.Skip(1), extensionNode.Next);
                        son.Children[keyRemain[0]] = grandSon1;

                        if (pathRemain.Length == 0)
                        {
                            Put(ref grandSon2, pathRemain, val);
                            son.Children[BranchNode.ChildCount - 1] = grandSon2;
                        }
                        else
                        {
                            Put(ref grandSon2, pathRemain.Skip(1), val);
                            son.Children[pathRemain[0]] = grandSon2;
                        }
                        db.Put(son);
                        if (prefix.Length > 0)
                        {
                            var exNode = new ExtensionNode()
                            {
                                Key = prefix,
                                Next = son,
                            };
                            db.Put(exNode);
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
                        if (path.Length == 0)
                        {
                            result = Put(ref branchNode.Children[BranchNode.ChildCount - 1], path, val);
                        }
                        else
                        {
                            result = Put(ref branchNode.Children[path[0]], path.Skip(1), val);
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
                            if (path.Length == 0)
                            {
                                newNode = val;
                            }
                            else
                            {
                                newNode = new ExtensionNode()
                                {
                                    Key = path,
                                    Next = val,
                                };
                                db.Put(newNode);
                            }
                            node = newNode;
                            if (val is LeafNode) db.Put(val);
                            return true;
                        }
                        newNode = Resolve(hashNode);
                        if (newNode is null) return false;
                        node = newNode;
                        return Put(ref node, path, val);
                    }
                default:
                    throw new System.InvalidOperationException("Invalid node type.");
            }
        }

        public bool TryDelete(byte[] key)
        {
            var path = key.ToNibbles();
            return TryDelete(ref root, path);
        }

        private bool TryDelete(ref MPTNode node, byte[] path)
        {
            switch (node)
            {
                case LeafNode _:
                    {
                        if (path.Length == 0)
                        {
                            node = HashNode.EmptyNode();
                            return true;
                        }
                        return false;
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.AsSpan().StartsWith(extensionNode.Key))
                        {
                            var result = TryDelete(ref extensionNode.Next, path.Skip(extensionNode.Key.Length));
                            if (!result) return false;
                            if (extensionNode.Next is HashNode hashNode && hashNode.IsEmptyNode)
                            {
                                node = extensionNode.Next;
                                return true;
                            }
                            if (extensionNode.Next is ExtensionNode en)
                            {
                                extensionNode.Key = extensionNode.Key.Concat(en.Key);
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
                        if (path.Length == 0)
                        {
                            result = TryDelete(ref branchNode.Children[BranchNode.ChildCount - 1], path);
                        }
                        else
                        {
                            result = TryDelete(ref branchNode.Children[path[0]], path.Skip(1));
                        }
                        if (!result) return false;
                        List<byte> childrenIndexes = new List<byte>();
                        for (int i = 0; i < BranchNode.ChildCount; i++)
                        {
                            if (branchNode.Children[i] is HashNode hn && hn.IsEmptyNode) continue;
                            childrenIndexes.Add((byte)i);
                        }
                        if (childrenIndexes.Count > 1)
                        {
                            branchNode.SetDirty();
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
                            if (lastChild is null) throw new System.ArgumentNullException("Invalid hash node");
                        }
                        if (lastChild is ExtensionNode exNode)
                        {
                            exNode.Key = Helper.Concat(childrenIndexes.ToArray(), exNode.Key);
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
                            return true;
                        }
                        var new_node = Resolve(hashNode);
                        if (new_node is null) return false;
                        node = new_node;
                        return TryDelete(ref node, path);
                    }
                default:
                    return false;
            }
        }

        public JObject ToJson()
        {
            return root.ToJson();
        }
    }
}

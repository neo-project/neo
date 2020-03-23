using Neo.IO.Json;
using System;

namespace Neo.Trie.MPT
{
    public class MPTTrie : MPTReadOnlyTrie, ITrie
    {
        private MPTDb db;
        public MPTTrie(byte[] root, IKVStore store) : base(root, store)
        {
            this.db = new MPTDb(store);
        }

        public bool Put(byte[] path, byte[] value)
        {
            path = path.ToNibbles();
            if (value.Length == 0)
            {
                return TryDelete(ref root, path);
            }
            var n = new ValueNode(value);
            return Put(ref root, path, n);
        }

        private bool Put(ref MPTNode node, byte[] path, MPTNode val)
        {
            switch (node)
            {
                case ValueNode valueNode:
                    {
                        if (path.Length == 0 && val is ValueNode v)
                        {
                            node = v;
                            db.Put(node);
                            return true;
                        }
                        return false;
                    }
                case ExtensionNode extensionNode:
                    {
                        var prefix = extensionNode.Key.CommonPrefix(path);
                        if (prefix.Length == extensionNode.Key.Length)
                        {
                            var result = Put(ref extensionNode.Next, path.Skip(prefix.Length), val);
                            if (result)
                            {
                                extensionNode.ResetFlag();
                                db.Put(extensionNode);
                            }
                            return result;
                        }

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
                            son.Children[BranchNode.CHILD_COUNT - 1] = grandSon2;
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
                        var result = false;
                        if (path.Length == 0)
                        {
                            result = Put(ref branchNode.Children[BranchNode.CHILD_COUNT - 1], path, val);
                        }
                        else
                        {
                            result = Put(ref branchNode.Children[path[0]], path.Skip(1), val);
                        }
                        if (result)
                        {
                            branchNode.ResetFlag();
                            db.Put(branchNode);
                        }
                        return result;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode)
                        {
                            var newNode = new ExtensionNode()
                            {
                                Key = path,
                                Next = val,
                            };
                            node = newNode;
                            if (!(val is HashNode)) db.Put(val);
                            db.Put(node);
                            return true;
                        }
                        var new_node = Resolve(hashNode);
                        if (new_node is null) return false;
                        return Put(ref node, path, val);
                    }
                default:
                    return false;
            }
        }

        public bool TryDelete(byte[] path)
        {
            path = path.ToNibbles();
            return TryDelete(ref root, path);
        }

        private bool TryDelete(ref MPTNode node, byte[] path)
        {
            switch (node)
            {
                case ValueNode valueNode:
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
                        var prefix = extensionNode.Key.CommonPrefix(path);
                        if (prefix.Length == extensionNode.Key.Length)
                        {
                            var result = TryDelete(ref extensionNode.Next, path.Skip(prefix.Length));
                            if (!result) return false;
                            if (extensionNode.Next is HashNode hashNode && hashNode.IsEmptyNode)
                            {
                                node = extensionNode.Next;
                                return true;
                            }
                            if (extensionNode.Next is ExtensionNode sn)
                            {
                                extensionNode.Key = extensionNode.Key.Concat(sn.Key);
                                extensionNode.Next = sn.Next;
                            }
                            extensionNode.ResetFlag();
                            db.Put(extensionNode);
                            return true;
                        }
                        return false;
                    }
                case BranchNode branchNode:
                    {
                        var result = false;
                        if (path.Length == 0)
                        {
                            result = TryDelete(ref branchNode.Children[BranchNode.CHILD_COUNT - 1], path);
                        }
                        else
                        {
                            result = TryDelete(ref branchNode.Children[path[0]], path.Skip(1));
                        }
                        if (!result) return false;
                        var childrenIndexes = Array.Empty<byte>();
                        for (int i = 0; i < BranchNode.CHILD_COUNT; i++)
                        {
                            if (branchNode.Children[i] is HashNode hn && hn.IsEmptyNode) continue;
                            childrenIndexes = childrenIndexes.Add((byte)i);
                        }
                        if (childrenIndexes.Length > 1)
                        {
                            branchNode.ResetFlag();
                            db.Put(branchNode);
                            return true;
                        }
                        var lastChildIndex = childrenIndexes[0];
                        var lastChild = branchNode.Children[lastChildIndex];
                        if (lastChildIndex == BranchNode.CHILD_COUNT - 1)
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
                            exNode.Key = childrenIndexes.Concat(exNode.Key);
                            exNode.ResetFlag();
                            db.Put(exNode);
                            node = exNode;
                            return true;
                        }
                        var newNode = new ExtensionNode()
                        {
                            Key = childrenIndexes,
                            Next = lastChild,
                        };
                        node = newNode;
                        db.Put(node);
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

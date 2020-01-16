using Neo.IO.Json;
using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public class MPTTrie : ITrie
    {
        private MPTDatabase db;
        private MPTNode root;

        public MPTTrie(MPTDatabase db, byte[] root)
        {
            if (db is null)
                throw new System.Exception();
            this.db = db;
            if (root.Length == 0)
            {
                this.root = HashNode.EmptyNode();
            }
            else
            {
                this.root = Resolve(root);
            }
        }

        public MPTTrie(MPTDatabase db, MPTNode root)
        {
            if (db is null)
                throw new System.Exception();
            this.db = db;
            if (root is null)
            {
                this.root = HashNode.EmptyNode();
            }
            else
            {
                this.root = root;
            }
        }

        public MPTNode Resolve(byte[] hash)
        {
            return db.Node(hash);
        }

        public bool TryGet(byte[] path, out byte[] value)
        {
            return tryGet(ref root, path, out value);
        }

        private bool tryGet(ref MPTNode node, byte[] path, out byte[] value)
        {
            switch (node)
            {
                case ValueNode valueNode:
                    {
                        if (path.Length == 0)
                        {
                            value = (byte[])valueNode.Value.Clone();
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        node = Resolve(hashNode.Hash);
                        return tryGet(ref node, path, out value);
                    }
                case FullNode fullNode:
                    {
                        if (path.Length == 0)
                        {
                            return tryGet(ref fullNode.Children[16], path, out value);
                        }
                        return tryGet(ref fullNode.Children[path[0]], path.Skip(1), out value);
                    }
                case ShortNode shortNode:
                    {
                        var prefix = shortNode.Key.CommonPrefix(path);
                        if (prefix.Length == shortNode.Key.Length)
                        {
                            return tryGet(ref shortNode.Next, path.Skip(prefix.Length), out value);
                        }
                        break;
                    }
            }
            value = new byte[] { };
            return false;
        }

        public bool TryPut(byte[] path, byte[] value)
        {
            var n = new ValueNode(value);
            path = (byte[])path.Clone();
            if (value.Length == 0)
            {
                return tryDelete(ref root, path);
            }
            return put(ref root, path, n);
        }

        private bool put(ref MPTNode node, byte[] path, MPTNode val)
        {
            var result = false;
            var oldHash = node.GetHash();
            switch (node)
            {
                case ValueNode valueNode:
                    {
                        if (path.Length == 0 && val is ValueNode vn)
                        {
                            node = val;
                            node.ResetFlag();
                            db.Put(node);
                            result = true;
                            break;
                        }
                        break;
                    }
                case ShortNode shortNode:
                    {
                        var prefix = shortNode.Key.CommonPrefix(path);
                        if (prefix.Length == shortNode.Key.Length)
                        {
                            result = put(ref shortNode.Next, path.Skip(prefix.Length), val);
                            if (result)
                            {
                                shortNode.ResetFlag();
                                db.Put(shortNode);
                            }
                            break;
                        }

                        var pathRemain = path.Skip(prefix.Length);
                        var keyRemain = shortNode.Key.Skip(prefix.Length);
                        var son = new FullNode();
                        MPTNode grandSon1 = HashNode.EmptyNode(), grandSon2 = HashNode.EmptyNode();
                        put(ref grandSon1, keyRemain.Skip(1), shortNode.Next);
                        son.Children[keyRemain[0]] = grandSon1;
                        if (pathRemain.Length == 0)
                        {
                            put(ref grandSon2, pathRemain, val);
                            son.Children[son.Children.Length] = grandSon2;
                        }
                        else
                        {
                            put(ref grandSon2, pathRemain.Skip(1), val);
                            son.Children[pathRemain[0]] = grandSon2;
                        }
                        db.Put(son);
                        if (0 < prefix.Length)
                        {
                            var extensionNode = new ShortNode()
                            {
                                Key = prefix,
                                Next = son,
                            };
                            node = extensionNode;
                            db.Put(node);
                        }
                        else
                        {
                            node = son;
                        }
                        result = true;
                        break;
                    }
                case FullNode fullNode:
                    {
                        if (path.Length == 0)
                        {
                            result = put(ref fullNode.Children[fullNode.Children.Length], path, val);
                        }
                        else
                        {
                            result = put(ref fullNode.Children[path[0]], path.Skip(1), val);
                        }
                        if (result)
                        {
                            fullNode.ResetFlag();
                            db.Put(fullNode);
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode)
                        {
                            var newNode = new ShortNode()
                            {
                                Key = path,
                                Next = val,
                            };
                            node = newNode;
                            db.Put(node);
                            result = true;
                            break;
                        }
                        node = Resolve(hashNode.Hash);
                        result = put(ref node, path, val);
                        break;
                    }
                default:
                    throw new System.Exception();
            }
            if (result) db.Delete(oldHash);
            return result;
        }

        public bool TryDelete(byte[] path)
        {
            return tryDelete(ref root, path);
        }

        private bool tryDelete(ref MPTNode node, byte[] path)
        {
            var result = false;
            var oldHash = node.GetHash();

            switch (node)
            {
                case ValueNode valueNode:
                    {
                        if (path.Length == 0)
                        {
                            node = HashNode.EmptyNode();
                            result = true;
                        }
                        break;
                    }
                case ShortNode shortNode:
                    {
                        var prefix = shortNode.Key.CommonPrefix(path);
                        if (prefix.Length == shortNode.Key.Length)
                        {
                            result = tryDelete(ref shortNode.Next, path.Skip(prefix.Length));
                            if (!result) break;
                            if (shortNode.Next is HashNode hashNode && hashNode.IsEmptyNode)
                            {
                                node = shortNode.Next;
                                db.Put(node);
                            }
                            if (shortNode.Next is ShortNode sn)
                            {
                                shortNode.Key = shortNode.Key.Concat(sn.Key);
                                shortNode.Next = sn.Next;
                                shortNode.ResetFlag();
                                db.Put(shortNode);
                            }
                            result = true;
                            break;
                        }
                        result = false;
                        break;
                    }
                case FullNode fullNode:
                    {
                        if (path.Length == 0)
                        {
                            result = tryDelete(ref fullNode.Children[fullNode.Children.Length], path);
                        }
                        else
                        {
                            result = tryDelete(ref fullNode.Children[path[0]], path.Skip(1));
                        }
                        if (!result) break;
                        var nonEmptyChildren = new byte[] { };
                        for (int i = 0; i < fullNode.Children.Length; i++)
                        {
                            if (fullNode.Children[i] is HashNode hn && hn.IsEmptyNode) continue;
                            nonEmptyChildren = nonEmptyChildren.Add((byte)i);
                        }
                        if (1 < nonEmptyChildren.Length)
                        {
                            fullNode.ResetFlag();
                            db.Put(fullNode);
                            break;
                        }
                        var childIndex = nonEmptyChildren[0];
                        var child = fullNode.Children[childIndex];
                        if (child is HashNode hashNode) child = Resolve(hashNode.Hash);
                        if (child is ShortNode shortNode)
                        {
                            db.Delete(shortNode.GetHash());
                            shortNode.Key = nonEmptyChildren.Concat(shortNode.Key);
                            db.Put(shortNode);
                            node = child;
                            break;
                        }
                        var newNode = new ShortNode()
                        {
                            Key = nonEmptyChildren,
                            Next = child,
                        };
                        node = newNode;
                        db.Put(node);
                        result = true;
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode)
                        {
                            result = false;
                            break;
                        }
                        node = Resolve(hashNode.Hash);
                        result = tryDelete(ref node, path);
                        break;
                    }
            }
            if (result) db.Delete(oldHash);
            return result;
        }

        public byte[] GetRoot()
        {
            return this.root.GetHash();
        }

        public Dictionary<byte[], byte[]> GetProof(byte[] path)
        {
            var dict = new Dictionary<byte[], byte[]> { };
            getProof(ref root, path, dict);
            return dict;
        }

        private void getProof(ref MPTNode node, byte[] path, Dictionary<byte[], byte[]> dict)
        {
            switch (node)
            {
                case ValueNode valueNode:
                    {
                        if (path.Length == 0)
                        {
                            dict.Add(valueNode.GetHash(), valueNode.Encode());
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        node = Resolve(hashNode.Hash);
                        dict.Add(node.GetHash(), node.Encode());
                        getProof(ref node, path, dict);
                        break;
                    }
                case FullNode fullNode:
                    {
                        dict.Add(fullNode.GetHash(), fullNode.Encode());
                        if (path.Length == 0)
                        {
                            getProof(ref fullNode.Children[16], path, dict);
                        }
                        else
                        {
                            getProof(ref fullNode.Children[path[0]], path.Skip(1), dict);
                        }
                        break;
                    }
                case ShortNode shortNode:
                    {
                        var prefix = shortNode.Key.CommonPrefix(path);
                        if (prefix.Length == shortNode.Key.Length)
                        {
                            dict.Add(shortNode.GetHash(), shortNode.Encode());
                            getProof(ref shortNode.Next, path.Skip(prefix.Length), dict);
                        }
                        break;
                    }
            }
        }

        public JObject ToJson()
        {
            return root.ToJson();
        }
    }
}
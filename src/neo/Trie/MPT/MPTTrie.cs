using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public class MPTTrie : ITrie
    {
        private MPTDatabase db;
        private MPTNode root;

        public MPTTrie(MPTDatabase db)
        {
            if (db is null)
                throw new System.Exception();
            this.db = db;
            var rbytes = db.GetRoot();
            if (rbytes is null || rbytes.Length == 0)
            {
                this.root = HashNode.EmptyNode();
            }
            else
            {
                this.root = Resolve(rbytes);
            }
        }

        public MPTNode Resolve(byte[] hash)
        {
            return db.Node(hash);
        }

        public bool TryGet(byte[] path, out byte[] value)
        {
            path = path.ToNibbles();
            return TryGet(ref root, path, out value);
        }

        private bool TryGet(ref MPTNode node, byte[] path, out byte[] value)
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
                        return TryGet(ref node, path, out value);
                    }
                case FullNode fullNode:
                    {
                        if (path.Length == 0)
                        {
                            return TryGet(ref fullNode.Children[16], path, out value);
                        }
                        return TryGet(ref fullNode.Children[path[0]], path.Skip(1), out value);
                    }
                case ShortNode shortNode:
                    {
                        var prefix = shortNode.Key.CommonPrefix(path);
                        if (prefix.Length == shortNode.Key.Length)
                        {
                            return TryGet(ref shortNode.Next, path.Skip(prefix.Length), out value);
                        }
                        break;
                    }
            }
            value = new byte[] { };
            return false;
        }

        public bool Put(byte[] path, byte[] value)
        {
            var n = new ValueNode(value);
            path = path.ToNibbles();
            if (value.Length == 0)
            {
                return TryDelete(ref root, path);
            }
            return Put(ref root, path, n);
        }

        private bool Put(ref MPTNode node, byte[] path, MPTNode val)
        {
            switch (node)
            {
                case ValueNode valueNode:
                    {
                        if (path.Length == 0 && val is ValueNode)
                        {
                            db.Delete(node.GetHash());
                            node = val;
                            db.Put(valueNode);
                            return true;
                        }
                        return false;
                    }
                case ShortNode shortNode:
                    {
                        var prefix = shortNode.Key.CommonPrefix(path);
                        var oldHash = shortNode.GetHash();
                        if (prefix.Length == shortNode.Key.Length)
                        {
                            var result = Put(ref shortNode.Next, path.Skip(prefix.Length), val);
                            if (result)
                            {
                                db.Delete(oldHash);
                                shortNode.ResetFlag();
                                db.Put(shortNode);
                            }
                            return result;
                        }

                        var pathRemain = path.Skip(prefix.Length);
                        var keyRemain = shortNode.Key.Skip(prefix.Length);
                        var son = new FullNode();
                        MPTNode grandSon1 = HashNode.EmptyNode(), grandSon2 = HashNode.EmptyNode();

                        Put(ref grandSon1, keyRemain.Skip(1), shortNode.Next);
                        db.Put(grandSon1);
                        son.Children[keyRemain[0]] = grandSon1;

                        if (pathRemain.Length == 0)
                        {
                            Put(ref grandSon2, pathRemain, val);
                            son.Children[son.Children.Length] = grandSon2;
                        }
                        else
                        {
                            Put(ref grandSon2, pathRemain.Skip(1), val);
                            son.Children[pathRemain[0]] = grandSon2;
                        }
                        db.Put(grandSon2);
                        db.Put(son);
                        if (0 < prefix.Length)
                        {
                            var extensionNode = new ShortNode()
                            {
                                Key = prefix,
                                Next = son,
                            };
                            db.Put(extensionNode);
                            node = extensionNode;
                        }
                        else
                        {
                            node = son;
                        }
                        db.Delete(oldHash);
                        return true;
                    }
                case FullNode fullNode:
                    {
                        var result = false;
                        var oldHash = fullNode.GetHash();
                        if (path.Length == 0)
                        {
                            result = Put(ref fullNode.Children[fullNode.Children.Length], path, val);
                        }
                        else
                        {
                            result = Put(ref fullNode.Children[path[0]], path.Skip(1), val);
                        }
                        if (result)
                        {
                            db.Delete(oldHash);
                            fullNode.ResetFlag();
                            db.Put(fullNode);
                        }
                        return result;
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
                            return true;
                        }
                        node = Resolve(hashNode.Hash);
                        return Put(ref node, path, val);
                    }
                default:
                    throw new System.Exception();
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
                            db.Delete(valueNode.GetHash());
                            node = HashNode.EmptyNode();
                            return true;
                        }
                        return false;
                    }
                case ShortNode shortNode:
                    {
                        var prefix = shortNode.Key.CommonPrefix(path);
                        var oldHash = shortNode.GetHash();
                        if (prefix.Length == shortNode.Key.Length)
                        {
                            var result = TryDelete(ref shortNode.Next, path.Skip(prefix.Length));
                            if (!result) return false;
                            db.Delete(oldHash);
                            if (shortNode.Next is HashNode hashNode && hashNode.IsEmptyNode)
                            {
                                node = shortNode.Next;
                            }
                            else if (shortNode.Next is ShortNode sn)
                            {
                                shortNode.Key = shortNode.Key.Concat(sn.Key);
                                shortNode.Next = sn.Next;
                                shortNode.ResetFlag();
                                db.Put(shortNode);
                            }
                            else
                            {
                                node.ResetFlag();
                                db.Put(shortNode);
                            }
                            return true;
                        }
                        return false;
                    }
                case FullNode fullNode:
                    {
                        var result = false;
                        var oldHash = fullNode.GetHash();
                        if (path.Length == 0)
                        {
                            result = TryDelete(ref fullNode.Children[fullNode.Children.Length], path);
                        }
                        else
                        {
                            result = TryDelete(ref fullNode.Children[path[0]], path.Skip(1));
                        }
                        if (!result) return false;
                        db.Delete(oldHash);
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
                            return true;
                        }
                        var childIndex = nonEmptyChildren[0];
                        var child = fullNode.Children[childIndex];
                        if (child is HashNode hashNode)
                            child = Resolve(hashNode.Hash);
                        if (child is ShortNode shortNode)
                        {
                            db.Delete(shortNode.GetHash());
                            shortNode.Key = nonEmptyChildren.Concat(shortNode.Key);
                            shortNode.ResetFlag();
                            db.Put(shortNode);
                            node = shortNode;
                            return true;
                        }
                        var newNode = new ShortNode()
                        {
                            Key = nonEmptyChildren,
                            Next = child,
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
                        node = Resolve(hashNode.Hash);
                        return TryDelete(ref node, path);
                    }
                default:
                    return false;
            }
        }

        public byte[] GetRoot()
        {
            return this.root.GetHash();
        }

        public Dictionary<byte[], byte[]> GetProof(byte[] path)
        {
            var dict = new Dictionary<byte[], byte[]> { };
            path = path.ToNibbles();
            GetProof(ref root, path, dict);
            return dict;
        }

        private void GetProof(ref MPTNode node, byte[] path, Dictionary<byte[], byte[]> dict)
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
                        GetProof(ref node, path, dict);
                        break;
                    }
                case FullNode fullNode:
                    {
                        dict.Add(fullNode.GetHash(), fullNode.Encode());
                        if (path.Length == 0)
                        {
                            GetProof(ref fullNode.Children[16], path, dict);
                        }
                        else
                        {
                            GetProof(ref fullNode.Children[path[0]], path.Skip(1), dict);
                        }
                        break;
                    }
                case ShortNode shortNode:
                    {
                        var prefix = shortNode.Key.CommonPrefix(path);
                        if (prefix.Length == shortNode.Key.Length)
                        {
                            dict.Add(shortNode.GetHash(), shortNode.Encode());
                            GetProof(ref shortNode.Next, path.Skip(prefix.Length), dict);
                        }
                        break;
                    }
            }
        }

        public void Commit()
        {
            db.PutRoot(GetRoot());
        }
    }
}

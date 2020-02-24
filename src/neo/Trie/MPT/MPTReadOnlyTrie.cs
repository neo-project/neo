using Neo.Persistence;
using System;
using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public class MPTReadOnlyTrie : IReadOnlyTrie
    {
        protected MPTDatabase db;
        protected MPTNode root;

        public MPTReadOnlyTrie(ISnapshot store)
        {
            if (store is null)
                throw new System.ArgumentNullException();

            this.db = new MPTDatabase(store);

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
            value = Array.Empty<byte>();
            return false;
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

        public bool VerifyProof(byte[] path, Dictionary<byte[], byte[]> proof)
        {
            return true;
        }
    }
}

using Neo.Persistence;
using System;
using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public class MPTReadOnlyTrie : IReadOnlyTrie
    {
        private MPTReadOnlyDb rodb;
        protected MPTNode root;

        public MPTReadOnlyTrie(byte[] root, IKVReadOnlyStore store)
        {
            if (store is null)
                throw new System.ArgumentNullException();

            this.rodb = new MPTReadOnlyDb(store);

            if (root is null || root.Length == 0)
            {
                this.root = HashNode.EmptyNode();
            }
            else
            {
                this.root = new HashNode(root);
            }
        }

        public MPTNode Resolve(HashNode hn)
        {
            return rodb.Node(hn.Hash);
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
                        var new_node = Resolve(hashNode);
                        if (new_node is null) throw new System.ArgumentNullException("Invalid hash node");
                        node = new_node;
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
            return root.GetHash();
        }

        public bool GetProof(byte[] path, out HashSet<byte[]> set)
        {
            set = new HashSet<byte[]>(ByteArrayEqualityComparer.Default);
            path = path.ToNibbles();
            return GetProof(ref root, path, set);
        }

        private bool GetProof(ref MPTNode node, byte[] path, HashSet<byte[]> set)
        {
            switch (node)
            {
                case ValueNode valueNode:
                    {
                        if (path.Length == 0)
                        {
                            set.Add(valueNode.Encode());
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        var new_node = Resolve(hashNode);
                        if (new_node is null) throw new System.ArgumentNullException("Invalid hash node");
                        node = new_node;
                        return GetProof(ref node, path, set);
                    }
                case FullNode fullNode:
                    {
                        set.Add(fullNode.Encode());
                        if (path.Length == 0)
                        {
                            return GetProof(ref fullNode.Children[16], path, set);
                        }
                        return GetProof(ref fullNode.Children[path[0]], path.Skip(1), set);
                    }
                case ShortNode shortNode:
                    {
                        var prefix = shortNode.Key.CommonPrefix(path);
                        if (prefix.Length == shortNode.Key.Length)
                        {
                            set.Add(shortNode.Encode());
                            return GetProof(ref shortNode.Next, path.Skip(prefix.Length), set);
                        }
                        break;
                    }
            }
            return false;
        }

        public static bool VerifyProof(byte[] root, byte[] path, HashSet<byte[]> proof, out byte[] value)
        {
            var store = new MPTProofStore(proof);
            var trie = new MPTReadOnlyTrie(root, store);
            return trie.TryGet(path, out value);
        }
    }
}

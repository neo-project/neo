using Neo.Persistence;
using System;
using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public class MPTReadOnlyTrie : IReadOnlyTrie
    {
        private MPTReadOnlyDb rodb;
        protected MPTNode root;

        public MPTReadOnlyTrie(UInt256 root, IKVReadOnlyStore store)
        {
            if (store is null)
                throw new System.ArgumentNullException();

            this.rodb = new MPTReadOnlyDb(store);

            if (root is null)
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
                case LeafNode leafNode:
                    {
                        if (path.Length == 0)
                        {
                            value = (byte[])leafNode.Value.Clone();
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        var newNode = Resolve(hashNode);
                        if (newNode is null) break;
                        node = newNode;
                        return TryGet(ref node, path, out value);
                    }
                case BranchNode branchNode:
                    {
                        if (path.Length == 0)
                        {
                            return TryGet(ref branchNode.Children[16], path, out value);
                        }
                        return TryGet(ref branchNode.Children[path[0]], path.Skip(1), out value);
                    }
                case ExtensionNode extensionNode:
                    {
                        var prefix = extensionNode.Key.CommonPrefix(path);
                        if (prefix.Length == extensionNode.Key.Length)
                        {
                            return TryGet(ref extensionNode.Next, path.Skip(prefix.Length), out value);
                        }
                        break;
                    }
            }
            value = Array.Empty<byte>();
            return false;
        }

        public UInt256 GetRoot()
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
                case LeafNode leafNode:
                    {
                        if (path.Length == 0)
                        {
                            set.Add(leafNode.Encode());
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        var new_node = Resolve(hashNode);
                        if (new_node is null) break;
                        node = new_node;
                        return GetProof(ref node, path, set);
                    }
                case BranchNode branchNode:
                    {
                        set.Add(branchNode.Encode());
                        if (path.Length == 0)
                        {
                            return GetProof(ref branchNode.Children[16], path, set);
                        }
                        return GetProof(ref branchNode.Children[path[0]], path.Skip(1), set);
                    }
                case ExtensionNode extensionNode:
                    {
                        var prefix = extensionNode.Key.CommonPrefix(path);
                        if (prefix.Length == extensionNode.Key.Length)
                        {
                            set.Add(extensionNode.Encode());
                            return GetProof(ref extensionNode.Next, path.Skip(prefix.Length), set);
                        }
                        break;
                    }
            }
            return false;
        }

        public static bool VerifyProof(UInt256 root, byte[] path, HashSet<byte[]> proof, out byte[] value)
        {
            var store = new MPTProofStore(proof);
            var trie = new MPTReadOnlyTrie(root, store);
            return trie.TryGet(path, out value);
        }
    }
}

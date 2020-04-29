using Neo.IO;
using Neo.Persistence;
using System;
using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public partial class MPTReadOnlyTrie<TKey, TValue>
        where TKey : notnull, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        public bool GetProof(TKey key, out HashSet<byte[]> set)
        {
            set = new HashSet<byte[]>(ByteArrayEqualityComparer.Default);
            var path = key.ToArray().ToNibbles();
            if (path.Length == 0) return false;
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
                        var newNode = Resolve(hashNode);
                        if (newNode is null) break;
                        node = newNode;
                        return GetProof(ref node, path, set);
                    }
                case BranchNode branchNode:
                    {
                        set.Add(branchNode.Encode());
                        if (path.Length == 0)
                        {
                            return GetProof(ref branchNode.Children[BranchNode.ChildCount - 1], path, set);
                        }
                        return GetProof(ref branchNode.Children[path[0]], path[1..], set);
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.AsSpan().StartsWith(extensionNode.Key))
                        {
                            set.Add(extensionNode.Encode());
                            return GetProof(ref extensionNode.Next, path[extensionNode.Key.Length..], set);
                        }
                        break;
                    }
            }
            return false;
        }

        public static TValue VerifyProof(UInt256 root, TKey key, HashSet<byte[]> proof)
        {
            var store = new MPTProofStore(proof);
            var trie = new MPTReadOnlyTrie<TKey, TValue>(root, store, 0);
            return trie.Get(key);
        }
    }
}

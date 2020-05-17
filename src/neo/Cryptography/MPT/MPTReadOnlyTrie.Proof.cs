using Neo.IO;
using Neo.Persistence;
using System;
using System.Collections.Generic;

namespace Neo.Cryptography.MPT
{
    public partial class MPTReadOnlyTrie<TKey, TValue>
        where TKey : notnull, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        public bool GetProof(TKey key, out HashSet<byte[]> set)
        {
            set = new HashSet<byte[]>(ByteArrayEqualityComparer.Default);
            var path = ToNibbles(key.ToArray());
            if (path.Length < 1) return false;
            return GetProof(ref root, path, set);
        }

        private bool GetProof(ref MPTNode node, ReadOnlySpan<byte> path, HashSet<byte[]> set)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (path.Length < 1)
                        {
                            set.Add(leafNode.Encode());
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmpty) break;
                        var newNode = Resolve(hashNode);
                        if (newNode is null) break;
                        node = newNode;
                        return GetProof(ref node, path, set);
                    }
                case BranchNode branchNode:
                    {
                        set.Add(branchNode.Encode());
                        if (path.Length < 1)
                        {
                            return GetProof(ref branchNode.Children[BranchNode.ChildCount - 1], path, set);
                        }
                        return GetProof(ref branchNode.Children[path[0]], path[1..], set);
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.StartsWith(extensionNode.Key))
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

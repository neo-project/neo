using Neo.IO;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using static Neo.Helper;

namespace Neo.Cryptography.MPT
{
    partial class MPTTrie<TKey, TValue>
    {
        public HashSet<byte[]> GetProof(TKey key)
        {
            var path = ToNibbles(key.ToArray());
            if (path.Length == 0) return null;
            HashSet<byte[]> set = new HashSet<byte[]>(ByteArrayEqualityComparer.Default);
            if (!GetProof(ref root, path, set)) return null;
            return set;
        }

        private bool GetProof(ref MPTNode node, ReadOnlySpan<byte> path, HashSet<byte[]> set)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (path.IsEmpty)
                        {
                            set.Add(leafNode.Encode());
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmpty) break;
                        var newNode = Resolve(hashNode.Hash);
                        if (newNode is null) throw new KeyNotFoundException("Internal error, can't resolve hash when mpt getproof");
                        node = newNode;
                        return GetProof(ref node, path, set);
                    }
                case BranchNode branchNode:
                    {
                        set.Add(branchNode.Encode());
                        if (path.IsEmpty)
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
            using var memoryStore = new MemoryStore();
            foreach (byte[] data in proof)
                memoryStore.Put(Prefix, Crypto.Hash256(data), new ByteArrayWrapper(Concat(data, new byte[] { 1 })).ToArray());
            using ISnapshot snapshot = memoryStore.GetSnapshot();
            var trie = new MPTTrie<TKey, TValue>(snapshot, root, false);
            return trie[key];
        }
    }
}

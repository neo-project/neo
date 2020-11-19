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
            switch (node.Type)
            {
                case NodeType.LeafNode:
                    {
                        if (path.IsEmpty)
                        {
                            set.Add(node.ToArrayWithoutReference());
                            return true;
                        }
                        break;
                    }
                case NodeType.Empty:
                    break;
                case NodeType.HashNode:
                    {
                        var newNode = cache.Resolve(node.Hash);
                        if (newNode is null) throw new InvalidOperationException("Internal error, can't resolve hash when mpt getproof");
                        node = newNode;
                        return GetProof(ref node, path, set);
                    }
                case NodeType.BranchNode:
                    {
                        set.Add(node.ToArrayWithoutReference());
                        if (path.IsEmpty)
                        {
                            return GetProof(ref node.Children[MPTNode.BranchChildCount - 1], path, set);
                        }
                        return GetProof(ref node.Children[path[0]], path[1..], set);
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.StartsWith(node.Key))
                        {
                            set.Add(node.ToArrayWithoutReference());
                            return GetProof(ref node.Next, path[node.Key.Length..], set);
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
                memoryStore.Put(Prefix, Crypto.Hash256(data), Concat(data, new byte[] { 1 }));
            using ISnapshot snapshot = memoryStore.GetSnapshot();
            var trie = new MPTTrie<TKey, TValue>(snapshot, root, false);
            return trie[key];
        }
    }
}

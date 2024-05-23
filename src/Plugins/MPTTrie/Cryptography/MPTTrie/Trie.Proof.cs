// Copyright (C) 2015-2024 The Neo Project.
//
// Trie.Proof.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using static Neo.Helper;

namespace Neo.Cryptography.MPTTrie
{
    partial class Trie
    {
        public bool TryGetProof(byte[] key, out HashSet<byte[]> proof)
        {
            var path = ToNibbles(key);
            if (path.Length == 0)
                throw new ArgumentException("could not be empty", nameof(key));
            if (path.Length > Node.MaxKeyLength)
                throw new ArgumentException("exceeds limit", nameof(key));
            proof = new HashSet<byte[]>(ByteArrayEqualityComparer.Default);
            return GetProof(ref root, path, proof);
        }

        private bool GetProof(ref Node node, ReadOnlySpan<byte> path, HashSet<byte[]> set)
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
                            return GetProof(ref node.Children[Node.BranchChildCount - 1], path, set);
                        }
                        return GetProof(ref node.Children[path[0]], path[1..], set);
                    }
                case NodeType.ExtensionNode:
                    {
                        if (path.StartsWith(node.Key.Span))
                        {
                            set.Add(node.ToArrayWithoutReference());
                            return GetProof(ref node.Next, path[node.Key.Length..], set);
                        }
                        break;
                    }
            }
            return false;
        }

        private static byte[] Key(byte[] hash)
        {
            byte[] buffer = new byte[hash.Length + 1];
            buffer[0] = Prefix;
            Buffer.BlockCopy(hash, 0, buffer, 1, hash.Length);
            return buffer;
        }

        public static byte[] VerifyProof(UInt256 root, byte[] key, HashSet<byte[]> proof)
        {
            using var memoryStore = new MemoryStore();
            foreach (byte[] data in proof)
                memoryStore.Put(Key(Crypto.Hash256(data)), Concat(data, new byte[] { 1 }));
            using ISnapshot snapshot = memoryStore.GetSnapshot();
            var trie = new Trie(snapshot, root, false);
            return trie[key];
        }
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// MerkleTree.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Cryptography
{
    /// <summary>
    /// Represents a merkle tree.
    /// </summary>
    public class MerkleTree
    {
        private readonly MerkleTreeNode? _root;

        /// <summary>
        /// The depth of the tree.
        /// </summary>
        public int Depth { get; }

        internal MerkleTree(UInt256[] hashes)
        {
            _root = Build(hashes.Select(p => new MerkleTreeNode { Hash = p }).ToArray());
            if (_root is null) return;

            var depth = 1;
            for (var i = _root; i.LeftChild != null; i = i.LeftChild)
                depth++;
            Depth = depth;
        }

        private static MerkleTreeNode? Build(MerkleTreeNode[] leaves)
        {
            if (leaves.Length == 0) return null;
            if (leaves.Length == 1) return leaves[0];

            Span<byte> buffer = stackalloc byte[64];
            var parents = new MerkleTreeNode[(leaves.Length + 1) / 2];
            for (var i = 0; i < parents.Length; i++)
            {
                parents[i] = new MerkleTreeNode
                {
                    LeftChild = leaves[i * 2]
                };
                leaves[i * 2].Parent = parents[i];
                if (i * 2 + 1 == leaves.Length)
                {
                    parents[i].RightChild = parents[i].LeftChild;
                }
                else
                {
                    parents[i].RightChild = leaves[i * 2 + 1];
                    leaves[i * 2 + 1].Parent = parents[i];
                }
                parents[i].Hash = Concat(buffer, parents[i].LeftChild.Hash, parents[i].RightChild.Hash);
            }
            return Build(parents); //TailCall
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt256 Concat(Span<byte> buffer, UInt256 hash1, UInt256 hash2)
        {
            hash1.Serialize(buffer);
            hash2.Serialize(buffer[32..]);

            return new UInt256(Crypto.Hash256(buffer));
        }

        /// <summary>
        /// Computes the root of the hash tree.
        /// </summary>
        /// <param name="hashes">The leaves of the hash tree.</param>
        /// <returns>The root of the hash tree.</returns>
        public static UInt256 ComputeRoot(UInt256[] hashes)
        {
            if (hashes.Length == 0) return UInt256.Zero;
            if (hashes.Length == 1) return hashes[0];

            var tree = new MerkleTree(hashes);
            return tree._root!.Hash;
        }

        private static void DepthFirstSearch(MerkleTreeNode node, List<UInt256> hashes)
        {
            if (node.LeftChild == null)
            {
                // if left is null, then right must be null
                hashes.Add(node.Hash);
            }
            else
            {
                DepthFirstSearch(node.LeftChild, hashes);
                DepthFirstSearch(node.RightChild, hashes);
            }
        }

        /// <summary>
        /// Gets all nodes of the hash tree in depth-first order.
        /// </summary>
        /// <returns>All nodes of the hash tree.</returns>
        public UInt256[] ToHashArray()
        {
            if (_root is null) return [];
            var hashes = new List<UInt256>();
            DepthFirstSearch(_root, hashes);
            return [.. hashes];
        }

        /// <summary>
        /// Trims the hash tree using the specified bit array.
        /// </summary>
        /// <param name="flags">The bit array to be used.</param>
        public void Trim(BitArray flags)
        {
            if (_root is null) return;
            flags = new BitArray(flags)
            {
                Length = 1 << (Depth - 1)
            };
            Trim(_root, 0, Depth, flags);
        }

        private static void Trim(MerkleTreeNode node, int index, int depth, BitArray flags)
        {
            if (depth == 1) return;
            if (node.LeftChild == null) return; // if left is null, then right must be null
            if (depth == 2)
            {
                if (!flags.Get(index * 2) && !flags.Get(index * 2 + 1))
                {
                    node.LeftChild = null;
                    node.RightChild = null;
                }
            }
            else
            {
                Trim(node.LeftChild, index * 2, depth - 1, flags);
                Trim(node.RightChild, index * 2 + 1, depth - 1, flags);
                if (node.LeftChild.LeftChild == null && node.RightChild.RightChild == null)
                {
                    node.LeftChild = null;
                    node.RightChild = null;
                }
            }
        }
    }
}

#nullable disable

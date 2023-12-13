// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Neo.IO;

namespace Neo.Cryptography
{
    /// <summary>
    /// Represents a merkle tree.
    /// </summary>
    public class MerkleTree
    {
        private readonly MerkleTreeNode root;

        /// <summary>
        /// The depth of the tree.
        /// </summary>
        public int Depth { get; }

        internal MerkleTree(UInt256[] hashes)
        {
            this.root = Build(hashes.Select(p => new MerkleTreeNode { Hash = p }).ToArray());
            if (root is null) return;
            int depth = 1;
            for (MerkleTreeNode i = root; i.LeftChild != null; i = i.LeftChild)
                depth++;
            this.Depth = depth;
        }

        private static MerkleTreeNode Build(MerkleTreeNode[] leaves)
        {
            if (leaves.Length == 0) return null;
            if (leaves.Length == 1) return leaves[0];

            Span<byte> buffer = stackalloc byte[64];
            MerkleTreeNode[] parents = new MerkleTreeNode[(leaves.Length + 1) / 2];
            for (int i = 0; i < parents.Length; i++)
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
            hash1.ToArray().CopyTo(buffer);
            hash2.ToArray().CopyTo(buffer[32..]);

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
            MerkleTree tree = new(hashes);
            return tree.root.Hash;
        }

        private static void DepthFirstSearch(MerkleTreeNode node, IList<UInt256> hashes)
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
            if (root is null) return Array.Empty<UInt256>();
            List<UInt256> hashes = new();
            DepthFirstSearch(root, hashes);
            return hashes.ToArray();
        }

        /// <summary>
        /// Trims the hash tree using the specified bit array.
        /// </summary>
        /// <param name="flags">The bit array to be used.</param>
        public void Trim(BitArray flags)
        {
            if (root is null) return;
            flags = new BitArray(flags)
            {
                Length = 1 << (Depth - 1)
            };
            Trim(root, 0, Depth, flags);
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

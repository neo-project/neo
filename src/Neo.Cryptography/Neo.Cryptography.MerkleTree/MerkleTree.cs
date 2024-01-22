// Copyright (C) 2015-2024 The Neo Project.
//
// MerkleTree.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.Utility;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Neo.Cryptography.MerkleTree
{
    /// <summary>
    /// Represents a merkle tree.
    /// </summary>
    public class MerkleTree<T> where T : IArrayConvertible, new()
    {
        private readonly MerkleTreeNode<T> root;

        /// <summary>
        /// The depth of the tree.
        /// </summary>
        public int Depth { get; }

        public MerkleTree(T[] hashes)
        {
            this.root = Build(hashes.Select(p => new MerkleTreeNode<T> { Hash = p }).ToArray());
            if (root is null) return;
            int depth = 1;
            for (var i = root; i.LeftChild != null; i = i.LeftChild)
                depth++;
            this.Depth = depth;
        }

        private static MerkleTreeNode<T> Build(MerkleTreeNode<T>[] leaves)
        {
            if (leaves.Length == 0) return null;
            if (leaves.Length == 1) return leaves[0];

            Span<byte> buffer = stackalloc byte[64];
            var parents = new MerkleTreeNode<T>[(leaves.Length + 1) / 2];
            for (int i = 0; i < parents.Length; i++)
            {
                parents[i] = new MerkleTreeNode<T>
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
        private static T Concat(Span<byte> buffer, T hash1, T hash2)
        {
            hash1.ToArray().CopyTo(buffer);
            hash2.ToArray().CopyTo(buffer[32..]);

            return (T)Activator.CreateInstance(typeof(T), Utility.Utility.Hash256(buffer));
        }

        /// <summary>
        /// Computes the root of the hash tree.
        /// </summary>
        /// <param name="hashes">The leaves of the hash tree.</param>
        /// <returns>The root of the hash tree.</returns>
        public static T ComputeRoot(T[] hashes)
        {
            if (hashes.Length == 0) return new T();
            if (hashes.Length == 1) return hashes[0];
            MerkleTree<T> tree = new(hashes);
            return tree.root.Hash;
        }

        private static void DepthFirstSearch(MerkleTreeNode<T> node, IList hashes)
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
        public T[] ToHashArray()
        {
            if (root is null) return Array.Empty<T>();
            List<T> hashes = new();
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

        private static void Trim(MerkleTreeNode<T> node, int index, int depth, BitArray flags)
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

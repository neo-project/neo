using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Cryptography
{
    /// <summary>
    /// 哈希树
    /// </summary>
    public class MerkleTree
    {
        private MerkleTreeNode root;

        public int Depth { get; private set; }

        internal MerkleTree(UInt256[] hashes)
        {
            if (hashes.Length == 0) throw new ArgumentException();
            this.root = Build(hashes.Select(p => new MerkleTreeNode { Hash = p }).ToArray());
            int depth = 1;
            for (MerkleTreeNode i = root; i.LeftChild != null; i = i.LeftChild)
                depth++;
            this.Depth = depth;
        }

        private static MerkleTreeNode Build(MerkleTreeNode[] leaves)
        {
            if (leaves.Length == 0) throw new ArgumentException();
            if (leaves.Length == 1) return leaves[0];
            MerkleTreeNode[] parents = new MerkleTreeNode[(leaves.Length + 1) / 2];
            for (int i = 0; i < parents.Length; i++)
            {
                parents[i] = new MerkleTreeNode();
                parents[i].LeftChild = leaves[i * 2];
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
                parents[i].Hash = new UInt256(Crypto.Default.Hash256(parents[i].LeftChild.Hash.ToArray().Concat(parents[i].RightChild.Hash.ToArray()).ToArray()));
            }
            return Build(parents); //TailCall
        }

        /// <summary>
        /// 计算根节点的值
        /// </summary>
        /// <param name="hashes">子节点列表</param>
        /// <returns>返回计算的结果</returns>
        public static UInt256 ComputeRoot(UInt256[] hashes)
        {
            if (hashes.Length == 0) throw new ArgumentException();
            if (hashes.Length == 1) return hashes[0];
            MerkleTree tree = new MerkleTree(hashes);
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

        // depth-first order
        public UInt256[] ToHashArray()
        {
            List<UInt256> hashes = new List<UInt256>();
            DepthFirstSearch(root, hashes);
            return hashes.ToArray();
        }

        public void Trim(BitArray flags)
        {
            flags = new BitArray(flags);
            flags.Length = 1 << (Depth - 1);
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

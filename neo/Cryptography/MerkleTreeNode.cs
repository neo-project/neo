namespace Neo.Cryptography
{
    internal class MerkleTreeNode
    {
        public UInt256 Hash;
        public MerkleTreeNode Parent;
        public MerkleTreeNode LeftChild;
        public MerkleTreeNode RightChild;

        public bool IsLeaf => LeftChild == null && RightChild == null;

        public bool IsRoot => Parent == null;
    }
}

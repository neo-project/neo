// Copyright (C) 2015-2026 The Neo Project.
//
// MerkleTreeNode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography
{
    class MerkleTreeNode
    {
        public UInt256? Hash { get; set; }
        public MerkleTreeNode? Parent { get; set; }
        public MerkleTreeNode? LeftChild { get; set; }
        public MerkleTreeNode? RightChild { get; set; }

        public bool IsLeaf => LeftChild == null && RightChild == null;
        public bool IsRoot => Parent == null;
    }
}

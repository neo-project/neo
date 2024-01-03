// Copyright (C) 2015-2024 The Neo Project.
//
// UT_MerkleTree.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.IO;
using System;
using System.Collections;
using System.Linq;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_MerkleTree
    {
        public UInt256 GetByteArrayHash(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0) throw new ArgumentNullException();
            var hash = new UInt256(Crypto.Hash256(byteArray));
            return hash;
        }

        [TestMethod]
        public void TestBuildAndDepthFirstSearch()
        {
            byte[] array1 = { 0x01 };
            var hash1 = GetByteArrayHash(array1);

            byte[] array2 = { 0x02 };
            var hash2 = GetByteArrayHash(array2);

            byte[] array3 = { 0x03 };
            var hash3 = GetByteArrayHash(array3);

            UInt256[] hashes = { hash1, hash2, hash3 };
            MerkleTree tree = new MerkleTree(hashes);
            var hashArray = tree.ToHashArray();
            hashArray[0].Should().Be(hash1);
            hashArray[1].Should().Be(hash2);
            hashArray[2].Should().Be(hash3);
            hashArray[3].Should().Be(hash3);

            var rootHash = MerkleTree.ComputeRoot(hashes);
            var hash4 = Crypto.Hash256(hash1.ToArray().Concat(hash2.ToArray()).ToArray());
            var hash5 = Crypto.Hash256(hash3.ToArray().Concat(hash3.ToArray()).ToArray());
            var result = new UInt256(Crypto.Hash256(hash4.ToArray().Concat(hash5.ToArray()).ToArray()));
            rootHash.Should().Be(result);
        }

        [TestMethod]
        public void TestTrim()
        {
            byte[] array1 = { 0x01 };
            var hash1 = GetByteArrayHash(array1);

            byte[] array2 = { 0x02 };
            var hash2 = GetByteArrayHash(array2);

            byte[] array3 = { 0x03 };
            var hash3 = GetByteArrayHash(array3);

            UInt256[] hashes = { hash1, hash2, hash3 };
            MerkleTree tree = new MerkleTree(hashes);

            bool[] boolArray = { false, false, false };
            BitArray bitArray = new BitArray(boolArray);
            tree.Trim(bitArray);
            var hashArray = tree.ToHashArray();

            hashArray.Length.Should().Be(1);
            var rootHash = MerkleTree.ComputeRoot(hashes);
            var hash4 = Crypto.Hash256(hash1.ToArray().Concat(hash2.ToArray()).ToArray());
            var hash5 = Crypto.Hash256(hash3.ToArray().Concat(hash3.ToArray()).ToArray());
            var result = new UInt256(Crypto.Hash256(hash4.ToArray().Concat(hash5.ToArray()).ToArray()));
            hashArray[0].Should().Be(result);
        }
    }
}

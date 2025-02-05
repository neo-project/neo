// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Header.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Header
    {
        private static readonly string s_headerHex =
            "0000000000000000000000000000000000000000000000000000000000000000000000007227ba7b747f1a9" +
            "8f68679d4a98b68927646ab195a6f56b542ca5a0e6a412662493ed0e58f0100000000000000000000000000" +
            "0000000000000000000000000000000000000000000001000111";

        [TestMethod]
        public void Size_Get()
        {
            UInt256 val256 = UInt256.Zero;
            var uut = TestUtils.MakeHeader(null, val256);
            // blockbase 4 + 64 + 1 + 32 + 4 + 4 + 20 + 4
            // header 1
            Assert.AreEqual(113, uut.Size); // 105 + nonce
        }

        [TestMethod]
        public void GetHashCodeTest()
        {
            UInt256 val256 = UInt256.Zero;
            var uut = TestUtils.MakeHeader(null, val256);
            Assert.AreEqual(uut.Hash.GetHashCode(), uut.GetHashCode());
        }

        [TestMethod]
        public void TrimTest()
        {
            UInt256 val256 = UInt256.Zero;
            var snapshotCache = TestBlockchain.GetTestSnapshotCache().CloneCache();
            var uut = TestUtils.MakeHeader(null, val256);
            uut.Witness = new Witness()
            {
                InvocationScript = Array.Empty<byte>(),
                VerificationScript = Array.Empty<byte>()
            };

            TestUtils.BlocksAdd(snapshotCache, uut.Hash, new TrimmedBlock()
            {
                Header = new Header
                {
                    Timestamp = uut.Timestamp,
                    PrevHash = uut.PrevHash,
                    MerkleRoot = uut.MerkleRoot,
                    NextConsensus = uut.NextConsensus,
                    Witness = uut.Witness
                },
                Hashes = Array.Empty<UInt256>()
            });

            var trim = NativeContract.Ledger.GetTrimmedBlock(snapshotCache, uut.Hash);
            var header = trim.Header;

            Assert.AreEqual(uut.Version, header.Version);
            Assert.AreEqual(uut.PrevHash, header.PrevHash);
            Assert.AreEqual(uut.MerkleRoot, header.MerkleRoot);
            Assert.AreEqual(uut.Timestamp, header.Timestamp);
            Assert.AreEqual(uut.Index, header.Index);
            Assert.AreEqual(uut.NextConsensus, header.NextConsensus);
            CollectionAssert.AreEqual(uut.Witness.InvocationScript.ToArray(), header.Witness.InvocationScript.ToArray());
            CollectionAssert.AreEqual(uut.Witness.VerificationScript.ToArray(), header.Witness.VerificationScript.ToArray());
            Assert.AreEqual(0, trim.Hashes.Length);
        }

        [TestMethod]
        public void Deserialize()
        {
            var uut = TestUtils.MakeHeader(null, UInt256.Zero);
            MemoryReader reader = new(s_headerHex.HexToBytes());
            uut.Deserialize(ref reader);
        }

        [TestMethod]
        public void Equals_Null()
        {
            var uut = new Header();
            Assert.IsFalse(uut.Equals(null));
        }


        [TestMethod]
        public void Equals_SameHeader()
        {
            var uut = new Header();
            Assert.IsTrue(uut.Equals(uut));
        }

        [TestMethod]
        public void Equals_SameHash()
        {
            UInt256 prevHash = new(TestUtils.GetByteArray(32, 0x42));
            var uut = TestUtils.MakeHeader(null, prevHash);
            var header = TestUtils.MakeHeader(null, prevHash);

            Assert.IsTrue(uut.Equals(header));
        }

        [TestMethod]
        public void Equals_SameObject()
        {
            var uut = new Header();
            Assert.IsTrue(uut.Equals((object)uut));
        }

        [TestMethod]
        public void Serialize()
        {
            var uut = TestUtils.MakeHeader(null, UInt256.Zero);
            Assert.AreEqual(s_headerHex, uut.ToArray().ToHexString());
        }

        [TestMethod]
        public void Witness()
        {
            IVerifiable item = new Header();
            Action actual = () => item.Witnesses = null;
            Assert.ThrowsException<ArgumentNullException>(actual);

            item.Witnesses = [new()];
            Assert.AreEqual(1, item.Witnesses.Length);
        }
    }
}

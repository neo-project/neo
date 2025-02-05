// Copyright (C) 2015-2025 The Neo Project.
//
// UT_TrimmedBlock.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.IO;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_TrimmedBlock
    {
        public static TrimmedBlock GetTrimmedBlockWithNoTransaction()
        {
            return new TrimmedBlock
            {
                Header = new Header
                {
                    MerkleRoot = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff02"),
                    PrevHash = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01"),
                    Timestamp = new DateTime(1988, 06, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Index = 1,
                    NextConsensus = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"),
                    Witness = new Witness
                    {
                        InvocationScript = Array.Empty<byte>(),
                        VerificationScript = new[] { (byte)OpCode.PUSH1 }
                    },
                },
                Hashes = Array.Empty<UInt256>()
            };
        }

        [TestMethod]
        public void TestGetBlock()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var tx1 = TestUtils.GetTransaction(UInt160.Zero);
            tx1.Script = new byte[] { 0x01,0x01,0x01,0x01,
                                      0x01,0x01,0x01,0x01,
                                      0x01,0x01,0x01,0x01,
                                      0x01,0x01,0x01,0x01 };
            var state1 = new TransactionState
            {
                Transaction = tx1,
                BlockIndex = 1
            };
            var tx2 = TestUtils.GetTransaction(UInt160.Zero);
            tx2.Script = new byte[] { 0x01,0x01,0x01,0x01,
                                      0x01,0x01,0x01,0x01,
                                      0x01,0x01,0x01,0x01,
                                      0x01,0x01,0x01,0x02 };
            var state2 = new TransactionState
            {
                Transaction = tx2,
                BlockIndex = 1
            };
            TestUtils.TransactionAdd(snapshotCache, state1, state2);

            TrimmedBlock tblock = GetTrimmedBlockWithNoTransaction();
            tblock.Hashes = new UInt256[] { tx1.Hash, tx2.Hash };
            TestUtils.BlocksAdd(snapshotCache, tblock.Hash, tblock);

            Block block = NativeContract.Ledger.GetBlock(snapshotCache, tblock.Hash);

            Assert.AreEqual((uint)1, block.Index);
            Assert.AreEqual(UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff02"), block.MerkleRoot);
            Assert.AreEqual(2, block.Transactions.Length);
            Assert.AreEqual(tx1.Hash, block.Transactions[0].Hash);
            Assert.AreEqual(tblock.Header.Witness.InvocationScript.Span.ToHexString(), block.Witness.InvocationScript.Span.ToHexString());
            Assert.AreEqual(tblock.Header.Witness.VerificationScript.Span.ToHexString(), block.Witness.VerificationScript.Span.ToHexString());
        }

        [TestMethod]
        public void TestGetHeader()
        {
            TrimmedBlock tblock = GetTrimmedBlockWithNoTransaction();
            Header header = tblock.Header;
            Assert.AreEqual(UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01"), header.PrevHash);
            Assert.AreEqual(UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff02"), header.MerkleRoot);
        }

        [TestMethod]
        public void TestGetSize()
        {
            TrimmedBlock tblock = GetTrimmedBlockWithNoTransaction();
            tblock.Hashes = new UInt256[] { TestUtils.GetTransaction(UInt160.Zero).Hash };
            Assert.AreEqual(146, tblock.Size); // 138 + 8
        }

        [TestMethod]
        public void TestDeserialize()
        {
            TrimmedBlock tblock = GetTrimmedBlockWithNoTransaction();
            tblock.Hashes = new UInt256[] { TestUtils.GetTransaction(UInt160.Zero).Hash };
            var newBlock = new TrimmedBlock();
            using (MemoryStream ms = new(1024))
            using (BinaryWriter writer = new(ms))
            {
                tblock.Serialize(writer);
                MemoryReader reader = new(ms.ToArray());
                newBlock.Deserialize(ref reader);
            }
            Assert.AreEqual(newBlock.Hashes.Length, tblock.Hashes.Length);
            Assert.AreEqual(newBlock.Header.ToJson(ProtocolSettings.Default).ToString(), tblock.Header.ToJson(TestProtocolSettings.Default).ToString());
        }
    }
}

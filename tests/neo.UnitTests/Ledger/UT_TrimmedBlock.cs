using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.UnitTests.SmartContract;
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
            var snapshot = TestBlockchain.GetTestSnapshot();
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
            UT_SmartContractHelper.TransactionAdd(snapshot, state1, state2);

            TrimmedBlock tblock = GetTrimmedBlockWithNoTransaction();
            tblock.Hashes = new UInt256[] { tx1.Hash, tx2.Hash };
            UT_SmartContractHelper.BlocksAdd(snapshot, tblock.Hash, tblock);

            Block block = NativeContract.Ledger.GetBlock(snapshot, tblock.Hash);

            block.Index.Should().Be(1);
            block.MerkleRoot.Should().Be(UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff02"));
            block.Transactions.Length.Should().Be(2);
            block.Transactions[0].Hash.Should().Be(tx1.Hash);
            block.Witness.InvocationScript.ToHexString().Should().Be(tblock.Header.Witness.InvocationScript.ToHexString());
            block.Witness.VerificationScript.ToHexString().Should().Be(tblock.Header.Witness.VerificationScript.ToHexString());
        }

        [TestMethod]
        public void TestGetHeader()
        {
            TrimmedBlock tblock = GetTrimmedBlockWithNoTransaction();
            Header header = tblock.Header;
            header.PrevHash.Should().Be(UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01"));
            header.MerkleRoot.Should().Be(UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff02"));
        }

        [TestMethod]
        public void TestGetSize()
        {
            TrimmedBlock tblock = GetTrimmedBlockWithNoTransaction();
            tblock.Hashes = new UInt256[] { TestUtils.GetTransaction(UInt160.Zero).Hash };
            tblock.Size.Should().Be(146); // 138 + 8
        }

        [TestMethod]
        public void TestDeserialize()
        {
            TrimmedBlock tblock = GetTrimmedBlockWithNoTransaction();
            tblock.Hashes = new UInt256[] { TestUtils.GetTransaction(UInt160.Zero).Hash };
            var newBlock = new TrimmedBlock();
            using (MemoryStream ms = new(1024))
            using (BinaryWriter writer = new(ms))
            using (BinaryReader reader = new(ms))
            {
                tblock.Serialize(writer);
                ms.Seek(0, SeekOrigin.Begin);
                newBlock.Deserialize(reader);
            }
            tblock.Hashes.Length.Should().Be(newBlock.Hashes.Length);
            tblock.Header.ToJson(ProtocolSettings.Default).ToString().Should().Be(newBlock.Header.ToJson(ProtocolSettings.Default).ToString());
        }
    }
}

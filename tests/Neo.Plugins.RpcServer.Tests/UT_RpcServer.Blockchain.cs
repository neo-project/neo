// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcServer.Blockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests.SmartContract;
using Neo.VM;
using System;

namespace Neo.Plugins.RpcServer.Tests
{
    public partial class UT_RpcServer
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
        public void TestGetBestBlockHash()
        {
            var key = new KeyBuilder(-4, 12);
            var expectedHash = UInt256.Zero;

            var snapshot = _neoSystem.GetSnapshot();
            var b = snapshot.GetAndChange(key, () => new StorageItem(new HashIndexState())).GetInteroperable<HashIndexState>();
            b.Hash = UInt256.Zero;
            b.Index = 100;
            snapshot.Commit();
            var result = _rpcServer.GetBestBlockHash([]);
            // Assert
            Assert.AreEqual(expectedHash.ToString(), result.AsString());
        }

        [TestMethod]
        public void TestGetBlockByHash()
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
            block.Witness.InvocationScript.Span.ToHexString().Should().Be(tblock.Header.Witness.InvocationScript.Span.ToHexString());
            block.Witness.VerificationScript.Span.ToHexString().Should().Be(tblock.Header.Witness.VerificationScript.Span.ToHexString());


            NativeContract.Ledger.GetBlock(mockSnapshot.Object, blockHash).Returns(block);

            var parameters = new JArray(blockHash.ToString(), true);

            // Act
            var result = _rpcServer.GetBlock(parameters);

            // Assert
            Assert.IsInstanceOfType(result, typeof(JObject));
        }

        //
        // [TestMethod]
        // public void TestGetBlockCount()
        // {
        //     // Arrange
        //     var mockSnapshot = new Mock<ISnapshot>();
        //     _systemMock.Setup(s => s.StoreView).Returns(mockSnapshot.Object);
        //     NativeContract.Ledger.CurrentIndex(mockSnapshot.Object).Returns(1234u);
        //
        //     // Act
        //     var result = _rpcServer.GetBlockCount(new JArray());
        //
        //     // Assert
        //     Assert.AreEqual(1235u, result.AsNumber());
        // }
    }
}

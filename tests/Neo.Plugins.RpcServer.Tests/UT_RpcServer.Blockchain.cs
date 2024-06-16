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

using Akka.Util.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using System;

namespace Neo.Plugins.RpcServer.Tests
{
    public partial class UT_RpcServer
    {

        [TestMethod]
        public void TestGetBestBlockHash()
        {
            var key = NativeContract.Ledger.CreateStorageKey(12);
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
            var snapshot = _neoSystem.GetSnapshot();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            var parameters = new JArray(block.Hash.ToString(), false);
            var result = _rpcServer.GetBlock(parameters);
            var blockArr = Convert.FromBase64String(result.AsString());
            var block2 = blockArr.AsSerializable<Block>();
            block2.Transactions.ForEach(tx =>
            {
                Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(UnitTests.TestProtocolSettings.Default));
            });
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

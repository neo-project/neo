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

using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Text;
using Neo.Json;

namespace Neo.Plugins.RpcServer.Tests
{
    public partial class UT_RpcServer
    {

        [TestMethod]
        public void TestGetBestBlockHash()
        {
            // Arrange
            var expectedHash = UInt256.Parse("0x0");

            _iSnapshotMock.Setup(s => s.TryGet(new byte[]{0x00, 0x00, 0x00})).Returns([0x00]);
            NativeContract.Ledger.CurrentHash(_neoSystem.StoreView);

            // Act
            var result = _rpcServer.GetBestBlockHash(new JArray());

            // Assert
            Assert.AreEqual(expectedHash.ToString(), result.AsString());
        }
        //
        // [TestMethod]
        // public void TestGetBlockByHash()
        // {
        //     // Arrange
        //     var blockHash = UInt256.Parse("0x0");
        //     var mockSnapshot = new Mock<ISnapshot>();
        //     var block = new Block();
        //     _systemMock.Setup(s => s.StoreView).Returns(mockSnapshot.Object);
        //     NativeContract.Ledger.GetBlock(mockSnapshot.Object, blockHash).Returns(block);
        //
        //     var parameters = new JArray(blockHash.ToString(), true);
        //
        //     // Act
        //     var result = _rpcServer.GetBlock(parameters);
        //
        //     // Assert
        //     Assert.IsInstanceOfType(result, typeof(JObject));
        // }
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

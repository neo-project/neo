// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcServer.cs file belongs to the neo project and is free
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
using Neo.Persistence;
using System;
using System.Text;

namespace Neo.Plugins.RpcServer.Tests
{
    [TestClass]
    public partial class UT_RpcServer
    {
        private Mock<MockNeoSystem> _systemMock;
        private SnapshotCache _snapshotCache;
        private MemoryPool _memoryPool;
        private RpcServerSettings _settings;
        private RpcServer _rpcServer;

        [TestInitialize]
        public void TestSetup()
        {
            // Mock IReadOnlyStore
            var mockStore = new Mock<IReadOnlyStore>();

            // Initialize SnapshotCache with the mock IReadOnlyStore
            _snapshotCache = new SnapshotCache(mockStore.Object);

            // Initialize NeoSystem
            var neoSystem = new NeoSystem(TestProtocolSettings.Default, new TestBlockchain.StoreProvider());

            // Initialize MemoryPool with the NeoSystem
            _memoryPool = new MemoryPool(neoSystem);

            // Set up the mock system with the correct constructor arguments
            _systemMock = new Mock<MockNeoSystem>(_snapshotCache, _memoryPool);

            _rpcServer = new RpcServer(_systemMock.Object, RpcServerSettings.Default);
        }

        // [TestMethod]
        // public void TestCheckAuth_ValidCredentials_ReturnsTrue()
        // {
        //     var context = new DefaultHttpContext();
        //     context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:testpass"));
        //     var result = _rpcServer.CheckAuth(context);
        //     Assert.IsTrue(result);
        // }
    }
}

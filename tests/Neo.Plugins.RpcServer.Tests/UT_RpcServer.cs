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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Persistence;

namespace Neo.Plugins.RpcServer.Tests
{
    [TestClass]
    public partial class UT_RpcServer
    {
        private Mock<NeoSystem> _mockSystem;
        private Mock<SnapshotCache> _mockSnapshot;
        private Mock<MemoryPool> _mockMemPool;
        private RpcServer _rpcServer;


        [TestInitialize]
        public void Initialize()
        {
            // Set up the mock system
            _mockSystem = new Mock<NeoSystem>(TestProtocolSettings.Default, new TestBlockchain.StoreProvider());

            // Set up the mock snapshot
            _mockSnapshot = new Mock<SnapshotCache>();
            _mockSystem.Setup(s => s.GetSnapshot()).Returns(_mockSnapshot.Object);

            // Set up the mock memory pool
            _mockMemPool = new Mock<MemoryPool>(_mockSystem.Object);
            _mockSystem.SetupGet(s => s.MemPool).Returns(_mockMemPool.Object);

            // Initialize the RpcServer with the mock system
            _rpcServer = new RpcServer(_mockSystem.Object, new RpcServerSettings());
        }
    }
}

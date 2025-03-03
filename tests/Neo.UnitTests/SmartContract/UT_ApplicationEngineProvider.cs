// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ApplicationEngineProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System.Reflection;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngineProvider
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
            ApplicationEngine.Provider = null;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ApplicationEngine.Provider = null;
        }

        [TestMethod]
        public void TestSetAppEngineProvider()
        {
            ApplicationEngine.Provider = new TestProvider();
            var snapshot = _snapshotCache.CloneCache();

            using var appEngine = ApplicationEngine.Create(TriggerType.Application,
                null, snapshot, gas: 0, settings: TestBlockchain.TheNeoSystem.Settings);
            Assert.IsTrue(appEngine is TestEngine);
        }

        [TestMethod]
        public void TestDefaultAppEngineProvider()
        {
            var snapshot = _snapshotCache.CloneCache();
            using var appEngine = ApplicationEngine.Create(TriggerType.Application,
                null, snapshot, gas: 0, settings: TestBlockchain.TheNeoSystem.Settings);
            Assert.IsTrue(appEngine is ApplicationEngine);
        }

        [TestMethod]
        public void TestInitNonce()
        {
            var block = new Block { Header = new() { Nonce = 0x0102030405060708 } };
            using var app = new TestEngine(TriggerType.Application,
                null, null, block, TestBlockchain.TheNeoSystem.Settings, 0, null, null);

            var nonceData = typeof(ApplicationEngine)
                .GetField("nonceData", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(app) as byte[];
            Assert.IsNotNull(nonceData);
            Assert.AreEqual(nonceData.ToHexString(), "08070605040302010000000000000000");
        }

        class TestProvider : IApplicationEngineProvider
        {
            public ApplicationEngine Create(TriggerType trigger, IVerifiable container, DataCache snapshot,
                Block persistingBlock, ProtocolSettings settings, long gas, IDiagnostic diagnostic, JumpTable jumpTable)
            {
                return new TestEngine(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic, jumpTable);
            }
        }

        class TestEngine : ApplicationEngine
        {
            public TestEngine(TriggerType trigger, IVerifiable container, DataCache snapshotCache,
                Block persistingBlock, ProtocolSettings settings, long gas, IDiagnostic diagnostic, JumpTable jumpTable)
                : base(trigger, container, snapshotCache, persistingBlock, settings, gas, diagnostic, jumpTable)
            {
            }
        }
    }
}

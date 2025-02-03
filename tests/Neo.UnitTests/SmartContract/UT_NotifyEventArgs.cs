// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NotifyEventArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_NotifyEventArgs
    {
        private StorageCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void TestGetScriptContainer()
        {
            IVerifiable container = new TestVerifiable();
            UInt160 script_hash = new byte[] { 0x00 }.ToScriptHash();
            NotifyEventArgs args = new NotifyEventArgs(container, script_hash, "Test", null);
            Assert.AreEqual(container, args.ScriptContainer);
        }

        [TestMethod]
        public void TestIssue3300() // https://github.com/neo-project/neo/issues/3300
        {
            var snapshot = _snapshotCache.CloneCache();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default, gas: 1100_00000000);
            using (var script = new ScriptBuilder())
            {
                // Build call script calling disallowed method.
                script.Emit(OpCode.NOP);
                // Mock executing state to be a contract-based.
                engine.LoadScript(script.ToArray());
            }

            var ns = new Array(engine.ReferenceCounter);
            for (var i = 0; i < 500; i++)
            {
                ns.Add("");
            };

            var hash = UInt160.Parse("0x179ab5d297fd34ecd48643894242fc3527f42853");
            engine.SendNotification(hash, "Test", ns);
            // This should have being 0, but we have optimized the vm to not clean the reference counter
            // unless it is necessary, so the reference counter will be 1000.
            // Same reason why its 1504 instead of 504.
            Assert.AreEqual(1000, engine.ReferenceCounter.Count);
            // This will make a deepcopy for the notification, along with the 500 state items.
            engine.GetNotifications(hash);
            // With the fix of issue 3300, the reference counter calculates not only
            // the notifaction items, but also the subitems of the notification state.
            Assert.AreEqual(1504, engine.ReferenceCounter.Count);
        }
    }
}

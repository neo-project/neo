// Copyright (C) 2015-2025 The Neo Project.
//
// UT_TestApplicationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.SmartContract;
using Neo.Build.Core.Tests.Helpers;
using Neo.VM;
using System.Linq;

namespace Neo.Build.Core.Tests.SmartContract
{
    [TestClass]
    public class UT_TestApplicationEngine
    {
        [TestMethod]
        public void TestApplicationEngine()
        {
            var engine = new TestApplicationEngine(TestNode.BuildSettings, TestNode.NeoSystem.GetSnapshotCache(), TestNode.FactoryLogger);

            var account = TestNode.Wallet.GetDefaultAccount();

            using var sb = new ScriptBuilder()
                .EmitPush("Hello World!");

            engine.LoadScript(sb.ToArray());
            var state = engine.Execute();

            Assert.AreEqual(VMState.HALT, state);
            Assert.AreEqual(1, engine.ResultStack.Count);

            var stackItem = engine.ResultStack.First();
            var stackItemString = stackItem.GetString();

            Assert.AreEqual("Hello World!", stackItemString);
        }
    }
}

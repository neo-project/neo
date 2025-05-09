// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ApplicationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Factories;
using Neo.Build.Core.SmartContract;
using Neo.Build.Core.Tests.Helpers;
using Neo.SmartContract.Native;
using Neo.VM;

namespace Neo.Build.Core.Tests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine
    {
        [TestMethod]
        public void TestMethod1()
        {
            var engine = new DebugApplicationEngine(TestNode.NeoSystem.Settings,
                TestNode.NeoSystem.StoreView,
                20_00000000L,
                new(),
                Neo.SmartContract.TriggerType.Application,
                null,
                NativeContract.Ledger.GetBlock(TestNode.NeoSystem.StoreView, 5_864_901),
                null,
                TestNode.FactoryLogger,
                ApplicationEngineFactory.SystemCallBaseServices);

            engine.LoadScript(new byte[] { (byte)OpCode.RET });

            engine.Execute();
        }
    }
}

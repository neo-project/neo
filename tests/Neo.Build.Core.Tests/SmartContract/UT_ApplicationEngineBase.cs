// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ApplicationEngineBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Builders;
using Neo.Build.Core.Tests.Helpers;
using Neo.Build.Core.Tests.Helpers.SmartContract;
using Neo.Builders;
using Neo.SmartContract;
using Neo.VM;
using System.Numerics;

namespace Neo.Build.Core.Tests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngineBase
    {
        [TestMethod]
        public void TestUnitTestApplicationEngineSimple()
        {
            var pb = BlockBuilder
                .CreateNext(TestNode.NeoSystem.GenesisBlock)
                .Build();

            using var appEngine = new UnitTestApplicationEngine(
                TestNode.NeoSystem.Settings,
                TestNode.NeoSystem.StoreView,
                persistingBlock: pb,
                loggerFactory: TestDefaults.FactoryLogger,
                container: TransactionBuilder.CreateEmpty().Build());

            using var sb = new ScriptBuilder()
                .EmitSysCall(ApplicationEngine.System_Runtime_GetRandom);

            appEngine.LoadScript(sb.ToArray());

            Assert.AreEqual(VMState.HALT, appEngine.Execute());
            Assert.AreEqual(1, appEngine.ResultStack.Count);

            var actualRandomNumber = appEngine.ResultStack.Pop().GetInteger();
            Assert.IsTrue(BigInteger.Zero < actualRandomNumber);
        }

        [TestMethod]
        public void TestApplicationEngineSimple()
        {
            ApplicationEngine.Provider = UnitTestApplicationEngineProvider.Instance;

            var pb = BlockBuilder
                .CreateNext(TestNode.NeoSystem.GenesisBlock)
                .Build();

            using var appEngine = ApplicationEngine.Create(
                TriggerType.Application,
                TransactionBuilder.CreateEmpty().Build(),
                TestNode.NeoSystem.StoreView,
                settings: TestNode.NeoSystem.Settings,
                persistingBlock: pb);

            using var sb = new ScriptBuilder()
                .EmitSysCall(ApplicationEngine.System_Runtime_GetRandom);

            appEngine.LoadScript(sb.ToArray());

            Assert.AreEqual(VMState.HALT, appEngine.Execute());
            Assert.AreEqual(1, appEngine.ResultStack.Count);

            var actualRandomNumber = appEngine.ResultStack.Pop().GetInteger();
            Assert.IsTrue(BigInteger.Zero < actualRandomNumber);
        }
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ApplicationEngineExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Extensions.SmartContract;
using Neo.Build.Core.Tests.Helpers;
using Neo.Builders;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;

namespace Neo.Build.Core.Tests.Extensions
{
    [TestClass]
    public class UT_ApplicationEngineExtensions
    {
        [TestMethod]
        public void TestGetContractState()
        {
            using var appEngine = ApplicationEngine.Create(
                TriggerType.Application,
                TransactionBuilder.CreateEmpty().Build(),
                TestNode.NeoSystem.StoreView,
                settings: TestNode.NeoSystem.Settings);

            var actualContractState = appEngine.GetContractState<NeoToken>();
            Assert.IsNotNull(actualContractState);
            Assert.AreEqual(nameof(NeoToken), actualContractState.Manifest.Name);

            actualContractState = appEngine.GetContractState(nameof(GasToken));
            Assert.IsNotNull(actualContractState);
            Assert.AreEqual(nameof(GasToken), actualContractState.Manifest.Name);
        }

        [TestMethod]
        public void TestGetContractStorages()
        {
            using var appEngine = ApplicationEngine.Create(
                TriggerType.Application,
                TransactionBuilder.CreateEmpty().Build(),
                TestNode.NeoSystem.StoreView,
                settings: TestNode.NeoSystem.Settings);

            var actualContractStorages = appEngine.GetContractStorages<NeoToken>();
            Assert.IsNotNull(actualContractStorages);
            Assert.AreEqual(6, actualContractStorages.Count);
        }

        [TestMethod]
        public void TestExecuteScript()
        {
            using var sb = new ScriptBuilder()
                .Emit(OpCode.PUSHT);

            using var appEngine = ApplicationEngine.Create(
                TriggerType.Application,
                TransactionBuilder.CreateEmpty().Build(),
                TestNode.NeoSystem.StoreView,
                settings: TestNode.NeoSystem.Settings);

            var actualVMState = appEngine.ExecuteScript(sb.ToArray());
            Assert.AreEqual(VMState.HALT, actualVMState);
            Assert.AreEqual(1, appEngine.ResultStack.Count);
            Assert.IsTrue(appEngine.ResultStack[0].GetBoolean());
        }
    }
}

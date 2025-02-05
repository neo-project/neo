// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ApplicationEngine.Contract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System.Linq;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void TestCreateStandardAccount()
        {
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default;
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default, gas: 1100_00000000);

            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_CreateStandardAccount, settings.StandbyCommittee[0].EncodePoint(true));
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            Assert.AreEqual(Contract.CreateSignatureRedeemScript(settings.StandbyCommittee[0]).ToScriptHash(), new UInt160(result.GetSpan()));
        }

        [TestMethod]
        public void TestCreateStandardMultisigAccount()
        {
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default;
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_CreateMultisigAccount, new object[]
            {
                2,
                3,
                settings.StandbyCommittee[0].EncodePoint(true),
                settings.StandbyCommittee[1].EncodePoint(true),
                settings.StandbyCommittee[2].EncodePoint(true)
            });
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            Assert.AreEqual(Contract.CreateMultiSigRedeemScript(2, settings.StandbyCommittee.Take(3).ToArray()).ToScriptHash(), new UInt160(result.GetSpan()));
        }
    }
}

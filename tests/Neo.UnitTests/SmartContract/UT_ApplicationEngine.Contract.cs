// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ApplicationEngine.Contract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using System.Linq;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
        [TestMethod]
        public void TestCreateStandardAccount()
        {
            var settings = TestProtocolSettings.Default;
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, null, settings: TestProtocolSettings.Default, gas: 1100_00000000);

            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_CreateStandardAccount, settings.StandbyCommittee[0].EncodePoint(true));
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            new UInt160(result.GetSpan()).Should().Be(Contract.CreateSignatureRedeemScript(settings.StandbyCommittee[0]).ToScriptHash());
        }

        [TestMethod]
        public void TestCreateStandardMultisigAccount()
        {
            var settings = TestProtocolSettings.Default;
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, null, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

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
            new UInt160(result.GetSpan()).Should().Be(Contract.CreateMultiSigRedeemScript(2, settings.StandbyCommittee.Take(3).ToArray()).ToScriptHash());
        }
    }
}

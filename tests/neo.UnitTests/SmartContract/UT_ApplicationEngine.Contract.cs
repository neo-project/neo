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
            var settings = ProtocolSettings.Default;
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, null, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

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
            var settings = ProtocolSettings.Default;
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, null, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_CreateStandardMultisigAccount, new object[] { 2,
                new object[] {
                    settings.StandbyCommittee[0].EncodePoint(true) ,
                    settings.StandbyCommittee[1].EncodePoint(true),
                    settings.StandbyCommittee[2].EncodePoint(true)
                }
            });
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            new UInt160(result.GetSpan()).Should().Be(Contract.CreateMultiSigRedeemScript(2, settings.StandbyCommittee.Take(3).ToArray()).ToScriptHash());
        }
    }
}

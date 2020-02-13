using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OraclePolicy
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void Check_GetPerRequestFee()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            // Fake blockchain
            OraclePolicyContract contract = NativeContract.OraclePolicy;
            Assert.AreEqual(contract.GetPerRequestFee(snapshot), 1000);
        }

        [TestMethod]
        public void Check_GetTimeOutMilliSeconds()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            // Fake blockchain
            OraclePolicyContract contract = NativeContract.OraclePolicy;
            Assert.AreEqual(contract.GetPerRequestFee(snapshot), 1000);
        }

        [TestMethod]
        public void Check_GetOracleValidators()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            // Fake blockchain
            OraclePolicyContract contract = NativeContract.OraclePolicy;
            ECPoint[] obj = contract.GetOracleValidators(snapshot);
            Assert.AreEqual(obj.Length, 7);
        }

        [TestMethod]
        public void Test_DelegateOracleValidator()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            // Fake blockchain
            OraclePolicyContract contract = NativeContract.OraclePolicy;
            ECPoint[] obj = contract.GetOracleValidators(snapshot);
            Assert.AreEqual(obj.Length, 7);

            // Without signature
            byte[] privateKey1 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair1 = new KeyPair(privateKey1);
            ECPoint pubkey1 = keyPair1.PublicKey;

            byte[] privateKey2 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair2 = new KeyPair(privateKey2);
            ECPoint pubkey2 = keyPair2.PublicKey;

            Array array = new Array();
            array.Add(StackItem.FromInterface(privateKey1));
            array.Add(StackItem.FromInterface(privateKey2));

/*            var ret = NativeContract.OraclePolicy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
                "DelegateOracleValidator", new ContractParameter(ContractParameterType.Hash160) { Value = 1 }, new ContractParameter(ContractParameterType.Array) { Value = array });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeFalse();*/
        }
        internal static (bool State, bool Result) Check_DelegateOracleValidator(StoreView snapshot, byte[] account, byte[][] pubkeys, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
    new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.OraclePolicy.Script);

            var script = new ScriptBuilder();

            foreach (var ec in pubkeys) script.EmitPush(ec);
            script.EmitPush(pubkeys.Length);
            script.Emit(OpCode.PACK);

            script.EmitPush(account.ToArray());
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush("DelegateOracleValidator");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.ToBoolean());
        }
    }
}

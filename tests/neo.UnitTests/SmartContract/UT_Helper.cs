using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using static Neo.SmartContract.Helper;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Helper
    {
        private KeyPair _key;

        [TestInitialize]
        public void Init()
        {
            var pk = new byte[32];
            new Random().NextBytes(pk);
            _key = new KeyPair(pk);
        }

        [TestMethod]
        public void TestGetContractHash()
        {
            var nef = new NefFile()
            {
                Compiler = "test",
                Source = string.Empty,
                Tokens = Array.Empty<MethodToken>(),
                Script = new byte[] { 1, 2, 3 }
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);

            Assert.AreEqual("0x9b9628e4f1611af90e761eea8cc21372380c74b6", Neo.SmartContract.Helper.GetContractHash(UInt160.Zero, nef.CheckSum, "").ToString());
            Assert.AreEqual("0x66eec404d86b918d084e62a29ac9990e3b6f4286", Neo.SmartContract.Helper.GetContractHash(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"), nef.CheckSum, "").ToString());
        }

        [TestMethod]
        public void TestIsMultiSigContract()
        {
            var case1 = new byte[]
            {
                0, 2, 12, 33, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221,
                221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 12, 33, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 0,
            };
            Assert.IsFalse(IsMultiSigContract(case1));

            var case2 = new byte[]
            {
                18, 12, 33, 2, 111, 240, 59, 148, 146, 65, 206, 29, 173, 212, 53, 25, 230, 150, 14, 10, 133, 180, 26,
                105, 160, 92, 50, 129, 3, 170, 43, 206, 21, 148, 202, 22, 12, 33, 2, 111, 240, 59, 148, 146, 65, 206,
                29, 173, 212, 53, 25, 230, 150, 14, 10, 133, 180, 26, 105, 160, 92, 50, 129, 3, 170, 43, 206, 21, 148,
                202, 22, 18
            };
            Assert.IsFalse(IsMultiSigContract(case2));
        }

        [TestMethod]
        public void TestSignatureContractCost()
        {
            var contract = Contract.CreateSignatureContract(_key.PublicKey);

            var tx = TestUtils.CreateRandomHashTransaction();
            tx.Signers[0].Account = contract.ScriptHash;

            using ScriptBuilder invocationScript = new();
            invocationScript.EmitPush(Neo.Wallets.Helper.Sign(tx, _key, ProtocolSettings.Default.Network));
            tx.Witnesses = new Witness[] { new Witness() { InvocationScript = invocationScript.ToArray(), VerificationScript = contract.Script } };

            using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, null, null, ProtocolSettings.Default);
            engine.LoadScript(contract.Script);
            engine.LoadScript(new Script(invocationScript.ToArray(), true), configureState: p => p.CallFlags = CallFlags.None);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());

            Assert.AreEqual(Neo.SmartContract.Helper.SignatureContractCost() * PolicyContract.DefaultExecFeeFactor, engine.GasConsumed);
        }

        [TestMethod]
        public void TestMultiSignatureContractCost()
        {
            var contract = Contract.CreateMultiSigContract(1, new ECPoint[] { _key.PublicKey });

            var tx = TestUtils.CreateRandomHashTransaction();
            tx.Signers[0].Account = contract.ScriptHash;

            using ScriptBuilder invocationScript = new();
            invocationScript.EmitPush(Neo.Wallets.Helper.Sign(tx, _key, ProtocolSettings.Default.Network));

            using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, null, null, ProtocolSettings.Default);
            engine.LoadScript(contract.Script);
            engine.LoadScript(new Script(invocationScript.ToArray(), true), configureState: p => p.CallFlags = CallFlags.None);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());

            Assert.AreEqual(Neo.SmartContract.Helper.MultiSignatureContractCost(1, 1) * PolicyContract.DefaultExecFeeFactor, engine.GasConsumed);
        }
    }
}

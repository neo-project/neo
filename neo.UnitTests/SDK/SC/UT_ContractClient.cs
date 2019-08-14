using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SDK.SC;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.SDK.TX;
using Neo.VM;
using Neo.Wallets;

namespace Neo.UnitTests.SDK.SC
{
    [TestClass]
    public class UT_ContractClient
    {
        Mock<RpcClient> rpcClientMock;
        readonly KeyPair keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
        UInt160 Sender => keyPair1.ScriptHash;

        [TestInitialize]
        public void TestSetup()
        {
            rpcClientMock = UT_TxManager.MockRpcClient(Sender, new byte[0]);
        }

        private void MockInvokeScript(byte[] script, RpcInvokeResult result)
        {
            rpcClientMock.Setup(p => p.InvokeScript(script)).Returns(result).Verifiable();
        }

        [TestMethod]
        public void TestMakeScript()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);

            Assert.AreEqual("14000000000000000000000000000000000000000051c10962616c616e63654f66142582d1b275e86c8f0e93a9b2facd5fdb760976a168627d5b52",
                            testScript.ToHexString());
        }

        [TestMethod]
        public void TestInvoke()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);
            MockInvokeScript(testScript, new RpcInvokeResult { Stack = new[] { new ContractParameter { Type = ContractParameterType.ByteArray, Value = "00e057eb481b".HexToBytes() } } });

            ContractClient contractClient = new ContractClient(rpcClientMock.Object);
            var result = contractClient.TestInvoke(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);

            Assert.AreEqual(30000000000000L, (long)result.Stack[0].ToStackItem().GetBigInteger());
        }

        [TestMethod]
        public void TestDeployContract()
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(InteropService.Neo_Contract_Create, new byte[1], ContractFeatures.HasStorage | ContractFeatures.Payable);
                script = sb.ToArray();
            }

            MockInvokeScript(script, new RpcInvokeResult { GasConsumed = "100" });

            ContractClient contractClient = new ContractClient(rpcClientMock.Object);
            var result = contractClient.DeployContract(new byte[1], true, true, keyPair1);

            Assert.IsNotNull(result);
        }
    }
}

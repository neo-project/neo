using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System.Numerics;

namespace Neo.UnitTests.Network.RPC
{
    [TestClass]
    public class UT_Nep5API
    {
        Mock<RpcClient> rpcClientMock;
        readonly KeyPair keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
        UInt160 Sender => keyPair1.ScriptHash;
        Nep5API nep5API;

        [TestInitialize]
        public void TestSetup()
        {
            rpcClientMock = UT_TransactionManager.MockRpcClient(Sender, new byte[0]);
            nep5API = new Nep5API(rpcClientMock.Object);
        }

        private void MockInvokeScript(byte[] script, RpcInvokeResult result)
        {
            rpcClientMock.Setup(p => p.InvokeScript(script)).Returns(result).Verifiable();
        }

        [TestMethod]
        public void TestBalanceOf()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);
            MockInvokeScript(testScript, new RpcInvokeResult { Stack = new[] { new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(10000) } } });

            var balance = nep5API.BalanceOf(NativeContract.GAS.Hash, UInt160.Zero);
            Assert.AreEqual(10000, (int)balance);
        }

        [TestMethod]
        public void TestGetName()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.GAS.Hash, "name");
            MockInvokeScript(testScript, new RpcInvokeResult { Stack = new[] { new ContractParameter { Type = ContractParameterType.String, Value = NativeContract.GAS.Name } } });

            var result = nep5API.Name(NativeContract.GAS.Hash);
            Assert.AreEqual(NativeContract.GAS.Name, result);
        }

        [TestMethod]
        public void TestGetSymbol()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.GAS.Hash, "symbol");
            MockInvokeScript(testScript, new RpcInvokeResult { Stack = new[] { new ContractParameter { Type = ContractParameterType.String, Value = NativeContract.GAS.Symbol } } });

            var result = nep5API.Symbol(NativeContract.GAS.Hash);
            Assert.AreEqual(NativeContract.GAS.Symbol, result);
        }

        [TestMethod]
        public void TestGetDecimals()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.GAS.Hash, "decimals");
            MockInvokeScript(testScript, new RpcInvokeResult { Stack = new[] { new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(NativeContract.GAS.Decimals) } } });

            var result = nep5API.Decimals(NativeContract.GAS.Hash);
            Assert.AreEqual(NativeContract.GAS.Decimals, (byte)result);
        }

        [TestMethod]
        public void TestGetTotalSupply()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.GAS.Hash, "totalSupply");
            MockInvokeScript(testScript, new RpcInvokeResult { Stack = new[] { new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) } } });

            var result = nep5API.TotalSupply(NativeContract.GAS.Hash);
            Assert.AreEqual(1_00000000, (int)result);
        }

        [TestMethod]
        public void TestTransfer()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.GAS.Hash, "transfer", Sender, UInt160.Zero, new BigInteger(1_00000000));
            MockInvokeScript(testScript, new RpcInvokeResult { GasConsumed = "1000000" });

            var result = nep5API.Transfer(NativeContract.GAS.Hash, keyPair1, UInt160.Zero, new BigInteger(1_00000000));

            Assert.IsNotNull(result);
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Network.RPC;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System.Numerics;

namespace Neo.UnitTests.Network.RPC
{
    [TestClass]
    public class UT_Nep5API
    {
        Mock<RpcClient> rpcClientMock;
        KeyPair keyPair1;
        UInt160 sender;
        Nep5API nep5API;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            sender = Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
            rpcClientMock = UT_TransactionManager.MockRpcClient(sender, new byte[0]);
            nep5API = new Nep5API(rpcClientMock.Object);
        }

        [TestMethod]
        public void TestBalanceOf()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("balanceOf", UInt160.Zero);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(10000) });

            var balance = nep5API.BalanceOf(NativeContract.GAS.Hash, UInt160.Zero);
            Assert.AreEqual(10000, (int)balance);
        }

        [TestMethod]
        public void TestGetName()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("name");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.String, Value = NativeContract.GAS.Name });

            var result = nep5API.Name(NativeContract.GAS.Hash);
            Assert.AreEqual(NativeContract.GAS.Name, result);
        }

        [TestMethod]
        public void TestGetSymbol()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("symbol");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.String, Value = NativeContract.GAS.Symbol });

            var result = nep5API.Symbol(NativeContract.GAS.Hash);
            Assert.AreEqual(NativeContract.GAS.Symbol, result);
        }

        [TestMethod]
        public void TestGetDecimals()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("decimals");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(NativeContract.GAS.Decimals) });

            var result = nep5API.Decimals(NativeContract.GAS.Hash);
            Assert.AreEqual(NativeContract.GAS.Decimals, (byte)result);
        }

        [TestMethod]
        public void TestGetTotalSupply()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("totalSupply");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            var result = nep5API.TotalSupply(NativeContract.GAS.Hash);
            Assert.AreEqual(1_00000000, (int)result);
        }

        [TestMethod]
        public void TestTransfer()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("transfer", sender, UInt160.Zero, new BigInteger(1_00000000));
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter());

            var result = nep5API.Transfer(NativeContract.GAS.Hash, keyPair1, UInt160.Zero, new BigInteger(1_00000000));
            Assert.IsNotNull(result);
        }
    }
}

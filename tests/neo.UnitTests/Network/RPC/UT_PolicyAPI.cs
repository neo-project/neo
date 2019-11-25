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
    public class UT_PolicyAPI
    {
        Mock<RpcClient> rpcClientMock;
        KeyPair keyPair1;
        UInt160 sender;
        PolicyAPI policyAPI;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            sender = Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
            rpcClientMock = UT_TransactionManager.MockRpcClient(sender, new byte[0]);
            policyAPI = new PolicyAPI(rpcClientMock.Object);
        }

        [TestMethod]
        public void TestGetMaxTransactionsPerBlock()
        {
            byte[] testScript = NativeContract.Policy.Hash.MakeScript("getMaxTransactionsPerBlock");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(512) });

            var result = policyAPI.GetMaxTransactionsPerBlock();
            Assert.AreEqual(512u, result);
        }

        [TestMethod]
        public void TestGetMaxBlockSize()
        {
            byte[] testScript = NativeContract.Policy.Hash.MakeScript("getMaxBlockSize");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1024u * 256u) });

            var result = policyAPI.GetMaxBlockSize();
            Assert.AreEqual(1024u * 256u, result);
        }

        [TestMethod]
        public void TestGetFeePerByte()
        {
            byte[] testScript = NativeContract.Policy.Hash.MakeScript("getFeePerByte");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1000) });

            var result = policyAPI.GetFeePerByte();
            Assert.AreEqual(1000L, result);
        }

        [TestMethod]
        public void TestGetBlockedAccounts()
        {
            byte[] testScript = NativeContract.Policy.Hash.MakeScript("getBlockedAccounts");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Array, Value = new[] { new ContractParameter { Type = ContractParameterType.Hash160, Value = UInt160.Zero } } });

            var result = policyAPI.GetBlockedAccounts();
            Assert.AreEqual(UInt160.Zero, result[0]);
        }
    }
}

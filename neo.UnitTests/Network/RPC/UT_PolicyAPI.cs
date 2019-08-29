using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Network.RPC;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System.Numerics;

namespace Neo.UnitTests.Network.RPC
{
    [TestClass]
    public class UT_PolicyAPI
    {
        Mock<RpcClient> rpcClientMock;
        readonly KeyPair keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
        UInt160 Sender => Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
        PolicyAPI policyAPI;

        [TestInitialize]
        public void TestSetup()
        {
            rpcClientMock = UT_TransactionManager.MockRpcClient(Sender, new byte[0]);
            policyAPI = new PolicyAPI(rpcClientMock.Object);
        }

        [TestMethod]
        public void TestGetMaxTransactionsPerBlock()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.Policy.Hash, "getMaxTransactionsPerBlock");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(512) });

            var result = policyAPI.GetMaxTransactionsPerBlock();
            Assert.AreEqual(512, (int)result);
        }

        [TestMethod]
        public void TestGetFeePerByte()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.Policy.Hash, "getFeePerByte");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1000) });

            var result = policyAPI.GetFeePerByte();
            Assert.AreEqual(1000, (int)result);
        }

        [TestMethod]
        public void TestGetBlockedAccounts()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.Policy.Hash, "getBlockedAccounts");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Array, Value = new[] { new ContractParameter { Type = ContractParameterType.Hash160, Value = UInt160.Zero } } });

            var result = policyAPI.GetBlockedAccounts();
            Assert.AreEqual(UInt160.Zero, result[0]);
        }
    }
}

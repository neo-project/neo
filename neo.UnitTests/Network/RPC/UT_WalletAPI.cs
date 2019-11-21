using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System.Numerics;

namespace Neo.UnitTests.Network.RPC
{
    [TestClass]
    public class UT_WalletAPI
    {
        Mock<RpcClient> rpcClientMock;
        KeyPair keyPair1;
        string address1;
        UInt160 sender;
        WalletAPI walletAPI;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            sender = Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
            address1 = Neo.Wallets.Helper.ToAddress(sender);
            rpcClientMock = UT_TransactionManager.MockRpcClient(sender, new byte[0]);
            walletAPI = new WalletAPI(rpcClientMock.Object);
        }

        [TestMethod]
        public void TestGetUnclaimedGas()
        {
            byte[] testScript = NativeContract.NEO.Hash.MakeScript("unclaimedGas", sender, 99);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            var balance = walletAPI.GetUnclaimedGas(address1);
            Assert.AreEqual(1.1m, balance);
        }

        [TestMethod]
        public void TestGetNeoBalance()
        {
            byte[] testScript = NativeContract.NEO.Hash.MakeScript("balanceOf", sender);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            var balance = walletAPI.GetNeoBalance(address1);
            Assert.AreEqual(1_00000000u, balance);
        }

        [TestMethod]
        public void TestGetGasBalance()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("balanceOf", sender);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            var balance = walletAPI.GetGasBalance(address1);
            Assert.AreEqual(1.1m, balance);
        }

        [TestMethod]
        public void TestGetTokenBalance()
        {
            byte[] testScript = UInt160.Zero.MakeScript("balanceOf", sender);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            var balance = walletAPI.GetTokenBalance(UInt160.Zero.ToString(), address1);
            Assert.AreEqual(1_10000000, balance);
        }

        [TestMethod]
        public void TestClaimGas()
        {
            byte[] balanceScript = NativeContract.NEO.Hash.MakeScript("balanceOf", sender);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, balanceScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            byte[] testScript = NativeContract.NEO.Hash.MakeScript("transfer", sender, sender, new BigInteger(1_00000000));
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            rpcClientMock.Setup(p => p.RpcSend("sendrawtransaction", It.IsAny<JObject>())).Returns(true);

            var tranaction = walletAPI.ClaimGas(keyPair1.Export());
            Assert.AreEqual(testScript.ToHexString(), tranaction.Script.ToHexString());
        }

        [TestMethod]
        public void TestTransfer()
        {
            byte[] decimalsScript = NativeContract.GAS.Hash.MakeScript("decimals");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, decimalsScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(8) });

            byte[] testScript = NativeContract.GAS.Hash.MakeScript("transfer", sender, UInt160.Zero, NativeContract.GAS.Factor * 100);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            rpcClientMock.Setup(p => p.RpcSend("sendrawtransaction", It.IsAny<JObject>())).Returns(true);

            var tranaction = walletAPI.Transfer(NativeContract.GAS.Hash.ToString(), keyPair1.Export(), UInt160.Zero.ToAddress(), 100, 1.1m);
            Assert.AreEqual(testScript.ToHexString(), tranaction.Script.ToHexString());
        }

        [TestMethod]
        public void TestWaitTransaction()
        {
            Transaction transaction = TestUtils.GetTransaction();
            rpcClientMock.Setup(p => p.RpcSend("getrawtransaction", It.Is<JObject[]>(j => j[0].AsString() == transaction.Hash.ToString())))
                .Returns(new RpcTransaction { Transaction = transaction, VMState = VMState.HALT, BlockHash = UInt256.Zero, BlockTime = 100, Confirmations = 1 }.ToJson());

            var tx = walletAPI.WaitTransaction(transaction).Result;
            Assert.AreEqual(VMState.HALT, tx.VMState);
            Assert.AreEqual(UInt256.Zero, tx.BlockHash);
        }
    }
}

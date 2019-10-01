using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.Network.RPC
{
    [TestClass]
    public class UT_TransactionManager
    {
        TransactionManager txManager;
        Mock<RpcClient> rpcClientMock;
        KeyPair keyPair1;
        KeyPair keyPair2;
        UInt160 sender;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            keyPair2 = new KeyPair(Wallet.GetPrivateKeyFromWIF("L2LGkrwiNmUAnWYb1XGd5mv7v2eDf6P4F3gHyXSrNJJR4ArmBp7Q"));
            sender = Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
            rpcClientMock = MockRpcClient(sender, new byte[1]);
        }

        public static Mock<RpcClient> MockRpcClient(UInt160 sender, byte[] script)
        {
            var mockRpc = new Mock<RpcClient>(MockBehavior.Strict, "http://seed1.neo.org:10331");

            // MockHeight
            mockRpc.Setup(p => p.RpcSend("getblockcount")).Returns(100).Verifiable();

            // MockGasBalance
            byte[] balanceScript = NativeContract.GAS.Hash.MakeScript("balanceOf", sender);
            var balanceResult = new ContractParameter() { Type = ContractParameterType.Integer, Value = BigInteger.Parse("10000000000000000") };

            MockInvokeScript(mockRpc, balanceScript, balanceResult);

            // MockFeePerByte
            byte[] policyScript = NativeContract.Policy.Hash.MakeScript("getFeePerByte");
            var policyResult = new ContractParameter() { Type = ContractParameterType.Integer, Value = BigInteger.Parse("1000") };

            MockInvokeScript(mockRpc, policyScript, policyResult);

            // MockGasConsumed
            var result = new ContractParameter();
            MockInvokeScript(mockRpc, script, result);

            return mockRpc;
        }

        public static void MockInvokeScript(Mock<RpcClient> mockClient, byte[] script, params ContractParameter[] parameters)
        {
            var result = new RpcInvokeResult()
            {
                Stack = parameters,
                GasConsumed = "100",
                Script = script.ToHexString(),
                State = "",
                Tx = ""
            };

            mockClient.Setup(p => p.RpcSend("invokescript", It.Is<JObject>(j => j.AsString() == script.ToHexString())))
                .Returns(result.ToJson())
                .Verifiable();
        }

        [TestMethod]
        public void TestMakeTransaction()
        {
            txManager = new TransactionManager(rpcClientMock.Object, sender);

            TransactionAttribute[] attributes = new TransactionAttribute[1]
            {
                new TransactionAttribute
                {
                    Usage = TransactionAttributeUsage.Url,
                    Data = "53616d706c6555726c".HexToBytes() // "SampleUrl"
                }
            };

            byte[] script = new byte[1];
            txManager.MakeTransaction(script, attributes, null, 60000);

            var tx = txManager.Tx;
            Assert.AreEqual("53616d706c6555726c", tx.Attributes[0].Data.ToHexString());
            Assert.AreEqual(0, tx.SystemFee % (long)NativeContract.GAS.Factor);
            Assert.AreEqual(60000, tx.NetworkFee);
        }

        [TestMethod]
        public void TestSign()
        {
            txManager = new TransactionManager(rpcClientMock.Object, sender);

            TransactionAttribute[] attributes = new TransactionAttribute[1]
            {
                new TransactionAttribute
                {
                    Usage = TransactionAttributeUsage.Url,
                    Data = "53616d706c6555726c".HexToBytes() // "SampleUrl"
                }
            };

            byte[] script = new byte[1];
            txManager.MakeTransaction(script, attributes)
                .AddSignature(keyPair1)
                .Sign();

            // get signature from Witnesses
            var tx = txManager.Tx;
            byte[] signature = tx.Witnesses[0].InvocationScript.Skip(1).ToArray();

            Assert.IsTrue(Crypto.Default.VerifySignature(tx.GetHashData(), signature, keyPair1.PublicKey.EncodePoint(false).Skip(1).ToArray()));

            // duplicate sign should not add new witness
            txManager.AddSignature(keyPair1).Sign();
            Assert.AreEqual(1, txManager.Tx.Witnesses.Length);

            // throw exception when the KeyPair is wrong
            Assert.ThrowsException<Exception>(() => txManager.AddSignature(keyPair2));
        }

        [TestMethod]
        public void TestSignMulti()
        {
            txManager = new TransactionManager(rpcClientMock.Object, sender);

            var multiContract = Contract.CreateMultiSigContract(2, keyPair1.PublicKey, keyPair2.PublicKey);

            // Cosigner needs multi signature
            Cosigner[] cosigners = new Cosigner[1]
            {
                new Cosigner
                {
                    Account = multiContract.ScriptHash,
                    Scopes = WitnessScope.Global
                }
            };

            byte[] script = new byte[1];
            txManager.MakeTransaction(script, null, cosigners, 0_10000000)
                .AddMultiSig(keyPair1, 2, keyPair1.PublicKey, keyPair2.PublicKey)
                .AddMultiSig(keyPair2, 2, keyPair1.PublicKey, keyPair2.PublicKey)
                .AddSignature(keyPair1)
                .Sign();

            var store = TestBlockchain.GetStore();
            var snapshot = store.GetSnapshot();

            var tx = txManager.Tx;
            Assert.IsTrue(tx.VerifyWitnesses(snapshot, tx.NetworkFee));
        }

        [TestMethod]
        public void TestAddWitness()
        {
            txManager = new TransactionManager(rpcClientMock.Object, sender);

            // Cosigner as contract scripthash
            Cosigner[] cosigners = new Cosigner[1]
            {
                new Cosigner
                {
                    Account = UInt160.Zero,
                    Scopes = WitnessScope.Global
                }
            };

            byte[] script = new byte[1];
            txManager.MakeTransaction(script, null, cosigners, 0_10000000);
            txManager.AddWitness(UInt160.Zero);
            txManager.AddSignature(keyPair1);
            txManager.Sign();

            var tx = txManager.Tx;
            Assert.AreEqual(2, tx.Witnesses.Length);
            Assert.AreEqual(0, tx.Witnesses[0].VerificationScript.Length);
            Assert.AreEqual(0, tx.Witnesses[0].InvocationScript.Length);
        }
    }
}

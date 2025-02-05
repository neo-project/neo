// Copyright (C) 2015-2025 The Neo Project.
//
// UT_TransactionManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.Network.RPC.Tests
{
    [TestClass]
    public class UT_TransactionManager
    {
        TransactionManager txManager;
        Mock<RpcClient> rpcClientMock;
        Mock<RpcClient> multiSigMock;
        KeyPair keyPair1;
        KeyPair keyPair2;
        UInt160 sender;
        UInt160 multiHash;
        RpcClient client;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            keyPair2 = new KeyPair(Wallet.GetPrivateKeyFromWIF("L2LGkrwiNmUAnWYb1XGd5mv7v2eDf6P4F3gHyXSrNJJR4ArmBp7Q"));
            sender = Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
            multiHash = Contract.CreateMultiSigContract(2, new ECPoint[] { keyPair1.PublicKey, keyPair2.PublicKey }).ScriptHash;
            rpcClientMock = MockRpcClient(sender, new byte[1]);
            client = rpcClientMock.Object;
            multiSigMock = MockMultiSig(multiHash, new byte[1]);
        }

        public static Mock<RpcClient> MockRpcClient(UInt160 sender, byte[] script)
        {
            var mockRpc = new Mock<RpcClient>(MockBehavior.Strict, new Uri("http://seed1.neo.org:10331"), null, null, null);

            // MockHeight
            mockRpc.Setup(p => p.RpcSendAsync("getblockcount")).ReturnsAsync(100).Verifiable();

            // calculatenetworkfee
            var networkfee = new JObject();
            networkfee["networkfee"] = 100000000;
            mockRpc.Setup(p => p.RpcSendAsync("calculatenetworkfee", It.Is<JToken[]>(u => true)))
                .ReturnsAsync(networkfee)
                .Verifiable();

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

        public static Mock<RpcClient> MockMultiSig(UInt160 multiHash, byte[] script)
        {
            var mockRpc = new Mock<RpcClient>(MockBehavior.Strict, new Uri("http://seed1.neo.org:10331"), null, null, null);

            // MockHeight
            mockRpc.Setup(p => p.RpcSendAsync("getblockcount")).ReturnsAsync(100).Verifiable();

            // calculatenetworkfee
            var networkfee = new JObject();
            networkfee["networkfee"] = 100000000;
            mockRpc.Setup(p => p.RpcSendAsync("calculatenetworkfee", It.Is<JToken[]>(u => true)))
                .ReturnsAsync(networkfee)
                .Verifiable();

            // MockGasBalance
            byte[] balanceScript = NativeContract.GAS.Hash.MakeScript("balanceOf", multiHash);
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
                Stack = parameters.Select(p => p.ToStackItem()).ToArray(),
                GasConsumed = 100,
                Script = Convert.ToBase64String(script),
                State = VMState.HALT
            };

            mockClient.Setup(p => p.RpcSendAsync("invokescript", It.Is<JToken[]>(j =>
                Convert.FromBase64String(j[0].AsString()).SequenceEqual(script))))
                .ReturnsAsync(result.ToJson())
                .Verifiable();
        }

        [TestMethod]
        public async Task TestMakeTransaction()
        {
            Signer[] signers = new Signer[1]
            {
                new Signer
                {
                    Account = sender,
                    Scopes= WitnessScope.Global
                }
            };

            byte[] script = new byte[1];
            txManager = await TransactionManager.MakeTransactionAsync(rpcClientMock.Object, script, signers);

            var tx = txManager.Tx;
            Assert.AreEqual(WitnessScope.Global, tx.Signers[0].Scopes);
        }

        [TestMethod]
        public async Task TestSign()
        {
            Signer[] signers = new Signer[1]
            {
                new Signer
                {
                    Account  =  sender,
                    Scopes = WitnessScope.Global
                }
            };

            byte[] script = new byte[1];
            txManager = await TransactionManager.MakeTransactionAsync(client, script, signers);
            await txManager
                .AddSignature(keyPair1)
                .SignAsync();

            // get signature from Witnesses
            var tx = txManager.Tx;
            ReadOnlyMemory<byte> signature = tx.Witnesses[0].InvocationScript[2..];

            Assert.IsTrue(Crypto.VerifySignature(tx.GetSignData(client.protocolSettings.Network), signature.Span, keyPair1.PublicKey));
            // verify network fee and system fee
            Assert.AreEqual(100000000/*Mock*/, tx.NetworkFee);
            Assert.AreEqual(100, tx.SystemFee);

            // duplicate sign should not add new witness
            await ThrowsAsync<Exception>(async () => await txManager.AddSignature(keyPair1).SignAsync());
            Assert.AreEqual(null, txManager.Tx.Witnesses);

            // throw exception when the KeyPair is wrong
            await ThrowsAsync<Exception>(async () => await txManager.AddSignature(keyPair2).SignAsync());
        }

        // https://docs.microsoft.com/en-us/archive/msdn-magazine/2014/november/async-programming-unit-testing-asynchronous-code#testing-exceptions
        static async Task<TException> ThrowsAsync<TException>(Func<Task> action, bool allowDerivedTypes = true)
            where TException : Exception
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                if (allowDerivedTypes && !(ex is TException))
                    throw new Exception("Delegate threw exception of type " +
                    ex.GetType().Name + ", but " + typeof(TException).Name +
                    " or a derived type was expected.", ex);
                if (!allowDerivedTypes && ex.GetType() != typeof(TException))
                    throw new Exception("Delegate threw exception of type " +
                    ex.GetType().Name + ", but " + typeof(TException).Name +
                    " was expected.", ex);
                return (TException)ex;
            }
            throw new Exception("Delegate did not throw expected exception " +
            typeof(TException).Name + ".");
        }

        [TestMethod]
        public async Task TestSignMulti()
        {
            // Cosigner needs multi signature
            Signer[] signers = new Signer[1]
            {
                new Signer
                {
                    Account = multiHash,
                    Scopes = WitnessScope.Global
                }
            };

            byte[] script = new byte[1];
            txManager = await TransactionManager.MakeTransactionAsync(multiSigMock.Object, script, signers);
            await txManager
                .AddMultiSig(keyPair1, 2, keyPair1.PublicKey, keyPair2.PublicKey)
                .AddMultiSig(keyPair2, 2, keyPair1.PublicKey, keyPair2.PublicKey)
                .SignAsync();
        }

        [TestMethod]
        public async Task TestAddWitness()
        {
            // Cosigner as contract scripthash
            Signer[] signers = new Signer[2]
            {
                new Signer
                {
                    Account = sender,
                    Scopes = WitnessScope.Global
                },
                new Signer
                {
                    Account = UInt160.Zero,
                    Scopes = WitnessScope.Global
                }
            };

            byte[] script = new byte[1];
            txManager = await TransactionManager.MakeTransactionAsync(rpcClientMock.Object, script, signers);
            txManager.AddWitness(UInt160.Zero);
            txManager.AddSignature(keyPair1);
            await txManager.SignAsync();

            var tx = txManager.Tx;
            Assert.AreEqual(2, tx.Witnesses.Length);
            Assert.AreEqual(40, tx.Witnesses[0].VerificationScript.Length);
            Assert.AreEqual(66, tx.Witnesses[0].InvocationScript.Length);
        }
    }
}

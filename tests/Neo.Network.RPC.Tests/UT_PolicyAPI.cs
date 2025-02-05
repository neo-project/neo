// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PolicyAPI.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.Network.RPC.Tests
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
        public async Task TestGetExecFeeFactor()
        {
            byte[] testScript = NativeContract.Policy.Hash.MakeScript("getExecFeeFactor");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(30) });

            var result = await policyAPI.GetExecFeeFactorAsync();
            Assert.AreEqual(30u, result);
        }

        [TestMethod]
        public async Task TestGetStoragePrice()
        {
            byte[] testScript = NativeContract.Policy.Hash.MakeScript("getStoragePrice");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(100000) });

            var result = await policyAPI.GetStoragePriceAsync();
            Assert.AreEqual(100000u, result);
        }

        [TestMethod]
        public async Task TestGetFeePerByte()
        {
            byte[] testScript = NativeContract.Policy.Hash.MakeScript("getFeePerByte");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1000) });

            var result = await policyAPI.GetFeePerByteAsync();
            Assert.AreEqual(1000L, result);
        }

        [TestMethod]
        public async Task TestIsBlocked()
        {
            byte[] testScript = NativeContract.Policy.Hash.MakeScript("isBlocked", UInt160.Zero);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Boolean, Value = true });
            var result = await policyAPI.IsBlockedAsync(UInt160.Zero);
            Assert.AreEqual(true, result);
        }
    }
}

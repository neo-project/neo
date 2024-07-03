// Copyright (C) 2015-2024 The Neo Project.
//
// UT_WalletAPI.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Cryptography.ECC;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.Network.RPC.Tests
{
    [TestClass]
    public class UT_WalletAPI
    {
        Mock<RpcClient> rpcClientMock;
        KeyPair keyPair1;
        string address1;
        UInt160 sender;
        WalletAPI walletAPI;
        UInt160 multiSender;
        RpcClient client;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            sender = Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
            multiSender = Contract.CreateMultiSigContract(1, new ECPoint[] { keyPair1.PublicKey }).ScriptHash;
            rpcClientMock = UT_TransactionManager.MockRpcClient(sender, []);
            client = rpcClientMock.Object;
            address1 = Wallets.Helper.ToAddress(sender, client.protocolSettings.AddressVersion);
            walletAPI = new WalletAPI(rpcClientMock.Object);
        }

        [TestMethod]
        public async Task TestGetUnclaimedGas()
        {
            byte[] testScript = NativeContract.NEO.Hash.MakeScript("unclaimedGas", sender, 99);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            var balance = await walletAPI.GetUnclaimedGasAsync(address1);
            Assert.AreEqual(1.1m, balance);
        }

        [TestMethod]
        public async Task TestGetNeoBalance()
        {
            byte[] testScript = NativeContract.NEO.Hash.MakeScript("balanceOf", sender);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            var balance = await walletAPI.GetNeoBalanceAsync(address1);
            Assert.AreEqual(1_00000000u, balance);
        }

        [TestMethod]
        public async Task TestGetGasBalance()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("balanceOf", sender);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            var balance = await walletAPI.GetGasBalanceAsync(address1);
            Assert.AreEqual(1.1m, balance);
        }

        [TestMethod]
        public async Task TestGetTokenBalance()
        {
            byte[] testScript = UInt160.Zero.MakeScript("balanceOf", sender);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            var balance = await walletAPI.GetTokenBalanceAsync(UInt160.Zero.ToString(), address1);
            Assert.AreEqual(1_10000000, balance);
        }

        [TestMethod]
        public async Task TestClaimGas()
        {
            byte[] balanceScript = NativeContract.NEO.Hash.MakeScript("balanceOf", sender);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, balanceScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            byte[] testScript = NativeContract.NEO.Hash.MakeScript("transfer", sender, sender, new BigInteger(1_00000000), null);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            var json = new JObject();
            json["hash"] = UInt256.Zero.ToString();
            rpcClientMock.Setup(p => p.RpcSendAsync("sendrawtransaction", It.IsAny<JToken>())).ReturnsAsync(json);

            var tranaction = await walletAPI.ClaimGasAsync(keyPair1.Export(), false);
            Assert.AreEqual(testScript.ToHexString(), tranaction.Script.Span.ToHexString());
        }

        [TestMethod]
        public async Task TestTransfer()
        {
            byte[] decimalsScript = NativeContract.GAS.Hash.MakeScript("decimals");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, decimalsScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(8) });

            byte[] testScript = NativeContract.GAS.Hash.MakeScript("transfer", sender, UInt160.Zero, NativeContract.GAS.Factor * 100, null)
                .Concat(new[] { (byte)OpCode.ASSERT })
                .ToArray();
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            var json = new JObject();
            json["hash"] = UInt256.Zero.ToString();
            rpcClientMock.Setup(p => p.RpcSendAsync("sendrawtransaction", It.IsAny<JToken>())).ReturnsAsync(json);

            var tranaction = await walletAPI.TransferAsync(NativeContract.GAS.Hash.ToString(), keyPair1.Export(), UInt160.Zero.ToAddress(client.protocolSettings.AddressVersion), 100, null, true);
            Assert.AreEqual(testScript.ToHexString(), tranaction.Script.Span.ToHexString());
        }

        [TestMethod]
        public async Task TestTransferfromMultiSigAccount()
        {
            byte[] balanceScript = NativeContract.GAS.Hash.MakeScript("balanceOf", multiSender);
            var balanceResult = new ContractParameter() { Type = ContractParameterType.Integer, Value = BigInteger.Parse("10000000000000000") };

            UT_TransactionManager.MockInvokeScript(rpcClientMock, balanceScript, balanceResult);

            byte[] decimalsScript = NativeContract.GAS.Hash.MakeScript("decimals");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, decimalsScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(8) });

            byte[] testScript = NativeContract.GAS.Hash.MakeScript("transfer", multiSender, UInt160.Zero, NativeContract.GAS.Factor * 100, null)
                .Concat(new[] { (byte)OpCode.ASSERT })
                .ToArray();
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            var json = new JObject();
            json["hash"] = UInt256.Zero.ToString();
            rpcClientMock.Setup(p => p.RpcSendAsync("sendrawtransaction", It.IsAny<JToken>())).ReturnsAsync(json);

            var tranaction = await walletAPI.TransferAsync(NativeContract.GAS.Hash, 1, [keyPair1.PublicKey], [keyPair1], UInt160.Zero, NativeContract.GAS.Factor * 100, null, true);
            Assert.AreEqual(testScript.ToHexString(), tranaction.Script.Span.ToHexString());

            try
            {
                tranaction = await walletAPI.TransferAsync(NativeContract.GAS.Hash, 2, [keyPair1.PublicKey], [keyPair1], UInt160.Zero, NativeContract.GAS.Factor * 100, null, true);
                Assert.Fail();
            }
            catch (System.Exception e)
            {
                Assert.AreEqual(e.Message, $"Need at least 2 KeyPairs for signing!");
            }

            testScript = NativeContract.GAS.Hash.MakeScript("transfer", multiSender, UInt160.Zero, NativeContract.GAS.Factor * 100, string.Empty)
                .Concat(new[] { (byte)OpCode.ASSERT })
                .ToArray();
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_10000000) });

            tranaction = await walletAPI.TransferAsync(NativeContract.GAS.Hash, 1, [keyPair1.PublicKey], [keyPair1], UInt160.Zero, NativeContract.GAS.Factor * 100, string.Empty, true);
            Assert.AreEqual(testScript.ToHexString(), tranaction.Script.Span.ToHexString());
        }

        [TestMethod]
        public async Task TestWaitTransaction()
        {
            Transaction transaction = TestUtils.GetTransaction();
            rpcClientMock.Setup(p => p.RpcSendAsync("getrawtransaction", It.Is<JToken[]>(j => j[0].AsString() == transaction.Hash.ToString())))
                .ReturnsAsync(new RpcTransaction { Transaction = transaction, VMState = VMState.HALT, BlockHash = UInt256.Zero, BlockTime = 100, Confirmations = 1 }.ToJson(client.protocolSettings));

            var tx = await walletAPI.WaitTransactionAsync(transaction);
            Assert.AreEqual(VMState.HALT, tx.VMState);
            Assert.AreEqual(UInt256.Zero, tx.BlockHash);
        }
    }
}

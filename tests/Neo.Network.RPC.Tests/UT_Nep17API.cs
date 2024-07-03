// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Nep17API.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Json;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static Neo.Helper;

namespace Neo.Network.RPC.Tests
{
    [TestClass]
    public class UT_Nep17API
    {
        Mock<RpcClient> rpcClientMock;
        KeyPair keyPair1;
        UInt160 sender;
        Nep17API nep17API;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            sender = Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
            rpcClientMock = UT_TransactionManager.MockRpcClient(sender, []);
            nep17API = new Nep17API(rpcClientMock.Object);
        }

        [TestMethod]
        public async Task TestBalanceOf()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("balanceOf", UInt160.Zero);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(10000) });

            var balance = await nep17API.BalanceOfAsync(NativeContract.GAS.Hash, UInt160.Zero);
            Assert.AreEqual(10000, (int)balance);
        }

        [TestMethod]
        public async Task TestGetSymbol()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("symbol");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.String, Value = NativeContract.GAS.Symbol });

            var result = await nep17API.SymbolAsync(NativeContract.GAS.Hash);
            Assert.AreEqual(NativeContract.GAS.Symbol, result);
        }

        [TestMethod]
        public async Task TestGetDecimals()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("decimals");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(NativeContract.GAS.Decimals) });

            var result = await nep17API.DecimalsAsync(NativeContract.GAS.Hash);
            Assert.AreEqual(NativeContract.GAS.Decimals, result);
        }

        [TestMethod]
        public async Task TestGetTotalSupply()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("totalSupply");
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            var result = await nep17API.TotalSupplyAsync(NativeContract.GAS.Hash);
            Assert.AreEqual(1_00000000, (int)result);
        }

        [TestMethod]
        public async Task TestGetTokenInfo()
        {
            UInt160 scriptHash = NativeContract.GAS.Hash;
            byte[] testScript = Concat(
                scriptHash.MakeScript("symbol"),
                scriptHash.MakeScript("decimals"),
                scriptHash.MakeScript("totalSupply"));
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript,
                new ContractParameter { Type = ContractParameterType.String, Value = NativeContract.GAS.Symbol },
                new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(NativeContract.GAS.Decimals) },
                new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            scriptHash = NativeContract.NEO.Hash;
            testScript = Concat(
                scriptHash.MakeScript("symbol"),
                scriptHash.MakeScript("decimals"),
                scriptHash.MakeScript("totalSupply"));
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript,
                new ContractParameter { Type = ContractParameterType.String, Value = NativeContract.NEO.Symbol },
                new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(NativeContract.NEO.Decimals) },
                new ContractParameter { Type = ContractParameterType.Integer, Value = new BigInteger(1_00000000) });

            var tests = TestUtils.RpcTestCases.Where(p => p.Name == "getcontractstateasync");
            var haveGasTokenUT = false;
            var haveNeoTokenUT = false;
            foreach (var test in tests)
            {
                rpcClientMock.Setup(p => p.RpcSendAsync("getcontractstate", It.Is<JToken[]>(u => true)))
                .ReturnsAsync(test.Response.Result)
                .Verifiable();
                if (test.Request.Params[0].AsString() == NativeContract.GAS.Hash.ToString() || test.Request.Params[0].AsString().Equals(NativeContract.GAS.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    var result = await nep17API.GetTokenInfoAsync(NativeContract.GAS.Name.ToLower());
                    Assert.AreEqual(NativeContract.GAS.Symbol, result.Symbol);
                    Assert.AreEqual(8, result.Decimals);
                    Assert.AreEqual(1_00000000, (int)result.TotalSupply);
                    Assert.AreEqual("GasToken", result.Name);

                    result = await nep17API.GetTokenInfoAsync(NativeContract.GAS.Hash);
                    Assert.AreEqual(NativeContract.GAS.Symbol, result.Symbol);
                    Assert.AreEqual(8, result.Decimals);
                    Assert.AreEqual(1_00000000, (int)result.TotalSupply);
                    Assert.AreEqual("GasToken", result.Name);
                    haveGasTokenUT = true;
                }
                else if (test.Request.Params[0].AsString() == NativeContract.NEO.Hash.ToString() || test.Request.Params[0].AsString().Equals(NativeContract.NEO.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    var result = await nep17API.GetTokenInfoAsync(NativeContract.NEO.Name.ToLower());
                    Assert.AreEqual(NativeContract.NEO.Symbol, result.Symbol);
                    Assert.AreEqual(0, result.Decimals);
                    Assert.AreEqual(1_00000000, (int)result.TotalSupply);
                    Assert.AreEqual("NeoToken", result.Name);

                    result = await nep17API.GetTokenInfoAsync(NativeContract.NEO.Hash);
                    Assert.AreEqual(NativeContract.NEO.Symbol, result.Symbol);
                    Assert.AreEqual(0, result.Decimals);
                    Assert.AreEqual(1_00000000, (int)result.TotalSupply);
                    Assert.AreEqual("NeoToken", result.Name);
                    haveNeoTokenUT = true;
                }
            }
            Assert.IsTrue(haveGasTokenUT && haveNeoTokenUT); //Update RpcTestCases.json
        }

        [TestMethod]
        public async Task TestTransfer()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("transfer", sender, UInt160.Zero, new BigInteger(1_00000000), null)
                .Concat(new[] { (byte)OpCode.ASSERT })
                .ToArray();
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter());

            var client = rpcClientMock.Object;
            var result = await nep17API.CreateTransferTxAsync(NativeContract.GAS.Hash, keyPair1, UInt160.Zero, new BigInteger(1_00000000), null, true);

            testScript = NativeContract.GAS.Hash.MakeScript("transfer", sender, UInt160.Zero, new BigInteger(1_00000000), string.Empty)
                .Concat(new[] { (byte)OpCode.ASSERT })
                .ToArray();
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter());

            result = await nep17API.CreateTransferTxAsync(NativeContract.GAS.Hash, keyPair1, UInt160.Zero, new BigInteger(1_00000000), string.Empty, true);
            Assert.IsNotNull(result);
        }
    }
}

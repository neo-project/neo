// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcModels.cs file belongs to the neo project and is free
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
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using System;
using System.Linq;
using System.Net.Http;

namespace Neo.Network.RPC.Tests
{
    [TestClass()]
    public class UT_RpcModels
    {
        RpcClient rpc;
        Mock<HttpMessageHandler> handlerMock;

        [TestInitialize]
        public void TestSetup()
        {
            handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            // use real http client with mocked handler here
            var httpClient = new HttpClient(handlerMock.Object);
            rpc = new RpcClient(httpClient, new Uri("http://seed1.neo.org:10331"), null);
        }

        [TestMethod()]
        public void TestRpcAccount()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.ImportPrivKeyAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcAccount.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcApplicationLog()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetApplicationLogAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcApplicationLog.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcBlock()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetBlockAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcBlock.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestRpcBlockHeader()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetBlockHeaderAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcBlockHeader.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestGetContractState()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetContractStateAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcContractState.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());

            var nef = RpcNefFile.FromJson((JObject)json["nef"]);
            Assert.AreEqual(json["nef"].ToString(), nef.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcInvokeResult()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.InvokeFunctionAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcInvokeResult.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcMethodToken()
        {
            var json = """{"hash":"0x0e1b9bfaa44e60311f6f3c96cfcd6d12c2fc3add","method":"test","paramcount":1,"hasreturnvalue":true,"callflags":"All"}""";
            var item = RpcMethodToken.FromJson((JObject)JToken.Parse(json));
            Assert.AreEqual("0x0e1b9bfaa44e60311f6f3c96cfcd6d12c2fc3add", item.Hash.ToString());
            Assert.AreEqual("test", item.Method);
            Assert.AreEqual(1, item.ParametersCount);
            Assert.AreEqual(true, item.HasReturnValue);
            Assert.AreEqual(CallFlags.All, item.CallFlags);
            Assert.AreEqual(json, item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcNep17Balances()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetNep17BalancesAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcNep17Balances.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestRpcNep17Transfers()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetNep17TransfersAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcNep17Transfers.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestRpcPeers()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetPeersAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcPeers.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcPlugin()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.ListPluginsAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = ((JArray)json).Select(p => RpcPlugin.FromJson((JObject)p));
            Assert.AreEqual(json.ToString(), ((JArray)item.Select(p => p.ToJson()).ToArray()).ToString());
        }

        [TestMethod()]
        public void TestRpcRawMemPool()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetRawMempoolBothAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcRawMemPool.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcTransaction()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetRawTransactionAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcTransaction.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestRpcTransferOut()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.SendManyAsync), StringComparison.CurrentCultureIgnoreCase)).Request.Params[1];
            var item = ((JArray)json).Select(p => RpcTransferOut.FromJson((JObject)p, rpc.protocolSettings));
            Assert.AreEqual(json.ToString(), ((JArray)item.Select(p => p.ToJson(rpc.protocolSettings)).ToArray()).ToString());
        }

        [TestMethod()]
        public void TestRpcValidateAddressResult()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.ValidateAddressAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcValidateAddressResult.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcValidator()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetNextBlockValidatorsAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = ((JArray)json).Select(p => RpcValidator.FromJson((JObject)p));
            Assert.AreEqual(json.ToString(), ((JArray)item.Select(p => p.ToJson()).ToArray()).ToString());
        }

        [TestMethod()]
        public void TestRpcVersion()
        {
            var json = TestUtils.RpcTestCases
                .Find(p => p.Name.Equals(nameof(RpcClient.GetVersionAsync), StringComparison.CurrentCultureIgnoreCase))
                .Response
                .Result;
            var item = RpcVersion.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod]
        public void TestRpcStack()
        {
            var stack = new RpcStack()
            {
                Type = "Boolean",
                Value = true,
            };

            var expectedJsonString = "{\"type\":\"Boolean\",\"value\":true}";
            var actualJsonString = stack.ToJson().ToString();

            Assert.AreEqual(expectedJsonString, actualJsonString);
        }
    }
}

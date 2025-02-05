// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcModels.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Json;
using Neo.Network.RPC.Models;
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
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.ImportPrivKeyAsync).ToLower()).Response.Result;
            var item = RpcAccount.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcApplicationLog()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetApplicationLogAsync).ToLower()).Response.Result;
            var item = RpcApplicationLog.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcBlock()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetBlockAsync).ToLower()).Response.Result;
            var item = RpcBlock.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestRpcBlockHeader()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetBlockHeaderAsync).ToLower()).Response.Result;
            var item = RpcBlockHeader.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestGetContractState()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetContractStateAsync).ToLower()).Response.Result;
            var item = RpcContractState.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());

            var nef = RpcNefFile.FromJson((JObject)json["nef"]);
            Assert.AreEqual(json["nef"].ToString(), nef.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcInvokeResult()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.InvokeFunctionAsync).ToLower()).Response.Result;
            var item = RpcInvokeResult.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcMethodToken()
        {
            RpcMethodToken.FromJson((JObject)JToken.Parse("{\"hash\": \"0x0e1b9bfaa44e60311f6f3c96cfcd6d12c2fc3add\", \"method\":\"test\",\"paramcount\":\"1\",\"hasreturnvalue\":\"true\",\"callflags\":\"All\"}"));
        }

        [TestMethod()]
        public void TestRpcNep17Balances()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetNep17BalancesAsync).ToLower()).Response.Result;
            var item = RpcNep17Balances.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestRpcNep17Transfers()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetNep17TransfersAsync).ToLower()).Response.Result;
            var item = RpcNep17Transfers.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestRpcPeers()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetPeersAsync).ToLower()).Response.Result;
            var item = RpcPeers.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcPlugin()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.ListPluginsAsync).ToLower()).Response.Result;
            var item = ((JArray)json).Select(p => RpcPlugin.FromJson((JObject)p));
            Assert.AreEqual(json.ToString(), ((JArray)item.Select(p => p.ToJson()).ToArray()).ToString());
        }

        [TestMethod()]
        public void TestRpcRawMemPool()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetRawMempoolBothAsync).ToLower()).Response.Result;
            var item = RpcRawMemPool.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcTransaction()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetRawTransactionAsync).ToLower()).Response.Result;
            var item = RpcTransaction.FromJson((JObject)json, rpc.protocolSettings);
            Assert.AreEqual(json.ToString(), item.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public void TestRpcTransferOut()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.SendManyAsync).ToLower()).Request.Params[1];
            var item = ((JArray)json).Select(p => RpcTransferOut.FromJson((JObject)p, rpc.protocolSettings));
            Assert.AreEqual(json.ToString(), ((JArray)item.Select(p => p.ToJson(rpc.protocolSettings)).ToArray()).ToString());
        }

        [TestMethod()]
        public void TestRpcValidateAddressResult()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.ValidateAddressAsync).ToLower()).Response.Result;
            var item = RpcValidateAddressResult.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }

        [TestMethod()]
        public void TestRpcValidator()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetNextBlockValidatorsAsync).ToLower()).Response.Result;
            var item = ((JArray)json).Select(p => RpcValidator.FromJson((JObject)p));
            Assert.AreEqual(json.ToString(), ((JArray)item.Select(p => p.ToJson()).ToArray()).ToString());
        }

        [TestMethod()]
        public void TestRpcVersion()
        {
            JToken json = TestUtils.RpcTestCases.Find(p => p.Name == nameof(RpcClient.GetVersionAsync).ToLower()).Response.Result;
            var item = RpcVersion.FromJson((JObject)json);
            Assert.AreEqual(json.ToString(), item.ToJson().ToString());
        }
    }
}

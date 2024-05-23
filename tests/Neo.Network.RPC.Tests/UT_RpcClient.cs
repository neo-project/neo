// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network.RPC.Tests
{
    [TestClass]
    public class UT_RpcClient
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
            foreach (var test in TestUtils.RpcTestCases)
            {
                MockResponse(test.Request, test.Response);
            }
        }

        private void MockResponse(RpcRequest request, RpcResponse response)
        {
            handlerMock.Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.Is<HttpRequestMessage>(p => p.Content.ReadAsStringAsync().Result == request.ToJson().ToString()),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(response.ToJson().ToString()),
               })
               .Verifiable();
        }

        [TestMethod]
        public async Task TestErrorResponse()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == (nameof(rpc.SendRawTransactionAsync) + "error").ToLower());
            try
            {
                var result = await rpc.SendRawTransactionAsync(Convert.FromBase64String(test.Request.Params[0].AsString()).AsSerializable<Transaction>());
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(-500, ex.HResult);
                Assert.AreEqual("InsufficientFunds", ex.Message);
            }
        }

        [TestMethod]
        public async Task TestNoThrowErrorResponse()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == (nameof(rpc.SendRawTransactionAsync) + "error").ToLower());
            handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock.Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(test.Response.ToJson().ToString()),
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new RpcClient(httpClient, new Uri("http://seed1.neo.org:10331"), null);
            var response = await client.SendAsync(test.Request, false);

            Assert.IsNull(response.Result);
            Assert.IsNotNull(response.Error);
            Assert.AreEqual(-500, response.Error.Code);
            Assert.AreEqual("InsufficientFunds", response.Error.Message);
        }

        [TestMethod]
        public void TestConstructorByUrlAndDispose()
        {
            //dummy url for test
            var client = new RpcClient(new Uri("http://www.xxx.yyy"));
            Action action = () => client.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestConstructorWithBasicAuth()
        {
            var client = new RpcClient(new Uri("http://www.xxx.yyy"), "krain", "123456");
            client.Dispose();
        }

        #region Blockchain

        [TestMethod]
        public async Task TestGetBestBlockHash()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetBestBlockHashAsync).ToLower());
            var result = await rpc.GetBestBlockHashAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetBlockHex()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name == nameof(rpc.GetBlockHexAsync).ToLower());
            foreach (var test in tests)
            {
                var result = await rpc.GetBlockHexAsync(test.Request.Params[0].AsString());
                Assert.AreEqual(test.Response.Result.AsString(), result);
            }
        }

        [TestMethod]
        public async Task TestGetBlock()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name == nameof(rpc.GetBlockAsync).ToLower());
            foreach (var test in tests)
            {
                var result = await rpc.GetBlockAsync(test.Request.Params[0].AsString());
                Assert.AreEqual(test.Response.Result.AsString(), result.ToJson(rpc.protocolSettings).ToString());
            }
        }

        [TestMethod]
        public async Task TestGetBlockHeaderCount()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetBlockHeaderCountAsync).ToLower());
            var result = await rpc.GetBlockHeaderCountAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetBlockCount()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetBlockCountAsync).ToLower());
            var result = await rpc.GetBlockCountAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetBlockHash()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetBlockHashAsync).ToLower());
            var result = await rpc.GetBlockHashAsync((uint)test.Request.Params[0].AsNumber());
            Assert.AreEqual(test.Response.Result.AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetBlockHeaderHex()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name == nameof(rpc.GetBlockHeaderHexAsync).ToLower());
            foreach (var test in tests)
            {
                var result = await rpc.GetBlockHeaderHexAsync(test.Request.Params[0].AsString());
                Assert.AreEqual(test.Response.Result.AsString(), result);
            }
        }

        [TestMethod]
        public async Task TestGetBlockHeader()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name == nameof(rpc.GetBlockHeaderAsync).ToLower());
            foreach (var test in tests)
            {
                var result = await rpc.GetBlockHeaderAsync(test.Request.Params[0].AsString());
                Assert.AreEqual(test.Response.Result.ToString(), result.ToJson(rpc.protocolSettings).ToString());
            }
        }

        [TestMethod]
        public async Task TestGetCommittee()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name == nameof(rpc.GetCommitteeAsync).ToLower());
            foreach (var test in tests)
            {
                var result = await rpc.GetCommitteeAsync();
                Assert.AreEqual(test.Response.Result.ToString(), ((JArray)result.Select(p => (JToken)p).ToArray()).ToString());
            }
        }

        [TestMethod]
        public async Task TestGetContractState()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name == nameof(rpc.GetContractStateAsync).ToLower());
            foreach (var test in tests)
            {
                var result = await rpc.GetContractStateAsync(test.Request.Params[0].AsString());
                Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());
            }
        }

        [TestMethod]
        public async Task TestGetNativeContracts()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name == nameof(rpc.GetNativeContractsAsync).ToLower());
            foreach (var test in tests)
            {
                var result = await rpc.GetNativeContractsAsync();
                Assert.AreEqual(test.Response.Result.ToString(), ((JArray)result.Select(p => p.ToJson()).ToArray()).ToString());
            }
        }

        [TestMethod]
        public async Task TestGetRawMempool()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetRawMempoolAsync).ToLower());
            var result = await rpc.GetRawMempoolAsync();
            Assert.AreEqual(test.Response.Result.ToString(), ((JArray)result.Select(p => (JToken)p).ToArray()).ToString());
        }

        [TestMethod]
        public async Task TestGetRawMempoolBoth()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetRawMempoolBothAsync).ToLower());
            var result = await rpc.GetRawMempoolBothAsync();
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());
        }

        [TestMethod]
        public async Task TestGetRawTransactionHex()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetRawTransactionHexAsync).ToLower());
            var result = await rpc.GetRawTransactionHexAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetRawTransaction()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetRawTransactionAsync).ToLower());
            var result = await rpc.GetRawTransactionAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod]
        public async Task TestGetStorage()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetStorageAsync).ToLower());
            var result = await rpc.GetStorageAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString());
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetTransactionHeight()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetTransactionHeightAsync).ToLower());
            var result = await rpc.GetTransactionHeightAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetNextBlockValidators()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetNextBlockValidatorsAsync).ToLower());
            var result = await rpc.GetNextBlockValidatorsAsync();
            Assert.AreEqual(test.Response.Result.ToString(), ((JArray)result.Select(p => p.ToJson()).ToArray()).ToString());
        }

        #endregion Blockchain

        #region Node

        [TestMethod]
        public async Task TestGetConnectionCount()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetConnectionCountAsync).ToLower());
            var result = await rpc.GetConnectionCountAsync();
            Assert.AreEqual(test.Response.Result.ToString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetPeers()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetPeersAsync).ToLower());
            var result = await rpc.GetPeersAsync();
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());
        }

        [TestMethod]
        public async Task TestGetVersion()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetVersionAsync).ToLower());
            var result = await rpc.GetVersionAsync();
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());
        }

        [TestMethod]
        public async Task TestSendRawTransaction()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.SendRawTransactionAsync).ToLower());
            var result = await rpc.SendRawTransactionAsync(Convert.FromBase64String(test.Request.Params[0].AsString()).AsSerializable<Transaction>());
            Assert.AreEqual(test.Response.Result["hash"].AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestSubmitBlock()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.SubmitBlockAsync).ToLower());
            var result = await rpc.SubmitBlockAsync(Convert.FromBase64String(test.Request.Params[0].AsString()));
            Assert.AreEqual(test.Response.Result["hash"].AsString(), result.ToString());
        }

        #endregion Node

        #region SmartContract

        [TestMethod]
        public async Task TestInvokeFunction()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.InvokeFunctionAsync).ToLower());
            var result = await rpc.InvokeFunctionAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString(),
                ((JArray)test.Request.Params[2]).Select(p => RpcStack.FromJson((JObject)p)).ToArray());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());

            // TODO test verify method
        }

        [TestMethod]
        public async Task TestInvokeScript()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.InvokeScriptAsync).ToLower());
            var result = await rpc.InvokeScriptAsync(Convert.FromBase64String(test.Request.Params[0].AsString()));
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());
        }

        [TestMethod]
        public async Task TestGetUnclaimedGas()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetUnclaimedGasAsync).ToLower());
            var result = await rpc.GetUnclaimedGasAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(result.ToJson().AsString(), RpcUnclaimedGas.FromJson(result.ToJson()).ToJson().AsString());
            Assert.AreEqual(test.Response.Result["unclaimed"].AsString(), result.Unclaimed.ToString());
        }

        #endregion SmartContract

        #region Utilities

        [TestMethod]
        public async Task TestListPlugins()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.ListPluginsAsync).ToLower());
            var result = await rpc.ListPluginsAsync();
            Assert.AreEqual(test.Response.Result.ToString(), ((JArray)result.Select(p => p.ToJson()).ToArray()).ToString());
        }

        [TestMethod]
        public async Task TestValidateAddress()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.ValidateAddressAsync).ToLower());
            var result = await rpc.ValidateAddressAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());
        }

        #endregion Utilities

        #region Wallet

        [TestMethod]
        public async Task TestCloseWallet()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.CloseWalletAsync).ToLower());
            var result = await rpc.CloseWalletAsync();
            Assert.AreEqual(test.Response.Result.AsBoolean(), result);
        }

        [TestMethod]
        public async Task TestDumpPrivKey()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.DumpPrivKeyAsync).ToLower());
            var result = await rpc.DumpPrivKeyAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetNewAddress()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetNewAddressAsync).ToLower());
            var result = await rpc.GetNewAddressAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetWalletBalance()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetWalletBalanceAsync).ToLower());
            var result = await rpc.GetWalletBalanceAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result["balance"].AsString(), result.Value.ToString());
        }

        [TestMethod]
        public async Task TestGetWalletUnclaimedGas()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetWalletUnclaimedGasAsync).ToLower());
            var result = await rpc.GetWalletUnclaimedGasAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestImportPrivKey()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.ImportPrivKeyAsync).ToLower());
            var result = await rpc.ImportPrivKeyAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());
        }

        [TestMethod]
        public async Task TestListAddress()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.ListAddressAsync).ToLower());
            var result = await rpc.ListAddressAsync();
            Assert.AreEqual(test.Response.Result.ToString(), ((JArray)result.Select(p => p.ToJson()).ToArray()).ToString());
        }

        [TestMethod]
        public async Task TestOpenWallet()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.OpenWalletAsync).ToLower());
            var result = await rpc.OpenWalletAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString());
            Assert.AreEqual(test.Response.Result.AsBoolean(), result);
        }

        [TestMethod]
        public async Task TestSendFrom()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.SendFromAsync).ToLower());
            var result = await rpc.SendFromAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString(),
                test.Request.Params[2].AsString(), test.Request.Params[3].AsString());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToString());
        }

        [TestMethod]
        public async Task TestSendMany()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.SendManyAsync).ToLower());
            var result = await rpc.SendManyAsync(test.Request.Params[0].AsString(), ((JArray)test.Request.Params[1]).Select(p => RpcTransferOut.FromJson((JObject)p, rpc.protocolSettings)));
            Assert.AreEqual(test.Response.Result.ToString(), result.ToString());
        }

        [TestMethod]
        public async Task TestSendToAddress()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.SendToAddressAsync).ToLower());
            var result = await rpc.SendToAddressAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString(), test.Request.Params[2].AsString());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToString());
        }

        #endregion Wallet

        #region Plugins

        [TestMethod()]
        public async Task GetApplicationLogTest()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetApplicationLogAsync).ToLower());
            var result = await rpc.GetApplicationLogAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());
        }

        [TestMethod()]
        public async Task GetApplicationLogTest_TriggerType()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == (nameof(rpc.GetApplicationLogAsync) + "_triggertype").ToLower());
            var result = await rpc.GetApplicationLogAsync(test.Request.Params[0].AsString(), TriggerType.OnPersist);
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson().ToString());
        }

        [TestMethod()]
        public async Task GetNep17TransfersTest()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetNep17TransfersAsync).ToLower());
            var result = await rpc.GetNep17TransfersAsync(test.Request.Params[0].AsString(), (ulong)test.Request.Params[1].AsNumber(), (ulong)test.Request.Params[2].AsNumber());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson(rpc.protocolSettings).ToString());
            test = TestUtils.RpcTestCases.Find(p => p.Name == (nameof(rpc.GetNep17TransfersAsync).ToLower() + "_with_null_transferaddress"));
            result = await rpc.GetNep17TransfersAsync(test.Request.Params[0].AsString(), (ulong)test.Request.Params[1].AsNumber(), (ulong)test.Request.Params[2].AsNumber());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson(rpc.protocolSettings).ToString());
        }

        [TestMethod()]
        public async Task GetNep17BalancesTest()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name == nameof(rpc.GetNep17BalancesAsync).ToLower());
            var result = await rpc.GetNep17BalancesAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToJson(rpc.protocolSettings).ToString());
        }

        #endregion Plugins
    }
}

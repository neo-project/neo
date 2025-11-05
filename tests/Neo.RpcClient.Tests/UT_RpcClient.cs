// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
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
                  ItExpr.Is<HttpRequestMessage>(p => p.Content.ReadAsStringAsync().Result == request.ToJson().StrictToString(false)),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(response.ToJson().StrictToString(false)),
               })
               .Verifiable();
        }

        [TestMethod]
        public async Task TestErrorResponse()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.SendRawTransactionAsync) + "error", StringComparison.CurrentCultureIgnoreCase));
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
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.SendRawTransactionAsync) + "error", StringComparison.CurrentCultureIgnoreCase));
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
                   Content = new StringContent(test.Response.ToJson().StrictToString(false)),
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
            try
            {
                action();
            }
            catch
            {
                Assert.Fail("Dispose should not throw exception");
            }
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
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetBestBlockHashAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetBestBlockHashAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetBlockHex()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name.Equals(nameof(rpc.GetBlockHexAsync), StringComparison.CurrentCultureIgnoreCase));
            foreach (var test in tests)
            {
                var result = await rpc.GetBlockHexAsync(test.Request.Params[0].AsString());
                Assert.AreEqual(test.Response.Result.AsString(), result);
            }
        }

        [TestMethod]
        public async Task TestGetBlock()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name.Equals(nameof(rpc.GetBlockAsync), StringComparison.CurrentCultureIgnoreCase));
            foreach (var test in tests)
            {
                var result = await rpc.GetBlockAsync(test.Request.Params[0].AsString());
                Assert.AreEqual(test.Response.Result.AsString(), result.ToJson(rpc.protocolSettings).StrictToString(false));
            }
        }

        [TestMethod]
        public async Task TestGetBlockHeaderCount()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetBlockHeaderCountAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetBlockHeaderCountAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetBlockCount()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetBlockCountAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetBlockCountAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetBlockHash()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetBlockHashAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetBlockHashAsync((uint)test.Request.Params[0].AsNumber());
            Assert.AreEqual(test.Response.Result.AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetBlockHeaderHex()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name.Equals(nameof(rpc.GetBlockHeaderHexAsync), StringComparison.CurrentCultureIgnoreCase));
            foreach (var test in tests)
            {
                var result = await rpc.GetBlockHeaderHexAsync(test.Request.Params[0].AsString());
                Assert.AreEqual(test.Response.Result.AsString(), result);
            }
        }

        [TestMethod]
        public async Task TestGetBlockHeader()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name.Equals(nameof(rpc.GetBlockHeaderAsync), StringComparison.CurrentCultureIgnoreCase));
            foreach (var test in tests)
            {
                var result = await rpc.GetBlockHeaderAsync(test.Request.Params[0].AsString());
                Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson(rpc.protocolSettings).StrictToString(false));
            }
        }

        [TestMethod]
        public async Task TestGetCommittee()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name.Equals(nameof(rpc.GetCommitteeAsync), StringComparison.CurrentCultureIgnoreCase));
            foreach (var test in tests)
            {
                var result = await rpc.GetCommitteeAsync();
                Assert.AreEqual(test.Response.Result.StrictToString(false), new JsonArray(result.Select(p => (JsonNode)p).ToArray()).StrictToString(false));
            }
        }

        [TestMethod]
        public async Task TestGetContractState()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name.Equals(nameof(rpc.GetContractStateAsync), StringComparison.CurrentCultureIgnoreCase));
            foreach (var test in tests)
            {
                var type = test.Request.Params[0].GetType().Name;
                if (type == "JString")
                {
                    var result = await rpc.GetContractStateAsync(test.Request.Params[0].AsString());
                    Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
                }
                if (type == "JNumber")
                {
                    var result = await rpc.GetContractStateAsync((int)test.Request.Params[0].AsNumber());
                    Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
                }
            }
        }

        [TestMethod]
        public async Task TestGetNativeContracts()
        {
            var tests = TestUtils.RpcTestCases.Where(p => p.Name.Equals(nameof(rpc.GetNativeContractsAsync), StringComparison.CurrentCultureIgnoreCase));
            foreach (var test in tests)
            {
                var result = await rpc.GetNativeContractsAsync();
                Assert.AreEqual(test.Response.Result.StrictToString(false), new JsonArray(result.Select(p => p.ToJson()).ToArray()).StrictToString(false));
            }
        }

        [TestMethod]
        public async Task TestGetRawMempool()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetRawMempoolAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetRawMempoolAsync();
            Assert.AreEqual(test.Response.Result.StrictToString(false), new JsonArray(result.Select(p => (JsonNode)p).ToArray()).StrictToString(false));
        }

        [TestMethod]
        public async Task TestGetRawMempoolBoth()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetRawMempoolBothAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetRawMempoolBothAsync();
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
        }

        [TestMethod]
        public async Task TestGetRawTransactionHex()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetRawTransactionHexAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetRawTransactionHexAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetRawTransaction()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetRawTransactionAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetRawTransactionAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson(rpc.protocolSettings).StrictToString(false));
        }

        [TestMethod]
        public async Task TestGetStorage()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetStorageAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetStorageAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString());
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetTransactionHeight()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetTransactionHeightAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetTransactionHeightAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.ToString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetNextBlockValidators()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetNextBlockValidatorsAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetNextBlockValidatorsAsync();
            Assert.AreEqual(test.Response.Result.StrictToString(false), new JsonArray(result.Select(p => p.ToJson()).ToArray()).StrictToString(false));
        }

        #endregion Blockchain

        #region Node

        [TestMethod]
        public async Task TestGetConnectionCount()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetConnectionCountAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetConnectionCountAsync();
            Assert.AreEqual(test.Response.Result.ToString(), result.ToString());
        }

        [TestMethod]
        public async Task TestGetPeers()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetPeersAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetPeersAsync();
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
        }

        [TestMethod]
        public async Task TestGetVersion()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetVersionAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetVersionAsync();
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
        }

        [TestMethod]
        public async Task TestSendRawTransaction()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.SendRawTransactionAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.SendRawTransactionAsync(Convert.FromBase64String(test.Request.Params[0].AsString()).AsSerializable<Transaction>());
            Assert.AreEqual(test.Response.Result["hash"].AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestSubmitBlock()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.SubmitBlockAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.SubmitBlockAsync(Convert.FromBase64String(test.Request.Params[0].AsString()));
            Assert.AreEqual(test.Response.Result["hash"].AsString(), result.ToString());
        }

        #endregion Node

        #region SmartContract

        [TestMethod]
        public async Task TestInvokeFunction()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.InvokeFunctionAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.InvokeFunctionAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString(),
                ((JsonArray)test.Request.Params[2]).Select(p => RpcStack.FromJson((JsonObject)p)).ToArray());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));

            // TODO test verify method
        }

        [TestMethod]
        public async Task TestInvokeScript()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.InvokeScriptAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.InvokeScriptAsync(Convert.FromBase64String(test.Request.Params[0].AsString()));
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
        }

        [TestMethod]
        public async Task TestGetUnclaimedGas()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetUnclaimedGasAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetUnclaimedGasAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(result.ToJson().AsString(), RpcUnclaimedGas.FromJson(result.ToJson()).ToJson().AsString());
            Assert.AreEqual(test.Response.Result["unclaimed"].AsString(), result.Unclaimed.ToString());
        }

        #endregion SmartContract

        #region Utilities

        [TestMethod]
        public async Task TestListPlugins()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.ListPluginsAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.ListPluginsAsync();
            Assert.AreEqual(test.Response.Result.StrictToString(false), new JsonArray(result.Select(p => p.ToJson()).ToArray()).StrictToString(false));
        }

        [TestMethod]
        public async Task TestValidateAddress()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.ValidateAddressAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.ValidateAddressAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
        }

        #endregion Utilities

        #region Wallet

        [TestMethod]
        public async Task TestCloseWallet()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.CloseWalletAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.CloseWalletAsync();
            Assert.AreEqual(test.Response.Result.GetValue<bool>(), result);
        }

        [TestMethod]
        public async Task TestDumpPrivKey()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.DumpPrivKeyAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.DumpPrivKeyAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetNewAddress()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetNewAddressAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetNewAddressAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result);
        }

        [TestMethod]
        public async Task TestGetWalletBalance()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetWalletBalanceAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetWalletBalanceAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result["balance"].AsString(), result.Value.ToString());
        }

        [TestMethod]
        public async Task TestGetWalletUnclaimedGas()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetWalletUnclaimedGasAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetWalletUnclaimedGasAsync();
            Assert.AreEqual(test.Response.Result.AsString(), result.ToString());
        }

        [TestMethod]
        public async Task TestImportPrivKey()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.ImportPrivKeyAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.ImportPrivKeyAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
        }

        [TestMethod]
        public async Task TestListAddress()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.ListAddressAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.ListAddressAsync();
            Assert.AreEqual(test.Response.Result.StrictToString(false), new JsonArray(result.Select(p => p.ToJson()).ToArray()).StrictToString(false));
        }

        [TestMethod]
        public async Task TestOpenWallet()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.OpenWalletAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.OpenWalletAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString());
            Assert.AreEqual(test.Response.Result.GetValue<bool>(), result);
        }

        [TestMethod]
        public async Task TestSendFrom()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.SendFromAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.SendFromAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString(),
                test.Request.Params[2].AsString(), test.Request.Params[3].AsString());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.StrictToString(false));
        }

        [TestMethod]
        public async Task TestSendMany()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.SendManyAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.SendManyAsync(test.Request.Params[0].AsString(), ((JsonArray)test.Request.Params[1]).Select(p => RpcTransferOut.FromJson((JsonObject)p, rpc.protocolSettings)));
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.StrictToString(false));
        }

        [TestMethod]
        public async Task TestSendToAddress()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.SendToAddressAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.SendToAddressAsync(test.Request.Params[0].AsString(), test.Request.Params[1].AsString(), test.Request.Params[2].AsString());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.StrictToString(false));
        }

        #endregion Wallet

        #region Plugins

        [TestMethod()]
        public async Task GetApplicationLogTest()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetApplicationLogAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetApplicationLogAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
        }

        [TestMethod()]
        public async Task GetApplicationLogTest_TriggerType()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetApplicationLogAsync) + "_triggertype", StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetApplicationLogAsync(test.Request.Params[0].AsString(), TriggerType.OnPersist);
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson().StrictToString(false));
        }

        [TestMethod()]
        public async Task GetNep17TransfersTest()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetNep17TransfersAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetNep17TransfersAsync(test.Request.Params[0].AsString(), (ulong)test.Request.Params[1].AsNumber(), (ulong)test.Request.Params[2].AsNumber());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson(rpc.protocolSettings).StrictToString(false));
            test = TestUtils.RpcTestCases.Find(p => p.Name == (nameof(rpc.GetNep17TransfersAsync).ToLower() + "_with_null_transferaddress"));
            result = await rpc.GetNep17TransfersAsync(test.Request.Params[0].AsString(), (ulong)test.Request.Params[1].AsNumber(), (ulong)test.Request.Params[2].AsNumber());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson(rpc.protocolSettings).StrictToString(false));
        }

        [TestMethod()]
        public async Task GetNep17BalancesTest()
        {
            var test = TestUtils.RpcTestCases.Find(p => p.Name.Equals(nameof(rpc.GetNep17BalancesAsync), StringComparison.CurrentCultureIgnoreCase));
            var result = await rpc.GetNep17BalancesAsync(test.Request.Params[0].AsString());
            Assert.AreEqual(test.Response.Result.StrictToString(false), result.ToJson(rpc.protocolSettings).StrictToString(false));
        }

        #endregion Plugins
    }
}
